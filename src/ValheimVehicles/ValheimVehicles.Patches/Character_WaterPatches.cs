using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.LayerUtils;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Patches;

public class Character_WaterPatches
{
  // Allows player model to be inside bound collider
  internal static float PlayerOffset = 0f;
  private static WaterVolume? _previousWaterVolume = null;

  // Patches

  [HarmonyPatch(typeof(Character), nameof(Character.InWater))]
  [HarmonyPostfix]
  public static void InWater(Character __instance, bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    SetIsUnderWaterInVehicle(__instance, ref __result);
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

    var data = VehicleOnboardController.GetOnboardCharacterData(__instance);
    if (data?.OnboardController is null) return true;
    if (__instance.IsTeleporting() ||
        (UnityEngine.Object)__instance.GetStandingOnShip() !=
        (UnityEngine.Object)null || __instance.IsAttachedToShip())
      __instance.m_cashedInLiquidDepth = 0.0f;

    // this might be required to avoid the tar bug
    __instance.m_cashedInLiquidDepth = 0.0f;

    var liquidDepth =
      GetLiquidDepthFromBounds(data.OnboardController, __instance);
    UpdateLiquidDepthValues(__instance, liquidDepth);
    return false;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquid))]
  [HarmonyPrefix]
  public static void Character_InLiquid(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    SetIsUnderWaterInVehicle(__instance, ref __result);
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
    IsInLiquidSwimDepth(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth),
    typeof(float))]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth2(Character __instance,
    ref bool __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    IsInLiquidSwimDepth(__instance, ref __result);
  }

  public static void IsInLiquidSwimDepth(Character character, ref bool result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    if (!WaterConfig.IsAllowedUnderwater(character)) return;
    if (WaterZoneController.IsCharacterInWaterFreeZone(character))
    {
      result = false;
      character.m_swimTimer = 999f;
    }
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

    var success = UpdateLiquidDepth(__instance, level, type);
    // needs to return false since we are handling this.
    var handled = !success;
    return handled;
  }

  // helpers

  // the vehicle onboard collider controls the water level for players. So they can go below sea level
  public static void UpdateLiquidDepthValues(Character character,
    float liquidLevel, LiquidType liquidType = LiquidType.Water,
    bool cacheBust = false)
  {
    switch (liquidType)
    {
      case LiquidType.Water:
        character.m_waterLevel = liquidLevel;
        if (character.m_tarLevel > character.m_waterLevel &&
            WaterZoneController.IsCharacterInWaterFreeZone(character))
        {
          character.m_tarLevel = -10000f;
        }

        break;
      case LiquidType.Tar:
        break;
      case LiquidType.All:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(liquidType), liquidType,
          null);
    }

    character.m_liquidLevel = Mathf.Max(
      WaterConfig.UnderwaterMaxDiveDepth.Value,
      liquidLevel);

    if (cacheBust)
    {
      character.m_cashedInLiquidDepth = 0f;
    }

    GameCameraPatch.RequestUpdate();
  }


  public static bool UpdateWaterDepth(Character character)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return false;

    var waterHeight = Floating.GetWaterLevel(character.transform.position,
      ref _previousWaterVolume);
    return UpdateLiquidDepth(character, waterHeight, LiquidType.Water);
  }

  private static float GetDepthFromOnboardCollider(Character character,
    float currentDepth, Collider onboardCollider)
  {
    var maxDistance =
      Mathf.Clamp(character.transform.position.y, 10,
        100); // You can set this to a specific value if needed
    var results = new RaycastHit[5];
    var size = Physics.RaycastNonAlloc(character.transform.position,
      Vector3.down, results, maxDistance, LayerMask.GetMask("piece"));


    var isValid = false;
    // Perform the raycast
    if (size > 0)
    {
      foreach (var raycastHit in results)
      {
        var piecesController = raycastHit.transform.root
          .GetComponent<VehiclePiecesController>();
        if (piecesController)
        {
          isValid = true;
          break;
        }
      }
    }

    // may have to check the difference between current depth and onboard collider.
    if (isValid)
    {
      return onboardCollider.transform.position.y -
        onboardCollider.bounds.extents.y + PlayerOffset;
    }

    return currentDepth;
  }

  public static bool UpdateLiquidDepth(Character character,
    float level,
    LiquidType type = LiquidType.Water)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Everywhere)
    {
      // we do not need to set liquid level to anything besides 0
      // 2 is swim level, so higher allows for swimming in theory
      // todo confirm this works
      UpdateLiquidDepthValues(character, type == LiquidType.Water ? 3f : level,
        type);
      return true;
    }

    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.DEBUG_WaterZoneOnly)
    {
      var isInWaterFreeZone =
        WaterZoneController.IsCharacterInWaterFreeZone(character);
      if (!isInWaterFreeZone) return false;
      // setting to 0 depth for water free zone. Might be inaccurate for a partial underwater base...will fix in future
      UpdateLiquidDepthValues(character, 0f, type);
      return true;
    }

    if (WaterConfig.UnderwaterAccessMode.Value !=
        WaterConfig.UnderwaterAccessModeType.OnboardOnly)
      return false;

    float liquidDepth = 0;
    var isOnVehicle =
      VehicleOnboardController.GetCharacterVehicleMovementController(
        character.GetZDOID(), out var controller);

    // these apparently need to be manually set
    if (!isOnVehicle || controller?.OnboardCollider?.bounds == null)
    {
      // var waterHeight = Floating.GetWaterLevel(character.transform.position,
      //   ref _previousWaterVolume);

      UpdateLiquidDepthValues(character, level);
      return true;
    }

    if (WaterConfig.DEBUG_HasLiquidDepthOverride.Value)
    {
      liquidDepth = WaterConfig.DEBUG_LiquidDepthOverride.Value;
    }
    else
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.OnboardOnly)
      {
        liquidDepth =
          GetDepthFromOnboardCollider(character, level,
            controller.OnboardCollider);
      }
    }

    // return false here as a safety measure and reset caches
    if (Mathf.Approximately(liquidDepth, 0f))
    {
      var waterHeight = Floating.GetWaterLevel(character.transform.position,
        ref _previousWaterVolume);
      UpdateLiquidDepthValues(character, waterHeight);

      return true;
    }

    UpdateLiquidDepthValues(character, liquidDepth);
    return true;
  }

  /// <summary>
  /// Collider is centered by world position. We need to subtract the lowest position to get the value of the lowest point for water.
  /// </summary>
  /// <param name="controller"></param>
  public static float GetLiquidDepthFromBounds(
    VehicleOnboardController controller, Character character)
  {
    if (WaterConfig.DEBUG_HasLiquidDepthOverride.Value)
    {
      return WaterConfig.DEBUG_LiquidDepthOverride.Value;
    }

    if (controller.OnboardCollider?.bounds == null)
      return character.m_cashedInLiquidDepth;

    var waterHeight = Floating.GetWaterLevel(character.transform.position,
      ref _previousWaterVolume);

    return GetDepthFromOnboardCollider(character, waterHeight,
      controller.OnboardCollider);
  }

  // ignores other character types, in future might be worth checking for other types too.
  private static void SetIsUnderWaterInVehicle(Character characterInstance,
    ref bool result)
  {
    // do nothing if false already
    if (result == false) return;
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    if (!WaterConfig.IsAllowedUnderwater(characterInstance)) return;
    var vpcController = characterInstance.transform.root
      .GetComponent<VehiclePiecesController>();
    if (!(bool)vpcController) return;

    result = false;
    VehicleOnboardController.UpdateUnderwaterState(
      characterInstance, true);
  }
}