using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;


namespace ValheimVehicles.Patches;

/// <summary>
/// Valheim 0.219.1 (bogwitch) patch
/// </summary>
/// <param name="p"></param>
/// todo add patches for monster areas too. Could prevent spawning of monsters in theory and add booleans to allow spawning when the vehicle is re-rendered as the bounds would then be accurate and prevent spawns.
/// <param name="__result"></param>
/// <returns></returns>
public static class EffectsArea_VehiclePatches
{
  [HarmonyPatch(typeof(EffectArea),
    nameof(EffectArea.IsPointPlus025InsideBurningArea))]
  [HarmonyPrefix]
  [HarmonyPriority(Priority.VeryHigh)]
  private static bool EffectArea_GetBurningAreaPointPlus025(Vector3 p,
    ref bool __result)
  {
    EffectArea effectArea = null!;
    var prefixOutput = GetPrefix_EffectWithinVehicleArea(p, ref effectArea);
    __result = effectArea != null;
    return prefixOutput;
  }

  [HarmonyPatch(typeof(EffectArea),
    nameof(EffectArea.GetBurningAreaPointPlus025))]
  [HarmonyPrefix]
  [HarmonyPriority(Priority.VeryHigh)]
  private static bool EffectArea_GetBurningAreaPointPlus025(Vector3 p,
    ref EffectArea __result)
  {
    return GetPrefix_EffectWithinVehicleArea(p, ref __result);
  }

  private static bool GetPrefix_EffectWithinVehicleArea(Vector3 p,
    ref EffectArea __result)
  {
    if (!VehiclePiecesController.IsPointWithin(p, out var piecesController))
    {
      return true;
    }

    if (piecesController == null) return true;

    // we cannot be certain of the ZDOID.
    var matchingInstance =
      piecesController.cachedVehicleBurningEffectAreas.Values.FirstOrDefault(
        x => x.m_collider.bounds.Contains(p));

    if (matchingInstance == null)
    {
      return true;
    }

    __result = matchingInstance;

    return false;
  }
}