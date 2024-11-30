using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public static class TutorialConfig
{
  private static ConfigFile Config = null!;
  public static ConfigEntry<bool> HasVehicleAnchoredWarning;

  private const string SectionKey = "Tutorial";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    HasVehicleAnchoredWarning = Config.Bind(SectionKey,
      $"HasVehicleAnchoredWarning", true,
      ZdoWatcher.ZdoWatcher.Config.ConfigHelpers.CreateConfigDescription(
        "Shows the anchored status if the player attempts to move the vehicle when anchored. A clearer way to tell players to remove the anchor so they can move.",
        false, false));
  }
}