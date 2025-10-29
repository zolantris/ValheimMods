using System.Collections.Generic;
using Eldritch.Core;
using HarmonyLib;
namespace Eldritch.Valheim;

[HarmonyPatch]
public class Patch_Humanoid
{
  // Prefab names you use for xeno creatures (add all variants)
  private static readonly int XenoHash1 = "Eldritch_XenoAdult_Creature".GetStableHashCode();
  private static readonly int XenoHash2 = "Eldritch_XenoAdult".GetStableHashCode();
  private static readonly HashSet<int> XenoPrefabHashes = new() { XenoHash1, XenoHash2 };

  // Fast guard: check prefab hash (server-auth) or name fallback (editor/local)
  private static bool IsXeno(Humanoid h)
  {
    var nview = h.GetComponent<ZNetView>();
    var zdo = nview ? nview.GetZDO() : null;
    if (zdo != null)
    {
      // Valheim stores prefab as stable hash on ZDO
      return XenoPrefabHashes.Contains(zdo.m_prefab);
    }

    // Fallback if no ZDO yet (e.g., local editor play): compare stripped name
    var name = h.gameObject.name;
    if (name.EndsWith("(Clone)")) name = name[..^7];
    return name == "Eldritch_XenoAdult_Creature" || name == "Eldritch_XenoAdult";
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
  [HarmonyPrefix]
  private static bool Humanoid_StartAttack(Humanoid __instance, ref bool __result)
  {
    // Ultra-cheap: bail for everything that isn’t our Xeno without touching components
    if (!IsXeno(__instance)) return true;

    // Only now touch our driver once
    var driver = __instance.GetComponent<XenoDroneAI>();
    var ai = __instance.GetComponent<MonsterAI>();
    if (driver == null || ai == null) return true; // fall back to vanilla if we’re not fully wired

    var target = ai.GetTargetCreature();
    if (target == null) return true;

    // Route to our custom attack (damage via anim events)
    driver.PrimaryTarget = target.transform;
    driver.StartAttackBehavior();

    __result = true; // report "attack started" to vanilla state machines
    return false; // skip vanilla StartAttack body
  }
}