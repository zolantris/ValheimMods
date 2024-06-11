using System;
using System.Collections.Generic;
using ZdoWatcher;

namespace DynamicLocations;

internal class ZdoWatcherDelegate
{
  // var playerSpawn = zdo.GetInt(VehicleZdoVars.DynamicLocationSpawn, -1);
  // var playerLocation = zdo.GetInt(VehicleZdoVars.DynamicLocationLogout);
  public static readonly int DynamicLocationSpawn = "DynamicLocation_Spawn".GetStableHashCode();
  public static readonly int DynamicLocationLogout = "DynamicLocation_Logout".GetStableHashCode();

  public static Dictionary<int, ZDOID> DynamicSpawns;

  public void RegisterToZdoManager()
  {
    ZdoWatchManager.OnDeserialize += (OnZdoRegister);
    ZdoWatchManager.OnLoad += (OnZdoRegister);
  }

  private static void OnZdoRegister(ZDO zdo)
  {
  }

  public static void OnZdoUnRegister(ZDO zdo)
  {
  }
}