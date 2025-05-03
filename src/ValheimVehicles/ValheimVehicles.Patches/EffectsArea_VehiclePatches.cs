using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;


namespace ValheimVehicles.Patches;

/// <summary>
/// Valheim 0.219.1 (bogwitch) patch
///
/// These are fireplace and cooking fixes.
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
    GetPrefix_EffectWithinVehicleArea(p, ref effectArea, out var shouldSkipOriginalMethod);
    __result = effectArea != null;
    return shouldSkipOriginalMethod;
  }

  [HarmonyPatch(typeof(EffectArea),
    nameof(EffectArea.GetBurningAreaPointPlus025))]
  [HarmonyPrefix]
  [HarmonyPriority(Priority.VeryHigh)]
  private static bool EffectArea_GetBurningAreaPointPlus025(Vector3 p,
    ref EffectArea __result)
  {
    GetPrefix_EffectWithinVehicleArea(p, ref __result, out var shouldSkipOriginalMethod);
    return shouldSkipOriginalMethod;
  }

  private static void GetPrefix_EffectWithinVehicleArea(Vector3 p,
    ref EffectArea __result, out bool shouldSkipOriginalMethod)
  {
    shouldSkipOriginalMethod = false;

    if (VehiclePiecesController.IsPointWithinEffectsArea(p, out var matchingEffectsArea) && matchingEffectsArea != null)
    {
      __result = matchingEffectsArea;
      shouldSkipOriginalMethod = true;
      return;
    }

    shouldSkipOriginalMethod = false;
  }
}