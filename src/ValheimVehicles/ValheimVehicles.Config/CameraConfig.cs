using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Config;

public static class CameraConfig
{
  private const string CameraOptimizationsKey = "Camera Optimizations";
  private const string CameraZoomKey = "Camera Zoom";
  public const int cameraZoomMultiplier = 8;

  private static ConfigFile Config = null!;
  
  public static ConfigEntry<bool> CameraOcclusionEnabled = null!;
  public static ConfigEntry<bool> CameraZoomOverridesEnabled = null!;

  // occulision specific config
  public static ConfigEntry<float> CameraOcclusionInterval = null!;
  public static ConfigEntry<float> DistanceToKeepObjects = null!;

  // zoom
  public static ConfigEntry<float> CameraZoomMaxDistance = null!;

  /// <summary>
  /// Debug will always run this. However onboard controller should control this normally.
  /// </summary>
  public static void OnCameraZoomChange()
  {
#if DEBUG
    if (!CameraZoomOverridesEnabled.Value || CameraZoomMaxDistance.Value == 0)
    {
      return;
    }

    if (Camera.main == null)
    {
      return;
    }

    var mainCamera = Camera.main.GetComponent<GameCamera>();
    if (!mainCamera) return;

    mainCamera.m_maxDistance = Mathf.Lerp(cameraZoomMultiplier, Mathf.Pow(cameraZoomMultiplier, 2), CameraZoomMaxDistance.Value);
#endif
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    CameraOcclusionInterval = Config.Bind(CameraOptimizationsKey,
      "CameraOcclusionInterval", 0.1f,
      ConfigHelpers.CreateConfigDescription(
        "Interval in seconds at which the camera will hide meshes in attempt to consolidate FPS / GPU memory.",
        false, false, new AcceptableValueRange<float>(0.01f, 30f)));

    CameraOcclusionEnabled = Config.Bind(CameraOptimizationsKey,
      "UNSTABLE_CameraOcclusionEnabled", false, ConfigHelpers.CreateConfigDescription(
        $"Unstable config, this will possible get you more performance but parts of the vehicle will be hidden when rapidly panning. This Enables hiding active raft pieces at specific intervals. This will hide only the rendered texture.",
        false, false));

    DistanceToKeepObjects = Config.Bind(CameraOptimizationsKey,
      "DistanceToKeepObjects", 5f,
      ConfigHelpers.CreateConfigDescription(
        $"Threshold at which to retain a object even if it's through a wall.",
        false, false, new AcceptableValueRange<float>(0, 20f)));

    CameraZoomOverridesEnabled = Config.Bind(CameraZoomKey,
      "CameraZoom Enabled", false, ConfigHelpers.CreateConfigDescription(
        $"Overrides the camera zoom while on the vehicle. Values are configured through other keys.",
        true));
    CameraZoomMaxDistance = config.Bind(CameraZoomKey,
      "CameraZoomMaxDistance",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Allows the camera to zoom out between 8 and 64 meters. Percentage based zoom.",
        true, true, new AcceptableValueRange<float>(0f, 1f)));

    CameraZoomMaxDistance.SettingChanged +=
      (_, _) => OnCameraZoomChange();
    CameraOcclusionInterval.SettingChanged += (sender, args) =>
      VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
    CameraOcclusionEnabled.SettingChanged += (sender, args) =>
      VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
  }
}