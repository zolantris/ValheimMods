using BepInEx.Configuration;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Zolantris.Shared;

namespace ValheimVehicles.Config;

public static class VehicleDebugConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool> AllowDebugCommandsForNonAdmins;
  public static ConfigEntry<bool> AllowEditCommandsForNonAdmins;

  public static ConfigEntry<float> CommandsWindowPosX;

  public static ConfigEntry<float> CommandsWindowPosY;

  public static ConfigEntry<float> VehicleConfigWindowPosX;

  public static ConfigEntry<float> VehicleConfigWindowPosY;

  public static ConfigEntry<int> ButtonFontSize;

  public static ConfigEntry<int> TitleFontSize;

  public static ConfigEntry<bool> HasDebugSails { get; set; }


  public static ConfigEntry<bool>
    AutoShowVehicleColliders { get; private set; } = null!;


  public static float OriginalCameraZoomOutDistance = 8f;

  public static ConfigEntry<bool>
    HasAutoAnchorDelay { get; private set; } = null!;

  public static ConfigEntry<bool>
    DebugMetricsEnabled { get; private set; } = null!;

  public static ConfigEntry<float>
    DebugMetricsTimer { get; private set; } = null!;

  public static ConfigEntry<float>
    VehicleBoundsRebuildDelayPerPiece { get; private set; } = null!;

  public static ConfigEntry<bool>
    DisableVehicleCube { get; private set; } = null!;

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
  private const string VehiclePiecesSectionName = "Vehicle Pieces";

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
    AllowEditCommandsForNonAdmins = config.Bind(SectionName, "AllowDebugCommandsForNonAdmins",
      true,
      ConfigHelpers.CreateConfigDescription(
        "This will allow non-admins the ability to use vehicle creative to edit their vehicle. Non-admins can still use vehicle sway and config commands to edit their ship. This config is provided to improve realism at the cost of convenience.",
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

    // todo possibly move this config-value to a vehicleConfig or vehiclePieceConfig file
    VehicleBoundsRebuildDelayPerPiece = Config.Bind(VehiclePiecesSectionName,
      "VehicleBoundsRebuildDelayPerPiece", 0.02f,
      ConfigHelpers.CreateConfigDescription(
        $"The delay time that is added per piece the vehicle has on it for recalculating vehicle bounds. Example 2000 * 0.02 = 40seconds delay.  Values are clamped at {VehiclePiecesController.RebuildPieceMinDelay} and max value: {VehiclePiecesController.RebuildPieceMaxDelay} so even smaller vehicles rebuild at the min value and large >2k piece vehicles build at the max value.",
        false, true, new AcceptableValueRange<float>(0.01f, 0.1f)));

    DisableVehicleCube = Config.Bind(VehiclePiecesSectionName,
      "DisableVehicleCube", false,
      ConfigHelpers.CreateConfigDescription(
        $"The raft will no longer be a cube. It will place pieces in world position. This will allow for teleporting and other rapid location / login fixes to work better. It might cause large vehicles to clip/break if they are rendered out of a zone.",
        true, true));

    HasDebugSails = Config.Bind("Debug", "HasDebugSails", false,
      ConfigHelpers.CreateConfigDescription(
        "Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.",
        false, true));
    
    CommandsWindowPosX = Config.Bind(SectionName, "CommandsWindowPosX", 0f);
    CommandsWindowPosY = Config.Bind(SectionName, "CommandsWindowPosY", 0f);
    VehicleConfigWindowPosX = Config.Bind(SectionName, "ConfigWindowPosX", 0f);
    VehicleConfigWindowPosY = Config.Bind(SectionName, "ConfigWindowPosY", 0f);
    ButtonFontSize = Config.Bind(SectionName, "ButtonFontSize", 16);
    TitleFontSize = Config.Bind(SectionName, "LabelFontSize", 22);

    DisableVehicleCube.SettingChanged += (sender, args) =>
    {
      VehiclePiecesController.CanUseActualPiecePosition = DisableVehicleCube.Value;
    };
    VehicleBoundsRebuildDelayPerPiece.SettingChanged += (sender, args) =>
    {
      VehiclePiecesController.BoundsDelayPerPiece = VehicleBoundsRebuildDelayPerPiece.Value;
    };
    // onChanged
    AutoShowVehicleColliders.SettingChanged +=
      (_, _) => OnShowVehicleDebugMenuChange();
    DebugMetricsEnabled.SettingChanged += (_, _) => OnMetricsUpdate();
    DebugMetricsTimer.SettingChanged +=
      (_, _) => OnMetricsUpdate();
    VehicleDebugMenuEnabled.SettingChanged += (_, _) => OnToggleVehicleDebugMenu();
  }
}