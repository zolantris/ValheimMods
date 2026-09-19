using System;
using System.Reflection;
using ValheimVehicles.Controllers;
using ValheimVehicles.Integrations;
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
    SwivelComponentBridge.InitZdo(zdo);
  }

  private static void OnZdoDeserialize(ZDO zdo)
  {
    VehiclePiecesController.InitZdo(zdo);
    SwivelComponentBridge.InitZdo(zdo);
  }

  public static void OnZdoReset(ZDO zdo)
  {
    // never delete the zdo when saving which creates a clone of the zdo data during save but doesn't unload / remove the zdo, if the delete happens the onzdoload/deserialize do not call and that zdo is lost and does not restore to the global gamesession vehicle zdo tracker.
    if (zdo.SaveClone)
    {
      return;
    }

    VehiclePiecesController.RemoveZDO(zdo);
    SwivelComponentBridge.RemoveZdo(zdo);
  }
}