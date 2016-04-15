namespace com.mtaulty.OxfordVerify.RestApi
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Net.Http;
  using System.Text;
  using System.Threading.Tasks;
  using Windows.Storage.Streams;

  // Lazy not turning this into a proper service and injecting it.
  class RestClient
  {
    public RestClient(string oxfordKey)
    {
      this.oxfordKey = oxfordKey;
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
    public async Task<EnrollmentResult> EnrollAsync(Guid profileId,
      IInputStream inputStream)
    {
      var uri = new Uri(
          $"{OXFORD_BASE_URL}{OXFORD_VERIFICATION_PROFILES_ENDPOINT}/" +
          Uri.EscapeDataString(profileId.ToString()) +
          $"/{OXFORD_ENROLL}");

      var result = await this.SendPcmStreamToOxfordEndpointAsync<EnrollmentResult>(
        uri, inputStream);

      return (result);
    }
    async Task<T> SendPcmStreamToOxfordEndpointAsync<T>(Uri uri, IInputStream inputStream)
    {
      StreamContent content = new StreamContent(inputStream.AsStreamForRead());

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
            OXFORD_SUB_KEY_HEADER, this.oxfordKey);
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
    string oxfordKey;
  }
}
