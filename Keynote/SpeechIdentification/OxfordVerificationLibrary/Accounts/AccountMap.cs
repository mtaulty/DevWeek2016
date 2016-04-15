namespace com.mtaulty.OxfordVerify.Accounts
{
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;
  using Windows.Storage;

  static class AccountMap
  {
    static AccountMap()
    {
      accountMap = new Dictionary<string, Guid>();
    }
    static async Task InitialiseAsync()
    {
      if (!loaded)
      {
        try
        {
          var file = await ApplicationData.Current.LocalFolder.GetFileAsync(
            FILENAME);

          var text = await FileIO.ReadTextAsync(file);

          var deserialized = JsonConvert.DeserializeObject<
            Dictionary<string, Guid>>(text);

          accountMap = deserialized;

          loaded = true;
        }
        catch (FileNotFoundException)
        {
        }
      }
    }
    public static async Task<Guid?> GetGuidForUserNameAsync(string userName)
    {
      Guid? guid = null;

      await InitialiseAsync();

      if (accountMap.ContainsKey(userName))
      {
        guid = accountMap[userName];
      }
      return (guid);
    }
    public static async Task SetGuidForUserNameAsync(string userName,
      Guid guid)
    {
      await InitialiseAsync();

      accountMap[userName] = guid;

      var serialized = JsonConvert.SerializeObject(accountMap);

      var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
        FILENAME, CreationCollisionOption.ReplaceExisting);

      await FileIO.WriteTextAsync(file, serialized);
    }
    public static async Task<IReadOnlyList<string>> GetAllAccountNamesAsync()
    {
      await InitialiseAsync();

      List<string> entries = new List<string>();

      if (accountMap != null)
      {
        entries.AddRange(accountMap.Keys);
      }

      return (entries.AsReadOnly());
    }
    static bool loaded;
    static readonly string FILENAME = "map.json";
    static Dictionary<string, Guid> accountMap;
  }
}