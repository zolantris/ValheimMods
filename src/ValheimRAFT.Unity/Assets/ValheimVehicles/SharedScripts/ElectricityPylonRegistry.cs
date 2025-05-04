// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class ElectricityPylonRegistry
  {
    public static readonly List<ElectricPylon> All = new();

    public static event Action? OnPylonListChanged;

    public static void Add(ElectricPylon pylon)
    {
      if (!All.Contains(pylon))
      {
        All.Add(pylon);
        OnPylonListChanged?.Invoke();
      }
    }

    public static void Remove(ElectricPylon pylon)
    {
      if (All.Remove(pylon))
      {
        OnPylonListChanged?.Invoke();
      }
    }
  }
}