using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using BepInEx.Configuration;
using ComfyLib;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
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
    UNSAFE_BlockingColliderOffset { get; private set; } = null!;

  public static ConfigEntry<WaterMeshFlipModeType>
    FlipWatermeshMode { get; private set; } = null!;
  
  public static ConfigEntry<bool>
    HasUnderwaterHullBubbleEffect { get; private set; } = null!;
  
  public static ConfigEntry<Color>
    UnderwaterBubbleEffectColor { get; private set; } = null!;

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
  public static ConfigEntry<bool> AllowMonsterEntitesUnderwater = null!;


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

  public static ConfigEntry<bool> WaterBallastEnabled =
    null!;

  public static ConfigEntry<bool>
    EXPERIMENTAL_AboveSurfaceBallastUsesShipMass = null!;

  public static ConfigEntry<float>
    AboveSurfaceBallastMaxShipSizeAboveWater = null!;

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
    DEBUG_WaterDisplacementMeshPrimitive = config.Bind(SectionKeyDebug,
      "DEBUG_WaterDisplacementMeshPrimitive",
      PrimitiveType.Cube,
      ConfigHelpers.CreateConfigDescription(
        "Lets you choose from the water displacement mesh primitives. These will be stored as ZDOs. Not super user friendly yet...",
        true, true));

    var depthRanges = new AcceptableValueRange<float>(0f, 50f);

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

    DEBUG_WaveSizeMultiplier = Config.Bind(
      SectionKey,
      "DEBUG_WaveSizeMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Make the big waves applies to DEBUG builds only. This is a direct multiplier to height might not work as expected. Debug value for fun",
        false, false, new AcceptableValueRange<float>(0, 5f)));
  }

  private const string aboveSurfaceConfigKey =
    "EXPERIMENTAL_AboveSurfaceBallastUsesShipMass";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    BindDebugConfig(Config);

    WaterBallastEnabled = Config.Bind(
      SectionKey,
      "WaterBallastEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Similar to flight mechanics but at sea. Defaults with Space/Jump to increase height and Sneak/Shift to decrease height uses the same flight comamnds.",
        true, true));

    AboveSurfaceBallastMaxShipSizeAboveWater = Config.Bind(
      SectionKey,
      "AboveSurfaceBallastMaxShipSizeAboveWater",
      0.5f,
      ConfigHelpers.CreateConfigDescription(
        $"A fixed value to set for all vehicles. Will not be applied if config <{aboveSurfaceConfigKey}> is enabled and the ship weight is more than this value. Set it to 100% to always allow the full height of the ship above the surface.",
        true, true, new AcceptableValueRange<float>(0f, 1f)));

    EXPERIMENTAL_AboveSurfaceBallastUsesShipMass = Config.Bind(
      SectionKey,
      "EXPERIMENTAL_AboveSurfaceBallastUsesShipMass",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Ships with high mass to volume will not be able to ballast well above the surface. This adds a ship mass to onboard volume calculation. The calculation is experimental so it might be inaccurate. For now mass to volume includes the length width heigth in a box around the ship. It's unrealistic as of 2.4.0 as this includes emptyspace above water. Eventually this will be calculated via displacement (empty volume with wall all around it) calculation.",
        true, true));

    UNSAFE_BlockingColliderOffset = Config.Bind(
      SectionKey,
      "UNSAFE_BlockingColliderOffset",
      0.2f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the relative offset from the float collider. Can be negative or positive. Recommended is near the float collider. Slightly above it. Leave this alone unless you know what you are doing.",
        true, true, new AcceptableValueRange<float>(-10f, 10f)));

    AllowMonsterEntitesUnderwater = Config.Bind(
      SectionKey,
      "AllowMonsterEntitesUnderwater",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Allows Monster entities onto the ship and underwater. This means they can go underwater similar to player.",
        true, true));

    AllowedEntiesList = Config.Bind(
      SectionKey,
      "AllowedEntiesList",
      "",
      ConfigHelpers.CreateConfigDescription(
        "List separated by comma for entities that are allowed on the ship. For simplicity consider enabling monsters and tame creatures.",
        true, true));

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

    UnderwaterFogEnabled = Config.Bind(
      SectionKey,
      "UnderwaterFogEnabled",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));
    UnderWaterFogColor = Config.Bind(
      SectionKey,
      "UnderwaterFogColor",
      new Color(0.10f, 0.23f, 0.07f, 1.00f),
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));

    UnderWaterFogIntensity = Config.Bind(
      SectionKey,
      "UnderwaterFogIntensity",
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
    
    HasUnderwaterHullBubbleEffect = Config.Bind(
      SectionKey,
      "HasUnderwaterHullBubbleEffect",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds an underwater bubble conforming around the vehicle hull. Allowing for a underwater like distortion effect without needing to use fog.",
        true, true));
    
    UnderwaterBubbleEffectColor = Config.Bind(
      SectionKey,
      "UnderwaterBubbleEffectColor",
      new Color(0f, 0.4f, 0.4f, 0.8f),
      ConfigHelpers.CreateConfigDescription(
        "Set the underwater bubble color",
        true));

    UnderwaterBubbleEffectColor.SettingChanged +=  (sender, args) =>
      ConvexHullComponent.UpdatePropertiesForAllComponents();
    
    AllowedEntiesList.SettingChanged +=
      (sender, args) => OnAllowListUpdate();
    UnderwaterAccessMode.SettingChanged +=
      (sender, args) => OnCharacterWaterModeUpdate();
    OnAllowListUpdate();
  }
}