using BepInEx.Configuration;
using ValheimRAFT;
using ValheimVehicles.Vehicles.Components;
using Zolantris.Shared;

namespace ValheimVehicles.Config;

public static class VehicleDebugConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool> AllowDebugCommandsForNonAdmins;

  public static ConfigEntry<float> CommandsWindowPosX;

  public static ConfigEntry<float> CommandsWindowPosY;

  public static ConfigEntry<float> VehicleConfigWindowPosX;

  public static ConfigEntry<float> VehicleConfigWindowPosY;

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
    ValheimRaftPlugin.Instance.AddRemoveVehicleGui();
  }

  private static void OnMetricsUpdate()
  {
    BatchedLogger.IsLoggingEnabled = DebugMetricsEnabled.Value;
    if (BatchedLogger.IsLoggingEnabled)
    {
      BatchedLogger.BatchIntervalFrequencyInSeconds = DebugMetricsTimer.Value;
    }
  }

  private static void OnToggleVehicleDebugMenu()
  {
    if (VehicleGui.Instance == null)
    {
      ValheimRaftPlugin.Instance.AddRemoveVehicleGui();
    }
    else
    {
      
      VehicleGui.Instance.InitPanel();
    VehicleGui.SetCommandsPanelState(VehicleDebugMenuEnabled.Value);
    }
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    AllowDebugCommandsForNonAdmins = config.Bind(SectionName, "AllowDebugCommandsForNonAdmins",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Will will allow all debug commands for non-admins. Turning this to false will only allow debug (cheat) commands if the user is an admin.",
        true, true));

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

    SyncShipPhysicsOnAllClients =
      Config.Bind(SectionName, "SyncShipPhysicsOnAllClients", false,
        ConfigHelpers.CreateConfigDescription(
          "Makes all clients sync physics. This will likely cause a desync in physics but could fix some problems with physics not updating in time for some clients as all clients would control physics.",
          true, true));

    VehiclePieceBoundsRecalculationDelay = Config.Bind(SectionName,
      "VehiclePieceBoundsRecalculationDelay", 10,
      ConfigHelpers.CreateConfigDescription(
        "The delay time at which the vehicle will recalculate bounds after placing a piece. This recalculation can be a bit heavy so it's debounced a minimum of 1 seconds but could be increased up to 30 seconds for folks that want to build a pause for a bit.",
        false, true, new AcceptableValueRange<int>(1, 30)));

    CommandsWindowPosX = Config.Bind(SectionName, "CommandsWindowPosX", 0f);
    CommandsWindowPosY = Config.Bind(SectionName, "CommandsWindowPosY", 0f);
    VehicleConfigWindowPosX = Config.Bind(SectionName, "ConfigWindowPosX", 0f);
    VehicleConfigWindowPosY = Config.Bind(SectionName, "ConfigWindowPosY", 0f);
    ButtonFontSize = Config.Bind(SectionName, "ButtonFontSize", 16);
    TitleFontSize = Config.Bind(SectionName, "LabelFontSize", 22);

    // onChanged
    AutoShowVehicleColliders.SettingChanged +=
      (_, _) => OnShowVehicleDebugMenuChange();
    DebugMetricsEnabled.SettingChanged += (_, _) => OnMetricsUpdate();
    DebugMetricsTimer.SettingChanged +=
      (_, _) => OnMetricsUpdate();
    VehicleDebugMenuEnabled.SettingChanged += (_, _) => OnToggleVehicleDebugMenu();
  }
}