using BepInEx.Configuration;
using ValheimRAFT;
using Zolantris.Shared;

namespace ValheimVehicles.Config;

public static class VehicleDebugConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> WindowPosX;

  public static ConfigEntry<float> WindowPosY;

  public static ConfigEntry<int> ButtonFontSize;

  public static ConfigEntry<int> TitleFontSize;

  public static ConfigEntry<bool>
    AutoShowVehicleColliders { get; private set; } = null!;


  public static float OriginalCameraZoomOutDistance = 8f;

  public static ConfigEntry<bool>
    HasAutoAnchorDelay { get; private set; } = null!;

  public static ConfigEntry<bool>
    DebugMetricsEnabled { get; private set; } = null!;

  public static ConfigEntry<float>
    DebugMetricsTimer { get; private set; } = null!;

  public static ConfigEntry<int>
    VehiclePieceBoundsRecalculationDelay { get; private set; } = null!;

  public static ConfigEntry<float> AutoAnchorDelayTime { get; private set; } =
    null!;

  // public static ConfigEntry<bool>
  //   ForceTakeoverShipControls { get; private set; } = null!;

  public static ConfigEntry<bool>
    PositionAutoFix { get; private set; } = null!;

  public static ConfigEntry<float>
    PositionAutoFixThreshold { get; private set; } = null!;

  public static ConfigEntry<bool>
    VehicleDebugMenuEnabled { get; private set; } = null!;


  public static ConfigEntry<bool> SyncShipPhysicsOnAllClients
  {
    get;
    private set;
  } = null!;


  private const string SectionName = "Vehicle Debugging";

  private static void OnShowVehicleDebugMenuChange()
  {
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui(VehicleDebugMenuEnabled
      .Value);
  }

  private static void OnMetricsUpdate()
  {
    BatchedLogger.IsLoggingEnabled = DebugMetricsEnabled.Value;
    if (BatchedLogger.IsLoggingEnabled)
    {
      BatchedLogger.BatchIntervalFrequencyInSeconds = DebugMetricsTimer.Value;
    }
  }




  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    DebugMetricsEnabled = config.Bind(SectionName, "DebugMetricsEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Will locally log metrics for ValheimVehicles mods. Meant for debugging functional delays",
        false, true));
    DebugMetricsTimer = config.Bind(SectionName, "DebugMetricsTimer",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds that the logs output. Lower is performance heavy. Do not have this set to a low value. Requires EnableDebugMetrics to be enabled to update.",
        false, true));

    HasAutoAnchorDelay = config.Bind(SectionName, "HasAutoAnchorDelay",
      false,
      ConfigHelpers.CreateConfigDescription(
        "For realism, the ship continues even when nobody is onboard. This is meant for debugging logout points but also could be useful for realism",
        true, true));
    AutoAnchorDelayTime = config.Bind(SectionName,
      "AutoAnchorDelayTimeInSeconds",
      10f,
      ConfigHelpers.CreateConfigDescription(
        "For realism, the ship continues for X amount of time until it either unrenders or a player stops it.",
        true, true, new AcceptableValueRange<float>(0f, 60f)));

    AutoShowVehicleColliders = config.Bind(SectionName,
      "Always Show Vehicle Colliders",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Automatically shows the vehicle colliders useful for debugging the vehicle",
        true, true));
    VehicleDebugMenuEnabled = config.Bind(SectionName, "Vehicle Debug Menu",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable the VehicleDebugMenu. This shows a GUI menu which has a few shortcuts to debugging/controlling vehicles.",
        true, true));

    PositionAutoFix = config.Bind(SectionName,
      "UNSTABLE_PositionAutoFix",
      false,
      ConfigHelpers.CreateConfigDescription(
        "UNSTABLE due to vehicle center point shifting and not being in center of actual vehicle pieces - Automatically moves the vehicle if buried/stuck underground. The close to 0 the closer it will be to teleporting the vehicle above the ground. The higher the number the more lenient it is. Recommended to keep this number above 10. Lower can make the vehicle trigger an infinite loop of teleporting upwards and then falling and re-telporting while gaining velocity",
        true, true));
    PositionAutoFixThreshold = config.Bind(SectionName,
      "positionAutoFixThreshold",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "Threshold for autofixing stuck vehicles. Large values are less accurate but smaller values may trigger the autofix too frequently",
        true, true, new AcceptableValueRange<float>(0, 10f)));

    SyncShipPhysicsOnAllClients =
      Config.Bind("Debug", "SyncShipPhysicsOnAllClients", false,
        ConfigHelpers.CreateConfigDescription(
          "Makes all clients sync physics",
          true, true));

    VehiclePieceBoundsRecalculationDelay = Config.Bind("Debug",
      "VehiclePieceBoundsRecalculationDelay", 10,
      ConfigHelpers.CreateConfigDescription(
        "The delay time at which the vehicle will recalculate bounds after placing a piece. This recalculation can be a bit heavy so it's debounced a minimum of 1 seconds but could be increased up to 30 seconds for folks that want to build a pause for a bit.",
        false, true, new AcceptableValueRange<int>(1, 30)));

    WindowPosX = Config.Bind(SectionName, "WindowPosX", 0f);
    WindowPosY = Config.Bind(SectionName, "WindowPosY", 0f);
    ButtonFontSize = Config.Bind(SectionName, "ButtonFontSize", 16);
    TitleFontSize = Config.Bind(SectionName, "LabelFontSize", 22);

    // onChanged
    AutoShowVehicleColliders.SettingChanged +=
      (_, _) => OnShowVehicleDebugMenuChange();
    DebugMetricsEnabled.SettingChanged += (_, _) => OnMetricsUpdate();
    DebugMetricsTimer.SettingChanged +=
      (_, _) => OnMetricsUpdate();

 
  }
}