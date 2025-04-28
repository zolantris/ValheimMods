using System.Collections.Generic;
using System.Configuration;
using HarmonyLib;
using UnityEngine;

using ValheimVehicles.Config;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

/// <summary>
/// from vikings do swim might get things working 
/// </summary>
[HarmonyPatch]
public class GameCamera_WaterPatches
{
  public static float CameraPositionY = 0f;

  public static float? prevFogDensity;
  public static bool? prevFog;
  public static Color? prevFogColor;
  public static Vector2i? prevFogZone = Vector2i.zero;

  // Meant to be updated by WaterVolumePatches
  public static bool CanUpdateFog;
  public static bool previousSurfaceState;

  public static void UpdateFogBasedOnEnvironment()
  {
    var currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
    var isNight = EnvMan.IsNight();
    var color = !EnvMan.IsNight()
      ? currentEnvironment.m_fogColorDay
      : currentEnvironment.m_fogColorNight;
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
    var currentZone = GetCurrentZone();
    if (WaterVolume_WaterPatches.IsCameraAboveWater)
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
    else
    {
      prevFogDensity = RenderSettings.fogDensity;
      prevFog = RenderSettings.fog;
      prevFogColor = RenderSettings.fogColor;
      prevFogZone = currentZone;

      RenderSettings.fogColor = WaterConfig.UnderWaterFogColor.Value;
      RenderSettings.fogDensity = WaterConfig.UnderWaterFogIntensity.Value;
      RenderSettings.fog = WaterConfig.UnderwaterFogEnabled.Value;
    }

    CanUpdateFog = false;
  }

  public static void RequestUpdate(
    bool isAboveWater)
  {
    if (isAboveWater != previousSurfaceState) CanUpdateFog = true;

    previousSurfaceState = isAboveWater;
  }

  public static LayerMask BlockingWaterMask = new();

  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
  [HarmonyPostfix]
  public static void GameCameraGetWaterMaskOnAwake(GameCamera __instance)
  {
    if (GameCamera.m_instance.m_camera == Camera.main) BlockingWaterMask = __instance.m_blockCameraMask;
  }

  public const int underwaterCameraZoom = -5000;

  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
  [HarmonyPostfix]
  public static void GameCameraUpdateCameraPatch(GameCamera __instance,
    Camera ___m_camera)
  {
    if (GameCamera.m_instance.m_camera != Camera.main) return;
    if (Player.m_localPlayer == null ||
        WaterConfig.UnderwaterAccessMode.Value ==
        WaterConfig.UnderwaterAccessModeType.Disabled)
      return;

    UpdateFogSettings();
    CameraPositionY = ___m_camera.gameObject.transform.position.y;

    // This is the most important flag, it prevents camera smashing into the watermesh.
    // negative value due to it allowing zoom further out
    // fallthrough logic
    __instance.m_minWaterDistance = underwaterCameraZoom;
  }
}