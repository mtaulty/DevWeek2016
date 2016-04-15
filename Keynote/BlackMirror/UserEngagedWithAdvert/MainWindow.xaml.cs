using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UserEngagedWithAdvert
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      this.Loaded += OnLoaded;
    }
    void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.videoIsPlaying = true;
      this.mediaElement.Play();
      this.timeSinceLastChange = new Stopwatch();
      this.timeSinceLastChange.Start();

      this.cameraSession = PXCMSession.CreateInstance();

      this.cameraManager = this.cameraSession.CreateSenseManager();

      if (this.cameraManager.EnableFace().IsSuccessful())
      {
        this.faceModule = this.cameraManager.QueryFace();

        this.ConfigureFace();

        this.faceData = this.faceModule.CreateOutput();

        this.cameraManager.Init(
          new PXCMSenseManager.Handler()
          {
            onModuleProcessedFrame = this.OnFaceProcessed
          }
        );
        this.cameraManager.StreamFrames(false);
      }
    }
    pxcmStatus OnFaceProcessed(int mid, PXCMBase module, PXCMCapture.Sample sample)
    {
      PXCMFaceData.Face face = null;

      this.description = string.Empty;

      var colorWidth = sample.color.QueryInfo().width;
      var colorHeight = sample.color.QueryInfo().height;

      if (this.faceData.Update().IsSuccessful())
      {
        // Do we see anybody watching?
        var numberOfFaces = this.faceData.QueryNumberOfDetectedFaces();

        var videoShouldBePlaying = numberOfFaces == 1;

        if (videoShouldBePlaying)
        {
          face = this.faceData.QueryFaceByIndex(0);

          this.UpdatePulseText(face);

          videoShouldBePlaying =
            videoShouldBePlaying && this.IsUsersHeadAngleSuitable(face);

          videoShouldBePlaying =
            videoShouldBePlaying && this.IsUserLookingAtScreen(face);

          videoShouldBePlaying =
            videoShouldBePlaying && this.IsUserExcited(face);
        }
        else
        {
          this.description = "no-one is watching";
        }

        this.SetVideoPlayPause(videoShouldBePlaying);
      }
      this.Redraw(face, colorWidth, colorHeight);

      return (pxcmStatus.PXCM_STATUS_NO_ERROR);
    }

    void Redraw(PXCMFaceData.Face face,
      int colorWidth, int colorHeight)
    {
      if (face != null)
      {
        var landmarks = face.QueryLandmarks()?.QueryPoints(
          out this.landmarks);

        if (landmarks == true)
        {
          this.Dispatcher.InvokeAsync(() =>
          {
            this.drawCanvas.Children.Clear();

            if (landmarks == true)
            {
              foreach (var landmark in this.landmarks)
              {
                var scaledX =
                  ((double)landmark.image.x / (double)colorWidth) *
                  this.drawCanvas.ActualWidth;

                var scaledY =
                  ((double)landmark.image.y / (double)colorHeight) *
                  this.drawCanvas.ActualHeight;

                this.drawCanvas.Children.Add(this.MakeEllipse(
                  scaledX, scaledY));
              }
            }
          });
        }
      }
    }
    Ellipse MakeEllipse(double x, double y)
    {
      Ellipse ellipse = new Ellipse()
      {
        Width = LANDMARK_ELLIPSE_WIDTH,
        Height = LANDMARK_ELLIPSE_WIDTH,
        Fill = Brushes.White
      };
      Canvas.SetLeft(ellipse, x);
      Canvas.SetTop(ellipse, y);
      return (ellipse);
    }

    void UpdatePulseText(PXCMFaceData.Face face)
    {
      string text = string.Empty;
      var pulse = face.QueryPulse();
      if (pulse != null)
      {
        var rate = pulse.QueryHeartRate();
        if (rate > 0)
        {
          text = rate.ToString("F0");
        }
      }
      this.Dispatcher.InvokeAsync(() =>
      {
        this.labelPulse.Text = text;
      });
    }

    bool IsUserExcited(PXCMFaceData.Face face)
    {
      bool excited = true;

      var pulse = face.QueryPulse();

      if (pulse != null)
      {
        var rate = pulse.QueryHeartRate();

        excited =
          (rate <= 0) || (rate > 75.0f);

        if (!excited)
        {
          this.description =
            "user is not reaching expected excitement level of 75 bpm";
        }
      }
      return (excited);
    }
    bool IsUsersHeadAngleSuitable(PXCMFaceData.Face face)
    {
      bool headAngleOk = false;
      var minYawAngle = -10.0f;
      var maxYawAngle = 10.0f;
      var minPitchAngle = 0.0f;
      var maxPitchAngle = 60.0f;

      var pose = face.QueryPose();

      if (pose != null)
      {
        PXCMFaceData.PoseEulerAngles angles;

        if (pose.QueryPoseAngles(out angles))
        {
          // We'll just look at the yaw of the head. Rotation left and right around the neck.
          headAngleOk =
            ((angles.yaw >= minYawAngle) && (angles.yaw <= maxYawAngle)) &&
            ((angles.pitch >= minPitchAngle) && (angles.pitch <= maxPitchAngle));
        }
        else
        {
          // We assume true for this.
          headAngleOk = true;
        }
      }
      if (!headAngleOk)
      {
        this.description = "please turn your head to the screen";
      }
      return (headAngleOk);
    }
    bool IsUserLookingAtScreen(PXCMFaceData.Face face)
    {
      bool lookingAtScreen = true;

      var eyesNotLookingAtScreen = new PXCMFaceData.ExpressionsData.FaceExpression[]
      {
        PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT,
        PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT,
        PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_DOWN,
        PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_UP,
        PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN
      };
      var expressions = face.QueryExpressions();

      if (expressions != null)
      {
        foreach (var expression in eyesNotLookingAtScreen)
        {
          PXCMFaceData.ExpressionsData.FaceExpressionResult result;

          expressions.QueryExpression(expression, out result);

          if (result.intensity == 100)
          {
            this.description = $"please look at the screen [{ExpressionToString(expression)}]";

            lookingAtScreen = false;
            break;
          }
        }
      }
      return (lookingAtScreen);
    }
    static string ExpressionToString(
      PXCMFaceData.ExpressionsData.FaceExpression expression)
    {
      return (
        string.Join(
          " ",
          expression.ToString().ToLower().Split('_')));
    }
    void SetVideoPlayPause(bool videoShouldBePlaying)
    {
      if (this.videoIsPlaying != videoShouldBePlaying)
      {
        this.Dispatcher.InvokeAsync(
          () =>
          {
            if (this.timeSinceLastChange.Elapsed.Milliseconds > 500)
            {
              this.statusLabel.Content = this.description;

              this.videoIsPlaying = videoShouldBePlaying;

              if (this.videoIsPlaying)
              {
                this.mediaElement.Play();
              }
              else
              {
                this.mediaElement.Pause();
              }
              this.timeSinceLastChange.Restart();
            }
          }
        );
      }
    }
    void ConfigureFace()
    {
      using (var configuration = this.faceModule.CreateActiveConfiguration())
      {
        // Configure it...
        using (var config = this.faceModule.CreateActiveConfiguration())
        {
          // We want face detection. Only doing 1 face for now.
          config.detection.isEnabled = true;
          config.detection.maxTrackedFaces = 1;

          // We want face landmarks. Only doing 1 face for now.
          config.landmarks.isEnabled = true;
          config.landmarks.maxTrackedFaces = 1;

          // We want pulse data. Only doing 1 face for now.
          var pulseConfig = config.QueryPulse();
          pulseConfig.properties.maxTrackedFaces = 1;
          pulseConfig.Enable();

          // We want expressions. Only doing 1 face for now.
          var expressionConfig = config.QueryExpressions();
          expressionConfig.EnableAllExpressions();
          expressionConfig.Enable();

          config.ApplyChanges();
        }
      }
    }
    void OnMediaEnded(object sender, RoutedEventArgs e)
    {
      this.mediaElement.Position = TimeSpan.FromSeconds(0);

      if (this.videoIsPlaying)
      {
        this.mediaElement.Play();
      }
    }
    PXCMSession cameraSession;
    PXCMSenseManager cameraManager;
    PXCMFaceModule faceModule;
    PXCMFaceData faceData;
    bool videoIsPlaying;
    Stopwatch timeSinceLastChange;
    string description;
    PXCMFaceData.LandmarkPoint[] landmarks;
    const int LANDMARK_ELLIPSE_WIDTH = 10;
  }
}
