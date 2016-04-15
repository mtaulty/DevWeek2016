using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App336
{
  class DisplayTextViewModel : ViewModelBase
  {
    public string DisplayText
    {
      get
      {
        return (this.displayText);
      }
      set
      {
        base.SetProperty(ref this.displayText, value);
      }
    }
    string displayText;

    public float ProgressMinimum
    {
      get
      {
        return (this.progressMinimum);
      }
      set
      {
        base.SetProperty(ref this.progressMinimum, value);
      }
    }
    float progressMinimum;


    public float ProgressMaximum
    {
      get
      {
        return (this.progressMaximum);
      }
      set
      {
        base.SetProperty(ref this.progressMaximum, value);
      }
    }
    float progressMaximum;

    public float ProgressValue
    {
      get
      {
        return (this.progressValue);
      }
      set
      {
        base.SetProperty(ref this.progressValue, value);
      }
    }
    float progressValue;


    public string UserToAdd
    {
      get
      {
        return (this.userToAdd);
      }
      set
      {
        base.SetProperty(ref this.userToAdd, value);
      }
    }
    string userToAdd;

  }
}
