namespace com.mtaulty.OxfordVerify
{
  public enum VerificationStatus
  {
    Accept,
    Reject
  }
  public enum Confidence
  {
    Low,
    Normal,
    High
  }
  public class VerificationResult
  {
    public VerificationStatus Result { get; set; }
    public Confidence Confidence { get; set; }
    public string Phrase { get; set; }
  }
}
