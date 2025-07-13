using HarmonyLib;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Patches;

public class MineRock_Patches
{

  /// <summary>
  /// Used to prevent valheim built-in minerock5 from barfing when it hits more than the max allocated items in the spherecast.
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="hit"></param>
  [HarmonyPrefix]
  [HarmonyPatch(typeof(MineRock5), "Damage")]
  public static bool Damage(MineRock5 __instance, HitData hit)
  {
    if ((Object)__instance.m_nview == (Object)null || !__instance.m_nview.IsValid() || __instance.m_hitAreas == null)
      return false;
    if ((Object)hit.m_hitCollider == (Object)null || (double)hit.m_radius > 0.0)
    {
      var num1 = 0;

      MineRock5.m_tempColliderSet.Clear();

      // todo might need to locally refer to m_tempColliders as a list instead of using a static one which could then fight against other instances of minerock hits.
      var num2 = Physics.OverlapSphereNonAlloc(hit.m_point, (double)hit.m_radius > 0.0 ? hit.m_radius : 0.05f, MineRock5.m_tempColliders, MineRock5.m_rayMask);

      var maxCount = Mathf.Min(MineRock5.m_tempColliders.Length, num2);

      for (var index = 0; index < maxCount; ++index)
      {
        var currentTempCollider = MineRock5.m_tempColliders[index];
        if (currentTempCollider == null) continue;
        var currentTempColliderParent = currentTempCollider.transform.parent;
        if (currentTempColliderParent == null) continue;

        if (currentTempColliderParent == __instance.transform || currentTempColliderParent.parent != null && currentTempColliderParent.parent == __instance.transform)
          MineRock5.m_tempColliderSet.Add(MineRock5.m_tempColliders[index]);
      }
      if (MineRock5.m_tempColliderSet.Count > 0)
      {
        foreach (var tempCollider in MineRock5.m_tempColliderSet)
        {
          var areaIndex = __instance.GetAreaIndex(tempCollider);
          if (areaIndex >= 0)
          {
            ++num1;
            __instance.m_nview.InvokeRPC("RPC_Damage", (object)hit, (object)areaIndex);
            if (__instance.m_allDestroyed)
              return false;
            ;
          }
        }
      }
      if (num1 != 0)
        return false;
      LoggerProvider.LogDebugDebounced($"Minerock hit has no collider or invalid hit area on {__instance.gameObject.name}");
    }
    else
    {
      var areaIndex = __instance.GetAreaIndex(hit.m_hitCollider);
      if (areaIndex < 0)
        LoggerProvider.LogDebugDebounced($"Invalid hit area on {__instance.gameObject.name}");
      else
        __instance.m_nview.InvokeRPC("RPC_Damage", (object)hit, (object)areaIndex);
    }
    return false;
  }
}