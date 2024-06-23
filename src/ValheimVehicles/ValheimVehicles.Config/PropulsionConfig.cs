using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> TurnPowerNoRudder { get; set; }
  public static ConfigEntry<float> TurnPowerWithRudder { get; set; }

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
    TurnPowerWithRudder = Config.Bind(SectionName, "turningPowerWithRudder", 3f,
      ConfigHelpers.CreateConfigDescription(
        "Set the turning power with a rudder", true));
  }
}