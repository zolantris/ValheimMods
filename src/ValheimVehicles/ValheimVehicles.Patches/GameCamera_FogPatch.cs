using System.Configuration;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT.Patches;
using ValheimVehicles.Config;

namespace ValheimVehicles.Patches;

/// <summary>
/// from vikings do swim might get things working 
/// </summary>
[HarmonyPatch]
internal class GameCameraPatch
{
  public static float CameraPositionY = 0f;

  public static float prevFogDensity;
  public static bool prevFog;
  public static Color prevFogColor;
  public static bool hasPrevValues;
  public static bool MustRestorePrevValues;


  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
  [HarmonyPostfix]
  public static void GameCameraUpdateCameraPatch(GameCamera __instance,
    Camera ___m_camera)
  {
    if (Player.m_localPlayer == null ||
        WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled)
    {
      return;
    }

    // This is the most important flag, it prevents camera smashing into the watermesh.
    __instance.m_minWaterDistance = -5000f;


    if (WaterVolumePatch.IsCameraAboveWater && MustRestorePrevValues)
    {
      switch (hasPrevValues)
      {
        case false:
          prevFogDensity = RenderSettings.fogDensity;
          prevFog = RenderSettings.fog;
          prevFogColor = RenderSettings.fogColor;
          hasPrevValues = true;
          break;
        case true:
          RenderSettings.fogDensity = prevFogDensity;
          RenderSettings.fog = prevFog;
          RenderSettings.fogColor = prevFogColor;

          prevFogDensity = RenderSettings.fogDensity;
          prevFog = RenderSettings.fog;
          prevFogColor = RenderSettings.fogColor;

          hasPrevValues = false;
          break;
      }
    }

    // Do not do anything if your player is swimming, as this is only related to player perspective.
    if (Player.m_localPlayer.IsSwimming())
    {
      return;
    }

    CameraPositionY = ___m_camera.gameObject.transform.position.y;


    if (WaterVolumePatch.IsCameraAboveWater)
    {
      MustRestorePrevValues = true;
      __instance.m_maxDistance = 20f;
    }

    if (!WaterVolumePatch.IsCameraAboveWater)
    {
      RenderSettings.fogColor = WaterConfig.UnderWaterFogColor.Value;
      RenderSettings.fogDensity = WaterConfig.UnderWaterFogIntensity.Value;
      RenderSettings.fog = WaterConfig.UnderwaterFogEnabled.Value;
    }
  }
}