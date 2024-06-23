using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ZdoWatcher;

namespace ValheimVehicles;

public abstract class ZdoWatcherDelegate
{
  public static void RegisterToZdoManager()
  {
    // call same method for both actions as it doesn't matter here
    ZdoWatchManager.OnDeserialize += (OnZdoLoad);
    ZdoWatchManager.OnLoad += (OnZdoLoad);
    ZdoWatchManager.OnReset += (OnZdoReset);
  }

  private static void OnZdoLoad(ZDO zdo)
  {
    BaseVehicleController.InitZdo(zdo);

    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      MoveableBaseRootComponent.InitZDO(zdo);
    }
  }

  public static void OnZdoReset(ZDO zdo)
  {
    BaseVehicleController.RemoveZDO(zdo);

    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      MoveableBaseRootComponent.RemoveZDO(zdo);
    }
  }
}