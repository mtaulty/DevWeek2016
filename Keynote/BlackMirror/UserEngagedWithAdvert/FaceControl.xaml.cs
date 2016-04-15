namespace WpfRealsenseFacialDetection.Controls
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Shapes;
  using WpfRealsenseFacialDetection.Utility;

  using Expression = PXCMFaceData.ExpressionsData.FaceExpression;
  using ExpressionResult = PXCMFaceData.ExpressionsData.FaceExpressionResult;

  public partial class FaceControl : UserControl, IFrameProcessor
  {
    public FaceControl()
    {
      InitializeComponent();
    }
    // This control is making use of the face module from RealSense.
    public int RealSenseModuleId
    {
      get { return (PXCMFaceModule.CUID); }
    }
    public void Initialise(PXCMSenseManager senseManager)
    {
      this.senseManager = senseManager;

      // We configure by switching on the face module.
      this.senseManager.EnableFace();

      // Now, grab that module.
      this.faceModule = this.senseManager.QueryFace();

      // Configure it...
      using (var config = faceModule.CreateActiveConfiguration())
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

        config.ApplyChanges().ThrowOnFail();
      }
      this.faceData = this.faceModule.CreateOutput();
    }
    public void ProcessFrame(PXCMCapture.Sample sample)
    {
      this.faceBox = null;
      this.heartRate = null;
      this.landmarks = null;
      this.expressions = new Dictionary<string, float>();

      if (this.faceData.Update().Succeeded())
      {
        var firstFace = this.faceData.QueryFaces().FirstOrDefault();

        if (firstFace != null)
        {
          // face detection - the bounding rectangle of the face.
          var localFaceBox = default(PXCMRectI32);

          if (firstFace.QueryDetection()?.QueryBoundingRect(
            out localFaceBox) == true)
          {
            this.faceBox = localFaceBox;
          }

          // facial landmarks
          var landmarks = firstFace.QueryLandmarks()?.QueryPoints(
            out this.landmarks);

          // heart rate.
          this.heartRate = firstFace.QueryPulse()?.QueryHeartRate();

          // facial expressions
          var expressions = firstFace.QueryExpressions();

          if (expressions != null)
          {
            foreach (Expression expressionType in Enum.GetValues(typeof(Expression)))
            {
              ExpressionResult expressionResult = default(ExpressionResult);

              if (expressions.QueryExpression(expressionType, out expressionResult))
              {
                this.expressions[expressionType.GetName()] = expressionResult.intensity;
              }
            }
          }
        }
      }
    }
    public void DrawUI(PXCMCapture.Sample sample)
    {
      this.canvas.Children.Clear();

      // Draw a box around the face.
      if (this.faceBox.HasValue)
      {
        this.canvas.Children.Add(this.MakeRectangle(this.faceBox.Value));
      }

      // Draw circles for each of the facial landmarks.
      if (this.landmarks != null)
      {
        foreach (var landmark in this.landmarks)
        {
          this.canvas.Children.Add(this.MakeEllipse(landmark.image));
        }
      }
      var medIntensityExpressions = 
        this.expressions.Where(e => e.Value > LOW_INTENSITY);

      var highIntensityExpressions =
        medIntensityExpressions.Where(e => e.Value > HIGH_INTENSITY);

      var missingExpressions =
        this.expressions.Where(e => e.Value <= LOW_INTENSITY);

      this.PopulateLabelFromExpressions(this.txtExpressions, highIntensityExpressions);
      this.PopulateLabelFromExpressions(this.txtPossibleExpressions, medIntensityExpressions);
      this.PopulateLabelFromExpressions(this.txtMissingExpressions, missingExpressions);

      this.txtHeartRate.Text =
        this.heartRate.HasValue ? $"{this.heartRate:G2} bpm" : string.Empty;                   
    }
    void PopulateLabelFromExpressions(TextBlock txtBlock, 
      IEnumerable<KeyValuePair<string, float>> expressionSet)
    {
      var stringBuilder = new StringBuilder();

      foreach (var entry in expressionSet)
      {
        stringBuilder.AppendLine(
          $"{entry.Key} ({entry.Value}%)");
      }
      txtBlock.Text = stringBuilder.ToString();
    }
    Rectangle MakeRectangle(PXCMRectI32 rect)
    {
      Rectangle rectangle = new Rectangle()
      {
        Width = rect.w,
        Height = rect.h,
        Stroke = Brushes.Silver
      };
      Canvas.SetLeft(rectangle, rect.x);
      Canvas.SetTop(rectangle, rect.y);
      return (rectangle);
    }
    Ellipse MakeEllipse(PXCMPointF32 point)
    {
      Ellipse ellipse = new Ellipse()
      {
        Width = LANDMARK_ELLIPSE_WIDTH,
        Height = LANDMARK_ELLIPSE_WIDTH,
        Fill = Brushes.Orange
      };
      Canvas.SetLeft(ellipse, point.x);
      Canvas.SetTop(ellipse, point.y);
      return (ellipse);
    }
    PXCMFaceData.LandmarkPoint[] landmarks;
    float? heartRate;
    PXCMRectI32? faceBox;

    Dictionary<string, float> expressions;
    PXCMFaceData faceData;
    PXCMFaceModule faceModule;
    PXCMSenseManager senseManager;

    const int LOW_INTENSITY = 30;
    const int HIGH_INTENSITY = 80;
    const int MIN_CONFIDENCE = 50;
    const int LANDMARK_ELLIPSE_WIDTH = 5;
  }
}