using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Patches;

public class Character_WaterPatches
{
  // todo this might need to be a dictionary to prevent issues with static values if Character applies to other entities.
  // public static bool IsUnderWaterInVehicle = false;
  // public static bool IsSwimming = false;
  // public static bool IsUnderwaterInVehicle = false;

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

  // todo this is inaccurate, it needs to match the player character.
  // [HarmonyPatch(typeof(Character), nameof(Character.IsSwimming))]
  // [HarmonyPostfix]
  // public static void IsSwimmingListener(bool __result)
  // {
  //   if (IsSwimming 
  //   IsSwimming = __result;
  // }

  /// <summary>
  /// Todo possibly patch this directly so it does not apply unless the flag is enabled. 
  /// </summary>
  /// <param name="__instance"></param>
  /// <returns></returns>
  [HarmonyPatch(typeof(Character), nameof(Character.SetLiquidLevel))]
  [HarmonyPrefix]
  public static bool Character_SetLiquidLevel(Character __instance)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return false;
    if (__instance.gameObject.name != "Player(Clone)") return false;
    if (VehicleOnboardController.GetCharacterVehicleMovementController(
          __instance.GetZDOID(), out var controller))
    {
      var liquidDepth = VehicleDebugConfig.HasBoatLiquidDepthOverride.Value
        ? VehicleDebugConfig.VehicleLiquidDepthOverride.Value
        : controller.onboardCollider.bounds.min.y;
      __instance.m_liquidLevel = liquidDepth;
      __instance.m_waterLevel = liquidDepth;
      // the vehicle onboard collider controls the water level for players. So they can go below sea level
      __instance.m_cashedInLiquidDepth = liquidDepth;

      // game calls this.
      // this.m_liquidLevel = Mathf.Max(this.m_waterLevel, this.m_tarLevel);

      return true;
    }

    return false;
  }

  /// <summary>
  /// Collider is centered by world position. We need to subtract the lowest position to get the value of the lowest point for water.
  /// </summary>
  /// <param name="controller"></param>
  public static float GetLiquidDepthFromBounds(
    VehicleOnboardController controller)
  {
    if (VehicleDebugConfig.HasBoatLiquidDepthOverride.Value)
    {
      return VehicleDebugConfig.VehicleLiquidDepthOverride.Value;
    }

    var extentsY = controller.onboardCollider.bounds.extents.y;
    var oboardColliderPositionY =
      controller.onboardCollider.transform.position.y;

    return oboardColliderPositionY - extentsY;
  }

  [HarmonyPatch(typeof(Character), nameof(Character.CalculateLiquidDepth))]
  [HarmonyPrefix]
  public static bool Character_CalculateLiquidDepth(Character __instance)
  {
    var data = VehicleOnboardController.GetOnboardCharacterData(__instance);
    if (data is null) return false;
    var liquidDepth = GetLiquidDepthFromBounds(data.controller);
    __instance.m_cashedInLiquidDepth = liquidDepth;
    __instance.m_liquidLevel = liquidDepth;
    return true;
  }

  // ignores other character types, in future might be worth checking for other types too.
  private static void SetIsUnderWaterInVehicle(Character characterInstance,
    ref bool result)
  {
    if (!WaterConfig.IsAllowedUnderwater(characterInstance)) return;
    var vpcController = characterInstance.transform.root
      .GetComponent<VehiclePiecesController>();
    if (!(bool)vpcController) return;

    result = false;
    VehicleOnboardController.UpdateUnderwaterState(
      characterInstance, true);
  }
}