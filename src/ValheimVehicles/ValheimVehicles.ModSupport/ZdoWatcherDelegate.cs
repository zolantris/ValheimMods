using ValheimRAFT;
using ValheimVehicles.Vehicles;
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

    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      MoveableBaseRootComponent.InitZDO(zdo);
    }
  }

  public static void OnZdoReset(ZDO zdo)
  {
    VehiclePiecesController.RemoveZDO(zdo);

    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      MoveableBaseRootComponent.RemoveZDO(zdo);
    }
  }
}