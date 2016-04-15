namespace App336
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Windows.Media.SpeechRecognition;
  using Windows.Media.SpeechSynthesis;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  static class ConversationResultExtensions
  {
    public static async Task<ConversationResult> SayAsync(
      this Task<ConversationResult> previous, string text)
    {
      var result = await previous;
      return (await result.Conversation.SayAsync(text));
    }
    public static async Task<ConversationResult> PauseAsync(
      this Task<ConversationResult> previous, TimeSpan delay)
    {
      var result = await previous;
      return (await result.Conversation.PauseAsync(delay));
    }
    public static async Task<ConversationResult> WaitForDigitsAsync(
      this Task<ConversationResult> previous)
    {
      var result = await previous;
      return (await result.Conversation.WaitForDigitsAsync());
    }
    public static async Task<ConversationResult> WaitForCommandsAsync(
      this Task<ConversationResult> previous, params string[] commands)
    {
      var result = await previous;
      return (await result.Conversation.WaitForCommandsAsync(commands));
    }
    public static async Task<ConversationResult> WaitForPersonNameAsync(
      this Task<ConversationResult> previous)
    {
      var result = await previous;
      return (await result.Conversation.WaitForPersonNameAsync());
    }
  }
  class ConversationResult
  {
    public Conversation Conversation { get; set; }
    public string Text { get; set; }
  }
  class Conversation
  {
    public Conversation(MediaElement mediaElement,
      Action<string> hypothesisHandler)
    {
      this.context = SynchronizationContext.Current;
      this.mediaElement = mediaElement;
      this.mediaElement.MediaEnded += this.OnMediaEnded;
      this.synthesizer = new SpeechSynthesizer();
      this.recognizer = new SpeechRecognizer();
      this.recognizer.HypothesisGenerated += OnHypothesisGenerated;
      this.hypothesisHandler = hypothesisHandler;
    }

    async void OnHypothesisGenerated(SpeechRecognizer sender, 
      SpeechRecognitionHypothesisGeneratedEventArgs args)
    {
      if (this.hypothesisHandler != null)
      {
        this.context.Post(
          _ =>
          {
            this.hypothesisHandler(args.Hypothesis.Text);
          },
          null);
      }
    }

    public async Task<ConversationResult> PauseAsync(TimeSpan delay)
    {
      await Task.Delay(delay);
      return (new ConversationResult()
      {
        Conversation = this
      });
    }
    public async Task<ConversationResult> SayAsync(string text)
    {
      using (var stream = await this.synthesizer.SynthesizeTextToStreamAsync(text))
      {
        this.mediaElement.SetSource(stream, string.Empty);
        this.taskDone = new TaskCompletionSource<bool>();
        this.mediaElement.Play();
        await this.taskDone.Task;
      }
      return (new ConversationResult()
      {
        Conversation = this
      });
    }
    public Task<ConversationResult> WaitForDigitsAsync()
    {
      return (this.WaitForDictationTopicAsync("Number"));
    }
    public async Task<ConversationResult> WaitForCommandsAsync(params string[] commands)
    {
      this.recognizer.Constraints.Clear();
      this.recognizer.Constraints.Add(new SpeechRecognitionListConstraint(commands));

      var returnValue = await this.ListenAsync();

      return (new ConversationResult()
      {
        Conversation = this,
        Text = returnValue
      });
    }
    public async Task<ConversationResult> WaitForDictationTopicAsync(string topic)
    {
      this.recognizer.Constraints.Clear();
      this.recognizer.Constraints.Add(
        new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation,
        topic));

      var result = await this.ListenAsync();

      return (new ConversationResult()
      {
        Conversation = this,
        Text = result
      });
    }
    public Task<ConversationResult> WaitForPersonNameAsync()
    {
      return (this.WaitForDictationTopicAsync("Person Name"));
    }
    async Task<string> ListenAsync()
    {
      await this.recognizer.CompileConstraintsAsync();

      var result = await this.recognizer.RecognizeAsync();
      var returnValue = string.Empty;

      if (result.Status == SpeechRecognitionResultStatus.Success)
      {
        returnValue = result.Text;
      }
      return (returnValue);
    }
    void OnMediaEnded(object sender, RoutedEventArgs e)
    {
      this.taskDone.SetResult(true);
    }
    SpeechRecognizer recognizer;
    TaskCompletionSource<bool> taskDone;
    SpeechSynthesizer synthesizer;
    MediaElement mediaElement;
    Action<string> hypothesisHandler;
    SynchronizationContext context;
  }
}
