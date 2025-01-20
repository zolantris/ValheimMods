using BepInEx.Configuration;
using ComfyLib;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Config;

public static class HudConfig
{
  private static ConfigFile Config = null!;
  public static ConfigEntry<bool> HudAnchorTextAboveAnchors = null!;
  public static ConfigEntry<int> HudAnchorMessageTimer = null!;
  public static ConfigEntry<int> HudAnchorTextSize = null!;

  private const string SectionKey = "Hud";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    HudAnchorTextAboveAnchors = Config.Bind(SectionKey,
      "HudAnchorTextAboveAnchors", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows the anchored status above vehicle anchors prefab. This text will update based on state change",
        false, false));

    HudAnchorMessageTimer = Config.Bind(SectionKey,
      "HideAnchorStateMessageTimer", 3, ConfigHelpers.CreateConfigDescription(
        $"Hides the {HudAnchorTextAboveAnchors.Description} after X seconds a specific amount of time has passed. Setting this to 0 will mean it never hides",
        false, false, new AcceptableValueRange<int>(0, 20)));

    HudAnchorTextSize = Config.Bind(SectionKey,
      "HudAnchorTextSize", AnchorMechanismController.baseTextSize,
      ConfigHelpers.CreateConfigDescription(
        $"Sets the anchor text size. Potentially Useful for those with different monitor sizes",
        false, false, new AcceptableValueRange<int>(0, 20)));

    HudAnchorTextAboveAnchors.SettingChanged += (sender, args) =>
      VehicleAnchorMechanismController.SyncHudAnchorValues();
    HudAnchorMessageTimer.SettingChanged += (sender, args) =>
      VehicleAnchorMechanismController.SyncHudAnchorValues();
  }
}