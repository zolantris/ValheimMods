using System;
using System.Dynamic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

public class WaterVolume_WaterPatches
{
  public enum CameraWaterStateTypes
  {
    AboveWater,
    BelowWater,
    ToBelow,
    ToAbove
  }

  public static float WaterLevelCamera = -10000f;

  public static CameraWaterStateTypes CameraWaterState =
    CameraWaterStateTypes.AboveWater;

  public static bool IsCameraAboveWater =>
    CameraWaterState is CameraWaterStateTypes.AboveWater
      or CameraWaterStateTypes.ToAbove;

  public static bool IsCameraBelowWater =>
    CameraWaterState is CameraWaterStateTypes.BelowWater
      or CameraWaterStateTypes.ToBelow;

  public static bool IsFlipped()
  {
    return WaterConfig.FlipWatermeshMode.Value switch
    {
      WaterConfig.WaterMeshFlipModeType.Disabled => false,
      WaterConfig.WaterMeshFlipModeType.Everywhere => IsCameraBelowWater,
      WaterConfig.WaterMeshFlipModeType.ExcludeOnboard =>
        IsCameraBelowWater &&
        !VehicleOnboardController.IsCharacterOnboard(Player.m_localPlayer),
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  private static int DepthProperty = Shader.PropertyToID("_depth");

  private static int GlobalWindProperty =
    Shader.PropertyToID("_UseGlobalWind");

  [HarmonyPatch(typeof(WaterVolume), "UpdateMaterials")]
  [HarmonyPrefix]
  public static void WaterVolumeUpdatePatchWaterVolume(WaterVolume __instance,
    ref float[] ___m_normalizedDepth)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    UpdateWaterLevel(__instance);
    AdjustWaterSurface(__instance, ___m_normalizedDepth);
  }

  private static void UpdateWaterLevel(WaterVolume __instance)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;
    if (GameCamera.instance)
    {
      var currentSurfaceLevel =
        __instance.GetWaterSurface(GameCamera.instance.transform.position);
      WaterLevelCamera = currentSurfaceLevel > ZoneSystem.instance.m_waterLevel
        ? ZoneSystem.instance.m_waterLevel
        : currentSurfaceLevel;
    }
  }

#if DEBUG
  [HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.CreateWave))]
  [HarmonyPostfix]
  private static void CreateWave(WaterVolume __instance, float __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    __result *= WaterConfig.DEBUG_WaveSizeMultiplier.Value;
  }
#endif

  private static float lastFlippedAllInvoke = 0f;

  private static CameraWaterStateTypes lastFlippedAllState =
    CameraWaterState = CameraWaterStateTypes.AboveWater;

  /// <summary>
  /// Updates all meshes to prevent them from getting out of sync
  /// </summary>
  public static void FlipAllWaterVolumes()
  {
    if (lastFlippedAllInvoke > 0.2f) lastFlippedAllInvoke = 0f;

    if (lastFlippedAllInvoke > 0f)
    {
      lastFlippedAllInvoke += Time.fixedTime;
      return;
    }

    if (lastFlippedAllState == CameraWaterState) return;


    foreach (var waterVolume in WaterVolume.Instances)
      UpdateMesh(waterVolume, waterVolume.m_normalizedDepth);

    lastFlippedAllInvoke += Time.fixedTime;
    lastFlippedAllState = CameraWaterState;
  }


  public static void UpdateCameraState()
  {
    if (GameCamera.instance == null) return;

    var isAbove = GameCamera.instance.transform.position.y > WaterLevelCamera;
    if (isAbove)
    {
      switch (CameraWaterState)
      {
        case CameraWaterStateTypes.AboveWater:
          break;
        case CameraWaterStateTypes.ToAbove:
          CameraWaterState = CameraWaterStateTypes.AboveWater;
          FlipAllWaterVolumes();
          break;
        case CameraWaterStateTypes.BelowWater:
        case CameraWaterStateTypes.ToBelow:
          CameraWaterState = CameraWaterStateTypes.ToAbove;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      return;
    }

    switch (CameraWaterState)
    {
      case CameraWaterStateTypes.BelowWater:
        return;
      case CameraWaterStateTypes.ToBelow:
        CameraWaterState = CameraWaterStateTypes.BelowWater;
        FlipAllWaterVolumes();
        return;
      case CameraWaterStateTypes.AboveWater:
      case CameraWaterStateTypes.ToAbove:
        CameraWaterState = CameraWaterStateTypes.ToBelow;
        return;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public static bool IsCurrentCameraAboveWater =>
    GameCamera_WaterPatches.CameraPositionY > WaterLevelCamera;

  public static bool CanFlipOnlyOffboard =>
    WaterConfig.FlipWatermeshMode.Value ==
    WaterConfig.WaterMeshFlipModeType
      .ExcludeOnboard;

  public static bool CanFlipEverywhere =>
    WaterConfig.FlipWatermeshMode.Value ==
    WaterConfig.WaterMeshFlipModeType.Everywhere;

  private static void AdjustWaterSurface(WaterVolume __instance,
    float[] normalizedDepth)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled ||
        WaterConfig.FlipWatermeshMode.Value ==
        WaterConfig.WaterMeshFlipModeType.Disabled) return;

    UpdateCameraState();
    UpdateMesh(__instance, normalizedDepth);
  }

  private static void UpdateMesh(WaterVolume __instance,
    float[] normalizedDepth)
  {
    var waterSurfaceTransform = __instance.m_waterSurface.transform;
    var isCurrentlyFlipped =
      waterSurfaceTransform.rotation.eulerAngles.y.Equals(180f);

    // will flip the surface if camera is below it.
    if ((IsCameraAboveWater && isCurrentlyFlipped) ||
        (IsCameraBelowWater && !isCurrentlyFlipped))
    {
      if (!isCurrentlyFlipped && (CanFlipEverywhere || (CanFlipOnlyOffboard &&
            !VehicleOnboardController.IsCharacterOnboard(
              Player.m_localPlayer))))
      {
        FlipWaterSurface(__instance, normalizedDepth);
        SetWaterSurfacePosition(waterSurfaceTransform, WaterLevelCamera);
      }

      if (isCurrentlyFlipped)
      {
        UnflipWaterSurface(__instance, normalizedDepth);
        SetWaterSurfacePosition(waterSurfaceTransform,
          WaterLevelCamera);
      }
    }
  }

  private static void SetWaterSurfacePosition(Transform transform,
    float height)
  {
    var position = transform.position;
    position.y = height;
    transform.position = position;
  }

  private static void FlipWaterSurface(WaterVolume __instance,
    float[] normalizedDepth)
  {
    __instance.m_waterSurface.transform.Rotate(180f, 0f, 0f);
    __instance.m_waterSurface.shadowCastingMode = ShadowCastingMode.TwoSided;

    SetDepthForWaterSurface(__instance, normalizedDepth, true);
  }

  private static void UnflipWaterSurface(WaterVolume __instance,
    float[] normalizedDepth)
  {
    __instance.m_waterSurface.transform.Rotate(-180f, 0f, 0f);
    SetDepthForWaterSurface(__instance, normalizedDepth, false);
  }

  private static void SetDepthForWaterSurface(WaterVolume __instance,
    float[] normalizedDepth, bool isFlipped)
  {
    var depthValues = isFlipped
      ?
      [
        -normalizedDepth[0],
        -normalizedDepth[1],
        -normalizedDepth[2],
        -normalizedDepth[3]
      ]
      : normalizedDepth;

    __instance.m_waterSurface.material.SetFloatArray(DepthProperty
      , depthValues);

    // likely not necesssary
    __instance.m_waterSurface.material.SetFloat(
      GlobalWindProperty,
      __instance.m_useGlobalWind ? 1f : 0f);
  }
}