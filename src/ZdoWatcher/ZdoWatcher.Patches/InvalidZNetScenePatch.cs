using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ZdoWatcher.ZdoWatcher.Config;

public class InvalidZNetScenePatch
{
  private static readonly List<ZDO> DestroyedInstanceZdos = new();

  /// <summary>
  /// Removes m_instances entries whose ZNetView was destroyed without going through ZNetScene
  /// (e.g. a child destroyed along with a vehicle hierarchy). Such entries keep ZDO.Created true,
  /// so the object is never re-created and ZNetScene.IsAreaReady never returns true, which hangs
  /// the respawn/teleport loading screen forever.
  /// </summary>
  public static void PurgeDestroyedInstances(ZNetScene scene, byte currentEarmark)
  {
    DestroyedInstanceZdos.Clear();
    foreach (var kvp in scene.m_instances)
    {
      if (!kvp.Value) DestroyedInstanceZdos.Add(kvp.Key);
    }

    if (DestroyedInstanceZdos.Count == 0) return;

    foreach (var zdo in DestroyedInstanceZdos)
    {
      scene.m_instances.Remove(zdo);
      if (zdo == null) continue;
      // allows ZNetScene.CreateObjects to instantiate it again while it is in range.
      zdo.Created = false;

      // mirror vanilla RemoveObjects for objects that are also out of range.
      if (zdo.TempRemoveEarmark != currentEarmark && !zdo.Persistent && zdo.IsOwner())
        ZDOMan.instance.DestroyZDO(zdo);
    }

    Logger.LogWarning(
      $"Purged {DestroyedInstanceZdos.Count} destroyed ZNetView instance(s) from ZNetScene that were not removed by ZNetScene. They will be re-created if still in range.");
    DestroyedInstanceZdos.Clear();
  }

  [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
  [HarmonyPrefix]
  private static bool RemoveObjects(ZNetScene __instance,
    List<ZDO> currentNearObjects,
    List<ZDO> currentDistantObjects,
    bool __runOriginal)
  {
    // Another prefix (e.g. ValheimVehicles) already replaced RemoveObjects this frame. Running a second
    // removal pass would destroy vehicle hierarchies it intentionally retained, orphaning their children in m_instances.
    if (!__runOriginal) return false;

    var num = (byte)(Time.frameCount & (int)byte.MaxValue);
    foreach (var currentNearObject in currentNearObjects)
      currentNearObject.TempRemoveEarmark = num;
    foreach (var currentDistantObject in currentDistantObjects)
      currentDistantObject.TempRemoveEarmark = num;

    PurgeDestroyedInstances(__instance, num);

    __instance.m_tempRemoved.Clear();
    foreach (var instanceKeyValPair in __instance.m_instances.Values)
    {
      if (!(bool)instanceKeyValPair)
        // if (instanceKeyValPair.Key != null)
        // {
        //   __instance.m_instances.Remove(instanceKeyValPair.Key);
        // }
        continue;

      if ((int)instanceKeyValPair.GetZDO().TempRemoveEarmark != (int)num)
        __instance.m_tempRemoved.Add(instanceKeyValPair);
    }

    for (var index = 0; index < __instance.m_tempRemoved.Count; ++index)
    {
      var znetView = __instance.m_tempRemoved[index];
      // Fix zNetremoval if invalid
      if (!(bool)znetView)
        // __instance.m_tempRemoved.Remove(__instance.m_tempRemoved[index]);
        continue;

      var zdo = znetView.GetZDO();
      znetView.ResetZDO();
      Object.Destroy((Object)znetView.gameObject);
      if (!zdo.Persistent && zdo.IsOwner())
        ZDOMan.instance.DestroyZDO(zdo);
      __instance.m_instances.Remove(zdo);
    }

    // always skips this. The ZNetScene is brittle with falsy instances
    return false;
  }
}