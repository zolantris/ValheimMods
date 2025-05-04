using System;
using System.Collections;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Controllers;
using ValheimVehicles.QuickStartWorld.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;
using Zolantris.Shared.Debug;

namespace ValheimVehicles;

// todo make this a full plugin
// [BepInPlugin(Guid, ModName, Version)]
// [BepInDependency(Jotunn.Main.ModGuid)]

/// <summary>
/// This is an internal plugin of ValheimRAFT. ValheimVehicles is the modern API for ValheimRAFT and should never directly reference ValheimRAFT.
/// </summary>
public class ValheimVehiclesPlugin : MonoBehaviour
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  internal const string ModName = "ValheimVehicles";
  public const string Guid = $"{Author}.{ModName}";
  public static bool HasRunSetup;
  private static bool HasCreatedConfig = false;
  private RetryGuard _languageRetry;

  private MapPinSync _mapPinSync;
  private SwivelUIPanelComponentIntegration _swivelUIPanel;
  private ScreenSizeWatcher _screenSizeWatcher;

  public static ValheimVehiclesPlugin Instance { get; private set; }
  private Coroutine? translationRoutine = null;

  private void Awake()
  {
    Instance = this;
    _languageRetry = new RetryGuard(Instance);
  }

  private IEnumerator UpdateTranslationsRoutine()
  {
    // bail at 10 seconds and add OnLanguageChanged regardless
    var timer = DebugSafeTimer.StartNew();
    while (timer.ElapsedMilliseconds < 10000 && !ModTranslations.CanRunLocalization())
    {
      yield return null;
    }
    Localization.OnLanguageChange += OnLanguageChanged;

    // must wait for next-frame otherwise Awake and other lifecycles might not have fired for translations api.
    yield return new WaitForFixedUpdate();
    OnLanguageChanged();

    // wait a bit then fire next update. ModTranslations API is flaky on init.
    yield return new WaitForSeconds(20f);
    _languageRetry.Reset();
    OnLanguageChanged();

    // do it 1 more time.
    yield return new WaitForSeconds(20f);
    _languageRetry.Reset();
    OnLanguageChanged();

    translationRoutine = null;
  }

  private void OnEnable()
  {
    Setup();
    translationRoutine = StartCoroutine(UpdateTranslationsRoutine());
  }

  private void OnDisable()
  {
    // cancels retries.
    CancelInvoke();

    if (translationRoutine != null)
    {
      StopCoroutine(translationRoutine);
      translationRoutine = null;
    }

    HasRunSetup = false;
    if (_mapPinSync != null)
    {
      Destroy(_mapPinSync);
    }

    if (Localization.instance != null)
    {

      Localization.OnLanguageChange -= OnLanguageChanged;
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
    _swivelUIPanel = gameObject.AddComponent<SwivelUIPanelComponentIntegration>();
    _screenSizeWatcher = gameObject.AddComponent<ScreenSizeWatcher>();
  }

  private void OnLanguageChanged()
  {
    ModTranslations.ForceUpdateTranslations();
    if (!ModTranslations.IsHealthy())
    {
      _languageRetry.Retry(OnLanguageChanged, 1f);
    }
    else
    {
      _languageRetry.Reset();
    }
  }


  /// <summary>
  /// This should only be called from the ValheimRAFT mod.
  /// </summary>
  /// <param name="config"></param>
  [UsedImplicitly]
  public static void CreateConfigFromValheimRAFTPluginConfig(ConfigFile config)
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
    VehicleGlobalConfig.BindConfig(config);
    GuiConfig.BindConfig(config);


#if DEBUG
    // Meant for only being run in debug builds for testing quickly
    QuickStartWorldConfig.BindConfig(config);
#endif

    HasCreatedConfig = true;
  }
}