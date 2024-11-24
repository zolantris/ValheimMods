using System.Collections.Generic;
using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public class PhysicsConfig
{
  public static ConfigEntry<bool> EnableCustomWaterMeshCreators =
    null!;

  public static ConfigEntry<bool> EnableCustomWaterMeshTestPrefabs =
    null!;

  private const string SectionKey = "Vehicle Physics";

  // flight
  public static ConfigEntry<float> flightAngularDamping = null!;
  public static ConfigEntry<float> flightSidewaysDamping = null!;
  public static ConfigEntry<float> flightDamping = null!;
  public static ConfigEntry<float> flightSteerForce = null!;
  public static ConfigEntry<float> flightSailForceFactor = null!;
  public static ConfigEntry<float> flightDrag = null!;
  public static ConfigEntry<float> flightAngularDrag = null!;

  // water
  public static ConfigEntry<float> waterAngularDamping = null!;
  public static ConfigEntry<float> waterSidewaysDamping = null!;
  public static ConfigEntry<float> waterDamping = null!;
  public static ConfigEntry<float> waterSteerForce = null!;
  public static ConfigEntry<float> waterSailForceFactor = null!;
  public static ConfigEntry<float> waterDrag = null!;
  public static ConfigEntry<float> waterAngularDrag = null!;

  public static ConfigEntry<float> submersibleAngularDamping = null!;
  public static ConfigEntry<float> submersibleSidewaysDamping = null!;
  public static ConfigEntry<float> submersibleDamping = null!;
  public static ConfigEntry<float> submersibleSteerForce = null!;
  public static ConfigEntry<float> submersibleSailForceFactor = null!;
  public static ConfigEntry<float> submersibleDrag = null!;
  public static ConfigEntry<float> submersibleAngularDrag = null!;


  private static ConfigFile Config { get; set; } = null!;

  private static AcceptableValueRange<float> SafeDampingRangeWaterVehicle =
    new AcceptableValueRange<float>(0.5f, 1f);

  private const string SailDampingExplaination =
    "Controls how much the wind pushes the boat in the direction of the wind. Lower values will allow the wind to push signficantly harder in the direction it faces. Recommended value is 0.5f for watervehicles. Air vehicles should be lower numbers but it makes them hard to use with sails so setting to 1f is recommended. Noting that 1f is unrealistic but makes sailing much more relaxed without requiring tacking.";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    var dampingDescription = ConfigHelpers.CreateConfigDescription(
      SailDampingExplaination,
      true, false, SafeDampingRangeWaterVehicle);

    flightAngularDamping = Config.Bind(SectionKey, "flightAngularDamping", 1f,
      dampingDescription);
    flightSidewaysDamping =
      Config.Bind(SectionKey, "flightSidewaysDamping", 1f, dampingDescription);
    flightDamping =
      Config.Bind(SectionKey, "flightDamping", 1f, dampingDescription);

    flightSteerForce = Config.Bind(SectionKey, "flightSteerForce", 1f);
    flightSailForceFactor =
      Config.Bind(SectionKey, "flightSailForceFactor", 0.1f);
    flightDrag = Config.Bind(SectionKey, "flightDrag", 1.2f);
    flightAngularDrag = Config.Bind(SectionKey, "flightAngularDrag", 1.2f);

    // water
    waterSteerForce = Config.Bind(SectionKey, "waterSteerForce", 1f);

    waterSidewaysDamping =
      Config.Bind(SectionKey, "waterSidewaysDamping", 0.5f,
        dampingDescription);
    waterAngularDamping = Config.Bind(SectionKey, "waterAngularDamping", 0.5f,
      dampingDescription);
    waterDamping = Config.Bind(SectionKey, "waterDamping", 0.5f,
      dampingDescription);

    waterSailForceFactor =
      Config.Bind(SectionKey, "waterSailForceFactor", 0.05f);
    waterDrag = Config.Bind(SectionKey, "waterDrag", 0.8f);
    waterAngularDrag = Config.Bind(SectionKey, "waterAngularDrag", 0.8f);

    // underwater
    submersibleAngularDamping =
      Config.Bind(SectionKey, "submersibleAngularDamping", 1f,
        dampingDescription);
    submersibleSidewaysDamping =
      Config.Bind(SectionKey, "submersibleSidewaysDamping", 1f,
        dampingDescription);
    submersibleDamping = Config.Bind(SectionKey, "submersibleDamping", 1f,
      dampingDescription);

    submersibleSteerForce =
      Config.Bind(SectionKey, "submersibleSteerForce", 0.5f);
    submersibleSailForceFactor =
      Config.Bind(SectionKey, "submersibleSailForceFactor", 0f);
    submersibleDrag = Config.Bind(SectionKey, "submersibleDrag", 1.5f);
    submersibleAngularDrag =
      Config.Bind(SectionKey, "submersibleAngularDrag", 1.5f);
  }
}