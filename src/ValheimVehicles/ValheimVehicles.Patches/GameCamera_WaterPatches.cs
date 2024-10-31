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

  public static float? prevFogDensity;
  public static bool? PrevCameraAboveWater = false;
  public static bool? prevFog;
  public static Color? prevFogColor;
  public static bool? hasPrevValues = false;
  public static Vector2i? prevFogZone = Vector2i.zero;

  // Meant to be updated by WaterVolumePatches
  public static bool MustRestorePrevValues;
  public static bool MustUpdateCamera;


  public static void UpdateFogBasedOnEnvironment()
  {
    EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
    var isNight = EnvMan.IsNight();
    Color color = ((!EnvMan.IsNight())
      ? currentEnvironment.m_fogColorDay
      : currentEnvironment.m_fogColorNight);
  }

  public static Vector2i GetCurrentZone()
  {
    var playerPos = GameCamera.instance.m_playerPos;
    var currentZone =
      ZoneSystem.GetZone(new Vector2(playerPos.x, playerPos.z));
    return currentZone;
  }

  public static void UpdateFogSettings()
  {
    if (!WaterConfig.UnderwaterFogEnabled.Value) return;
    if (!MustRestorePrevValues) return;
    var currentZone = GetCurrentZone();
    if (WaterVolumePatch.IsCameraAboveWater)
    {
      if (prevFogZone == currentZone)
      {
        if (prevFogDensity != null)
          RenderSettings.fogDensity = prevFogDensity.Value;
        if (prevFog != null)
          RenderSettings.fog = prevFog.Value;
        if (prevFogColor != null)
          RenderSettings.fogColor = prevFogColor.Value;
      }

      prevFogDensity = null;
      prevFog = null;
      prevFogColor = null;
      prevFogZone = null;
    }

    if (WaterVolumePatch.CameraWaterState ==
        WaterVolumePatch.CameraWaterStateTypes.ToBelow &&
        WaterConfig.UnderwaterFogEnabled.Value)
    {
      prevFogDensity = RenderSettings.fogDensity;
      prevFog = RenderSettings.fog;
      prevFogColor = RenderSettings.fogColor;
      prevFogZone = currentZone;

      RenderSettings.fogColor = WaterConfig.UnderWaterFogColor.Value;
      RenderSettings.fogDensity = WaterConfig.UnderWaterFogIntensity.Value;
      RenderSettings.fog = WaterConfig.UnderwaterFogEnabled.Value;
    }

    MustRestorePrevValues = false;
  }

  public static void RequestUpdate()
  {
    MustRestorePrevValues = true;
    MustUpdateCamera = true;
  }

  // todo fix jitters with low headroom at water level
  // [HarmonyPostfix(typeof(GameCamera), nameof(GameCamera.UpdateNearClipping))]


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

    UpdateFogSettings();
    CameraPositionY = ___m_camera.gameObject.transform.position.y;
    // __instance.m_waterClipping = true;

    // This is the most important flag, it prevents camera smashing into the watermesh.
    // negative value due to it allowing zoom further out
    if (WaterConfig.UnderwaterShipCameraZoom.Value != 0)
    {
      __instance.m_minWaterDistance =
        WaterConfig.UnderwaterShipCameraZoom.Value * -1;
    }
    else
    {
      // default
      __instance.m_minWaterDistance = 5f;
    }

    // Do not do anything if your player is swimming, as this is only related to player perspective.
    // if (Player.m_localPlayer.IsSwimming())
    // {
    //   __instance.m_minWaterDistance = 5f;
    //   return;
    // }

    //
    // if (WaterVolumePatch.IsCameraAboveWater && MustUpdateCamera)
    // {
    //   __instance.m_maxDistance = 20f;
    // }

    // if (!MustUpdateCamera) return;
    //
    // MustUpdateCamera = false;
  }
}