using BepInEx.Configuration;
using ComfyLib;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Config;

public static class HudConfig
{
  private static ConfigFile Config = null!;
  public static ConfigEntry<bool> HasVehicleAnchorStateTextAboveAnchors = null!;
  public static ConfigEntry<int> HideAnchorMessageTimer = null!;

  private const string SectionKey = "Hud";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    HasVehicleAnchorStateTextAboveAnchors = Config.Bind(SectionKey,
      "HaseAnchorPrefabStateTextAboveAnchors", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows the anchored status above vehicle anchors prefab. This text will update based on state change",
        false, false));
    
    HideAnchorMessageTimer = Config.Bind(SectionKey,
      "HideAnchorStateMessageTimer", 0, ConfigHelpers.CreateConfigDescription(
        $"Hides the {HasVehicleAnchorStateTextAboveAnchors.Description} after X seconds a specific amount of time has passed. Setting this to 0 will mean it never hides",
        false, false, new AcceptableValueRange<int>(0, 20)));
    
    HasVehicleAnchorStateTextAboveAnchors.SettingChanged += (sender, args) => VehicleAnchorMechanismController.SyncHudHideAnchorMessageTimer();
    HideAnchorMessageTimer.SettingChanged += (sender, args) => VehicleAnchorMechanismController.SyncHudHideAnchorMessageTimer();
  }
}