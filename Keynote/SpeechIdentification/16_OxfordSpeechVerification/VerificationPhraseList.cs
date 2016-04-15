namespace App336
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;

  static class VerificationPhraseList
  {
    public static async Task<VerificationPhrase> GetVerificationPhraseForProfileAsync(
      VerificationProfile profile)
    {
      if (phrases == null)
      {
        restClient = new OxfordSpeakerIdRestClient();
        var results = await restClient.GetVerificationPhrasesAsync();
        phrases = results.Reverse().ToArray();
      }
      var id = Guid.Parse(profile.VerificationProfileId.ToString());
      var bits = id.ToByteArray();
      var sum = bits.Sum(b => (int)b);
      var entry = sum % phrases.Length;
      return (phrases[entry]);
    }
    static OxfordSpeakerIdRestClient restClient;
    static VerificationPhrase[] phrases;
  }
}
