using System.Collections.Generic;
using System.Linq;
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
    OnboardOnly,
    // WaterZoneOnly // would require mesh collider, not ready
  }

  public static ConfigEntry<UnderwaterAccessModeType> UnderwaterAccessMode =
    null!;

  public static ConfigEntry<float> UnderwaterMaxCachedDiveDepth =
    null!;

  public static ConfigEntry<float> UnderwaterMaxDiveDepth =
    null!;

  public static ConfigEntry<bool> UnderwaterFogEnabled =
    null!;

  public static ConfigEntry<bool> AllowNonPlayerCharactersUnderwater =
    null!;

  public static ConfigEntry<Color> UnderWaterFogColor =
    null!;

  public static ConfigEntry<float> UnderWaterFogIntensity =
    null!;

  public static ConfigEntry<float> WaveSizeMultiplier =
    null!;

  public static ConfigEntry<string> AllowedEntiesList = null!;
  public static ConfigEntry<float> UnderwaterShipCameraZoom = null!;


  private const string SectionKey = "Underwater";

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
    if (listItems.Length > 0)
    {
      AllowList.AddRange(listItems);
    }
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

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
        "Make the big waves.",
        false, false, new AcceptableValueRange<float>(0, 50f)));

    UnderwaterShipCameraZoom = Config.Bind(
      SectionKey,
      "UnderwaterShipCameraZoom",
      500f,
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

    AllowNonPlayerCharactersUnderwater = Config.Bind(
      SectionKey,
      "AllowNonPlayerCharactersUnderwater",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds entities not considered players into the underwater onboard checks. This can cause performance issues, but if you are bringing animals it's important. Side-effects could include serpants and other water entites do not behave correctly",
        true));

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
      UnderwaterAccessModeType.OnboardOnly,
      ConfigHelpers.CreateConfigDescription(
        "Allows for walking underwater anywhere. If this is enabled it will override other walking flags.",
        true));

    AllowedEntiesList.SettingChanged +=
      (sender, args) => OnAllowListUpdate();
    OnAllowListUpdate();
  }
}