using System;
using ZdoWatcher;

namespace DynamicLocations;

public class ZdoWatcherDelegate
{
  // var playerSpawn = zdo.GetInt(VehicleZdoVars.DynamicLocationSpawn, -1);
  // var playerLocation = zdo.GetInt(VehicleZdoVars.DynamicLocationLogout);
  public static readonly int DynamicLocationSpawn = "DynamicLocation_Spawn".GetStableHashCode();
  public static readonly int DynamicLocationLogout = "DynamicLocation_Logout".GetStableHashCode();
  public static ZdoWatchManager Watcher;

  public void RegisterToZdoManager()
  {
    ZdoWatchManager.OnDeserialize += (zdo => OnZdoRegister(zdo));
  }

  public static void OnZdoRegister(ZDO zdo)
  {
  }

  public static void OnZdoUnRegister(ZDO zdo)
  {
  }
}