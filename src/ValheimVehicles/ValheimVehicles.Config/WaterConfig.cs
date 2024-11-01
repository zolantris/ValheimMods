using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using BepInEx.Configuration;
using ComfyLib;
using UnityEngine;

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

  public static ConfigEntry<PrimitiveType>
    DEBUG_WaterDisplacementMeshPrimitive { get; private set; } = null!;

  public static ConfigEntry<WaterMeshFlipModeType>
    FlipWatermeshMode { get; private set; } = null!;

  public static ConfigEntry<float>
    DEBUG_LiquidDepthOverride { get; private set; } = null!;

  public static ConfigEntry<bool>
    DEBUG_HasLiquidDepthOverride { get; private set; } = null!;

  public static ConfigEntry<UnderwaterAccessModeType> UnderwaterAccessMode =
    null!;

  public static ConfigEntry<float> UnderwaterMaxCachedDiveDepth =
    null!;

  public static ConfigEntry<float> UnderwaterMaxDiveDepth =
    null!;

  public static ConfigEntry<bool> UnderwaterFogEnabled =
    null!;

  // public static ConfigEntry<bool> AllowNonPlayerCharactersUnderwater =
  //   null!;

  public static ConfigEntry<Color> UnderWaterFogColor =
    null!;

  public static ConfigEntry<float> UnderWaterFogIntensity =
    null!;

  public static ConfigEntry<float> WaveSizeMultiplier =
    null!;

  public static ConfigEntry<string> AllowedEntiesList = null!;
  public static ConfigEntry<float> UnderwaterShipCameraZoom = null!;


  private const string SectionKey = "Underwater";
  private const string SectionKeyDebug = $"{SectionKey}: Debug";

  private static ConfigFile Config { get; set; } = null!;

  public static List<string> AllowList = new();

  public static bool IsAllowedUnderwater(Character character)
  {
    if (character == null || character.gameObject == null) return false;
    if (character.gameObject.name == "Player(Clone)")
    {
      return true;
    }

    return AllowList.Any(x =>
    {
      if (x == "") return false;
      return x == character.gameObject.name;
    });
  }

  private static void OnAllowListUpdate()
  {
    AllowList.Clear();
    var listItems = AllowedEntiesList.Value.Split(',');
    foreach (var listItem in listItems)
    {
      if (listItem == "") continue;
      AllowList.Add(listItem);
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


  public static void BindConfig(ConfigFile config)
  {
    Config = config;


    DEBUG_WaterDisplacementMeshPrimitive = config.Bind(SectionKeyDebug,
      "DEBUG_WaterDisplacementMeshPrimitive",
      PrimitiveType.Cube,
      ConfigHelpers.CreateConfigDescription(
        "Lets you choose from the water displacement mesh primitives. These will be stored as ZDOs. Not super user friendly yet...",
        true, true));

    DEBUG_HasLiquidDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_HasLiquidDepthOverride",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enables liquid depth overrides",
        true, true));
    DEBUG_LiquidDepthOverride = config.Bind(SectionKeyDebug,
      "DEBUG_LiquidDepthOverride",
      15f,
      ConfigHelpers.CreateConfigDescription(
        "Force Overrides the liquid depth for character on boats. Likely will cause bobbing effect as it fights the onboard collider if it is below it.",
        true, true, new AcceptableValueRange<float>(0f, 30f)));

    FlipWatermeshMode = Config.Bind(
      SectionKey,
      "FlipWatermeshMode",
      WaterMeshFlipModeType.Disabled,
      ConfigHelpers.CreateConfigDescription(
        "Flips the water mesh underwater. This can cause some jitters. Turn it on at your own risk. It's improve immersion. Recommended to keep off for now while onboard to prevent underwater jitters due to camera colliding rapidly when water flips",
        true, true));

    UnderwaterMaxDiveDepth = Config.Bind(
      SectionKey,
      "UnderwaterMaxDiveDepth",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Enforce a max depth for diving values higher will force the player to float to that value.",
        false, false, new AcceptableValueRange<float>(0, 50f)));
    UnderwaterMaxCachedDiveDepth = Config.Bind(
      SectionKey,
      "UnderwaterMaxCachedDiveDepth",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Enforce a max sinking depth for diving. Higher values can make the player swim to the depth instead of fall through the water. Recommended lower than 20",
        false, false, new AcceptableValueRange<float>(0, 50f)));
    WaveSizeMultiplier = Config.Bind(
      SectionKey,
      "WaveSizeMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Make the big waves. This is a direct multiplier to height",
        false, false, new AcceptableValueRange<float>(0, 5f)));

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
      new Color(0f, 0.57f, 0.6f),
      ConfigHelpers.CreateConfigDescription(
        "Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.",
        true));

    UnderWaterFogIntensity = Config.Bind(
      SectionKey,
      "Underwater Fog Intensity",
      0.2f,
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