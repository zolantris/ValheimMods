using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ZdoWatcher.ZdoWatcher.Config;

public class InvalidZNetScenePatch
{
  [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
  [HarmonyPrefix]
  private static bool RemoveObjects(ZNetScene __instance, List<ZDO> currentNearObjects,
    List<ZDO> currentDistantObjects)
  {
    byte num = (byte)(Time.frameCount & (int)byte.MaxValue);
    foreach (ZDO currentNearObject in currentNearObjects)
      currentNearObject.TempRemoveEarmark = num;
    foreach (ZDO currentDistantObject in currentDistantObjects)
      currentDistantObject.TempRemoveEarmark = num;
    __instance.m_tempRemoved.Clear();
    foreach (var znetView in __instance.m_instances.Values)
    {
      if (!(bool)znetView) continue;
      if ((int)znetView.GetZDO().TempRemoveEarmark != (int)num)
        __instance.m_tempRemoved.Add(znetView);
    }

    for (int index = 0; index < __instance.m_tempRemoved.Count; ++index)
    {
      ZNetView znetView = __instance.m_tempRemoved[index];
      if (!(bool)znetView) continue;
      ZDO zdo = znetView.GetZDO();
      znetView.ResetZDO();
      UnityEngine.Object.Destroy((UnityEngine.Object)znetView.gameObject);
      if (!zdo.Persistent && zdo.IsOwner())
        ZDOMan.instance.DestroyZDO(zdo);
      __instance.m_instances.Remove(zdo);
    }

    // always skips this. The ZNetScene is brittle with falsy instances
    return false;
  }
}