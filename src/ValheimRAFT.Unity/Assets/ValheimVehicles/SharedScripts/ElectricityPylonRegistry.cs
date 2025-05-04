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
    public static readonly List<ElectricityPylon> All = new();

    public static event Action? OnPylonListChanged;

    public static void Add(ElectricityPylon pylon)
    {
      if (!All.Contains(pylon))
      {
        All.Add(pylon);
        OnPylonListChanged?.Invoke();
      }
    }

    public static void Remove(ElectricityPylon pylon)
    {
      if (All.Remove(pylon))
      {
        OnPylonListChanged?.Invoke();
      }
    }
  }
}