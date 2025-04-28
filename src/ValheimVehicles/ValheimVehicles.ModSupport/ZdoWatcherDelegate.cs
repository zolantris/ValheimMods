

using ValheimVehicles.Controllers;
using ZdoWatcher;

namespace ValheimVehicles.ModSupport;

public abstract class ZdoWatcherDelegate
{
  public static void RegisterToZdoManager()
  {
    // call same method for both actions as it doesn't matter here
    ZdoWatchController.OnDeserialize += (OnZdoLoad);
    ZdoWatchController.OnLoad += (OnZdoLoad);
    ZdoWatchController.OnReset += (OnZdoReset);
  }

  private static void OnZdoLoad(ZDO zdo)
  {
    VehiclePiecesController.InitZdo(zdo);
  }

  public static void OnZdoReset(ZDO zdo)
  {
    VehiclePiecesController.RemoveZDO(zdo);
  }
}