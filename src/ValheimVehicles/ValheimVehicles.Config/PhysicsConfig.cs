using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Config;

public static class PhysicsConfig
{
  private const string SectionKey = "Vehicle Physics";

  private static string
    FloatationPhysicsSectionKey = $"{SectionKey}: Floatation";

  private static string VelocityModeSectionKey = $"{SectionKey}: Velocity Mode";

  // rudder speed
  public static ConfigEntry<float> VehicleRudderSpeedBack = null!;
  public static ConfigEntry<float> VehicleRudderSpeedSlow = null!;
  public static ConfigEntry<float> VehicleRudderSpeedHalf = null!;
  public static ConfigEntry<float> VehicleRudderSpeedFull = null!;

  // landvehicle speed
  public static ConfigEntry<float> VehicleLandSpeedBack = null!;
  public static ConfigEntry<float> VehicleLandSpeedSlow = null!;
  public static ConfigEntry<float> VehicleLandSpeedHalf = null!;
  public static ConfigEntry<float> VehicleLandSpeedFull = null!;

  // landvehicle turning
  public static ConfigEntry<float> VehicleLandTurnSpeed = null!;
  
  public static ConfigEntry<int> VehicleLandMaxTreadWidth = null!;
  public static ConfigEntry<int> VehicleLandMaxTreadLength = null!;

  // all vehicles
  public static ConfigEntry<float> VehicleCenterOfMassOffset = null!;
  public static ConfigEntry<float> VehicleLandTreadOffset = null!;

#if DEBUG
  // for debug only most of this content does not exist in prod.
  public static ConfigEntry<float> VehicleLandWheelRadius = null!;
  public static ConfigEntry<float> VehicleLandWheelMass = null!;
  public static ConfigEntry<bool> VehicleLandLockXZRotation = null!;
  public static ConfigEntry<float> VehicleLandSuspensionDistance = null!;
  public static ConfigEntry<float> VehicleLandWheelSuspensionSpring = null!;
  public static ConfigEntry<float> VehicleLandWheelSuspensionSpringDamper = null!;
  public static ConfigEntry<float> VehicleLandWheelSuspensionSpringTarget = null!;
  public static ConfigEntry<bool> DEBUG_VehicleLandShouldHideWheels = null!;
  public static ConfigEntry<bool> DEBUG_VehicleLandShouldSyncWheelPositions = null!;
#endif

  // flight
  public static ConfigEntry<float> flightAngularDamping = null!;
  public static ConfigEntry<float> flightSidewaysDamping = null!;
  public static ConfigEntry<float> flightDamping = null!;
  public static ConfigEntry<float> flightSteerForce = null!;
  public static ConfigEntry<float> flightSailForceFactor = null!;
  public static ConfigEntry<float> flightDrag = null!;
  public static ConfigEntry<float> flightAngularDrag = null!;

  public static ConfigEntry<float> landDrag = null!;
  public static ConfigEntry<float> landAngularDrag = null!;

  // water
  public static ConfigEntry<float> waterAngularDamping = null!;
  public static ConfigEntry<float> waterSidewaysDamping = null!;
  public static ConfigEntry<float> waterDamping = null!;
  public static ConfigEntry<float> waterSteerForce = null!;
  public static ConfigEntry<float> waterSailForceFactor = null!;
  public static ConfigEntry<float> waterDrag = null!;
  public static ConfigEntry<float> waterAngularDrag = null!;
  public static ConfigEntry<float> waterDeltaForceMultiplier = null!;

  public static ConfigEntry<float> submersibleAngularDamping = null!;
  public static ConfigEntry<float> submersibleSidewaysDamping = null!;
  public static ConfigEntry<float> submersibleDamping = null!;
  public static ConfigEntry<float> submersibleSteerForce = null!;
  public static ConfigEntry<float> submersibleSailForceFactor = null!;
  public static ConfigEntry<float> submersibleDrag = null!;
  public static ConfigEntry<float> submersibleAngularDrag = null!;

  // convex hull
  public static ConfigEntry<float> convexHullJoinDistanceThreshold = null!;
  public static ConfigEntry<Color> convexHullDebuggerColor = null!;
  public static ConfigEntry<bool> convexHullDebuggerForceEnabled = null!;
  public static ConfigEntry<Vector3> convexHullPreviewOffset = null!;

  // physics related to floatation and propulsion
  public static ConfigEntry<ForceMode> floatationVelocityMode = null!;
  public static ConfigEntry<ForceMode> turningVelocityMode = null!;
  public static ConfigEntry<ForceMode> sailingVelocityMode = null!;
  public static ConfigEntry<ForceMode> rudderVelocityMode = null!;
  public static ConfigEntry<ForceMode> flyingVelocityMode = null!;


  public static ConfigEntry<float> forceDistance = null!;
  public static ConfigEntry<float> force = null!;
  public static ConfigEntry<float> backwardForce = null!;

  // Camera (does not belong here)
  public static ConfigEntry<bool>
    removeCameraCollisionWithObjectsOnBoat = null!;


  public static ConfigEntry<CollisionDetectionMode>
    vehiclePiecesShipCollisionDetectionMode = null!;


  private static ConfigFile Config = null!;

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

  public enum HullFloatation
  {
    Average,
    AverageOfHullPieces,
    Center,
    Bottom,
    Top,
    Custom
  }

  public static ConfigEntry<HullFloatation> HullFloatationColliderLocation
  {
    get;
    set;
  }

  public static ConfigEntry<float> HullFloatationCustomColliderOffset
  {
    get;
    set;
  }

  public static ConfigEntry<float> MaxAngularVelocity = null!;

  public static ConfigEntry<float> MaxLinearVelocity = null!;
  public static ConfigEntry<float> MaxLinearYVelocity = null!;


  public static ConfigEntry<bool> EnableExactVehicleBounds = null!;


  private const string SailDampingExplaination =
    "Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.";

  private const string SailAngularDampingExplaination =
    "Controls how much the water pushes the boat from a vertical angle based on water and velocity. Lower values will cause more rocking and allow better turn rates. Higher values will make the vehicle more stable, but less turning angle and possibly less realistic. If you get motion-sickness this can allow tweaking sway without disabling it all and also prevent rapid turning.";

  private const string SailSidewaysDampingExplaination =
    "Controls how much the water pushes the boat sideways based on wind direction and velocity.";

  // may make this per version update as this can be very important to force reset people to defaults.
  private const string versionResetKey = ValheimRaftPlugin.Version;

  private static void OnPhysicsChangeForceUpdateAllVehiclePhysics(object sender,
    EventArgs eventArgs)
  {
    foreach (var vehicleMovementController in VehicleMovementController
               .Instances)
    {
      vehicleMovementController.UpdateVehicleStats(vehicleMovementController.GetCachedVehiclePhysicsState(), true);
    }
  }

  private static readonly AcceptableValueRange<float> StableSailForceRange =
    new(0.01f, 0.1f);

  /// <summary>
  /// Force overrides the value if any of the Physics values change after loading in a release.
  ///
  /// Also since this can trigger itself it will exit if the values are equal
  /// </summary>
  private static void ForceSetVehiclePhysics(ConfigEntry<ForceMode> entry)
  {
    if (ModEnvironment.IsRelease) return;
    if (entry.Value != ForceMode.VelocityChange)
      entry.Value = ForceMode.VelocityChange;
  }

  private static void ForceSetAllVehiclePhysics()
  {
    if (ModEnvironment.IsRelease) return;
    foreach (var velocityConfig in VelocityConfigs)
      ForceSetVehiclePhysics(velocityConfig);
  }

  private static void DebugOnlyConfig()
  {

#if DEBUG
    VehicleLandLockXZRotation = Config.Bind(PropulsionSection,
      "VehicleLand LockXZRotation",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Prevents XZ rotation on landvehicles DEBUG ONLY. Vehicles will always remain perfectly flat to horizon but this will limit upwards/downwards angular traversal", true, false));

    VehicleLandWheelMass = Config.Bind(PropulsionSection,
      "VehicleLand WheelMass",
      200f,
      ConfigHelpers.CreateConfigDescription(
        "The weight per wheel of the vehicle. This will allow more traction, but could slow down the vehicle at higher values. Experimental only", true, false, new AcceptableValueRange<float>(1f, 2000f)));
    VehicleLandWheelRadius = Config.Bind(PropulsionSection,
      "LandVehicle WheelRadius",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Wheel radius. Larger wheels have more traction. But may be less realistic.", true, false, new AcceptableValueRange<float>(0.25f, 5f)));

   
    DEBUG_VehicleLandShouldHideWheels = Config.Bind(PropulsionSection,
      "LandVehicle ShouldHideWheels",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Hides the wheel visual as wheels are not perfectly synced.", true, false));
    DEBUG_VehicleLandShouldSyncWheelPositions = Config.Bind(PropulsionSection,
      "LandVehicle ShouldSyncWheelPositions",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Toggles syncing of wheels to their actual collider position. Can cause desync with tracks.", true, false));

    VehicleLandSuspensionDistance = Config.Bind(PropulsionSection,
      "LandVehicle Suspension Distance",
      2.25f,
      ConfigHelpers.CreateConfigDescription(
        "Distance suspension distance between vehicle position and wheel position. Higher values push the vehicle up and make it more bouncy.", true, false, new AcceptableValueRange<float>(0.25f, 20f)));


    VehicleLandWheelSuspensionSpring = Config.Bind(PropulsionSection,
      "LandVehicle SuspensionSpring",
      35000f,
      ConfigHelpers.CreateConfigDescription(
        "Suspension spring value. This will control how much the vehicle bounces when it drops. No suspension will be a bit jarring but high suspension can cause lots of screen jump. Ensure a higher SuspensionSpringDamper to fix the bounce continuing.", true, false, new AcceptableValueRange<float>(0f, 50000f)));

    VehicleLandWheelSuspensionSpringDamper = Config.Bind(PropulsionSection,
      "LandVehicle SuspensionSpringDamper",
      1500f,
      ConfigHelpers.CreateConfigDescription(
        "Suspension spring damper value. This will control how much the vehicle stops bouncing. Higher values must be supplied for higher suspension spring values.", true, false, new AcceptableValueRange<float>(0f, 50000f)));

    VehicleLandWheelSuspensionSpringTarget = Config.Bind(PropulsionSection,
      "LandVehicle wheelSuspensionSpringTarget",
      0.4f,
      ConfigHelpers.CreateConfigDescription(
        "Suspension target. Between 0 and 1 it will determine the target spring position. This can allow for high suspension but also high targets", true, false, new AcceptableValueRange<float>(0f, 1f)));

    VehicleLandWheelSuspensionSpring.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandWheelSuspensionSpringDamper.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandWheelSuspensionSpringTarget.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandSuspensionDistance.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandWheelMass.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandWheelRadius.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
#endif
  }

  private static List<ConfigEntry<ForceMode>> VelocityConfigs =>
  [
    floatationVelocityMode, sailingVelocityMode, turningVelocityMode,
    flyingVelocityMode, rudderVelocityMode
  ];

  private static readonly AcceptableValueRange<float>
    maxLinearVelocityAcceptableValues =
      ModEnvironment.IsDebug
        ? new AcceptableValueRange<float>(1, 2000f)
        : new AcceptableValueRange<float>(1, 200f);

  private static readonly AcceptableValueRange<float>
    maxLinearYVelocityAcceptableValues = ModEnvironment.IsDebug
      ? new AcceptableValueRange<float>(1, 2000f)
      : new AcceptableValueRange<float>(1, 200f);

  public const string PropulsionSection = "Physics: Propulsion";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

#if DEBUG
    DebugOnlyConfig();
#endif

    VehicleRudderSpeedBack = Config.Bind(PropulsionSection, "Rudder Back Speed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Back speed of rudder, this will apply with sails", true));
    VehicleRudderSpeedSlow = Config.Bind(PropulsionSection, "Rudder Slow Speed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Slow speed of rudder, this will apply with sails", true));
    VehicleRudderSpeedHalf = Config.Bind(PropulsionSection, "Rudder Half Speed",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Half speed of rudder, this will apply with sails", true));
    VehicleRudderSpeedFull = Config.Bind(PropulsionSection, "Rudder Full Speed",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Full speed of rudder, this will apply with sails", true));

    VehicleLandSpeedBack = Config.Bind(PropulsionSection, "LandVehicle Back Speed", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Back speed of land vehicle.",
        true, false, new AcceptableValueRange<float>(0.0001f, 100f)));
    VehicleLandSpeedSlow = Config.Bind(PropulsionSection, "LandVehicle Slow Speed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Slow speed of land vehicle.",
        true, false, new AcceptableValueRange<float>(0.05f, 4f)));
    VehicleLandSpeedHalf = Config.Bind(PropulsionSection, "LandVehicle Half Speed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Half speed of land vehicle.",
        true, false, new AcceptableValueRange<float>(0.05f, 4f)));
    VehicleLandSpeedFull = Config.Bind(PropulsionSection, "LandVehicle Full Speed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the Full speed of land vehicle.",
        true, false, new AcceptableValueRange<float>(0.05f, 4f)));
    VehicleLandTurnSpeed = Config.Bind(PropulsionSection,
      "LandVehicle Turn Speed",
      0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Turn speed for landvehicles. Zero is half the normal speed, 50% is normal speed, and 100% is double normal speed.", true, false, new AcceptableValueRange<float>(0, 1f)));

    VehicleLandMaxTreadWidth = Config.Bind(PropulsionSection,
      "LandVehicle Max Tread Width",
      8,
      ConfigHelpers.CreateConfigDescription(
        "Max width the treads can expand to. Lower values will let you make motor bikes. This affects all vehicles though.", true, false, new AcceptableValueRange<int>(1, 20)));

    VehicleLandMaxTreadLength = Config.Bind(PropulsionSection,
      "LandVehicle Max Tread Length",
      20,
      ConfigHelpers.CreateConfigDescription(
        "Max length the treads can expand to.", true, false, new AcceptableValueRange<int>(4, 100)));

    VehicleCenterOfMassOffset = Config.Bind(PropulsionSection,
      "Vehicle CenterOfMassOffset",
      0.65f,
      ConfigHelpers.CreateConfigDescription(
        "Offset the center of mass by a percentage of vehicle total height. Should always be a positive number. Higher values will make the vehicle more sturdy as it will pivot lower. Too high a value will make the ship behave weirdly possibly flipping. 0 will be the center of all colliders within the physics of the vehicle. 100% will be 50% lower than the vehicle's collider. 50% will be the very bottom of the vehicle's collider.", true, true, new AcceptableValueRange<float>(0f, 1f)));



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
        true, true, StableSailForceRange);

    // flight
    flightDamping =
      Config.Bind(SectionKey, $"flightDamping_{versionResetKey}", 1f,
        dampingDescription);
    flightSidewaysDamping =
      Config.Bind(SectionKey, $"flightSidewaysDamping_{versionResetKey}", 2f,
        dampingSidewaysDescription);
    flightAngularDamping = Config.Bind(SectionKey,
      $"flightAngularDamping_{versionResetKey}", 1f,
      dampingAngularDescription);
    flightSteerForce = Config.Bind(SectionKey, "flightSteerForce", 1f,
      debugSailForceAndFactorDescription);
    flightSailForceFactor =
      Config.Bind(SectionKey, "UNSTABLE_flightSailForceFactor", 0.075f,
        debugSailForceAndFactorDescription);
    flightDrag = Config.Bind(SectionKey, "flightDrag", 1.2f);
    flightAngularDrag = Config.Bind(SectionKey, "flightAngularDrag", 1.2f);

    force = Config.Bind(SectionKey,
      $"force_{versionResetKey}", 1f,
      "EXPERIMENTAL_FORCE");
    forceDistance = Config.Bind(SectionKey,
      $"forceDistance_{versionResetKey}", 1f,
      "EXPERIMENTAL_FORCE_DISTANCE");

    backwardForce = Config.Bind(SectionKey,
      $"backwardForce_{versionResetKey}", 1f,
      "EXPERIMENTAL_BackwardFORCE");

    // water
    waterSteerForce = Config.Bind(SectionKey, "waterSteerForce", 1f);

    waterDamping = Config.Bind(SectionKey, $"waterDamping_{versionResetKey}",
      1f,
      dampingDescription);
    waterSidewaysDamping =
      Config.Bind(SectionKey, $"waterSidewaysDamping_{versionResetKey}", 2f,
        dampingSidewaysDescription);
    waterAngularDamping = Config.Bind(SectionKey,
      $"waterAngularDamping_{versionResetKey}", 1f,
      dampingAngularDescription);

    waterSailForceFactor =
      Config.Bind(SectionKey, "UNSTABLE_waterSailForceFactor", 0.05f,
        debugSailForceAndFactorDescription
      );
    waterDrag = Config.Bind(SectionKey, "waterDrag", 0.8f);
    waterAngularDrag = Config.Bind(SectionKey, "waterAngularDrag", 0.8f);

    // underwater
    submersibleDamping = Config.Bind(SectionKey,
      $"submersibleDamping_{versionResetKey}", 1f,
      dampingDescription);
    submersibleSidewaysDamping =
      Config.Bind(SectionKey, $"submersibleSidewaysDamping_{versionResetKey}",
        2f,
        dampingSidewaysDescription);
    submersibleAngularDamping =
      Config.Bind(SectionKey, $"submersibleAngularDamping_{versionResetKey}",
        1f,
        dampingAngularDescription);

    submersibleSteerForce =
      Config.Bind(SectionKey, "submersibleSteerForce", 1f);
    submersibleSailForceFactor =
      Config.Bind(SectionKey, "UNSTABLE_submersibleSailForceFactor", 0.05f,
        debugSailForceAndFactorDescription);
    submersibleDrag = Config.Bind(SectionKey, "submersibleDrag", 1.5f);
    submersibleAngularDrag =
      Config.Bind(SectionKey, "submersibleAngularDrag", 1.5f);

    var hullFloatationRange = new AcceptableValueRange<float>(-2f, 2f);
#if DEBUG
    hullFloatationRange = new AcceptableValueRange<float>(-50f, 50f);
#endif

    // landVehicles much more simple. No sails allowed etc.
    landDrag = Config.Bind(SectionKey, "landDrag", 0.05f);
    landAngularDrag = Config.Bind(SectionKey, "landAngularDrag", 1.2f);

    VehicleLandTreadOffset = Config.Bind(PropulsionSection,
      "LandVehicle TreadOffset",
      -1f,
      ConfigHelpers.CreateConfigDescription(
        "Wheel offset. Allowing for raising the treads higher. May require increasing suspension distance so the treads spawn then push the vehicle upwards. Negative lowers the wheels. Positive raises the treads", true, false, new AcceptableValueRange<float>(-10f, 10f)));
    VehicleLandTreadOffset.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();

    // guards for max values
    MaxLinearVelocity = Config.Bind(SectionKey, $"MaxVehicleLinearVelocity_{VersionedConfig.GetDynamicMinorVersionKey()}", 100f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the absolute max speed a vehicle can ever move in. This is X Y Z directions. This will prevent the ship from rapidly flying away. Try staying between 5 and 100. Higher values will increase potential of vehicle flying off to space or rapidly accelerating through objects before physics can apply to an unloaded zone.",
        true, false, maxLinearVelocityAcceptableValues));


    MaxLinearYVelocity = Config.Bind(SectionKey, "MaxVehicleLinearUpwardsVelocity",
      50f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the absolute max speed a vehicle can ever move in vertical direction. This will limit the ship capability to launch into space. Lower values are safer. Too low and the vehicle will not use gravity well",
        true, false, maxLinearYVelocityAcceptableValues));

    MaxAngularVelocity = Config.Bind(SectionKey, "MaxVehicleAngularVelocity",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the absolute max speed a vehicle can ROTATE in. Having a high value means the vehicle can spin out of control.",
        true, false, new AcceptableValueRange<float>(0.1f, 10f)));

    HullFloatationColliderLocation = Config.Bind(FloatationPhysicsSectionKey,
      "HullFloatationColliderLocation",
      HullFloatation.Custom,
      ConfigHelpers.CreateConfigDescription(
        "Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship. Custom allows for setting floatation between -20 and 20",
        true, false));

    HullFloatationCustomColliderOffset = Config.Bind(
      FloatationPhysicsSectionKey,
      "HullFloatation Custom Offset",
      0f,
      ConfigHelpers.CreateConfigDescription(
        "DEPRECATED!!! Will be removed soon, values of -2 and 2 are allowed. Anything above means you are likely not using this correctly. Please use CenterOfMass instead if your vehicle needs to pivot lower. Hull Floatation Collider Customization. Set this value and it will always make the ship float at that offset, will only work when HullFloatationColliderLocation=Custom. Positive numbers sink ship, negative will make ship float higher.",
        true, true, hullFloatationRange
      ));

    EnableExactVehicleBounds = Config.Bind(FloatationPhysicsSectionKey,
      $"EnableExactVehicleBounds_{ValheimRaftPlugin.Version}", true,
      ConfigHelpers.CreateConfigDescription(
        "Ensures that a piece placed within the raft is included in the float collider correctly. May not be accurate if the parent GameObjects are changing their scales above or below 1,1,1. Mods like Gizmo could be incompatible. This is enabled by default but may change per update if things are determined to be less stable. Changes Per mod version",
        true, true));

    vehiclePiecesShipCollisionDetectionMode = Config.Bind(
      FloatationPhysicsSectionKey,
      "vehiclePiecesShipCollisionDetectionMode",
      CollisionDetectionMode.Continuous,
      ConfigHelpers.CreateConfigDescription(
        "Set the collision mode of the vehicle ship pieces container. This the container that people walk on and use the boat. Collision Continuous will prevent people from passing through the boat. Other modes might improve performance like Discrete but cost in more jitter or lag.",
        true, true));
    vehiclePiecesShipCollisionDetectionMode.SettingChanged += (_, _) =>
    {
      foreach (var keyValuePair in VehicleShip.VehicleInstances)
      {
        if (keyValuePair.Value != null)
        {
          var pieceController = keyValuePair.Value.PiecesController;
          if (pieceController != null) pieceController.m_localRigidbody.collisionDetectionMode = vehiclePiecesShipCollisionDetectionMode.Value;
        }
      }
    };
    convexHullJoinDistanceThreshold = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullJoinDistanceThreshold",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "The threshold at which a vehicle's colliders are joined with another pieces colliders to make a singular hull. Higher numbers will join multiple pieces together into a singular hull. Lower numbers allow for splitting hulls out at the cost of performance.",
        true, true, new AcceptableValueRange<float>(0.1f, 10f)));

    convexHullDebuggerColor = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullDebuggerColor",
      new Color(0.10f, 0.23f, 0.07f, 0.5f),
      ConfigHelpers.CreateConfigDescription(
        "Allows the user to set the debugger hull color.",
        true, true));

    convexHullDebuggerForceEnabled = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullDebuggerForceEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Force enables the convex hull. This will be turned off if other commands are run or re-enabled if toggled.",
        true, true));

    convexHullPreviewOffset = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullPreviewOffset",
      new Vector3(0, 0, 0),
      ConfigHelpers.CreateConfigDescription(
        $"Sets the hull preview offset, this will allow previewing the hull side by side with your vehicle. This can only be seen if the {convexHullDebuggerForceEnabled.Definition} is true.",
        true, true));

    floatationVelocityMode = Config.Bind(VelocityModeSectionKey,
      "floatationVelocityMode", ForceMode.VelocityChange,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for",
        true, true));
    flyingVelocityMode = Config.Bind(VelocityModeSectionKey,
      "flyingVelocityMode", ForceMode.VelocityChange,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for",
        true, true));
    turningVelocityMode = Config.Bind(VelocityModeSectionKey,
      "turningVelocityMode", ForceMode.VelocityChange,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for",
        true, true));
    sailingVelocityMode = Config.Bind(VelocityModeSectionKey,
      "sailingVelocityMode", ForceMode.VelocityChange,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for",
        true, true));
    rudderVelocityMode = Config.Bind(VelocityModeSectionKey,
      "rudderVelocityMode", ForceMode.VelocityChange,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for",
        true, true));

    removeCameraCollisionWithObjectsOnBoat = Config.Bind(SectionKey,
      "EXPERIMENTAL removeCameraCollisionWithObjectsOnBoat", false,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL removes all collision of camera for objects on boat. Should significantly lower jitter when camera smashes into objects on boat it will force camera through it instead of pushing rapidly forward with vehicle force too. This will cause objects to pop in and out of view.",
        false, true));

    var waterForceDeltaMultiplierRange = ModEnvironment.IsDebug
      ? new AcceptableValueRange<float>(0.1f, 5000f)
      : new AcceptableValueRange<float>(10f, 50f);
    waterDeltaForceMultiplier = Config.Bind(SectionKey,
      "waterDeltaForceMultiplier", 50f,
      ConfigHelpers.CreateConfigDescription("Water delta force multiplier",
        true, true, waterForceDeltaMultiplierRange));

    MaxAngularVelocity.SettingChanged += (sender, args) =>
      VehicleMovementController.Instances.ForEach(x =>
        x.UpdateVehicleSpeedThrottle());
    MaxLinearVelocity.SettingChanged += (sender, args) =>
      VehicleMovementController.Instances.ForEach(x =>
        x.UpdateVehicleSpeedThrottle());
    
    VehicleLandTurnSpeed.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandMaxTreadWidth.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();
    VehicleLandMaxTreadLength.SettingChanged += (sender, args) => VehicleShip.UpdateAllWheelControllers();

    floatationVelocityMode.SettingChanged += (sender, args) =>
      ForceSetVehiclePhysics(floatationVelocityMode);
    flyingVelocityMode.SettingChanged += (sender, args) =>
      ForceSetVehiclePhysics(flyingVelocityMode);
    turningVelocityMode.SettingChanged += (sender, args) =>
      ForceSetVehiclePhysics(turningVelocityMode);
    sailingVelocityMode.SettingChanged += (sender, args) =>
      ForceSetVehiclePhysics(sailingVelocityMode);
    rudderVelocityMode.SettingChanged += (sender, args) =>
      ForceSetVehiclePhysics(rudderVelocityMode);

    ForceSetAllVehiclePhysics();

    removeCameraCollisionWithObjectsOnBoat.SettingChanged += (sender, args) =>
    {
      VehicleOnboardController.AddOrRemovePlayerBlockingCamera(
        Player.m_localPlayer);
    };

    convexHullDebuggerForceEnabled.SettingChanged += (_, __) =>
      ConvexHullComponent.UpdatePropertiesForAllComponents();
    convexHullDebuggerColor.SettingChanged += (_, __) =>
      ConvexHullComponent.UpdatePropertiesForAllComponents();
    convexHullPreviewOffset.SettingChanged += (_, __) =>
      ConvexHullComponent.UpdatePropertiesForAllComponents();

    VehicleCenterOfMassOffset.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    landAngularDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;
    landDrag.SettingChanged +=
      OnPhysicsChangeForceUpdateAllVehiclePhysics;

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