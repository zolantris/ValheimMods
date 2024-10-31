using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles.Controllers;

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

    [HarmonyPatch(typeof(WaterVolume), "TrochSin")]
    [HarmonyPrefix]
    public static bool FlippedTrochSin(ref float __result, float x, float k)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return false;

      if (IsFlipped())
      {
        __result =
          1.0f - (float)((Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5) + 0.5);
        return false;
      }

      return true;
    }

    public static bool IsWaveOverridesEnabled =>
      WaterConfig.FlipWatermeshMode.Value !=
      WaterConfig.WaterMeshFlipModeType.Disabled &&
      WaterConfig.UnderwaterAccessMode.Value !=
      WaterConfig.UnderwaterAccessModeType.Disabled;

    // Patch for GetWaterSurface
    [HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.GetWaterSurface))]
    [HarmonyPrefix]
    public static bool WaterVolume_GetWaterSurface(WaterVolume __instance,
      ref float __result, Vector3 point, float waveFactor = 1f)
    {
      if (!IsWaveOverridesEnabled) return true;

      var num = 0.0f;

      if (__instance.m_useGlobalWind)
      {
        var wrappedDayTimeSeconds = WaterVolume.s_wrappedDayTimeSeconds;
        var depth = __instance.Depth(point);
        num = (double)depth == 0.0
          ? 0.0f
          : __instance.CalcWave(point, depth, wrappedDayTimeSeconds,
            waveFactor);
      }

      // Adjust water surface height based on flip
      __result = __instance.transform.position.y + (IsFlipped() ? -num : num) +
                 __instance.m_surfaceOffset;

      // Additional adjustments if necessary
      if ((double)__instance.m_forceDepth < 0.0 &&
          (double)Utils.LengthXZ(point) > 10500.0)
        __result -= 100f;

      return false;
    }

    public static float[] AboveOceanDepth = new float[4];

    // Patch for CalcWave
    [HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.CalcWave),
      new[]
      {
        typeof(Vector3), typeof(float), typeof(Vector4), typeof(float),
        typeof(float)
      })]
    [HarmonyPrefix]
    public static bool PrefixCalcWave(WaterVolume __instance, Vector3 worldPos,
      float depth, Vector4 wind, float waterTime, float waveFactor,
      ref float __result)
    {
      if (!IsFlipped())
      {
        var oceanDepth = __instance.m_heightmap.GetOceanDepth();
        AboveOceanDepth = oceanDepth;
        return true;
      }

      var reversedDepthDirections = new List<Vector2>();

      reversedDepthDirections.Clear();
      foreach (var sCreateWaveDirection in WaterVolume.s_createWaveDirections)
      {
        reversedDepthDirections.Add(new Vector2(sCreateWaveDirection.x,
          -sCreateWaveDirection.y));
      }

      var reversedWaveTangents = new Vector2[10]
      {
        new Vector2(-WaterVolume.s_createWaveDirections[0].y,
          WaterVolume.s_createWaveDirections[0].x),
        new Vector2(-WaterVolume.s_createWaveDirections[1].y,
          WaterVolume.s_createWaveDirections[1].x),
        new Vector2(-WaterVolume.s_createWaveDirections[2].y,
          WaterVolume.s_createWaveDirections[2].x),
        new Vector2(-WaterVolume.s_createWaveDirections[3].y,
          WaterVolume.s_createWaveDirections[3].x),
        new Vector2(-WaterVolume.s_createWaveDirections[4].y,
          WaterVolume.s_createWaveDirections[4].x),
        new Vector2(-WaterVolume.s_createWaveDirections[5].y,
          WaterVolume.s_createWaveDirections[5].x),
        new Vector2(-WaterVolume.s_createWaveDirections[6].y,
          WaterVolume.s_createWaveDirections[6].x),
        new Vector2(-WaterVolume.s_createWaveDirections[7].y,
          WaterVolume.s_createWaveDirections[7].x),
        new Vector2(-WaterVolume.s_createWaveDirections[8].y,
          WaterVolume.s_createWaveDirections[8].x),
        new Vector2(-WaterVolume.s_createWaveDirections[9].y,
          WaterVolume.s_createWaveDirections[9].x)
      };

      reversedDepthDirections[0] =
        new Vector2(wind.x, reversedDepthDirections[0].y);
      reversedDepthDirections[0] =
        new Vector2(reversedDepthDirections[0].x, wind.z);
      reversedDepthDirections[0].Normalize();
      WaterVolume.s_createWaveTangents[0].x =
        -WaterVolume.s_createWaveDirections[0].y;
      WaterVolume.s_createWaveTangents[0].y =
        WaterVolume.s_createWaveDirections[0].x;
      float num1 = Mathf.Lerp(0.0f, wind.w, depth);
      float time = waterTime / 20f;
      double wave1 = (double)__instance.CreateWave(worldPos, time, 10f, 0.04f,
        8f,
        reversedDepthDirections[0],
        reversedWaveTangents[0], 0.5f);
      float wave2 = __instance.CreateWave(worldPos, time, 14.123f, 0.08f, 6f,
        reversedDepthDirections[1],
        reversedWaveTangents[1], 0.5f);
      float wave3 = __instance.CreateWave(worldPos, time, 22.312f, 0.1f, 4f,
        reversedDepthDirections[2],
        reversedWaveTangents[2], 0.5f);
      float wave4 = __instance.CreateWave(worldPos, time, 31.42f, 0.2f, 2f,
        reversedDepthDirections[3],
        reversedWaveTangents[3], 0.5f);
      float wave5 = __instance.CreateWave(worldPos, time, 35.42f, 0.4f, 1f,
        reversedDepthDirections[4],
        reversedWaveTangents[4], 0.5f);
      float wave6 = __instance.CreateWave(worldPos, time, 38.1223f, 1f, 0.8f,
        reversedDepthDirections[5],
        reversedWaveTangents[5], 0.7f);
      float wave7 = __instance.CreateWave(worldPos, time, 41.1223f, 1.2f,
        0.6f * waveFactor, reversedDepthDirections[6],
        reversedWaveTangents[6], 0.8f);
      float wave8 = __instance.CreateWave(worldPos, time, 51.5123f, 1.3f,
        0.4f * waveFactor, reversedDepthDirections[7],
        reversedWaveTangents[7], 0.9f);
      float wave9 = __instance.CreateWave(worldPos, time, 54.2f, 1.3f,
        0.3f * waveFactor, reversedDepthDirections[8],
        reversedWaveTangents[8], 0.9f);
      float wave10 = __instance.CreateWave(worldPos, time, 56.123f, 1.5f,
        0.2f * waveFactor, reversedDepthDirections[9],
        reversedWaveTangents[9], 0.9f);
      double num2 = (double)wave2;
      var outputRes = ((float)(wave1 + num2) + wave3 + wave4 + wave5 + wave6 +
                       wave7 +
                       wave8 + wave9 + wave10) * num1;

      // Call the original method to calculate the wave
      __result = outputRes;
      return false;
    }

    [HarmonyPatch(typeof(WaterVolume), "UpdateMaterials")]
    [HarmonyPrefix]
    public static void WaterVolumeUpdatePatchWaterVolume(WaterVolume __instance,
      ref float[] ___m_normalizedDepth)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return;
      UpdateCameraWaterLevel(__instance);
      AdjustWaterSurface(__instance, ___m_normalizedDepth);
    }

    private static void UpdateCameraWaterLevel(WaterVolume __instance)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return;
      if (GameCamera.instance)
      {
        WaterLevelCamera =
          __instance.GetWaterSurface(GameCamera.instance.transform.position);
      }

      var isCurrentCameraAboveWater =
        GameCameraPatch.CameraPositionY > WaterLevelCamera;
      UpdateCameraState(isCurrentCameraAboveWater);
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

    private static void AdjustWaterSurface(WaterVolume __instance,
      float[] normalizedDepth)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return;

      var waterSurfaceTransform = __instance.m_waterSurface.transform;
      var isCurrentlyFlipped =
        waterSurfaceTransform.rotation.eulerAngles.y.Equals(180f);

      // will flip the surface if camera is below it.
      if (IsCameraAboveWater && isCurrentlyFlipped ||
          IsCameraBelowWater && !isCurrentlyFlipped)
      {
        if (!isCurrentlyFlipped)
        {
          FlipWaterSurface(__instance, normalizedDepth);
          // SetWaterSurfacePosition(waterSurfaceTransform, WaterLevelCamera);
        }

        if (isCurrentlyFlipped)
        {
          UnflipWaterSurface(__instance, normalizedDepth);
          // SetWaterSurfacePosition(waterSurfaceTransform,
          //   WaterLevelCamera);
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
      // x flip swaps 1,1 to 0,1 IE indexes [0] [1] swap
      // x flip swaps 0,0 to 1,0 IE indexes [2] [3] swap
      float[] oceanDepth = __instance.m_heightmap.GetOceanDepth();
      var depthValues = isFlipped
        ?
        [
          //     // normalizedDepth[0],
          //     // normalizedDepth[0],
          //     // normalizedDepth[0],
          1 - normalizedDepth[1],
          1 - normalizedDepth[0],
          1 - normalizedDepth[3],
          1 - normalizedDepth[2],
          // normalizedDepth[1],
          // 1 - normalizedDepth[0],
          // 1 - normalizedDepth[1],
          // 1 - normalizedDepth[2],
          // 1 - normalizedDepth[3]
          // -normalizedDepth[0],
          // -normalizedDepth[1],
          // -normalizedDepth[2],
          // -normalizedDepth[3]
        ]
        : normalizedDepth;
      __instance.m_normalizedDepth[0] = depthValues[0];
      __instance.m_normalizedDepth[1] = depthValues[1];
      __instance.m_normalizedDepth[2] = depthValues[2];
      __instance.m_normalizedDepth[3] = depthValues[3];

      __instance.m_waterSurface.material.SetFloatArray(DepthProperty
        , depthValues);
      // __instance.m_waterSurface.material.SetFloat(
      //   GlobalWindProperty,
      //   __instance.m_useGlobalWind ? 1f : 0f);
    }
  }
}