namespace com.mtaulty.OxfordVerify.Audio
{
  using System;
  using System.Threading.Tasks;
  using Windows.Devices.Enumeration;
  using Windows.Media.Audio;
  using Windows.Media.Capture;
  using Windows.Media.Devices;
  using Windows.Media.MediaProperties;
  using Windows.Media.Render;
  using Windows.Storage;

  class AudioRecorder
  {
    public async Task StartRecordToFileAsync(StorageFile file)
    {
      var result = await AudioGraph.CreateAsync(
        new AudioGraphSettings(AudioRenderCategory.Media));

      if (result.Status == AudioGraphCreationStatus.Success)
      {
        this.graph = result.Graph;

        var microphone = await DeviceInformation.CreateFromIdAsync(
          MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default));

        // Low gives us 1 channel, 16-bits per sample, 16K sample rate.
        var outProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low);
        outProfile.Audio = AudioEncodingProperties.CreatePcm(16000, 1, 16);

        var inProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);

        var outputResult = await this.graph.CreateFileOutputNodeAsync(file,
          outProfile);

        if (outputResult.Status == AudioFileNodeCreationStatus.Success)
        {
          this.outputNode = outputResult.FileOutputNode;

          var inputResult = await this.graph.CreateDeviceInputNodeAsync(
            MediaCategory.Media,
            inProfile.Audio,
            microphone);

          if (inputResult.Status == AudioDeviceNodeCreationStatus.Success)
          {
            inputResult.DeviceInputNode.AddOutgoingConnection(
              this.outputNode);

            this.graph.Start();
          }
        }
      }
    }
    public async Task StopRecordAsync()
    {
      if (this.graph != null)
      {
        this.graph?.Stop();

        await this.outputNode.FinalizeAsync();

        // assuming that disposing the graph gets rid of the input/output nodes?
        this.graph?.Dispose();

        this.graph = null;
      }
    }
    AudioGraph graph;
    AudioFileOutputNode outputNode;
  }
}
