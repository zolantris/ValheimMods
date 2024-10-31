using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Patches;

public class Character_WaterPatches
{
  [HarmonyPatch(typeof(Character), nameof(Character.InWater))]
  [HarmonyPostfix]
  public static void InWater(Character __instance, bool __result)
  {
    SetIsUnderWaterInVehicle(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquid))]
  [HarmonyPrefix]
  public static void Character_InLiquid(Character __instance,
    ref bool __result)
  {
    SetIsUnderWaterInVehicle(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth), [])]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth1(Character __instance,
    ref bool __result)
  {
    IsInLiquidSwimDepth(__instance, ref __result);
  }

  [HarmonyPatch(typeof(Character), nameof(Character.InLiquidSwimDepth),
    typeof(float))]
  [HarmonyPostfix]
  public static void Character_InLiquidSwimDepth2(Character __instance,
    ref bool __result)
  {
    IsInLiquidSwimDepth(__instance, ref __result);
  }

  public static void IsInLiquidSwimDepth(Character character, ref bool result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    if (!WaterConfig.IsAllowedUnderwater(character)) return;
    if (VehicleOnboardController.IsCharacterOnboard(character))
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
  public static bool Character_SetLiquidLevel(Character __instance,
    LiquidType type, Component liquidObj)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return false;
    if (type == LiquidType.Tar) return false;
    if (!WaterConfig.IsAllowedUnderwater(__instance)) return false;
    if (__instance == null) return false;
    var handled = UpdateLiquidDepth(__instance);
    return handled;
  }

  // the vehicle onboard collider controls the water level for players. So they can go below sea level
  public static void UpdateLiquidDepthValues(Character character,
    float liquidLevel)
  {
    character.m_liquidLevel = Mathf.Max(
      WaterConfig.UnderwaterMaxDiveDepth.Value,
      liquidLevel);
    character.m_waterLevel = Mathf.Max(WaterConfig.UnderwaterMaxDiveDepth.Value,
      liquidLevel);
    character.m_cashedInLiquidDepth =
      Mathf.Max(WaterConfig.UnderwaterMaxCachedDiveDepth.Value, liquidLevel);
    GameCameraPatch.RequestUpdate();
  }

  private static WaterVolume? _previousWaterVolume = null;

  public static bool UpdateLiquidDepth(Character character)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Everywhere)
    {
      // we do not need to set liquid level to anything besides 0
      // 2 is swim level, so higher allows for swimming in theory
      UpdateLiquidDepthValues(character, 3);
      return true;
    }


    if (WaterConfig.UnderwaterAccessMode.Value !=
        WaterConfig.UnderwaterAccessModeType.OnboardOnly) return false;

    float liquidDepth = 0;
    var isOnVehicle =
      VehicleOnboardController.GetCharacterVehicleMovementController(
        character.GetZDOID(), out var controller);

    // these apparently need to be manually set
    if (!isOnVehicle || controller?.OnboardCollider?.bounds == null)
    {
      var waterHeight = Floating.GetWaterLevel(character.transform.position,
        ref _previousWaterVolume);

      UpdateLiquidDepthValues(character, waterHeight);
      return true;
    }

    if (VehicleDebugConfig.HasBoatLiquidDepthOverride.Value)
    {
      liquidDepth = VehicleDebugConfig.VehicleLiquidDepthOverride.Value;
    }
    else
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.OnboardOnly)
      {
        liquidDepth = controller.OnboardCollider.transform.position.y -
                      controller.OnboardCollider.bounds.extents.y;
      }
    }

    if (liquidDepth == 0)
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
    if (VehicleDebugConfig.HasBoatLiquidDepthOverride.Value)
    {
      return VehicleDebugConfig.VehicleLiquidDepthOverride.Value;
    }

    if (controller.OnboardCollider?.bounds == null)
      return character.m_cashedInLiquidDepth;

    var extentsY = controller.OnboardCollider.bounds.extents.y;
    var oboardColliderPositionY =
      controller.OnboardCollider.transform.position.y;

    return oboardColliderPositionY - extentsY;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.CalculateLiquidDepth))]
  [HarmonyPrefix]
  public static bool Character_CalculateLiquidDepth(Character __instance)
  {
    var data = VehicleOnboardController.GetOnboardCharacterData(__instance);
    if (data?.controller is null) return false;
    var liquidDepth = GetLiquidDepthFromBounds(data.controller, __instance);
    UpdateLiquidDepthValues(__instance, liquidDepth);
    return true;
  }

  // ignores other character types, in future might be worth checking for other types too.
  private static void SetIsUnderWaterInVehicle(Character characterInstance,
    ref bool result)
  {
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