using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using BepInEx.Configuration;
using ComfyLib;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Config;

public static class WaterConfig
{
  public enum UnderwaterAccessModeType
  {
    Disabled,
    Everywhere, // would require nothing, but you could die when falling.
    OnboardOnly, // everything on or near the vehicle is considered
    DEBUG_WaterZoneOnly // everything inside a waterzone is considered. Does not require a vehicle. Not ready
  }

  public enum WaterMeshFlipModeType
  {
    Disabled,
    Everywhere,
    ExcludeOnboard,
  }

  /// <summary>
  /// Debug
  /// </summary>
  public static ConfigEntry<PrimitiveType>
    DEBUG_WaterDisplacementMeshPrimitive { get; private set; } = null!;

  public static ConfigEntry<float>
    BlockingColliderOffset { get; private set; } = null!;

  public static ConfigEntry<WaterMeshFlipModeType>
    FlipWatermeshMode { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_LiquidDepthOverride { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_WaterDepthOverride { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_LiquidCacheDepthOverride { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_SwimDepthOverride { get; private set; } = null!;

  public static ConfigEntry<bool>
    DEBUG_HasDepthOverrides { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_ManualBallastOffset { get; private set; } = null!;

  public static ConfigEntry<bool>
    DEBUG_ManualBallastOffsetEnabled { get; private set; } = null!;

  /// <summary>
  /// Modes
  /// </summary>
  public static ConfigEntry<UnderwaterAccessModeType> UnderwaterAccessMode =
    null!;


  /// <summary>
  /// Waves
  /// </summary>
  public static ConfigEntry<float> DEBUG_WaveSizeMultiplier =
    null!;

  /// <summary>
  /// Entities
  /// </summary>
  public static ConfigEntry<string> AllowedEntiesList = null!;

  public static ConfigEntry<bool> AllowTamedEntiesUnderwater = null!;


  /// <summary>
  /// Camera
  /// </summary>
  public static ConfigEntry<float> UnderwaterShipCameraZoom = null!;

  public static ConfigEntry<bool> UnderwaterFogEnabled =
    null!;

  public static ConfigEntry<Color> UnderWaterFogColor =
    null!;

  public static ConfigEntry<float> UnderWaterFogIntensity =
    null!;

  public static ConfigEntry<bool> AutoBallast =
    null!;

  public static ConfigEntry<bool> ManualBallast =
    null!;

  public static ConfigEntry<float> DEBUG_AutoBallastOffsetMultiplier = null!;
  public static ConfigEntry<float> BallastSpeed = null!;
  // public ConfigEntry<KeyboardShortcut> ManualBallastModifierKey { get; set; }

  /// <summary>
  /// Other vars
  /// </summary>
  private const string SectionKey = "Underwater";

  private const string SectionKeyDebug = $"{SectionKey}: Debug";

  private static ConfigFile Config { get; set; } = null!;

  public static List<string> AllowList = new();

  private static void OnAllowListUpdate()
  {
    AllowList.Clear();
    var listItems = AllowedEntiesList.Value.Split(',');
    foreach (var listItem in listItems)
    {
      if (listItem == "") continue;
      AllowList.Add(listItem);
    }

    WaterZoneUtils.UpdateAllowList(AllowList);
  }

  private static void OnAutoBallastToggle()
  {
    foreach (var keyValuePair in VehicleShip.AllVehicles)
    {
      var piecesController = keyValuePair.Value.PiecesController;
      var movementController = keyValuePair.Value.MovementController;
      if (piecesController != null && movementController != null)
      {
        if (piecesController != null &&
            piecesController.FloatColliderDefaultPosition !=
            movementController.FloatCollider.transform.localPosition)
        {
          movementController.FloatCollider.transform.position =
            piecesController.FloatColliderDefaultPosition;
        }

        if (piecesController != null &&
            piecesController.BlockingColliderDefaultPosition !=
            movementController.BlockingCollider.transform.localPosition)
        {
          movementController.BlockingCollider.transform.position =
            piecesController.BlockingColliderDefaultPosition;
        }
      }
    }
  }

  private static void OnCharacterWaterModeUpdate()
  {
    foreach (var sCharacter in Character.s_characters)
    {
      sCharacter.InvalidateCachedLiquidDepth();
    }

    if (UnderwaterAccessMode.Value == UnderwaterAccessModeType.Disabled)
    {
      GameCamera.instance.m_minWaterDistance = 0.3f;
    }
  }

  public static bool IsDisabled => UnderwaterAccessMode.Value ==
                                   UnderwaterAccessModeType.Disabled;

  public static void BindDebugConfig(ConfigFile config)
  {
    DEBUG_ManualBallastOffsetEnabled = config.Bind(SectionKeyDebug,
      "DEBUG_ManualBallastOffsetEnabled",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Enable manual ballast testing.",
        true, true));
    DEBUG_ManualBallastOffset = config.Bind(SectionKeyDebug,
      "DEBUG_ManualBallastOffset",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Lets you test how ballast works by setting a value.",
        true, true, new AcceptableValueRange<float>(-100f, 100f)));

    DEBUG_WaterDisplacementMeshPrimitive = config.Bind(SectionKeyDebug,
      "DEBUG_WaterDisplacementMeshPrimitive",
      PrimitiveType.Cube,
      ConfigHelpers.CreateConfigDescription(
        "Lets you choose from the water displacement mesh primitives. These will be stored as ZDOs. Not super user friendly yet...",
        true, true));

    var depthRanges = new AcceptableValueRange<float>(-10000f, 50f);

    DEBUG_HasDepthOverrides = config.Bind(SectionKeyDebug,
      "DEBUG_HasDepthOverrides",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enables depth overrides",
        true, true));
    DEBUG_WaterDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_WaterDepthOverride",
      30f,
      ConfigHelpers.CreateConfigDescription(
        "Force Overrides the WATER depth for character on boats. Useful for testing how a player can swim to the lowest depth (liquid depth).",
        true, true, depthRanges));

    DEBUG_LiquidCacheDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_LiquidCacheDepthOverride",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Force Overrides the liquid CACHED depth for character on boats.",
        true, true, depthRanges));

    DEBUG_LiquidDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_LiquidDepthOverride",
      15f,
      ConfigHelpers.CreateConfigDescription(
        "Force Overrides the LIQUID depth for character on boats.",
        true, true, depthRanges));

    DEBUG_SwimDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_SwimDepthOverride",
      15f,
      ConfigHelpers.CreateConfigDescription(
        "Force Overrides the Swim depth for character on boats. Values above the swim depth force the player into a swim animation.",
        true, true, depthRanges));

    DEBUG_AutoBallastOffsetMultiplier = Config.Bind(
      SectionKeyDebug,
      "DEBUG_AutoBallastOffsetMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Adds more balast offset",
        true, true));

    DEBUG_WaveSizeMultiplier = Config.Bind(
      SectionKey,
      "DEBUG_WaveSizeMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Make the big waves applies to DEBUG builds only. This is a direct multiplier to height might not work as expected. Debug value for fun",
        false, false, new AcceptableValueRange<float>(0, 5f)));
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    BindDebugConfig(Config);

    ManualBallast = Config.Bind(
      SectionKey,
      "ManualBallast",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Similar to flight mechanics but at sea. Defaults with Space/Jump to increase height and Sneak/Shift to decrease height uses the same flight comamnds.",
        true, true));

    AutoBallast = Config.Bind(
      SectionKey,
      "AutoBallast",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Force moves the ship's float collider to the lowest section of the boat if that section is going to smash the ground",
        true, true));

    AutoBallast.SettingChanged += (sender, args) => OnAutoBallastToggle();

    BlockingColliderOffset = Config.Bind(
      SectionKey,
      "BlockingColliderOffset",
      0.2f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the relative offset from the float collider. Can be negative or positive. Recommended is near the float collider. Slightly above it.",
        true, true, new AcceptableValueRange<float>(-10f, 10f)));

    BallastSpeed = Config.Bind(
      SectionKey,
      "BallastSpeed",
      0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Adds more balast offset",
        true, true, new AcceptableValueRange<float>(0.001f, 1)));


    AllowTamedEntiesUnderwater = Config.Bind(
      SectionKey,
      "AllowTamedEntiesUnderwater",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Lets tamed animals underwater too. Could break or kill them depending on config.",
        true, true));

    FlipWatermeshMode = Config.Bind(
      SectionKey,
      "FlipWatermeshMode",
      WaterMeshFlipModeType.Disabled,
      ConfigHelpers.CreateConfigDescription(
        "Flips the water mesh underwater. This can cause some jitters. Turn it on at your own risk. It's improve immersion. Recommended to keep off if you dislike seeing a bit of tearing in the water meshes. Flipping camera above to below surface should fix things.",
        true, true));

    UnderwaterShipCameraZoom = Config.Bind(
      SectionKey,
      "UnderwaterShipCameraZoom",
      5000f,
      ConfigHelpers.CreateConfigDescription(
        "Zoom value to allow for underwater zooming. Will allow camera to go underwater at values above 0. 0 will reset camera back to default.",
        false, false, new AcceptableValueRange<float>(0, 5000f)));

    AllowedEntiesList = Config.Bind(
      SectionKey,
      "AllowedEntiesList",
      "",
      ConfigHelpers.CreateConfigDescription(
        "List separated by comma for entities that are allowed on the ship",
        true, true));

    // AllowNonPlayerCharactersUnderwater = Config.Bind(
    //   SectionKey,
    //   "AllowNonPlayerCharactersUnderwater",
    //   true,
    //   ConfigHelpers.CreateConfigDescription(
    //     "Adds entities not considered players into the underwater onboard checks. This can cause performance issues, but if you are bringing animals it's important. Side-effects could include serpants and other water entites do not behave correctly",
    //     true));

    UnderwaterFogEnabled = Config.Bind(
      SectionKey,
      "Use Underwater Fog",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));
    UnderWaterFogColor = Config.Bind(
      SectionKey,
      "Underwater fog color",
      new Color(0.10f, 0.23f, 0.07f, 1.00f),
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));

    UnderWaterFogIntensity = Config.Bind(
      SectionKey,
      "Underwater Fog Intensity",
      0.03f,
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));

    UnderwaterAccessMode = Config.Bind(
      SectionKey,
      "UnderwaterAccessMode",
      UnderwaterAccessModeType.Disabled,
      ConfigHelpers.CreateConfigDescription(
        "Allows for walking underwater, anywhere, or onship, or eventually within the water displaced area only. Disabled with remove all water logic. DEBUG_WaterZoneOnly is not supported yet",
        true, true));

    AllowedEntiesList.SettingChanged +=
      (sender, args) => OnAllowListUpdate();
    UnderwaterAccessMode.SettingChanged +=
      (sender, args) => OnCharacterWaterModeUpdate();
    OnAllowListUpdate();
  }
}