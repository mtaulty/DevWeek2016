namespace App336
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Text;
  using System.Threading.Tasks;
  using Windows.Storage.Streams;
  using System.IO;
  using System.Net.Http.Headers;

  // Lazy not turning this into a proper service and injecting it.
  class OxfordSpeakerIdRestClient
  {
    public OxfordSpeakerIdRestClient()
    {

    }
    byte[] HackOxfordWavPcmStream(IInputStream inputStream, out int offset)
    {
      var netStream = inputStream.AsStreamForRead();
      var bits = new byte[netStream.Length];
      netStream.Read(bits, 0, bits.Length);

      // original file length
      var pcmFileLength = BitConverter.ToInt32(bits, 4);

      // take away 36 bytes for the JUNK chunk
      pcmFileLength -= 36;

      // now copy 12 bytes from start of bytes to 36 bytes further on
      for (int i = 0; i < 12; i++)
      {
        bits[i + 36] = bits[i];
      }
      // now put modified file length into byts 40-43
      var newLengthBits = BitConverter.GetBytes(pcmFileLength);
      newLengthBits.CopyTo(bits, 40);

      // the bits that we want are now 36 onwards in this array
      offset = 36;

      return (bits);
    }
    public async Task<VerificationResult> VerifyAsync(Guid profileId,
      IInputStream inputStream)
    {
      var uri = new Uri(
        $"{OXFORD_BASE_URL}{OXFORD_VERIFICATION_ENDPOINT}" +
        Uri.EscapeDataString(profileId.ToString()));

      var result = await this.SendPcmStreamToOxfordEndpointAsync<VerificationResult>(
        uri, inputStream);

      return (result);
    }
    public async Task<EnrollmentResult> EnrollAsync(VerificationProfile profile,
      IInputStream inputStream)
    {
      var uri = new Uri(
          $"{OXFORD_BASE_URL}{OXFORD_VERIFICATION_PROFILES_ENDPOINT}/" +
          Uri.EscapeDataString(profile.VerificationProfileId.ToString()) +
          $"/{OXFORD_ENROLL}");

      var result = await this.SendPcmStreamToOxfordEndpointAsync<EnrollmentResult>(
        uri, inputStream);

      return (result);
    }
    async Task<T> SendPcmStreamToOxfordEndpointAsync<T>(Uri uri, IInputStream inputStream)
    {
      int offset;
      byte[] bits = this.HackOxfordWavPcmStream(inputStream, out offset);

      ByteArrayContent content = new ByteArrayContent(bits, offset, bits.Length - offset);

      var response = await this.HttpClient.PostAsync(uri, content);

      var result = await this.HandleHttpJsonResultAsync<T>(response);

      return (result);
    }
    public async Task RemoveVerificationProfileAsync(VerificationProfile profile)
    {
      var response = await this.HttpClient.DeleteAsync(
        new Uri($"{OXFORD_BASE_URL}/{OXFORD_VERIFICATION_PROFILES_ENDPOINT}/" +
          Uri.EscapeDataString(profile.VerificationProfileId.ToString())));

      this.ThrowOnFailStatus(response, string.Empty);
    }
    void ThrowOnFailStatus(HttpResponseMessage response, string content)
    {
      if (!response.IsSuccessStatusCode)
      {
        throw new HttpRequestException(
          $"Something went wrong - I got [{content}]");
      }
    }
    public async Task<IEnumerable<VerificationPhrase>> GetVerificationPhrasesAsync()
    {
      var results = await this.GetEndpointJsonResultAsync<VerificationPhrase[]>(
        OXFORD_VERIFICATION_PHRASES_ENDPOINT);

      return (results);
    }
    public async Task<VerificationProfile> AddVerificationProfileAsync()
    {
      var jObject = new JObject();

      jObject["locale"] = "en-us";

      var result = await this.PostEndpointJsonResultAsync<VerificationProfile>(
        OXFORD_VERIFICATION_PROFILES_ENDPOINT, jObject);

      return (result);
    }
    public async Task<IEnumerable<VerificationProfile>> GetVerificationProfilesAsync()
    {
      var results = await this.GetEndpointJsonResultAsync<VerificationProfile[]>(
        OXFORD_VERIFICATION_PROFILES_ENDPOINT);

      return (results);
    }
    async Task<T> PostEndpointJsonResultAsync<T>(string endpoint, JObject jsonObject)
    {
      var content = new StringContent(jsonObject.ToString(),
        Encoding.UTF8, "application/json");

      var response = await this.HttpClient.PostAsync(
        new Uri($"{OXFORD_BASE_URL}/{endpoint}"), content);

      var result = await this.HandleHttpJsonResultAsync<T>(response);

      return (result);
    }
    async Task<T> GetEndpointJsonResultAsync<T>(string endpoint)
    {
      var response = await this.HttpClient.GetAsync(new Uri(
        $"{OXFORD_BASE_URL}{endpoint}"));

      var result = await this.HandleHttpJsonResultAsync<T>(response);

      return (result);
    }
    async Task<T> HandleHttpJsonResultAsync<T>(HttpResponseMessage response)
    {
      var stringContent = await response.Content.ReadAsStringAsync();

      this.ThrowOnFailStatus(response, stringContent);

      var jsonObject = JsonConvert.DeserializeObject<T>(stringContent);

      return (jsonObject);
    }
    HttpClient HttpClient
    {
      get
      {
        if (this.httpClient == null)
        {
          this.httpClient = new HttpClient();
          this.httpClient.DefaultRequestHeaders.Add(
            OXFORD_SUB_KEY_HEADER, Keys.OxfordKey);
        }
        return (this.httpClient);
      }
    }
    static readonly string OXFORD_BASE_URL = "https://api.projectoxford.ai/spid/v1.0/";
    static readonly string OXFORD_ENROLL = "enroll";
    static readonly string OXFORD_VERIFICATION_ENDPOINT = "verify?verificationProfileId=";
    static readonly string OXFORD_VERIFICATION_PROFILES_ENDPOINT = "verificationProfiles";
    static readonly string OXFORD_VERIFICATION_PHRASES_ENDPOINT = "verificationPhrases?locale=en-us";
    static readonly string OXFORD_SUB_KEY_HEADER = "Ocp-Apim-Subscription-Key";
    HttpClient httpClient;
  }
}
