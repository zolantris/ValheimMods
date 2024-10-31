using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Config;

namespace ValheimVehicles.Patches
{
  [HarmonyPatch]
  internal class WaterVolumePatch
  {
    public enum CameraWaterStateTypes
    {
      AboveWater,
      BelowWater,
      ToBelow,
      ToAbove,
    }

    public static float WaterLevelCamera = 0f;
    public static CameraWaterStateTypes CameraWaterState;
    public static bool IsFlipped = false;
    private static int DepthProperty = Shader.PropertyToID("_depth");

    private static int GlobalWindProperty =
      Shader.PropertyToID("_UseGlobalWind");

    [HarmonyPatch(typeof(WaterVolume), "TrochSin")]
    [HarmonyPrefix]
    public static bool FlippedTrochSin(float __result, float x, float k)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return false;

      if (IsFlipped)
      {
        __result =
          1.0f - (float)((Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5) + 0.5);
      }

      return IsFlipped;
    }

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

    private static void UpdateCameraState(bool isAbove)
    {
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
            GameCameraPatch.RequestUpdate();
            return;
        }
      }
      else if (!isAbove)
      {
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
            GameCameraPatch.RequestUpdate();
            return;
        }
      }
    }

    public static bool IsCameraAboveWater =>
      CameraWaterState is CameraWaterStateTypes.AboveWater
        or CameraWaterStateTypes.ToAbove;

    private static void AdjustWaterSurface(WaterVolume __instance,
      float[] normalizedDepth)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return;

      var isCurrentCameraAboveWater =
        GameCameraPatch.CameraPositionY > WaterLevelCamera;
      UpdateCameraState(isCurrentCameraAboveWater);

      var waterSurfaceTransform = __instance.m_waterSurface.transform;
      IsFlipped =
        waterSurfaceTransform.rotation.eulerAngles.y.Equals(180f);

      if (!IsCameraAboveWater && !IsFlipped)
      {
        FlipWaterSurface(__instance, normalizedDepth);
        SetWaterSurfacePosition(waterSurfaceTransform, WaterLevelCamera);
      }

      if (IsCameraAboveWater && IsFlipped)
      {
        UnflipWaterSurface(__instance, normalizedDepth);
        SetWaterSurfacePosition(waterSurfaceTransform,
          WaterLevelCamera);
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
          // normalizedDepth[0],
          // normalizedDepth[0],
          // normalizedDepth[0],
          // normalizedDepth[0]
          // normalizedDepth[2],
          // normalizedDepth[0],
          // normalizedDepth[3],
          // normalizedDepth[1],
          // 1 - normalizedDepth[0],
          // 1 - normalizedDepth[1],
          // 1 - normalizedDepth[2],
          // 1 - normalizedDepth[3]
          -normalizedDepth[0],
          -normalizedDepth[1],
          -normalizedDepth[2],
          -normalizedDepth[3]
        ]
        : normalizedDepth;


      __instance.m_waterSurface.material.SetFloatArray(DepthProperty
        , depthValues);
      __instance.m_waterSurface.material.SetFloat(
        GlobalWindProperty,
        __instance.m_useGlobalWind ? 1f : 0f);
    }
  }
}