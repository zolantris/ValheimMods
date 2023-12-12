// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.Util.ListUtil
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System.Collections.Generic;

namespace ValheimRAFT.Util
{
  public static class ListUtil
  {
    public static void FastRemove<T>(this List<T> list, T item)
    {
      int index = list.IndexOf(item);
      if (index == -1)
        return;
      list[index] = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
    }

    public static void FastRemoveAt<T>(this List<T> list, int index)
    {
      if (index < 0 || index >= list.Count)
        return;
      list[index] = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
    }
  }
}