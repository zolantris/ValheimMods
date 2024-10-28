using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Config;

namespace ValheimVehicles.Patches
{
  [HarmonyPatch]
  internal class WaterVolumePatch
  {
    public static float WaterLevelCamera = 0f;
    public static bool IsCameraAboveWater;
    public static bool IsFlipped = false;

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

    private static float currentWaveHeight = 0f;

    // This will replace the original GetWaterSurface
    [HarmonyPatch(typeof(WaterVolume), "GetWaterSurface")]
    [HarmonyPrefix]
    public static bool GetWaterSurface(WaterVolume __instance, float __result,
      Vector3 point,
      float waveFactor = 1f)
    {
      if (WaterConfig.UnderwaterAccessMode.Value ==
          WaterConfig.UnderwaterAccessModeType.Disabled) return false;

      var num = 0.0f;
      if (__instance.m_useGlobalWind)
      {
        var wrappedDayTimeSeconds = WaterVolume.s_wrappedDayTimeSeconds;
        var depth = __instance.Depth(point);

        // Smoothly adjust wave height based on depth
        var waveHeight = Mathf.Lerp(__instance.m_forceDepth,
          __instance.CalcWave(point, depth, wrappedDayTimeSeconds, waveFactor),
          0.1f);
        num += waveHeight;
      }

      var waterSurface =
        __instance.transform.position.y + num + __instance.m_surfaceOffset;
      if ((double)__instance.m_forceDepth < 0.0 &&
          (double)Utils.LengthXZ(point) > 10500.0)
        waterSurface -= 100f;

      currentWaveHeight = waterSurface;
      __result = waterSurface;
      return true;
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

    private static void AdjustWaterSurface(WaterVolume __instance,
      float[] normalizedDepth)
    {
      IsCameraAboveWater = GameCameraPatch.CameraPositionY > WaterLevelCamera;
      Transform waterSurfaceTransform = __instance.m_waterSurface.transform;
      IsFlipped =
        waterSurfaceTransform.rotation.eulerAngles.y.Equals(180f);

      if (IsCameraAboveWater && !IsFlipped)
      {
        FlipWaterSurface(__instance, normalizedDepth);
        SetWaterSurfacePosition(waterSurfaceTransform, WaterLevelCamera);
      }
      else if (!IsCameraAboveWater && IsFlipped)
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
      float[] depthValues = isFlipped
        ?
        [
          // normalizedDepth[2],
          // normalizedDepth[0],
          // normalizedDepth[3],
          // normalizedDepth[1],
          1 - normalizedDepth[0],
          1 - normalizedDepth[1],
          1 - normalizedDepth[2],
          1 - normalizedDepth[3]
          // -normalizedDepth[0],
          // -normalizedDepth[1],
          // -normalizedDepth[2],
          // -normalizedDepth[3]
        ]
        : normalizedDepth;


      __instance.m_waterSurface.material.SetFloatArray(
        Shader.PropertyToID("_depth"), depthValues);
      __instance.m_waterSurface.material.SetFloat(
        Shader.PropertyToID("_UseGlobalWind"),
        __instance.m_useGlobalWind ? 1f : 0f);
    }
  }
}