using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication29
{
  using Microsoft.Kinect;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Media.Animation;
  using System.Windows.Shapes;
  public partial class MainWindow : Window
  {
    void DismissZoomMode()
    {
      this.PlaySound(@".\Sounds\ir_inter.wav");
      this.currentMode = Mode.Pan;
      this.zoomModeFrozenAnimation.Stop();
      this.zoomModeFrozenAnimation = null;
      ((RotateTransform)this.zoomModeTransformation.Children[1]).Angle = 0;
      ((ScaleTransform)this.zoomModeTransformation.Children[0]).ScaleX = 1;
      ((ScaleTransform)this.zoomModeTransformation.Children[0]).ScaleY = 1;
      this.zoomModeTransformation = null;
      this.videoManager.Play();
    }
    void TranslateVisualBrushes(double distance)
    {
      foreach (var item in this.visualBrushes)
      {
        var group = (TransformGroup)item.RelativeTransform;
        var translate = (TranslateTransform)group.Children[2];
        translate.X = distance;
      }
    }
    void SwitchAnimationsAndMediaBasedOnHandsArrivingOrLeaving(bool handsVisible)
    {
      if (handsVisible && !this.activated)
      {
        this.activated = true;

        this.currentMode = Mode.Pan;
        this.PlaySound(@"sounds\reveal.wav");
        this.videoManager.Play();
        ((Storyboard)this.Resources["AnimateIn"]).Begin();
      }
    }
    void PlaySound(string file)
    {
      this.player.Open(new Uri(file, UriKind.Relative));
      this.player.Play();
    }
    void PanModeHitTestAgainstMediaElements(
  HandState leftHandState,
  TrackingConfidence leftHandConfidence,
  CameraSpacePoint leftHandPosition,
  HandState rightHandState,
  TrackingConfidence rightHandConfidence,
  CameraSpacePoint rightHandPosition)
    {
      this.HitTestHandAgainstMediaElements(
        leftHandState, leftHandConfidence, leftHandPosition);
      this.HitTestHandAgainstMediaElements(
        rightHandState, rightHandConfidence, rightHandPosition);
    }
    void HitTestHandAgainstMediaElements(
      HandState handState,
      TrackingConfidence handConfidence,
      CameraSpacePoint handPosition)
    {
      if ((handState == HandState.Lasso) &&
        (handConfidence == TrackingConfidence.High))
      {
        var scaledPoint = this.ScaleCameraPointToDisplayPoint(handPosition);
        Path hitPath = null;

        VisualTreeHelper.HitTest(
          this.canvas,
          null,
          result =>
          {
            hitPath = result.VisualHit as Path;
            return (
            hitPath == null ? HitTestResultBehavior.Continue : HitTestResultBehavior.Stop);
          },
          new PointHitTestParameters(scaledPoint));

        if (hitPath != null)
        {
          this.zoomModeFrozenAnimation = this.pathLookups?[hitPath];

          if (this.zoomModeFrozenAnimation != null)
          {
            this.zoomModeTransformation = ((TransformGroup)hitPath.Fill.RelativeTransform);

            this.currentMode = Mode.Inspect;
            this.videoManager.Pause();
            this.PlaySound(@".\Sounds\ir_inter.wav");
            this.zoomModeFrozenAnimation.Begin();
          }
        }
      }
    }
    Point ScaleCameraPointToDisplayPoint(CameraSpacePoint cameraPoint)
    {
      var colorPoint =
        this.sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);

      var colorWidth =
        this.sensor.ColorFrameSource.FrameDescription.Width;

      var colorHeight =
        this.sensor.ColorFrameSource.FrameDescription.Height;

      var point = new Point()
      {
        X = (colorPoint.X / colorWidth) * this.handsCanvas.ActualWidth,
        Y = (colorPoint.Y / colorHeight) * this.handsCanvas.ActualHeight
      };
      return (point);
    }
    // The bits managing state for this shabby code which needs factoring
    // into proper independent classes if we were to do it properly.
    VideoManager videoManager;
    bool activated;
    MediaPlayer player;
    Point? dragStartLeftHand;
    Point? dragStartRightHand;
    Dictionary<Path, Storyboard> pathLookups;
    Mode currentMode;
    double? previousAngle;
    double? previousDistance;
    Storyboard zoomModeFrozenAnimation;
    TransformGroup zoomModeTransformation;
    const int PIXEL_TRACKING_DISTANCE = 250;
  }
}