using System.Collections.Generic;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ZdoWatcher;
using Zolantris.Shared;
namespace ValheimVehicles.Helpers;

public static class PersistentIdHelper
{
  private static readonly Dictionary<ZDO, int> _zdoToMBParentCache = new();

  public static void ClearMBParentCache()
  {
    _zdoToMBParentCache.Clear();
  }

  public static int GetMBParentId(ZDO? zdo)
  {
    if (zdo == null) return 0;
    if (_zdoToMBParentCache.TryGetValue(zdo, out var cachedId))
    {
      return cachedId;
    }
    cachedId = zdo.GetInt(VehicleZdoVars.MBParentId, 0);

    if (cachedId != 0)
    {
      _zdoToMBParentCache.TryAdd(zdo, cachedId);
    }

    return cachedId;
  }

  public static int GetPersistentIdFrom(ZNetView netView, ref int cache)
  {
    if (ZNetView.m_forceDisableInit || ZNetScene.instance == null || Game.instance == null)
      return 0;

    if (cache != 0) return cache;

    if (ZdoWatchController.Instance == null)
    {
      LoggerProvider.LogWarning("No ZdoWatchManager instance, this means something went wrong");
      return 0;
    }

    if (netView == null || netView.GetZDO() == null)
      return 0;

    cache = ZdoWatchController.Instance.GetOrCreatePersistentID(netView.GetZDO());
    return cache;
  }
}