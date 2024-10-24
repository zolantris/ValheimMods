using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public class ModSupportConfig
{
  public static ConfigEntry<bool> DynamicLocationsShouldSkipMovingPlayerToBed =
    null!;

  private const string SectionKey = "ModSupport:Generic";
  private const string DynamicLocationsKey = "ModSupport:DynamicLocations";

  private static ConfigFile Config { get; set; } = null!;

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    DynamicLocationsShouldSkipMovingPlayerToBed = Config.Bind(
      DynamicLocationsKey,
      "DynamicLocationLoginMovesPlayerToBed",
      true,
      ConfigHelpers.CreateConfigDescription(
        "login/logoff point moves player to last interacted bed or first bed on ship",
        true));
  }
}