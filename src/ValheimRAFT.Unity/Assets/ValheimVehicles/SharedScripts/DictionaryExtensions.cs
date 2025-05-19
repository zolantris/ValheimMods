// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
namespace ValheimVehicles.SharedScripts.Helpers
{
  public static class DictionaryExtensions
  {
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
      if (!dict.ContainsKey(key))
      {
        dict[key] = value;
        return true;
      }
      return false;
    }
  }
}