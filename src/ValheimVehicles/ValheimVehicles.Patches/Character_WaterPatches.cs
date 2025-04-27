using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using Zolantris.Shared;
using Logger = Jotunn.Logger;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Patches;

/// <summary>
/// Just WaterPatches, please reference WaterZoneHelpers for most of the logic
/// </summary>
public class Character_WaterPatches
{
  [HarmonyPatch(typeof(Character), nameof(Character.InWater))]
  [HarmonyPostfix]
  public static void InWater(Character __instance, bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneUtils.SetIsUnderWaterInVehicle(__instance, ref __result);
  }


  [HarmonyPatch(typeof(Character), nameof(Character.UpdateWater))]
  [HarmonyPostfix]
  public static void Character_RemoveThatTar(Character __instance)
  {
    if (WaterZoneUtils.IsAllowedUnderwater(__instance) &&
        VehicleOnboardController.IsCharacterOnboard(__instance))
    {
      if (__instance.m_tarEffects.HasEffects())
      {
        var tared =
          __instance.m_seman.GetStatusEffect(SEMan.s_statusEffectTared);
        if (tared != null)
        {
          __instance.m_seman.RemoveStatusEffect(SEMan.s_statusEffectTared);
        }
      }
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.CalculateLiquidDepth))]
  [HarmonyPrefix]
  public static bool Character_CalculateLiquidDepth(Character __instance)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return true;
    if (!WaterZoneUtils.IsAllowedUnderwater(__instance)) return true;
    WaterZoneUtils.UpdateDepthValues(__instance, LiquidType.Water);
    return false;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquid))]
  [HarmonyPrefix]
  public static void Character_InLiquid(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneUtils.SetIsUnderWaterInVehicle(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InTar))]
  [HarmonyPrefix]
  public static void Character_InTar(Character __instance, ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    if (VehicleOnboardController.IsCharacterOnboard(__instance))
    {
      // __instance.m_tarLevel = -10000f;
      __result = false;
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth), [])]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth1(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneUtils.IsInLiquidSwimDepth(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth),
    typeof(float))]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth2(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneUtils.IsInLiquidSwimDepth(__instance, ref __result);
  }

  /// <summary>
  /// Todo possibly patch this directly so it does not apply unless the flag is enabled. 
  /// </summary>
  /// <param name="__instance"></param>
  /// <returns></returns>
  [HarmonyPatch(typeof(Character), nameof(Character.SetLiquidLevel))]
  [HarmonyPrefix]
  public static bool Character_SetLiquidLevel(Character __instance, float level,
    LiquidType type, Component liquidObj)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return true;
    if (type == LiquidType.Tar) return true;
    if (!WaterZoneUtils.IsAllowedUnderwater(__instance)) return true;
    if (__instance == null) return true;

    var wasHandled =
      WaterZoneUtils.UpdateDepthFromMode(__instance, level, type);
    return !wasHandled;
  }
}