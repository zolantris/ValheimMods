using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> TurnPowerNoRudder { get; private set; } = null!;
  public static ConfigEntry<float> TurnPowerWithRudder { get; private set; } = null!;
  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } = null!;

  public static ConfigEntry<bool> ShouldLiftAnchorOnSpeedChange { get; private set; } = null!;

  public static ConfigEntry<bool> AllowBaseGameSailRotation { get; private set; } = null!;

  private const string SectionName = "Propulsion";

  /// <summary>
  /// Todo migrate ValheimRaftPlugin.CreatePropulsionConfig to here
  /// </summary>
  /// <param name="config"></param>
  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    TurnPowerNoRudder = Config.Bind(SectionName, "turningPowerNoRudder", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the base turning power of the steering wheel", true));

    TurnPowerWithRudder = Config.Bind(SectionName, "turningPowerWithRudder", 6f,
      ConfigHelpers.CreateConfigDescription(
        "Set the turning power with a rudder", true));

    EnableLandVehicles = Config.Bind(SectionName, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles can now float on land. What is realism. Experimental only until wheels are invented. Must use rudder speeds to move forwards.",
        true));

    AllowBaseGameSailRotation = Config.Bind(SectionName, "enableBaseGameSailRotation", true,
      ConfigHelpers.CreateConfigDescription(
        "Lets the baseGame sails Tiers1-4 to rotate based on wind direction",
        true));

    ShouldLiftAnchorOnSpeedChange = Config.Bind(SectionName, "shouldLiftAnchorOnSpeedChange", false,
      ConfigHelpers.CreateConfigDescription(
        "Lifts the anchor when using a speed change key, this is a QOL to prevent anchor from being required to be pressed when attempting to change the ship speed"));
  }
}