// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public static class PowerPylonRegistry
  {
    public static readonly List<PowerPylon> All = new();

    public static event Action? OnPylonListChanged;

    public static void Add(PowerPylon pylon)
    {
      if (!All.Contains(pylon))
      {
        All.Add(pylon);
        OnPylonListChanged?.Invoke();
      }
    }

    public static void Remove(PowerPylon pylon)
    {
      if (All.Remove(pylon))
      {
        OnPylonListChanged?.Invoke();
      }
    }
  }
}