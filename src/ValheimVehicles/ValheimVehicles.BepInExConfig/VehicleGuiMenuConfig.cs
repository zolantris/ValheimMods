using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Patches;
using ValheimVehicles.UI;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class VehicleGuiMenuConfig : BepInExBaseConfig<VehicleGuiMenuConfig>
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool> AllowDebugCommandsForNonAdmins;
  public static ConfigEntry<bool> AllowEditCommandsForNonAdmins;
  public static ConfigEntry<int> VehicleCreativeHeight;
  public static ConfigEntry<bool> HasDebugPieces;

  public static ConfigEntry<bool> HasDebugCannonTargets = null!;

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
    VehicleGui.AddRemoveVehicleGui();
  }

  private static void OnMetricsUpdate()
  {
    BatchedLogger.IsLoggingEnabled = DebugMetricsEnabled.Value;
    if (BatchedLogger.IsLoggingEnabled)
    {
      BatchedLogger.BatchIntervalFrequencyInSeconds = DebugMetricsTimer.Value;
    }

    ConvexHullCalculator.hasPointDumpingEnabled = DebugMetricsEnabled.Value;
  }

  private static void OnToggleVehicleDebugMenu()
  {
    if (VehicleGui.Instance == null)
    {
    }
    else
    {
      VehicleGui.Instance.InitPanel();
      VehicleGui.SetCommandsPanelState(VehicleDebugMenuEnabled.Value);
    }
  }

  public override void OnBindConfig(ConfigFile config)
  {
    Config = config;

    AllowDebugCommandsForNonAdmins = config.BindUnique(SectionName, "AllowDebugCommandsForNonAdmins",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Will allow all debug commands for non-admins. Turning this to false will only allow debug (cheat) commands if the user is an admin.",
        true, true));
    HasDebugCannonTargets = config.BindUnique(SectionName, "HasDebugCannonTargets",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Will allow debugging cannon targets.",
        false, false));

    HasDebugCannonTargets.SettingChanged += (sender, args) =>
    {
      RuntimeDebugLineDrawer.IsEnabled = HasDebugCannonTargets.Value;
    };
    RuntimeDebugLineDrawer.IsEnabled = HasDebugCannonTargets.Value;


    AllowEditCommandsForNonAdmins = config.BindUnique(SectionName, "AllowEditCommandsForNonAdmins",
      true,
      ConfigHelpers.CreateConfigDescription(
        "This will allow non-admins the ability to use vehicle creative to edit their vehicle. Non-admins can still use vehicle sway and config commands to edit their ship. This config is provided to improve realism at the cost of convenience.",
        true, true));

    VehicleCreativeHeight = config.BindUnique("Config", "VehicleCreativeHeight",
      0,
      ConfigHelpers.CreateConfigDescription(
        "Sets the vehicle creative command height, this value is relative to the current height of the ship, negative numbers will sink your ship temporarily",
        false, false, new AcceptableValueRange<int>(-50, 50)));

    DebugMetricsEnabled = config.BindUnique(SectionName, "DebugMetricsEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Will locally log metrics for ValheimVehicles mods. Meant for debugging functional delays, convexHull logic, and other long running processes. This can be log heavy but important to enable if the mod is having problems in order to report issues.",
        false, true));
    DebugMetricsTimer = config.BindUnique(SectionName, "DebugMetricsTimer",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "The interval in seconds that the logs output. Lower is performance heavy. Do not have this set to a low value. Requires EnableDebugMetrics to be enabled to update.",
        false, true));

    HasAutoAnchorDelay = config.BindUnique(SectionName, "HasAutoAnchorDelay",
      false,
      ConfigHelpers.CreateConfigDescription(
        "For realism, the ship continues even when nobody is onboard. This is meant for debugging logout points but also could be useful for realism",
        true, true));
    AutoAnchorDelayTime = config.BindUnique(SectionName,
      "AutoAnchorDelayTimeInSeconds",
      10f,
      ConfigHelpers.CreateConfigDescription(
        "For realism, the ship continues for X amount of time until it either unrenders or a player stops it.",
        true, true, new AcceptableValueRange<float>(0f, 60f)));

    AutoShowVehicleColliders = config.BindUnique(SectionName,
      "Always Show Vehicle Colliders",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Automatically shows the vehicle colliders useful for debugging the vehicle",
        true, true));
    VehicleDebugMenuEnabled = config.BindUnique(SectionName, "Vehicle Debug Menu",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable the VehicleDebugMenu. This shows a GUI menu which has a few shortcuts to debugging/controlling vehicles.",
        true, true));

    SyncShipPhysicsOnAllClients =
      config.BindUnique(SectionName, "SyncShipPhysicsOnAllClients", false,
        ConfigHelpers.CreateConfigDescription(
          "Makes all clients sync physics. This will likely cause a desync in physics but could fix some problems with physics not updating in time for some clients as all clients would control physics.",
          true, true));

    // todo possibly move this config-value to a vehicleConfig or vehiclePieceConfig file
    VehicleBoundsRebuildDelayPerPiece = config.BindUnique(VehiclePiecesSectionName,
      "VehicleBoundsRebuildDelayPerPiece", 0.02f,
      ConfigHelpers.CreateConfigDescription(
        $"The delay time that is added per piece the vehicle has on it for recalculating vehicle bounds. Example 2000 * 0.02 = 40seconds delay.  Values are clamped at {BasePiecesController.RebuildPieceMinDelay} and max value: {BasePiecesController.RebuildPieceMaxDelay} so even smaller vehicles rebuild at the min value and large >2k piece vehicles build at the max value.",
        false, true, new AcceptableValueRange<float>(0.00001f, 0.1f)));

    HasDebugSails = config.BindUnique("Debug", "HasDebugSails", false,
      ConfigHelpers.CreateConfigDescription(
        "Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.",
        false, true));
    HasDebugPieces = config.BindUnique("Debug", "HasDebugPieces", false,
      ConfigHelpers.CreateConfigDescription(
        "Outputs more debug information for the vehicle pieces controller which manages all pieces placement. Meant for debugging mod issues. Will cause performance issues and lots of logging when enabled.",
        false, true));


    CommandsWindowPosX = config.BindUnique(SectionName, "CommandsWindowPosX", 0f, ConfigHelpers.CreateConfigDescription("For vehicle commands window position"));
    CommandsWindowPosY = config.BindUnique(SectionName, "CommandsWindowPosY", 0f, ConfigHelpers.CreateConfigDescription("For vehicle commands window position"));
    VehicleConfigWindowPosX = config.BindUnique(SectionName, "ConfigWindowPosX", 0f, ConfigHelpers.CreateConfigDescription("For vehicle commands window position"));
    VehicleConfigWindowPosY = config.BindUnique(SectionName, "ConfigWindowPosY", 0f, ConfigHelpers.CreateConfigDescription("For vehicle commands window position"));

    ButtonFontSize = config.BindUnique(SectionName, "Debug_ButtonFontSize", 18, ConfigHelpers.CreateConfigDescription("For vehicle commands window button font"));
    TitleFontSize = config.BindUnique(SectionName, "Debug_LabelFontSize", 22, ConfigHelpers.CreateConfigDescription("For vehicle commands window font"));

    VehicleBoundsRebuildDelayPerPiece.SettingChanged += (sender, args) =>
    {
      BasePiecesController.RebuildBoundsDelayPerPiece = VehicleBoundsRebuildDelayPerPiece.Value;
    };
    BasePiecesController.RebuildBoundsDelayPerPiece = VehicleBoundsRebuildDelayPerPiece.Value;
    // onChanged
    AutoShowVehicleColliders.SettingChanged +=
      (_, _) => OnShowVehicleDebugMenuChange();
    DebugMetricsEnabled.SettingChanged += (_, _) => OnMetricsUpdate();
    DebugMetricsTimer.SettingChanged +=
      (_, _) => OnMetricsUpdate();
    VehicleDebugMenuEnabled.SettingChanged += (_, _) => OnToggleVehicleDebugMenu();
  }
}