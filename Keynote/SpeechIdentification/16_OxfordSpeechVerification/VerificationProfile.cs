namespace App336
{
  using System;

  class VerificationProfile : ViewModelBase
  {

    public Guid VerificationProfileId
    {
      get
      {
        return (this.verificationProfileId);
      }
      set
      {
        base.SetProperty(ref this.verificationProfileId, value);
      }
    }
    Guid verificationProfileId;


    public string Locale
    {
      get
      {
        return (this.locale);
      }
      set
      {
        base.SetProperty(ref this.locale, value);
      }
    }
    string locale;


    public int EnrollmentsCount
    {
      get
      {
        return (this.enrollmentsCount);
      }
      set
      {
        base.SetProperty(ref this.enrollmentsCount, value);
      }
    }
    int enrollmentsCount;


    public int RemainingEnrollmentsCount
    {
      get
      {
        return (this.remainingEnrollmentsCount);
      }
      set
      {
        base.SetProperty(ref this.remainingEnrollmentsCount, value);
      }
    }
    int remainingEnrollmentsCount;


    public DateTimeOffset CreatedDateTime
    {
      get
      {
        return (this.createdDateTime);
      }
      set
      {
        base.SetProperty(ref this.createdDateTime, value);
      }
    }
    DateTimeOffset createdDateTime;


    public DateTimeOffset LastActionDateTime
    {
      get
      {
        return (this.lastActionDateTime);
      }
      set
      {
        base.SetProperty(ref this.lastActionDateTime, value);
      }
    }
    DateTimeOffset lastActionDateTime;


    public string EnrollmentStatus
    {
      get
      {
        return (this.enrollmentStatus);
      }
      set
      {
        base.SetProperty(ref this.enrollmentStatus, value);
      }
    }
    string enrollmentStatus;

  }
}
