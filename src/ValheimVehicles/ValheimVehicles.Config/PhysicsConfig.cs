using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Constants;

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

  private static AcceptableValueRange<float>
    SafeDampingRangeWaterVehicle =
      new(0.05f,
        ModEnvironment.IsDebug ? 5000f : 1f);

  private static AcceptableValueRange<float>
    SafeAngularDampingRangeWaterVehicle =
      new(0.05f,
        ModEnvironment.IsDebug
          ? 5000f
          : 1f);

  private static AcceptableValueRange<float>
    SafeSidewaysDampingRangeWaterVehicle =
      new(0.5f,
        ModEnvironment.IsDebug
          ? 5000f
          : 5f);

  private const string SailDampingExplaination =
    "Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.";

  private const string SailAngularDampingExplaination =
    "Controls how much the water pushes the boat from a vertical angle based on water and velocity. Lower values will cause more rocking and allow better turn rates. Higher values will make the vehicle more stable, but less turning angle and possibly less realistic. If you get motion-sickness this can allow tweaking sway without disabling it all and also prevent rapid turning.";

  private const string SailSidewaysDampingExplaination =
    "Controls how much the water pushes the boat sideways based on wind direction and velocity.";

  // may make this per version update as this can be very important to force reset people to defaults.
  private const string DampingResetKey = "2.4.2";

  private static void OnPhysicsChangeForceUpdateAllVehiclePhysics(object sender,
    EventArgs eventArgs)
  {
    foreach (var vehicleMovementController in VehicleMovementController
               .Instances)
    {
      vehicleMovementController.UpdateVehicleStats(
        vehicleMovementController.IsFlying(),
        vehicleMovementController.IsSubmerged(), true);
    }
  }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    var dampingSidewaysDescription = ConfigHelpers.CreateConfigDescription(
      SailSidewaysDampingExplaination,
      true, true, SafeSidewaysDampingRangeWaterVehicle);

    var dampingAngularDescription = ConfigHelpers.CreateConfigDescription(
      SailAngularDampingExplaination,
      true, true, SafeAngularDampingRangeWaterVehicle);

    var dampingDescription = ConfigHelpers.CreateConfigDescription(
      SailDampingExplaination,
      true, true, SafeDampingRangeWaterVehicle);

    var debugSailForceAndFactorDescription =
      ConfigHelpers.CreateConfigDescription(
        "DEBUG, tweak sailing math. Not supported or tested. Do not mess with defaults. Do not use this unless you know what your doing.",
        true, true);

    // flight
    flightDamping =
      Config.Bind(SectionKey, $"flightDamping_{DampingResetKey}", 1f,
        dampingDescription);
    flightSidewaysDamping =
      Config.Bind(SectionKey, $"flightSidewaysDamping_{DampingResetKey}", 2f,
        dampingSidewaysDescription);
    flightAngularDamping = Config.Bind(SectionKey,
      $"flightAngularDamping_{DampingResetKey}", 1f,
      dampingAngularDescription);

    flightSteerForce = Config.Bind(SectionKey, "flightSteerForce", 1f,
      debugSailForceAndFactorDescription);
    flightSailForceFactor =
      Config.Bind(SectionKey, "flightSailForceFactor", 0.1f,
        debugSailForceAndFactorDescription);
    flightDrag = Config.Bind(SectionKey, "flightDrag", 1.2f);
    flightAngularDrag = Config.Bind(SectionKey, "flightAngularDrag", 1.2f);

    // water
    waterSteerForce = Config.Bind(SectionKey, "waterSteerForce", 1f);

    waterDamping = Config.Bind(SectionKey, $"waterDamping_{DampingResetKey}",
      1f,
      dampingDescription);
    waterSidewaysDamping =
      Config.Bind(SectionKey, $"waterSidewaysDamping_{DampingResetKey}", 2f,
        dampingSidewaysDescription);
    waterAngularDamping = Config.Bind(SectionKey,
      $"waterAngularDamping_{DampingResetKey}", 1f,
      dampingAngularDescription);

    waterSailForceFactor =
      Config.Bind(SectionKey, "waterSailForceFactor", 0.05f,
        debugSailForceAndFactorDescription
      );
    waterDrag = Config.Bind(SectionKey, "waterDrag", 0.8f);
    waterAngularDrag = Config.Bind(SectionKey, "waterAngularDrag", 0.8f);

    // underwater
    submersibleDamping = Config.Bind(SectionKey,
      $"submersibleDamping_{DampingResetKey}", 1f,
      dampingDescription);
    submersibleSidewaysDamping =
      Config.Bind(SectionKey, $"submersibleSidewaysDamping_{DampingResetKey}",
        5f,
        dampingSidewaysDescription);
    submersibleAngularDamping =
      Config.Bind(SectionKey, $"submersibleAngularDamping_{DampingResetKey}",
        1f,
        dampingAngularDescription);

    submersibleSteerForce =
      Config.Bind(SectionKey, "submersibleSteerForce", 0.5f);
    submersibleSailForceFactor =
      Config.Bind(SectionKey, "submersibleSailForceFactor", 0f,
        debugSailForceAndFactorDescription);
    submersibleDrag = Config.Bind(SectionKey, "submersibleDrag", 1.5f);
    submersibleAngularDrag =
      Config.Bind(SectionKey, "submersibleAngularDrag", 1.5f);

    flightDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightAngularDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightAngularDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightSidewaysDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    flightSailForceFactor.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;

    waterDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterAngularDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterAngularDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterSidewaysDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    waterSailForceFactor.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;

    submersibleDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleAngularDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleAngularDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleSidewaysDamping.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleSteerForce.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    submersibleSailForceFactor.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
  }
}