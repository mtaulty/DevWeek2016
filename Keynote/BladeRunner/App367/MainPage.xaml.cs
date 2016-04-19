namespace App367
{
  using Microsoft.Band;
  using Microsoft.Band.Sensors;
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.Storage;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  using Windows.UI.Xaml.Media.Animation;

  public sealed partial class MainPage : Page
  {
    public MainPage()
    {
      this.InitializeComponent();
      this.Loaded += OnLoaded;
      this.handlersAdded = false;
    }
    async void OnLoaded(object sender, RoutedEventArgs e)
    {
      var bands = await BandClientManager.Instance.GetBandsAsync();

      if (bands != null)
      {
        var firstBandInfo = bands.FirstOrDefault();

        if (firstBandInfo != null)
        {
          this.bandClient = await BandClientManager.Instance.ConnectAsync(firstBandInfo);

          if (this.bandClient != null)
          {
            await this.InitialiseForFirstUseAsync();
            this.bandClient.SensorManager.Contact.ReadingChanged += OnContactChanged;

            var currentContact = await this.bandClient.SensorManager.Contact.GetCurrentStateAsync();
            await this.ToggleForContactState(currentContact.State);

            await this.bandClient.SensorManager.Contact.StartReadingsAsync();
          }
        }
      }
    }
    async void OnContactChanged(object sender,
      BandSensorReadingEventArgs<IBandContactReading> e)
    {
      await this.DispatchAsync(
        () => this.ToggleForContactState(e.SensorReading.State));
    }
    async Task ToggleForContactState(BandContactState contactState)
    {
      string animation = string.Empty;

      if ((contactState == BandContactState.Worn) && !this.handlersAdded)
      {
        await AddHeartAndTemperatureHandlersAsync();
        this.handlersAdded = true;
        animation = "AnimateIn";
        this.ResetMedia();
      }
      else if ((contactState == BandContactState.NotWorn) && this.handlersAdded)
      {
        this.mediaElement.Stop();
        await RemoveHeartAndTemperatureHandlersAsync();
        animation = "AnimateOut";
      }
      if (!string.IsNullOrEmpty(animation))
      {
        await this.DispatchAsync(() =>
        {
          ((Storyboard)this.Resources[animation]).Begin();
        });
      }
    }
    async Task AddHeartAndTemperatureHandlersAsync()
    {
      this.bandClient.SensorManager.SkinTemperature.ReportingInterval =
        this.bandClient.SensorManager.SkinTemperature.SupportedReportingIntervals.Min();

      this.bandClient.SensorManager.SkinTemperature.ReadingChanged += this.OnSkinReadingChanged;

      // At the moment, this times out on me if we have connected->disconnected->connected
      // and I've yet to figure out why.
      await this.bandClient.SensorManager.SkinTemperature.StartReadingsAsync();

      this.bandClient.SensorManager.HeartRate.ReportingInterval =
        this.bandClient.SensorManager.HeartRate.SupportedReportingIntervals.Min();

      this.bandClient.SensorManager.HeartRate.ReadingChanged += this.OnHeartRateChanged;

      // same here, re: timeout.
      await this.bandClient.SensorManager.HeartRate.StartReadingsAsync();
    }
    async void OnHeartRateChanged(object sender,
      BandSensorReadingEventArgs<IBandHeartRateReading> e)
    {
      this.currentPulseRate = e.SensorReading.HeartRate;
      this.currentRateType = e.SensorReading.Quality.ToString();
      await this.AdjustMediaAsync();
    }
    async void OnSkinReadingChanged(object sender,
      BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
    {
      this.currentTemperature = e.SensorReading.Temperature;
      await this.AdjustTemperatureAsync();
    }
    void ResetMedia()
    {
      this.mediaElement.Play();
    }
    async Task RemoveHeartAndTemperatureHandlersAsync()
    {
      this.bandClient.SensorManager.SkinTemperature.ReadingChanged -= this.OnSkinReadingChanged;
      await this.bandClient.SensorManager.SkinTemperature.StopReadingsAsync();

      this.bandClient.SensorManager.HeartRate.ReadingChanged -= this.OnHeartRateChanged;
      await this.bandClient.SensorManager.HeartRate.StopReadingsAsync();

      this.handlersAdded = false;
    }
    async Task DispatchAsync(Action a)
    {
      await this.Dispatcher.RunAsync(
        Windows.UI.Core.CoreDispatcherPriority.Normal,
        () => a());
    }
    async Task AdjustMediaAsync()
    {
      await this.DispatchAsync(() =>
        {
          this.txtRate.Text =
            $"{this.currentPulseRate}bpm";

          var rate = (this.currentPulseRate / STARTING_PULSE_RATE);

          this.mediaElement.PlaybackRate = rate;
          this.mediaElement.DefaultPlaybackRate = rate;

          this.txtQuality.Text = this.currentRateType;
        }
      );
    }
    async Task AdjustTemperatureAsync()
    {
      await this.DispatchAsync(
        () =>
        {
          var ratio =
            (this.currentTemperature - MIN_TEMP) /
            (MAX_TEMP - MIN_TEMP);

          this.tempMarker.Offset = 1.0f - ratio;

          this.txtTemperature.Text = $"{this.currentTemperature:F1}c";
        }
      );
    }

    async Task InitialiseForFirstUseAsync()
    {
      const string initialisedSetting = "initialised";

      if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(initialisedSetting))
      {
        // We don't expect the user to say 'no' here, so haven't written for it.
        await this.bandClient.SensorManager.SkinTemperature.RequestUserConsentAsync();
        await this.bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
        await this.bandClient.SensorManager.Contact.RequestUserConsentAsync();
        ApplicationData.Current.LocalSettings.Values[initialisedSetting] = true;
      }
    }
    void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
    {
      this.ResetMedia();
    }
    bool handlersAdded;
    IBandClient bandClient;
    int currentPulseRate;
    double currentTemperature;
    string currentRateType;
    const float STARTING_PULSE_RATE = 36.0f;
    const double MIN_TEMP = 20.0f;
    const double MAX_TEMP = 40.0f;
  }
}
