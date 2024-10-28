using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ComfyLib;
using UnityEngine;

namespace ValheimVehicles.Config;

public class WaterConfig
{
  public enum UnderwaterAccessModeType
  {
    Disabled,
    Everywhere,
    OnboardOnly,
    WaterZoneOnly,
  }

  public static ConfigEntry<UnderwaterAccessModeType> UnderwaterAccessMode =
    null!;

  public static ConfigEntry<bool> UnderwaterFogEnabled =
    null!;

  public static ConfigEntry<bool> AllowNonPlayerCharactersUnderwater =
    null!;

  public static ConfigEntry<Color> UnderWaterFogColor =
    null!;

  public static ConfigEntry<float> UnderWaterFogIntensity =
    null!;

  public static ConfigEntry<string> AllowedEntiesList = null!;


  private const string SectionKey = "Underwater";

  private static ConfigFile Config { get; set; } = null!;

  public static List<string> AllowList = new();

  public static bool IsAllowedUnderwater(Character character)
  {
    if (character.gameObject.name == "Player(Clone)")
    {
      return true;
    }

    return AllowList.Any(x => character.gameObject.name.Contains(x));
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
      new Color(0f, 0.5f, 1f, 0.1f),
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
      UnderwaterAccessModeType.WaterZoneOnly,
      ConfigHelpers.CreateConfigDescription(
        "Allows for walking underwater anywhere. If this is enabled it will override other walking flags.",
        true));

    AllowedEntiesList.SettingChanged +=
      (sender, args) => OnAllowListUpdate();
    OnAllowListUpdate();
  }
}