namespace ModSync;

public class ModSyncUtils
{
  internal static dynamic FindByName(dynamic arr, string key, string value)
  {
    foreach (var obj in arr)
      if ((string)obj[key] == value)
        return obj;
    return null;
  }
}