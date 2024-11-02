using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.LayerUtils;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
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
    WaterZoneHelpers.SetIsUnderWaterInVehicle(__instance, ref __result);
  }

  /// <summary>
  /// Could be mostly a postfix. Can remove the tar effect if it appears this way.
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="dt"></param>
  /// <returns></returns>
  // [HarmonyPatch(typeof(Character), nameof(Character.UpdateWater))]
  // [HarmonyPrefix]
  // public static bool Character_UpdateWater(Character __instance, float dt)
  // {
  //   if (WaterConfig.UnderwaterAccessMode.Value ==
  //       WaterConfig.UnderwaterAccessModeType.Disabled) return true;
  //
  //   __instance.m_swimTimer += dt;
  //   float depth = __instance.InLiquidDepth();
  //   if (__instance.m_canSwim && __instance.InLiquidSwimDepth(depth))
  //     __instance.m_swimTimer = 0.0f;
  //   if (!__instance.m_nview.IsOwner() || !__instance.InLiquidWetDepth(depth))
  //     return false;
  //
  //   if ((double)__instance.m_waterLevel > (double)__instance.m_tarLevel)
  //   {
  //     __instance.m_seman.AddStatusEffect(SEMan.s_statusEffectWet, true);
  //     return false;
  //   }
  //   else
  //   {
  //     if ((double)__instance.m_tarLevel <= (double)__instance.m_waterLevel ||
  //         __instance.m_tolerateTar)
  //       return false;
  //     __instance.m_seman.AddStatusEffect(SEMan.s_statusEffectTared, true);
  //   }
  //
  //   return false;
  // }
  [HarmonyPatch(typeof(Character), nameof(Character.UpdateWater))]
  [HarmonyPostfix]
  public static void Character_RemoveThatTar(Character __instance)
  {
    if (WaterConfig.IsAllowedUnderwater(__instance) &&
        VehicleOnboardController.IsCharacterOnboard(__instance))
    {
      if (__instance.m_tarEffects.HasEffects())
      {
        __instance.m_seman.GetStatusEffect(SEMan.s_statusEffectTared);
      }
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.CalculateLiquidDepth))]
  [HarmonyPrefix]
  public static bool Character_CalculateLiquidDepth(Character __instance)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return true;
    if (!WaterConfig.IsAllowedUnderwater(__instance)) return true;

    var data = VehicleOnboardController.GetOnboardCharacterData(__instance);
    // if (data?.OnboardController is null) ;
    if (__instance.IsTeleporting() ||
        (UnityEngine.Object)__instance.GetStandingOnShip() !=
        (UnityEngine.Object)null || __instance.IsAttachedToShip())
      __instance.m_cashedInLiquidDepth = 0.0f;
    // this might be required to avoid the tar bug
    __instance.m_cashedInLiquidDepth = 0.0f;

    var liquidDepth =
      WaterZoneHelpers.GetLiquidDepthFromBounds(data?.OnboardController,
        __instance);
    WaterZoneHelpers.UpdateLiquidDepthValues(__instance, liquidDepth);
    return false;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquid))]
  [HarmonyPrefix]
  public static void Character_InLiquid(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneHelpers.SetIsUnderWaterInVehicle(__instance, ref __result);
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
    WaterZoneHelpers.IsInLiquidSwimDepth(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth),
    typeof(float))]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth2(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    WaterZoneHelpers.IsInLiquidSwimDepth(__instance, ref __result);
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
    if (!WaterConfig.IsAllowedUnderwater(__instance)) return true;
    if (__instance == null) return true;
    if (!VehicleOnboardController.IsCharacterOnboard(__instance))
    {
      return true;
    }

    var success = WaterZoneHelpers.UpdateLiquidDepth(__instance, level, type);
    // needs to return false since we are handling this.
    var handled = !success;
    return handled;
  }
}