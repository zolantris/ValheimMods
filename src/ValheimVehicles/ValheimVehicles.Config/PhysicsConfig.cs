using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Constants;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Config;

public static class PhysicsConfig
{
  public static ConfigEntry<bool> EnableCustomWaterMeshCreators =
    null!;

  public static ConfigEntry<bool> EnableCustomWaterMeshTestPrefabs =
    null!;

  private const string SectionKey = "Vehicle Physics";

  private static string
    FloatationPhysicsSectionKey = $"{SectionKey}: Floatation";

  private static string VelocityModeSectionKey = $"{SectionKey}: Velocity Mode";

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


  public static ConfigEntry<CollisionDetectionMode>
    vehiclePiecesShipCollisionDetectionMode = null!;


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

  public enum HullFloatation
  {
    Average,
    AverageOfHullPieces,
    Center,
    Bottom,
    Top,
    Custom,
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


  public static ConfigEntry<bool> EnableExactVehicleBounds { get; set; }


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

  private static readonly AcceptableValueRange<float> StableSailForceRange =
    new(0.01f, 0.1f);

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
        true, true, StableSailForceRange);

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


    forceDistance = Config.Bind(SectionKey,
      $"forceDistance_{DampingResetKey}", 5f,
      "EXPERIMENTAL_FORCE_DISTANCE");
    force = Config.Bind(SectionKey,
      $"force_{DampingResetKey}", 3f,
      "EXPERIMENTAL_FORCE");

    backwardForce = Config.Bind(SectionKey,
      $"backwardForce_{DampingResetKey}", 1f,
      "EXPERIMENTAL_BackwardFORCE");

    flightSteerForce = Config.Bind(SectionKey, "flightSteerForce", 1f,
      debugSailForceAndFactorDescription);
    flightSailForceFactor =
      Config.Bind(SectionKey, "UNSTABLE_flightSailForceFactor", 0.075f,
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
      Config.Bind(SectionKey, "UNSTABLE_waterSailForceFactor", 0.05f,
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
        2f,
        dampingSidewaysDescription);
    submersibleAngularDamping =
      Config.Bind(SectionKey, $"submersibleAngularDamping_{DampingResetKey}",
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

    var hullFloatationRange = new AcceptableValueRange<float>(-20f, 20f);
#if DEBUG
    hullFloatationRange = new AcceptableValueRange<float>(-50f, 50f);
#endif

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
        "Hull Floatation Collider Customization. Set this value and it will always make the ship float at that offset, will only work when HullFloatationColliderLocation=Custom. Positive numbers sink ship, negative will make ship float higher.",
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

    convexHullJoinDistanceThreshold = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullJoinDistanceThreshold",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "The threshold at which a vehicle's colliders are joined with another pieces colliders to make a singular hull. Higher numbers will join multiple pieces together into a singular hull. Lower numbers allow for splitting hulls out at the cost of performance.",
        true, true, new AcceptableValueRange<float>(0.1f, 10f)));

    convexHullDebuggerColor = Config.Bind(FloatationPhysicsSectionKey,
      "convexHullDebuggerColor",
      new Color(0, 0.60f, 0.60f, 0.20f),
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
      "floatationVelocityMode", ForceMode.Force,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode override so mass and vehicle size are accounted for",
        true, true));
    flyingVelocityMode = Config.Bind(VelocityModeSectionKey,
      "flyingVelocityMode", ForceMode.Force,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode override so mass and vehicle size are accounted for",
        true, true));
    turningVelocityMode = Config.Bind(VelocityModeSectionKey,
      "turningVelocityMode", ForceMode.Force,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode override so mass and vehicle size are accounted for",
        true, true));
    sailingVelocityMode = Config.Bind(VelocityModeSectionKey,
      "sailingVelocityMode", ForceMode.Force,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode override so mass and vehicle size are accounted for",
        true, true));
    rudderVelocityMode = Config.Bind(VelocityModeSectionKey,
      "rudderVelocityMode", ForceMode.Force,
      ConfigHelpers.CreateConfigDescription(
        "EXPERIMENTAL VelocityMode override so mass and vehicle size are accounted for",
        true, true));


    convexHullDebuggerForceEnabled.SettingChanged += (_, __) =>
      ConvexHullAPI.UpdatePropertiesForConvexHulls(
        convexHullPreviewOffset.Value,
        convexHullDebuggerForceEnabled.Value, convexHullDebuggerColor
          .Value);
    convexHullDebuggerColor.SettingChanged += (_, __) =>
      ConvexHullAPI.UpdatePropertiesForConvexHulls(
        convexHullPreviewOffset.Value,
        convexHullDebuggerForceEnabled.Value, convexHullDebuggerColor
          .Value);
    convexHullPreviewOffset.SettingChanged += (_, __) =>
      ConvexHullAPI.UpdatePropertiesForConvexHulls(
        convexHullPreviewOffset.Value,
        convexHullDebuggerForceEnabled.Value, convexHullDebuggerColor
          .Value);


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