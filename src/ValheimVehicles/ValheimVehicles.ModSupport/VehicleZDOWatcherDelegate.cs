using System;
using System.Reflection;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ZdoWatcher;
using Zolantris.Shared;

namespace ValheimVehicles.ModSupport;

public abstract class VehicleZDOWatcherDelegate
{
  public static void RegisterToZdoManager()
  {
    // call same method for both actions as it doesn't matter here
    ZdoWatchController.OnDeserialize += OnZdoDeserialize;
    ZdoWatchController.OnLoad += OnZdoLoad;
    ZdoWatchController.OnReset += OnZdoReset;
  }

  private static void OnZdoLoad(ZDO zdo)
  {
    VehiclePiecesController.InitZdo(zdo);
  }
  private static void OnZdoDeserialize(ZDO zdo)
  {
    VehiclePiecesController.InitZdo(zdo);
  }

  public static void OnZdoReset(ZDO zdo)
  {
    VehiclePiecesController.RemoveZDO(zdo);
  }
}