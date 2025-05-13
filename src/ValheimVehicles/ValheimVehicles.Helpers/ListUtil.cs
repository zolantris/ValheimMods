using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Helpers;

public static class ListUtil
{
  public static void FastRemove<T>(this List<T> list, T item)
  {
    var index = list.IndexOf(item);
    if (index != -1)
    {
      list[index] = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
    }
  }

  public static void CleanNullsFast<T>(this List<T> list) where T : class
  {
    for (var i = 0; i < list.Count; i++)
    {
      if (list[i] == null)
        list.FastRemoveAt(ref i);
    }
  }

  public static void FastRemoveAt<T>(this List<T> list, ref int index)
  {
    if (index >= 0 && index < list.Count)
    {
      list[index] = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
      index--;
    }
  }

  // Generic method to remove items with invalid key-value pairs
  public static void FastRemoveByKey<T>(this List<T> list, string key)
  {
    // Iterate through each item in the list
    for (var i = list.Count - 1; i >= 0; i--)
    {
      var item = list[i];

      // Check if the key exists in the object type T
      var property = typeof(T).GetProperty(key);

      // If the key doesn't exist, we skip this item
      if (property == null)
      {
        continue;
      }

      // Get the value of the property by reflection
      var value = property.GetValue(item);

      // If the value is null or default for reference types, remove the item
      if (value == null)
      {
        list.RemoveAt(i);
      }
    }
  }

  public static bool HasNonNullProperty<T>(T item, string key, bool hasDebug = false)
  {
    var property = typeof(T).GetProperty(key);
    var isPropertyValid = property != null;
    if (property == null || property.GetValue(item) == null)
    {
      if (hasDebug)
      {
        if (!isPropertyValid)
        {
          LoggerProvider.LogDebug($"key: {key} does not exist on type: {typeof(T).Name}");
        }
        else
        {
          LoggerProvider.LogDebug($"key: {key} is null on {typeof(T).Name}");
        }
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Validates any properties on an object that must not be null
  /// </summary>
  private static bool HasNonNullProperties<T>(T item, List<string>? propertyKeys)
  {
    if (propertyKeys == null || propertyKeys.Count == 0)
    {
      return true;
    }

    var isValid = true;

    foreach (var key in propertyKeys)
    {
      if (!HasNonNullProperty(item, key))
      {
        isValid = false;
        break;
      }
    }
    return isValid;
  }

  public static bool TryGetValidElement<T>(this List<T> list, ref int index, out T result, bool hasDebug = false) where T : class
  {
    return list.TryGetValidElement(ref index, null, out result, hasDebug);
  }

  // Example of how to use TryGetOrRemoveItem with key-based validation
  public static bool TryGetValidElement<T>(this List<T> list, ref int index, List<string>? propertyKeys, out T result, bool hasDebug = false) where T : class
  {
    result = null;
    if (index < 0 || index >= list.Count)
    {
      return false;
    }

    var item = list[index];
    if (item == null)
    {
      list.FastRemoveAt(ref index);
      return false;
    }

    if (!HasNonNullProperties(item, propertyKeys))
    {
      list.FastRemoveAt(ref index);
      return false;
    }

    result = item;

    return true;
  }
}