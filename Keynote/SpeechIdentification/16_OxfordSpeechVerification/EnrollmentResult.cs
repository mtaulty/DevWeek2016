namespace App336
{
  enum EnrollmentStatus
  {
    Enrolling,
    Training,
    Enrolled
  }
  class EnrollmentResult
  {
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public int EnrollmentsCount { get; set; }
    public int RemainingEnrollments { get; set; }
    public string Phrase { get; set; }
  }
}
