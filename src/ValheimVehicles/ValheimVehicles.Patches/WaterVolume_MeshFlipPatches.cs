using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Patches;

internal class WaterVolumePatch
{
  public enum CameraWaterStateTypes
  {
    AboveWater,
    BelowWater,
    ToBelow,
    ToAbove,
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
      WaterLevelCamera =
        __instance.GetWaterSurface(GameCamera.instance.transform.position);
    }
  }

  [HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.CreateWave))]
  [HarmonyPostfix]
  private static void CreateWave(WaterVolume __instance, float __result)
  {
    if (WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled) return;

    __result *= WaterConfig.WaveSizeMultiplier.Value;
  }

  public static void UpdateCameraState()
  {
    var isAbove = GameCamera.instance.transform.position.y > WaterLevelCamera;
    if (isAbove)
    {
      switch (CameraWaterState)
      {
        case CameraWaterStateTypes.AboveWater:
          return;
        case CameraWaterStateTypes.ToAbove:
          CameraWaterState = CameraWaterStateTypes.AboveWater;
          return;
        case CameraWaterStateTypes.BelowWater:
        case CameraWaterStateTypes.ToBelow:
          CameraWaterState = CameraWaterStateTypes.ToAbove;
          GameCameraPatch.RequestUpdate(CameraWaterState);
          return;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    switch (CameraWaterState)
    {
      case CameraWaterStateTypes.BelowWater:
        return;
      case CameraWaterStateTypes.ToBelow:
        CameraWaterState = CameraWaterStateTypes.BelowWater;
        return;
      case CameraWaterStateTypes.AboveWater:
      case CameraWaterStateTypes.ToAbove:
        CameraWaterState = CameraWaterStateTypes.ToBelow;
        GameCameraPatch.RequestUpdate(CameraWaterState);
        return;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public static bool IsCurrentCameraAboveWater =>
    GameCameraPatch.CameraPositionY > WaterLevelCamera;

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

    var waterSurfaceTransform = __instance.m_waterSurface.transform;
    var isCurrentlyFlipped =
      waterSurfaceTransform.rotation.eulerAngles.y.Equals(180f);

    // will flip the surface if camera is below it.
    if (IsCameraAboveWater && isCurrentlyFlipped ||
        IsCameraBelowWater && !isCurrentlyFlipped)
    {
      if (!isCurrentlyFlipped && (CanFlipEverywhere || CanFlipOnlyOffboard &&
            !VehicleOnboardController.IsCharacterOnboard(
              Player.m_localPlayer)))
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
    Vector3 position = transform.position;
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

  // possibly most accurate order
  // normalizedDepth[2],
  // normalizedDepth[0],
  // normalizedDepth[3],
  // normalizedDepth[1],

  private static void SetDepthForWaterSurface(WaterVolume __instance,
    float[] normalizedDepth, bool isFlipped)
  {
    var depthValues = isFlipped
      ?
      [
        //     // normalizedDepth[0],
        //     // normalizedDepth[0],
        //     // normalizedDepth[0],
        //     // normalizedDepth[0]
        //     // normalizedDepth[2],
        //     // normalizedDepth[0],
        //     // normalizedDepth[3],
        //     // normalizedDepth[1],
        //     // 1 - normalizedDepth[0],
        //     // 1 - normalizedDepth[1],
        //     // 1 - normalizedDepth[2],
        //     // 1 - normalizedDepth[3]
        -normalizedDepth[0],
        -normalizedDepth[1],
        -normalizedDepth[2],
        -normalizedDepth[3]
      ]
      : normalizedDepth;

    __instance.m_waterSurface.material.SetFloatArray(DepthProperty
      , depthValues);
    // __instance.m_waterSurface.material.SetFloat(
    //   GlobalWindProperty,
    //   __instance.m_useGlobalWind ? 1f : 0f);
  }
}