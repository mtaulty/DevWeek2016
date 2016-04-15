namespace WpfApplication29
{
  using Microsoft.Kinect;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Media.Animation;
  using System.Windows.Shapes;

  public partial class MainWindow : Window
  {
    enum Mode
    {
      Pan,
      Inspect
    }
    public MainWindow()
    {
      InitializeComponent();
      this.Loaded += OnLoaded;
    }
    void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.player = new MediaPlayer();
      this.previousAngle = null;
      this.currentMode = Mode.Pan;

      this.pathLookups = new Dictionary<Path, Storyboard>()
      {
        [this.path1] = ((Storyboard)this.Resources["Pop1"]),
        [this.path2] = ((Storyboard)this.Resources["Pop2"]),
        [this.path3] = ((Storyboard)this.Resources["Pop3"])
      };

      this.visualBrushes = new VisualBrush[]
      {
        (VisualBrush)this.Resources["visualBrush1"],
        (VisualBrush)this.Resources["visualBrush2"],
        (VisualBrush)this.Resources["visualBrush3"],
      };

      this.videoManager = new VideoManager(
        this.mediaElement1,
        this.mediaElement2,
        this.mediaElement3);

      this.activated = false;

      this.sensor = KinectSensor.GetDefault();

      this.sensor.Open();

      this.bodyFrameSource = this.sensor.BodyFrameSource;

      this.bodies = new Body[this.bodyFrameSource.BodyCount];

      this.bodyReader = this.bodyFrameSource.OpenReader();

      this.bodyReader.FrameArrived += OnBodyFrameArrived;
    }

    void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs args)
    {
      bool drawnHands = false;

      using (var frame = args.FrameReference?.AcquireFrame())
      {
        if (frame != null)
        {
          // Get an array of all the bodies (up to 6) that we can see.
          frame.GetAndRefreshBodyData(this.bodies);

          // We're going to ignore all but the first one.
          var firstBody = this.bodies.FirstOrDefault(b => b.IsTracked);

          if (firstBody != null)
          {
            // We have 25 joints to play with including 3 for each hand
            // (wrist, tip, center). We'll draw a circle around the tip
            // to keep it simple.
            var leftHandTip = firstBody.Joints[JointType.HandTipLeft];
            var rightHandTip = firstBody.Joints[JointType.HandTipRight];

            // do we *really* know where these are?
            if (JointsAreFullyTracked(leftHandTip, rightHandTip))
            {
              drawnHands = true;

              this.MoveHandToPosition(this.leftHandEllipse, leftHandTip.Position);

              this.MoveHandToPosition(this.rightHandEllipse, rightHandTip.Position);

              if (this.currentMode == Mode.Pan)
              {
                // Is the user dragging their hand?
                this.PanModeCheckForDraggingHands(
                  firstBody.HandLeftState,    // closed, open, etc.
                  leftHandTip,                // where is it?
                  firstBody.HandRightState,
                  rightHandTip);

                // Is the user pointing at a piece of video?
                this.PanModeHitTestAgainstMediaElements(
                  firstBody.HandLeftState,
                  firstBody.HandLeftConfidence,
                  leftHandTip.Position,
                  firstBody.HandRightState,
                  firstBody.HandRightConfidence,
                  rightHandTip.Position);
              }
              else
              {
                // Is the user rotating, scaling or dismissing?
                this.ZoomModeTrackHandStatesAndDistances(
                  leftHandTip,
                  firstBody.HandLeftState,
                  rightHandTip,
                  firstBody.HandRightState);
              }
            }
          }
        }
      }
      this.ShowHands(drawnHands);

      this.SwitchAnimationsAndMediaBasedOnHandsArrivingOrLeaving(drawnHands);
    }
    static bool JointsAreFullyTracked(params Joint[] joints)
    {
      return (joints.All(j => j.TrackingState == TrackingState.Tracked));
    }
    void ZoomModeTrackHandStatesAndDistances(
      Joint leftHandTip, HandState leftHandState,
      Joint rightHandTip, HandState rightHandState)
    {
      if (rightHandState == HandState.Closed)
      {
        DismissZoomMode();
      }
      if (leftHandState == HandState.Closed)
      {
        var leftHandPoint = this.ScaleCameraPointToDisplayPoint(leftHandTip.Position);
        var rightHandPoint = this.ScaleCameraPointToDisplayPoint(rightHandTip.Position);

        double yDiff = rightHandPoint.Y - leftHandPoint.Y;
        double xDiff = rightHandPoint.X - leftHandPoint.X;

        double angle = Math.Atan2(yDiff, xDiff) * (180 / Math.PI);
        double distance = Math.Sqrt(Math.Pow(yDiff, 2.0) + Math.Pow(xDiff, 2.0));

        if ((this.previousAngle.HasValue) && (this.zoomModeTransformation != null))
        {
          double angleDelta = angle - (double)this.previousAngle;
          ((RotateTransform)this.zoomModeTransformation.Children[1]).Angle += angleDelta;

          double distanceDelta = distance / this.previousDistance.Value;

          ((ScaleTransform)this.zoomModeTransformation.Children[0]).ScaleX = distanceDelta;
          ((ScaleTransform)this.zoomModeTransformation.Children[0]).ScaleY = distanceDelta;
        }
        this.previousAngle = angle;

        // We only take the first distance here and then we benchmark
        // against it for scaling.
        if (this.previousDistance == null)
        {
          this.previousDistance = distance;
        }
      }
      else
      {
        this.previousAngle = null;
        this.previousDistance = null;
      }
    }
    void PanModeCheckForDraggingHands(
      HandState leftHandState,
      Joint leftHandTip,
      HandState rightHandState,
      Joint rightHandTip)
    {
      var leftPosition = this.GetPositionOfDraggingHand(leftHandState, leftHandTip);
      var rightPosition = this.GetPositionOfDraggingHand(rightHandState, rightHandTip);

      if (leftPosition != null)
      {
        if (this.dragStartLeftHand == null)
        {
          this.dragStartLeftHand = leftPosition;
        }
        else
        {
          var dragDistance = leftPosition.Value.X - this.dragStartLeftHand.Value.X;

          if (dragDistance > PIXEL_TRACKING_DISTANCE)
          {
            this.dragStartLeftHand = null;
            this.TranslateVisualBrushes(0);
            this.videoManager.SlideRight();
          }
          else
          {
            this.TranslateVisualBrushes(
              dragDistance / (double)PIXEL_TRACKING_DISTANCE);
          }
        }
      }
      else
      {
        this.dragStartLeftHand = null;
      }
      if (rightPosition != null)
      {
        if (this.dragStartRightHand == null)
        {
          this.dragStartRightHand = rightPosition;
        }
        else
        {
          var dragDistance = this.dragStartRightHand.Value.X - rightPosition.Value.X;

          if (dragDistance > PIXEL_TRACKING_DISTANCE)
          {
            this.dragStartRightHand = null;
            this.TranslateVisualBrushes(0);
            this.videoManager.SlideLeft();
          }
          else
          {
            this.TranslateVisualBrushes(
              0 - (dragDistance / (double)PIXEL_TRACKING_DISTANCE));
          }
        }
      }
      else
      {
        this.dragStartRightHand = null;
      }
    }
    Point? GetPositionOfDraggingHand(HandState handState, Joint handTip)
    {
      return (
        handState == HandState.Closed ?
        this.ScaleCameraPointToDisplayPoint(handTip.Position) : (Point?)null);
    }
    void ShowHands(bool show = true)
    {
      this.leftHandEllipse.Visibility =
        show ? Visibility.Visible : Visibility.Collapsed;

      this.rightHandEllipse.Visibility =
        show ? Visibility.Visible : Visibility.Collapsed;
    }
    void MoveHandToPosition(Ellipse ellipse, CameraSpacePoint position)
    {
      var displayPoint = this.ScaleCameraPointToDisplayPoint(position);

      Canvas.SetLeft(ellipse, displayPoint.X - (ellipse.ActualWidth / 2.0));
      Canvas.SetTop(ellipse, displayPoint.Y - (ellipse.ActualHeight / 2.0));
    }
    // The bits dealing with the human body
    Body[] bodies;
    BodyFrameReader bodyReader;
    BodyFrameSource bodyFrameSource;
    KinectSensor sensor;
    VisualBrush[] visualBrushes;
  }
}
