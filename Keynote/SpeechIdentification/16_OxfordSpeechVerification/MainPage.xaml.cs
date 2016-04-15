namespace App336
{
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using Windows.UI.Xaml.Controls;
  using Windows.UI.Xaml.Navigation;
  using System;
  using System.Linq;
  using Windows.Storage;
  using Windows.UI.Xaml;
  using Windows.UI.Popups;
  using com.mtaulty.OxfordVerify;
  using Windows.Media.SpeechSynthesis;
  using Windows.Media.SpeechRecognition;
  using Windows.UI.Core;
  using Windows.UI.Xaml.Media.Animation;
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

      foreach (var accountName in accountNames)
      {
        for (int i = 0; i < 3; i++)
        {
          try
          {
            identified = await this.TryVerifyUserAsync(accountName);
            break;
          }
          catch
          {

          }
        }
        if (identified)
        {
          registeredUser = accountName;
          break;
        }
      }
      string response = "we've only just met, you'll have to plan your own day";

      if (identified)
      {
        switch (registeredUser.ToLower().Trim())
        {
          case andrewUser:
            response = "it looks like you're have another lie in followed by french toast";
            break;
          case mikeUser:
            response = "it looks like you've more code to write, crack on with it";
            break;
          default:
            break;
        }
      }
      await this.DisplayAndSayAsync($"{registeredUser}, calendar", response);
      this.BeginInteractionsAsync();
    }
    async Task<bool> TryVerifyUserAsync(string userName)
    {
      bool identified = false;

      var result = await this.RecordSpeechToFileAsync<VerificationResult>(
        userName,
        DEFAULT_RECORD_TIME,
        async () =>
        {
          var localResult = await this.oxfordClient.RecordAndVerifyUserAsync(
            userName, TimeSpan.FromSeconds(DEFAULT_RECORD_TIME));

          return (localResult);
        }
      );
      identified = (result.Result == VerificationStatus.Accept);

      return (identified);
    }
    async Task<T> RecordSpeechToFileAsync<T>(
      string userName,
      float recordTime,
      Func<Task<T>> asyncOxfordAction)
    {
      await this.DisplayAndSayAsync(
        "repeat the phrase", "when I say 'go'")
        .PauseAsync(TimeSpan.FromMilliseconds(500))
        .SayAsync("please repeat the phrase displayed");

      var phrase = await this.oxfordClient.GetVerificationPhraseForUserAsync(
        userName);

      this.data.DisplayText = phrase;

      await this.DisplayAndSayAsync(string.Empty, "go");

      this.SwitchState(VisualStates.Recording);

      var oxfordTask = asyncOxfordAction();

      var progressTask = this.RunProgressLoopAsync(0.0f, recordTime,
        TimeSpan.FromSeconds(recordTime));

      await Task.WhenAll(oxfordTask, progressTask);

      this.SwitchState(VisualStates.Default);

      return (oxfordTask.Result);
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
            this.data.UserToAdd,
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
    OxfordVerificationClient oxfordClient;
    Conversation conversation;
    DisplayTextViewModel data;
    static readonly float DEFAULT_RECORD_TIME = 5.0f;

    async void OnListenAsync(object sender, RoutedEventArgs e)
    {
      await this.BeginInteractionsAsync();
    }
  }
}
