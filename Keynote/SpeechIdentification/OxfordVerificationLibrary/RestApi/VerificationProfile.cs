namespace com.mtaulty.OxfordVerify
{
  using System;

  class VerificationProfile
  {
    public Guid VerificationProfileId { get; set; }
    public string Locale { get; set; }
    public int EnrollmentCount { get; set; }
    public int RemainingEnrollmentsCount { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset LastActionDateTime { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; }
  }
}
