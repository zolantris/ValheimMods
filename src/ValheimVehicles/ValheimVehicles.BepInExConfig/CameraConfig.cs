using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class CameraConfig : BepInExBaseConfig<CameraConfig>
{
  private const string CameraOptimizationsKey = "Camera Optimizations";
  private const string CameraZoomKey = "Camera Zoom";
  public const int cameraZoomMultiplier = 8;

  public static ConfigEntry<bool> CameraOcclusionEnabled = null!;
  public static ConfigEntry<bool> CameraZoomOverridesEnabled = null!;

  // occulision specific config
  public static ConfigEntry<float> CameraOcclusionInterval = null!;
  public static ConfigEntry<float> DistanceToKeepObjects = null!;

  // zoom
  public static ConfigEntry<float> VehicleCameraZoomMaxDistance = null!;

  /// <summary>
  /// Debug will always run this. However, OnboardController should control this normally.
  /// </summary>
  public static void OnCameraZoomChange()
  {
#if DEBUG
    if (!CameraZoomOverridesEnabled.Value || VehicleCameraZoomMaxDistance.Value == 0)
    {
      return;
    }

    if (Camera.main == null)
    {
      return;
    }

    var mainCamera = Camera.main.GetComponent<GameCamera>();
    if (!mainCamera) return;

    mainCamera.m_maxDistance = Mathf.Lerp(cameraZoomMultiplier, Mathf.Pow(cameraZoomMultiplier, 2), VehicleCameraZoomMaxDistance.Value);
#endif
  }

  public override void OnBindConfig(ConfigFile config)
  {
    CameraOcclusionInterval = config.BindUnique(CameraOptimizationsKey,
      "CameraOcclusionInterval", 0.1f,
      ConfigHelpers.CreateConfigDescription(
        "Interval in seconds at which the camera will hide meshes in attempt to consolidate FPS / GPU memory.",
        false, false, new AcceptableValueRange<float>(0.01f, 30f)));

    CameraOcclusionEnabled = config.BindUnique(CameraOptimizationsKey,
      "UNSTABLE_CameraOcclusionEnabled", false, ConfigHelpers.CreateConfigDescription(
        $"Unstable config, this will possible get you more performance but parts of the vehicle will be hidden when rapidly panning. This Enables hiding active raft pieces at specific intervals. This will hide only the rendered texture.",
        false, false));

    DistanceToKeepObjects = config.BindUnique(CameraOptimizationsKey,
      "UNSTABLE_DistanceToKeepObjects", 5f,
      ConfigHelpers.CreateConfigDescription(
        $"Threshold at which to retain a object even if it's through a wall.",
        false, false, new AcceptableValueRange<float>(0, 20f)));

    CameraZoomOverridesEnabled = config.BindUnique(CameraZoomKey,
      "VehicleCameraZoom_Enabled", false, ConfigHelpers.CreateConfigDescription(
        $"Overrides the camera zoom while on the vehicle. Values are configured through other keys.",
        true));
    VehicleCameraZoomMaxDistance = config.BindUnique(CameraZoomKey,
      "VehicleCameraZoomMaxDistance",
      0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Allows the camera to zoom out between 8 and 64 meters. Percentage based zoom.",
        true, true, new AcceptableValueRange<float>(0f, 1f)));

    VehicleCameraZoomMaxDistance.SettingChanged +=
      (_, _) => OnCameraZoomChange();
    CameraOcclusionInterval.SettingChanged += (sender, args) =>
      VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
    CameraOcclusionEnabled.SettingChanged += (sender, args) =>
      VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
  }
}