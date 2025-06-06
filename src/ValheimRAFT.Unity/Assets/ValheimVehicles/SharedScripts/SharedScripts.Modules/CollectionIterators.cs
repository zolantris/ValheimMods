// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
namespace ValheimVehicles.SharedScripts.Modules
{

  /// <summary>
  /// A collection of iterators for use in non-linq environments where code must not allocate additionally.
  /// </summary>
  public static class CollectionIterators
  {
    // Core iterator for .Any
    public static bool AnyCore<T>(IList<T> collection, Func<T, bool> predicate)
    {
      for (var i = 0; i < collection.Count; i++)
        if (predicate(collection[i]))
          return true;
      return false;
    }

    public static bool AnyCore<T>(IList<T> collection)
    {
      return collection != null && collection.Count > 0;
    }

    public static T FirstOrDefaultCore<T>(IList<T> collection, Func<T, bool> predicate)
    {
      for (var i = 0; i < collection.Count; i++)
        if (predicate(collection[i]))
          return collection[i];
      return default;
    }

    public static T FirstOrDefaultCore<T>(IList<T> collection)
    {
      return collection != null && collection.Count > 0 ? collection[0] : default;
    }

    public static int FindIndexCore<T>(IList<T> collection, Func<T, bool> predicate)
    {
      for (var i = 0; i < collection.Count; i++)
        if (predicate(collection[i]))
          return i;
      return -1;
    }
  }
}