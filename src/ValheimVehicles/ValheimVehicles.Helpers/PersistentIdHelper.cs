using ValheimVehicles.SharedScripts;
using ZdoWatcher;
namespace ValheimVehicles.Helpers;

public static class PersistentIdHelper
{
  public static int GetPersistentIdFrom(ZNetView nview, ref int cache)
  {
    if (ZNetView.m_forceDisableInit || ZNetScene.instance == null || Game.instance == null)
      return 0;

    if (cache != 0) return cache;

    if (ZdoWatchController.Instance == null)
    {
      LoggerProvider.LogWarning("No ZdoWatchManager instance, this means something went wrong");
      return 0;
    }

    if (!nview || nview.GetZDO() == null)
      return 0;

    cache = ZdoWatchController.Instance.GetOrCreatePersistentID(nview.GetZDO());
    return cache;
  }
}