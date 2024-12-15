using System.Collections.Generic;

namespace ValheimRAFT.Util;

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

  public static void FastRemoveAt<T>(this List<T> list, int index)
  {
    if (index >= 0 && index < list.Count)
    {
      list[index] = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
    }
  }
}