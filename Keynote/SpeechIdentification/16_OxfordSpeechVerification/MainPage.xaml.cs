namespace App336
{
  using System.Threading.Tasks;
  using Windows.UI.Xaml.Controls;
  using System;
  using Windows.UI.Xaml;
  using com.mtaulty.OxfordVerify;
  using Windows.Storage.Streams;

  public sealed partial class MainPage : Page
  {
    enum VisualStates
    {
      Default,
      Submitting,
      Recording
    }
    public MainPage()
    {
      this.InitializeComponent();
      this.data = new DisplayTextViewModel();
      this.DataContext = this.data;
      this.oxfordClient = new OxfordVerificationClient(Keys.OxfordKey);

      this.Loaded += OnLoaded;
    }

    async void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.conversation = new Conversation(
        this.mediaElement,
        hypothesis =>
        {
          this.data.DisplayText = hypothesis;
        }
      );
    }
    async Task BeginInteractionsAsync()
    {
      await WaitForUserToMentionCalendar();
      await this.GetCalendarDetailsAsync();
    }
    async Task WaitForUserToMentionCalendar()
    {
      await this.DisplayAndSayAsync("listening", "ok, I'm listening. Go ahead");

      while (true)
      {
        var result =
          await this.conversation.WaitForDictationTopicAsync(string.Empty);

        this.data.DisplayText = result.Text;

        if (result.Text.Contains("calendar"))
        {
          break;
        }
      }
    }
    async Task GetCalendarDetailsAsync()
    {
      var identified = false;
      const string andrewUser = "andrew";
      const string mikeUser = "mike";

      await this.DisplayAndSayAsync(
        "identification needed",
        "first, I need to identify you");

      var accountNames = await this.oxfordClient.GetAllAccountNamesAsync();
      var registeredUser = "not met";

      var recordedStream = await this.RecordSpeechToFileAsync<IRandomAccessStream>(
        DEFAULT_RECORD_TIME,
        async () =>
        {
          var speechStream = await this.oxfordClient.RecordSpeechToFileAsync(
            TimeSpan.FromSeconds(DEFAULT_RECORD_TIME));

          return (speechStream);
        }
      );

      foreach (var accountName in accountNames)
      {
        var localStream = recordedStream.CloneStream();

        identified = await this.TryVerifyUserAsync(
          accountName, localStream);

        if (identified)
        {
          registeredUser = accountName;
          break;
        }
      }
      recordedStream.Dispose();

      string response = "we've only just met, you'll have to plan your own day";

      if (identified)
      {
        switch (registeredUser.ToLower().Trim())
        {
          case andrewUser:
            response =
              "it looks like you're having another lie in, enjoy";
            break;
          case mikeUser:
            response =
              "it looks like you've more code to write, crack on with it";
            break;
          default:
            break;
        }
      }
      await this.DisplayAndSayAsync($"for {registeredUser}, today...", response);
      await this.BeginInteractionsAsync();
    }
    async Task<bool> TryVerifyUserAsync(string userName,
      IInputStream speechStream)
    {
      bool identified = false;

      var result = await 
        this.oxfordClient.VerifyUserAgainstSpeechAsync(
          userName, speechStream);

      identified = (result.Result == VerificationStatus.Accept);

      return (identified);
    }
    async Task<T> RecordSpeechToFileAsync<T>(
      float recordTime,
      Func<Task<T>> asyncOxfordAction)
    {
      await this.DisplayAndSayAsync(
        "repeat the phrase", "when I say 'go'")
        .PauseAsync(TimeSpan.FromMilliseconds(500))
        .SayAsync("please repeat the phrase displayed");

      var verificationPhrase = await this.oxfordClient.GetVerificationPhrase();

      this.data.DisplayText = verificationPhrase;

      await this.DisplayAndSayAsync(string.Empty, "go");

      this.SwitchState(VisualStates.Recording);

      var oxfordTask = asyncOxfordAction();

      var progressTask = this.RunProgressLoopAsync(0.0f, recordTime,
        TimeSpan.FromSeconds(recordTime));

      await Task.WhenAll(oxfordTask, progressTask);

      this.SwitchState(VisualStates.Default);

      return (oxfordTask.Result);
    }

    void SwitchState(VisualStates state)
    {
      VisualStateManager.GoToState(this, state.ToString(), true);
    }
    async Task<ConversationResult> DisplayAndSayAsync(string display, string say)
    {
      if (!string.IsNullOrEmpty(display))
      {
        this.data.DisplayText = display;
      }
      var result = await this.conversation.SayAsync(say);

      return (result);
    }
    async Task RegisterAsync()
    {
      var enrolled = false;

      while (!enrolled)
      {
        EnrollmentResult result = null;

        try
        {
          result = await this.RecordSpeechToFileAsync<EnrollmentResult>(
            DEFAULT_RECORD_TIME,
            async () =>
            {
              var localResult = await this.oxfordClient.RecordAndEnrollUserAsync(
                this.data.UserToAdd, TimeSpan.FromSeconds(DEFAULT_RECORD_TIME));

              return (localResult);
            }
          );
          enrolled = result.EnrollmentStatus == EnrollmentStatus.Enrolled;

          if (!enrolled)
          {
            await this.conversation.SayAsync(
              $"I'll need you repeat that just {result.RemainingEnrollments} more time");
          }
        }
        catch
        {
          await this.DisplayAndSayAsync(
            $"error", "we'll have to try that again");
        }
      }
      await this.DisplayAndSayAsync("done", "ok, you're enrolled");
    }
    async Task RunProgressLoopAsync(float minimum, float maximum, TimeSpan time)
    {
      this.data.ProgressMinimum = minimum;
      this.data.ProgressMaximum = maximum;
      this.data.ProgressValue = 0;
      var totalSeconds = time.TotalSeconds;
      var increment = (maximum - minimum) / totalSeconds;

      for (int i = 0; i < time.TotalSeconds; i++)
      {
        this.data.ProgressValue += (float)increment;
        await Task.Delay(TimeSpan.FromSeconds(1));
      }
      this.data.ProgressValue = 0;
    }
    async void OnAddUser(object sender, RoutedEventArgs e)
    {
      await this.RegisterAsync();
    }
    async void OnListenAsync(object sender, RoutedEventArgs e)
    {
      await this.BeginInteractionsAsync();
    }
    OxfordVerificationClient oxfordClient;
    Conversation conversation;
    DisplayTextViewModel data;
    static readonly float DEFAULT_RECORD_TIME = 5.0f;
  }
}
