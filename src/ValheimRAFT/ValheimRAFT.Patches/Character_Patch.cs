using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Character_Patch
{
  [HarmonyPatch(typeof(Character), "ApplyGroundForce")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Character_Patch_ApplyGroundForce(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.Method(typeof(Character), "GetStandingOnShip")))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Character_Patch), nameof(GetStandingOnShip)));
    return list;
  }

  public static object? GetStandingOnShip(Character __instance)
  {
    if (__instance.InNumShipVolumes == 0 || !__instance.IsOnGround() ||
        !(bool)__instance.m_lastGroundBody)
    {
      return null;
    }

    var lastOnShip = __instance.m_lastGroundBody.GetComponent<Ship>();

    if (lastOnShip)
    {
      return lastOnShip;
    }

    var bvc = __instance.m_lastGroundBody.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
    {
      return VehicleShipCompat.InitFromUnknown(bvc?.VehicleInstance);
    }

    /*
     * @deprecated old ship logic
     */
    var mb = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mb && (bool)mb.m_ship)
    {
      return VehicleShipCompat.InitFromUnknown(mb.m_ship);
    }

    return null;
  }

  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance, ref object? __result)
  {
    __result = GetStandingOnShip(__instance);
    return false;
  }

  [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
  [HarmonyPostfix]
  private static void UpdateGroundContact(Character __instance)
  {
    if (__instance is Player { m_debugFly: not false })
    {
      if (__instance.transform.parent != null) __instance.transform.SetParent(null);
      return;
    }

    BaseVehicleController? bvc = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      bvc = __instance.m_lastGroundBody.GetComponentInParent<BaseVehicleController>();
      if ((bool)bvc && __instance.transform.parent != bvc.transform)
      {
        __instance.transform.SetParent(bvc.transform);
        return;
      }
    }

    MoveableBaseRootComponent? mbr = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      mbr = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr && __instance.transform.parent != mbr.transform)
        __instance.transform.SetParent(mbr.transform);
    }

    if (!mbr && !bvc && __instance.transform.parent != null) __instance.transform.SetParent(null);
  }
}