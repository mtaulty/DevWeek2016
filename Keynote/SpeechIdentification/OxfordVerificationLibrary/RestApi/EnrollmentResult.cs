namespace com.mtaulty.OxfordVerify
{
  public enum EnrollmentStatus
  {
    Enrolling,
    Training,
    Enrolled,
    None
  }
  public class EnrollmentResult
  {
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public int EnrollmentsCount { get; set; }
    public int RemainingEnrollments { get; set; }
    public string Phrase { get; set; }
  }
}
