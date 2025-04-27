using System;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Controllers;
using ValheimVehicles.ModSupport;

namespace ValheimVehicles;

// todo make this a full plugin
// [BepInPlugin(Guid, ModName, Version)]
// [BepInDependency(Jotunn.Main.ModGuid)]

/// <summary>
/// This is an internal plugin of ValheimRAFT. ValheimVehicles is the modern API for ValheimRAFT and should never directly reference ValheimRAFT.
/// </summary>
public class ValheimVehiclesPlugin : MonoBehaviour
{
  public const string Author = "Zolantris";
  public const string Version = "1.0.0";
  internal const string ModName = "ValheimVehicles";
  public const string Guid = $"{Author}.{ModName}";
  public static bool HasRunSetup;
  private static bool HasCreatedConfig = false;
  private RetryGuard _rpcRegisterRetry;

  private MapPinSync _mapPinSync;

  public static ValheimVehiclesPlugin Instance { get; private set; }

  private void Awake()
  {
    Instance = this;
    _rpcRegisterRetry = new RetryGuard(Instance);
  }

  private void OnEnable()
  {
    Setup();
  }

  private void OnDisable()
  {
    HasRunSetup = false;
    if (_mapPinSync != null)
    {
      Destroy(_mapPinSync);
    }
  }

  public void Setup()
  {
    if (HasRunSetup) return;

    // components
    SetupComponents();

    HasRunSetup = true;
  }

  private void SetupComponents()
  {
    _mapPinSync = gameObject.AddComponent<MapPinSync>();
  }

  /// <summary>
  /// Localization.instance seems flake. Having a 50 second queue of calling it until it success 1 time should guard against problems.
  /// </summary>
  public void UpdateTranslations()
  {
    if (!_rpcRegisterRetry.CanRetry) return;
    if (Localization.instance == null)
    {
      _rpcRegisterRetry.Retry(UpdateTranslations, 1f);
      return;
    }

    ModTranslations.UpdateTranslations();
    if (!ModTranslations.IsHealthy())
    {
      _rpcRegisterRetry.Retry(UpdateTranslations, 1f);
    }
  }


  /// <summary>
  /// This should only be called from the ValheimRAFT mod.
  /// </summary>
  /// <param name="config"></param>
  [UsedImplicitly]
  public static void CreateConfigFromRAFTConfig(ConfigFile config)
  {
    if (HasCreatedConfig)
    {
      return;
    }

    PatchConfig.BindConfig(config);
    RamConfig.BindConfig(config);
    PrefabConfig.BindConfig(config);
    VehicleDebugConfig.BindConfig(config);
    PropulsionConfig.BindConfig(config);
    ModSupportConfig.BindConfig(config);
    CustomMeshConfig.BindConfig(config);
    WaterConfig.BindConfig(config);
    PhysicsConfig.BindConfig(config);
    MinimapConfig.BindConfig(config);
    HudConfig.BindConfig(config);
    CameraConfig.BindConfig(config);
    RenderingConfig.BindConfig(config);

#if DEBUG
    // Meant for only being run in debug builds for testing quickly
    QuickStartWorldConfig.BindConfig(config);
#endif

    HasCreatedConfig = true;
  }
}