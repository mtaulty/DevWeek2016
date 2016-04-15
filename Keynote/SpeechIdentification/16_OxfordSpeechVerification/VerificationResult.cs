namespace App336
{
  enum VerificationStatus
  {
    Accept,
    Reject
  }
  enum Confidence
  {
    Low,
    Normal,
    High
  }
  class VerificationResult
  {
    public VerificationStatus Result { get; set; }
    public Confidence Confidence { get; set; }
    public string Phrase { get; set; }
  }
}
