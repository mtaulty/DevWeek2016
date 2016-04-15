namespace WpfApplication29
{
  using System;
  using System.IO;
  using System.Windows.Controls;
  using System.Linq;
  using System.Windows.Media;
  class VideoManager
  {
    public VideoManager(params MediaElement[] mediaElements)
    {
      this.mediaElements = mediaElements;

      this.videos = Directory.GetFiles(VIDEO_FOLDER).Select(
        file => new Uri(file, UriKind.Relative)).ToArray();

      for (int i = 0; i < this.mediaElements.Length; i++)
      {
        int videoIndex = Math.Min(i, this.videos.Length - 1);

        this.mediaElements[i].Source = this.videos[videoIndex];

        this.mediaElements[i].MediaEnded += OnMediaEnded;
      }
    }
    public void Play()
    {
      foreach (var element in this.mediaElements)
      {
        element.Play();
      }
    }
    public void Stop()
    {
      foreach (var element in this.mediaElements)
      {
        element.Stop();
      }
    }
    public void Pause()
    {
      foreach (var element in this.mediaElements)
      {
        element.Pause();
      }
    }

    void OnMediaEnded(object sender, System.Windows.RoutedEventArgs e)
    {
      ((MediaElement)sender).Position = TimeSpan.FromSeconds(0);
      ((MediaElement)sender).Play();
    }
    public void SlideLeft()
    {
      if ((this.currentLeftMostIndex + this.mediaElements.Length) <
        (this.videos.Length))
      {
        this.currentLeftMostIndex++;
        this.MoveVideosToReflectLeftMost();
      }
    }
    public void SlideRight()
    {
      if (this.currentLeftMostIndex > 0)
      {
        this.currentLeftMostIndex--;
        MoveVideosToReflectLeftMost();
      }
    }    
    void MoveVideosToReflectLeftMost()
    {
      for (int i = 0; i < this.mediaElements.Length; i++)
      {
        this.mediaElements[i].Source = this.videos[
          this.currentLeftMostIndex + i];
      }
    }
    const string VIDEO_FOLDER = @".\videos";
    int currentLeftMostIndex = 0;
    Uri[] videos;
    MediaElement[] mediaElements;
  }
}
