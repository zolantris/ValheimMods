// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
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

    public static bool HasNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : UnityEngine.Object
    {
      foreach (var key in dictionary.Keys)
      {
        if (key == null)
        {
          return true;
        }
      }

      return false;
    }


    public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : UnityEngine.Object
    {
      if (!dictionary.HasNullKeys())
      {
        return;
      }

      foreach (var key in dictionary.Keys.ToArray())
      {
        if (key == null)
        {
          dictionary.Remove(key);
        }
      }
    }
  }
}