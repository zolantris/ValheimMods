using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ValheimVehicles.Attributes;
using ValheimVehicles.Config;
using ValheimVehicles.Patches;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
using Zolantris.Shared;

namespace ValheimVehicles.Helpers;

public static class WaterZoneUtils
{
  // Allows player model to be inside bound collider
  internal static float PlayerOffset = 0f;

  // helpers
  public static void IsInLiquidSwimDepth(Character character, ref bool result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    if (!WaterZoneUtils.IsAllowedUnderwater(character)) return;
    if (WaterZoneController.IsCharacterInWaterFreeZone(character))
    {
      result = false;
      character.m_swimTimer = 999f;
    }
  }

  private static readonly Regex _waterZoneRegexDefault =
    new(@"^Player\(Clone\)$");

  private static Regex _underwaterAllowList = _waterZoneRegexDefault;

  public static void UpdateAllowList(List<string> allowList)
  {
    if (allowList.Count == 0)
    {
      _underwaterAllowList = _waterZoneRegexDefault;
    }

    var pattern =
      string.Join("|",
        allowList.Select(Regex.Escape)); // Escape strings for regex
    _underwaterAllowList = new Regex($"^(?:{pattern})$", RegexOptions.Compiled);
  }

  public static bool IsAllowedUnderwater(Character character)
  {
    if (character == null || character.gameObject == null) return false;
    if (character.IsPlayer())
    {
      return true;
    }

    if (WaterConfig.AllowTamedEntiesUnderwater.Value && character.IsTamed())
    {
      return true;
    }

    return _underwaterAllowList.IsMatch(character.gameObject.name);
  }

  /// <summary>
  /// This is for any character. Meaning animals/enemies etc. Should not be used for player floatation or displacement checks directly. Please refer to IsOnboard which is a more extensive check for character.IsPlayer types.
  /// </summary>
  /// <param name="character"></param>
  /// <returns></returns>
  private static bool IsInVehicleShipBounds(Character character)
  {
    var piecesController =
      character.transform.root.GetComponent<VehiclePiecesController>();
    return piecesController != null;
  }

  /// <summary>
  /// todo integrate the CacheController to optimized caching per character check so these calls do not need to be done per frame or even fixedUpdate
  /// </summary>
  /// <param name="character"></param>
  /// <param name="waterZoneData"></param>
  /// <returns></returns>
  [GameCacheValue(name: "IsOnboard", intervalInSeconds: 1f)]
  public static bool IsOnboard(Character character,
    out WaterZoneCharacterData? waterZoneData)
  {
    waterZoneData = null;
    var isAllowedUnderwater = IsAllowedUnderwater(character);
    if (!isAllowedUnderwater)
    {
      return IsInVehicleShipBounds(character);
    }

    waterZoneData =
      VehicleOnboardController.GetOnboardCharacterData(character);
    var isCharacterOnboard = waterZoneData != null;

    // if (waterZoneData == null)
    // {
    //   return IsInVehicleShipBounds(character);
    // }

    var isCharacterStandingOnVehicle =
      HasShipUnderneath(character);
    return isCharacterOnboard && isCharacterStandingOnVehicle;
  }


  /// <summary>
  /// todo integrate the CacheController to optimized caching per character check so these calls do not need to be done per frame or even fixedUpdate
  /// </summary>
  /// <param name="character"></param>
  /// <returns></returns>
  [GameCacheValue(name: "IsOnboard", intervalInSeconds: 1f)]
  public static bool IsOnboard(Character character)
  {
    return IsOnboard(character, out _);
  }

  /// <summary>
  /// Used to evaluate if character is within a covered hull area. This is cached heavily.
  /// </summary>
  /// <param name="character"></param>
  /// <returns></returns>
  [GameCacheValue(intervalInSeconds: 2f)]
  public static bool IsWithinHull(Character character)
  {
    var hasPieceBelowHull =
      HasShipUnderneath(character);
    var hasPieceToLeft =
      HasShipToLeft(character);
    var hasPieceToRight =
      HasShipToRight(character);
    return hasPieceBelowHull && hasPieceToLeft && hasPieceToRight;
  }

  [GameCacheValue(intervalInSeconds: 0.5f)]
  public static bool HasShipToRight(Character character)
  {
    return HasShipInDirection(character.transform.position,
      character.transform.TransformDirection(Vector3.right));
  }

  [GameCacheValue(intervalInSeconds: 0.5f)]
  public static bool HasShipToLeft(Character character)
  {
    return HasShipInDirection(character.transform.position,
      character.transform.TransformDirection(Vector3.left));
  }

  [GameCacheValue(intervalInSeconds: 0.5f)]
  public static bool HasShipUnderneath(Character character)
  {
    return HasShipInDirection(character.transform.position,
      character.transform.TransformDirection(Vector3.down));
  }

  public static bool IsCharacterTheLocalPlayer(Character character)
  {
    return character.GetZDOID() == Player.m_localPlayer.GetZDOID();
  }

  /// <summary>
  /// cannot cache this as it need to be dynamic
  /// </summary>
  /// <param name="position"></param>
  /// <param name="direction"></param>
  /// <returns></returns>
  [MeasureTime]
  public static bool HasShipInDirection(Vector3 position, Vector3 direction)
  {
    var maxDistance =
      Mathf.Clamp(position.y, 10, 100); // Adjust as needed
    var results = new RaycastHit[5];
    var size = Physics.RaycastNonAlloc(position,
      direction, results, maxDistance, LayerMask.GetMask("piece"));

    var isValid = false;
    // Perform the raycast
    if (size > 0)
    {
      for (var i = 0; i < size; i++)
      {
        var resultItem = results[i];
        var piecesController = resultItem.transform.root
          .GetComponent<VehiclePiecesController>();
        if (piecesController)
        {
          isValid = true;
          break;
        }
      }
    }

    return isValid;
  }

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
      WaterConfig.DEBUG_UnderwaterMaxDiveDepth.Value,
      liquidLevel);

    if (cacheBust)
    {
      character.m_cashedInLiquidDepth = 0f;
    }

    if (IsCharacterTheLocalPlayer(character))
    {
      WaterVolumePatch.UpdateCameraState();
    }
  }

  [MeasureTime]
  public static bool UpdateWaterDepth(Character character)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return false;

    var waterHeight = GetWaterHeightFromWaterVolume(character);
    return UpdateLiquidDepth(character, waterHeight, LiquidType.Water);
  }


  private static float _currentDepth; // Current smoothed depth
  private static float _smoothDepthVelocity; // Velocity for SmoothDamp

  private static float GetLowestDepthFromVehicle(Character character,
    float currentDepth, VehicleOnboardController onboardController)
  {
    var isValid = HasShipUnderneath(character);

    // Check the difference between current depth and onboard collider.
    if (isValid)
    {
      // In theory this should work. But it does not since the collider does not align.
      var boundsInWater =
        Mathf.Min(
          onboardController?.PiecesController?.LowestPieceHeight ?? 0f,
          character.transform.position.y);
      return Mathf.Clamp(boundsInWater, 2f, currentDepth);
    }

    return currentDepth;
  }

  public static float RoundToNearestMultipleOfThree(float value)
  {
    return Mathf.Round(value / 3f) * 3f;
  }

  /// <summary>
  /// Likely not needed, but it's a way to prevent abrupt transitions.
  /// </summary>
  /// <param name="targetDepth"></param>
  /// <param name="smoothTime"></param>
  /// <returns></returns>
  public static float SmoothDepthUpdate(float targetDepth, float smoothTime)
  {
    // Smooth the transition to the target depth over time
    _currentDepth = Mathf.SmoothDamp(_currentDepth, targetDepth,
      ref _smoothDepthVelocity, smoothTime);
    return _currentDepth;
  }

  // private static float GetDepthFromOnboardCollider(Character character,
  //   float currentDepth, Collider onboardCollider)
  // {
  //   var maxDistance =
  //     Mathf.Clamp(character.transform.position.y, 10,
  //       100); // You can set this to a specific value if needed
  //   var results = new RaycastHit[5];
  //   var size = Physics.RaycastNonAlloc(character.transform.position,
  //     Vector3.down, results, maxDistance, LayerMask.GetMask("piece"));
  //
  //
  //   var isValid = false;
  //   // Perform the raycast
  //   if (size > 0)
  //   {
  //     for (var i = 0; i < size; i++)
  //     {
  //       var resultItem = results[i];
  //       var piecesController = resultItem.transform.root
  //         .GetComponent<VehiclePiecesController>();
  //       if (piecesController)
  //       {
  //         isValid = true;
  //         break;
  //       }
  //     }
  //   }
  //
  //   // may have to check the difference between current depth and onboard collider.
  //   if (isValid)
  //   {
  //     var boundsInWater = Mathf.Min(onboardCollider.transform.position.y -
  //                                   onboardCollider.bounds.extents.y,
  //       currentDepth);
  //     return Mathf.Clamp(onboardCollider.bounds.min.y, 2f, currentDepth);
  //   }
  //
  //   return currentDepth;
  // }

  public static bool UpdateLiquidDepth(Character character,
    float level,
    LiquidType type = LiquidType.Water)
  {
    if (WaterConfig.IsDisabled) return false;
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
          GetLowestDepthFromVehicle(character, level,
            controller.VehicleInstance);
      }
    }

    // TODO determine if need to update the water or if should leave alone in this block
    if (Mathf.Approximately(liquidDepth, 0f))
    {
      return true;
    }

    UpdateLiquidDepthValues(character, liquidDepth);
    return true;
  }

  public static Dictionary<ZDOID, WaterVolume> CharacterWaterVolumeRefs = [];

  private static float GetWaterHeightFromWaterVolume(Character character)
  {
    if (!CharacterWaterVolumeRefs.TryGetValue(character.GetZDOID(),
          out var waterVolume))
    {
      waterVolume = null;
    }

    var height = Floating.GetWaterLevel(character.transform.position,
      ref waterVolume);
    CharacterWaterVolumeRefs[character.GetZDOID()] = waterVolume;
    return height;
  }

  /// <summary>
  /// Collider is centered by world position. We need to subtract the lowest position to get the value of the lowest point for water.
  /// </summary>
  /// <param name="controller"></param>
  [MeasureTime]
  public static float GetLiquidDepthFromBounds(
    VehicleOnboardController? controller, Character character)
  {
    if (WaterConfig.DEBUG_HasLiquidDepthOverride.Value)
    {
      return WaterConfig.DEBUG_LiquidDepthOverride.Value;
    }

    var waterHeight = GetWaterHeightFromWaterVolume(character);
    if (controller?.OnboardCollider?.bounds == null)
      return waterHeight;

    return GetLowestDepthFromVehicle(character, waterHeight,
      controller.OnboardCollider);
  }

  // ignores other character types, in future might be worth checking for other types too.
  public static void SetIsUnderWaterInVehicle(Character characterInstance,
    ref bool result)
  {
    if (!result || WaterConfig.IsDisabled) return;
    if (!WaterZoneUtils.IsAllowedUnderwater(characterInstance)) return;

    var piecesController = characterInstance.transform.root
      .GetComponent<VehiclePiecesController>();
    if (!(bool)piecesController) return;

    result = false;
    VehicleOnboardController.UpdateUnderwaterState(
      characterInstance, true);
  }
}