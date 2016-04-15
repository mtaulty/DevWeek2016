namespace App336
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
    public static async Task<Guid?> GetGuidForAccountNumberAsync(string accountNumber)
    {
      Guid? guid = null;

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
        }
        catch (FileNotFoundException)
        {
        }
        if (accountMap.ContainsKey(accountNumber))
        {
          guid = accountMap[accountNumber];
        }
      }
      return (guid);
    }
    public static async Task SetGuidForAccountNumberAsync(string accountNumber,
      Guid guid)
    {
      accountMap[accountNumber] = guid;

      var serialized = JsonConvert.SerializeObject(accountMap);

      var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
        FILENAME, CreationCollisionOption.ReplaceExisting);

      await FileIO.WriteTextAsync(file, serialized);
    }
    static bool loaded;
    static readonly string FILENAME = "map.json";
    static Dictionary<string, Guid> accountMap;
  }
}