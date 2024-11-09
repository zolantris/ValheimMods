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


  // too bad array iteration of config is refused
  // List<(ConfigEntry<float>, float)> configs =
  // [
  //   // flight
  //   (flightAngularDamping, 5f),
  //   (flightAngularDamping, 5f),
  //   (flightSidewaysDamping, 5f),
  //   (flightDamping, 5f),
  //   (flightSteerForce, 1f),
  //   (flightSailForceFactor, 0.1f),
  //   (flightDrag, 0.5f),
  //   (flightAngularDrag, 0.5f),
  //   // water
  //   (waterAngularDamping, 1f),
  //   (waterSidewaysDamping, 1f),
  //   (waterSteerForce, 1f),
  //   (waterDamping, 5f),
  //   (waterSailForceFactor, 0.05f),
  //   (waterDrag, 0.8f),
  //   (waterAngularDrag, 0.8f),
  //   // underwater
  //   (submersibleAngularDamping, 1f),
  //   (submersibleSidewaysDamping, 1f),
  //   (submersibleSteerForce, 1f),
  //   (submersibleDamping, 5f),
  //   (submersibleSailForceFactor, 0.05f),
  //   (submersibleDrag, 0.8f),
  //   (submersibleAngularDrag, 0.8f),
  // ];

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    flightAngularDamping = Config.Bind(SectionKey, "flightAngularDamping", 1f);
    flightSidewaysDamping =
      Config.Bind(SectionKey, "flightSidewaysDamping", 1f);
    flightDamping = Config.Bind(SectionKey, "flightDamping", 1f);

    flightSteerForce = Config.Bind(SectionKey, "flightSteerForce", 1f);
    flightSailForceFactor =
      Config.Bind(SectionKey, "flightSailForceFactor", 0.1f);
    flightDrag = Config.Bind(SectionKey, "flightDrag", 1.2f);
    flightAngularDrag = Config.Bind(SectionKey, "flightAngularDrag", 1.2f);

    // water
    waterSteerForce = Config.Bind(SectionKey, "waterSteerForce", 1f);

    waterSidewaysDamping =
      Config.Bind(SectionKey, "waterSidewaysDamping", 0.05f);
    waterAngularDamping = Config.Bind(SectionKey, "waterAngularDamping", 0.05f);
    waterDamping = Config.Bind(SectionKey, "waterDamping", 0.05f);

    waterSailForceFactor =
      Config.Bind(SectionKey, "waterSailForceFactor", 0.05f);
    waterDrag = Config.Bind(SectionKey, "waterDrag", 0.8f);
    waterAngularDrag = Config.Bind(SectionKey, "waterAngularDrag", 0.8f);

    // underwater
    submersibleAngularDamping =
      Config.Bind(SectionKey, "submersibleAngularDamping", 0.5f);
    submersibleSidewaysDamping =
      Config.Bind(SectionKey, "submersibleSidewaysDamping", 0.5f);
    submersibleDamping = Config.Bind(SectionKey, "submersibleDamping", 0.5f);

    submersibleSteerForce =
      Config.Bind(SectionKey, "submersibleSteerForce", 0.5f);
    submersibleSailForceFactor =
      Config.Bind(SectionKey, "submersibleSailForceFactor", 0f);
    submersibleDrag = Config.Bind(SectionKey, "submersibleDrag", 1.5f);
    submersibleAngularDrag =
      Config.Bind(SectionKey, "submersibleAngularDrag", 1.5f);
  }
}