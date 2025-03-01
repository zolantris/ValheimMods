using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Propulsion.Sail;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Enums;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> FlightClimbingOffset { get; private set; } =
    null!;

  public static ConfigEntry<float> BallastClimbingOffset { get; private set; } =
    null!;
  public static ConfigEntry<float> SailingMassPercentageFactor { get; set; }
  public static ConfigEntry<bool> AllowFlight { get; set; }

  // Propulsion Configs
  public static ConfigEntry<bool> EnableCustomPropulsionConfig { get; set; }

  public static ConfigEntry<float> MaxSailSpeed { get; set; }
  public static ConfigEntry<float> SpeedCapMultiplier { get; set; }


  public static ConfigEntry<bool> FlightVerticalToggle { get; set; }
  public static ConfigEntry<bool> FlightHasRudderOnly { get; set; }


  public static ConfigEntry<float> SailTier1Area { get; set; }
  public static ConfigEntry<float> SailTier2Area { get; set; }
  public static ConfigEntry<float> SailTier3Area { get; set; }
  public static ConfigEntry<float> SailTier4Area { get; set; }
  public static ConfigEntry<float> SailCustomAreaTier1Multiplier { get; set; }

  public static ConfigEntry<bool> ShowShipStats { get; set; }
  
  public static ConfigEntry<float> VerticalSmoothingSpeed
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<bool> EXPERIMENTAL_LeanTowardsWindSailDirection
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<float>
    EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle { get; private set; } =
    null!;

  public static ConfigEntry<float> TurnPowerNoRudder { get; private set; } =
    null!;

  public static ConfigEntry<float> TurnPowerWithRudder { get; private set; } =
    null!;

  public static ConfigEntry<bool> SlowAndReverseWithoutControls
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<bool> ShouldLiftAnchorOnSpeedChange
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<float> WheelDeadZone { get; private set; } = null!;

  public static ConfigEntry<bool> AllowBaseGameSailRotation
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<VehiclePhysicsMode> DefaultPhysicsMode = null!;

  private const string SectionName = "Propulsion";

  /// <summary>
  /// Todo migrate ValheimRaftPlugin.CreatePropulsionConfig to here
  /// </summary>
  /// <param name="config"></param>
  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    AllowFlight = Config.Bind<bool>(SectionName, "AllowFlight", false,
      ConfigHelpers.CreateConfigDescription(
        "Allow the raft to fly (jump\\crouch to go up and down)", true));
    SailingMassPercentageFactor = Config.Bind(SectionName, "MassPercentage", 0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the mass percentage of the ship that will slow down the sails",
        true, false, new AcceptableValueRange<float>(0f, 1f)));
    
    DefaultPhysicsMode = Config.Bind(SectionName,
      "VehiclePhysicsMode", VehiclePhysicsMode.ForceSyncedRigidbody,
      ConfigHelpers.CreateConfigDescription(
        // "SyncRigidbody - Accurately syncs physics across clients, it causes jitters during high speed.\n" +
        "ForceSyncedRigidbody ignores all allowances that toggle SyncRigidbody related to flight. This will require a flight ascend value of 1 otherwise flight will be broken. Use this is there is problems with SyncRigidbody\n" +
        "Other methods removed after 2.5.0",
        // "DesyncedJointRigidbodyBody - is a new UNSTABLE (you have been warned) config that allows the player to smoothly move around the raft at high speeds even if they are not the host. Can cause the ship to glitch with anything that has to do with physics including ramps and other mods that add moving parts that could be added to the boat.",
        true));

    EXPERIMENTAL_LeanTowardsWindSailDirection = Config.Bind(SectionName,
      "EXPERIMENTAL_LeanTowardsWindSailDirection", false,
      ConfigHelpers.CreateConfigDescription(
        "Toggles a lean while sailing with wind power. Cosmetic only and does not work in multiplayer yet. Warning for those with motion sickness, this will increase your symptoms. Prepare your dramamine!",
        true, true));

    EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle = Config.Bind(SectionName,
      "EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle", 10f,
      ConfigHelpers.CreateConfigDescription(
        "Set the max lean angle when wind is hitting sides directly", true,
        true, new AcceptableValueRange<float>(0f, 30f)));

    TurnPowerNoRudder = Config.Bind(SectionName, "turningPowerNoRudder", 0.7f,
      ConfigHelpers.CreateConfigDescription(
        "Set the base turning power of the steering wheel without a rudder",
        true));

    TurnPowerWithRudder = Config.Bind(SectionName, "turningPowerWithRudder", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the turning power with a rudder prefab attached to the boat. This value overrides the turningPowerNoRudder config.",
        true));

    SlowAndReverseWithoutControls = Config.Bind(SectionName,
      "slowAndReverseWithoutControls", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles do not require controls while in slow and reverse with a person on them",
        true));

    AllowBaseGameSailRotation = Config.Bind(SectionName,
      "enableBaseGameSailRotation", true,
      ConfigHelpers.CreateConfigDescription(
        "Lets the baseGame sails Tiers1-4 to rotate based on wind direction",
        true));

    ShouldLiftAnchorOnSpeedChange = Config.Bind(SectionName,
      "shouldLiftAnchorOnSpeedChange", false,
      ConfigHelpers.CreateConfigDescription(
        "Lifts the anchor when using a speed change key, this is a QOL to prevent anchor from being required to be pressed when attempting to change the ship speed"));

    // vertical flight/ballast config

    FlightClimbingOffset = Config.Bind(SectionName,
      "FlightClimbingOffset",
      2f,
      ConfigHelpers.CreateConfigDescription(
        "Ascent and Descent speed for the vehicle in the air. This value is interpolated to prevent jitters.",
        true, true, new AcceptableValueRange<float>(0.01f, 10)));

    BallastClimbingOffset = Config.Bind(SectionName,
      "BallastClimbingOffset",
      2f,
      ConfigHelpers.CreateConfigDescription(
        "Ascent and Descent speed for the vehicle in the water. This value is interpolated to prevent jitters.",
        true, true, new AcceptableValueRange<float>(0.01f, 10)));

    VerticalSmoothingSpeed = Config.Bind(SectionName,
      "VerticalSmoothingSpeed",
      0.5f,
      ConfigHelpers.CreateConfigDescription(
        "This applies to both Ballast and Flight modes. The vehicle will use this value to interpolate the climbing offset. Meaning low value will be slower climbing/ballast and high values will be instant and match the offset. High values will result in jitters and potentially could throw people off the vehicle. Expect values of 0.01 and 1. IE 1% and 100%",
        true, true, new AcceptableValueRange<float>(0.01f, 1f)));

    // end vertical config.

    ShowShipStats = Config.Bind("Debug", "ShowShipState", true);
    MaxSailSpeed = Config.Bind(SectionName, "MaxSailSpeed", 30f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.",
        true, false, new AcceptableValueRange<float>(10, 200)));
    SpeedCapMultiplier = Config.Bind(SectionName, "SpeedCapMultiplier", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Sets the speed at which it becomes significantly harder to gain speed per sail area",
        true));

    // rudder

    EnableCustomPropulsionConfig = Config.Bind(SectionName,
      "EnableCustomPropulsionConfig", SailAreaForce.HasPropulsionConfigOverride,
      ConfigHelpers.CreateConfigDescription("Enables all custom propulsion values", true,
        true));

    SailCustomAreaTier1Multiplier = Config.Bind(SectionName,
      "SailCustomAreaTier1Multiplier",
      SailAreaForce.CustomTier1AreaForceMultiplier,
      ConfigHelpers.CreateConfigDescription(
        "Manual sets the sail wind area multiplier the custom tier1 sail. Currently there is only 1 tier",
        true, true)
    );

    SailTier1Area = Config.Bind(SectionName,
      "SailTier1Area", SailAreaForce.Tier1,
      ConfigHelpers.CreateConfigDescription(
        "Manual sets the sail wind area of the tier 1 sail.", true, true)
    );

    SailTier2Area = Config.Bind(SectionName,
      "SailTier2Area", SailAreaForce.Tier2,
      ConfigHelpers.CreateConfigDescription(
        "Manual sets the sail wind area of the tier 2 sail.", true, true));

    SailTier3Area = Config.Bind(SectionName,
      "SailTier3Area", SailAreaForce.Tier3,
      ConfigHelpers.CreateConfigDescription(
        "Manual sets the sail wind area of the tier 3 sail.", true, true));

    SailTier4Area = Config.Bind(SectionName,
      "SailTier4Area", SailAreaForce.Tier4,
      ConfigHelpers.CreateConfigDescription(
        "Manual sets the sail wind area of the tier 4 sail.", true, true));

    FlightVerticalToggle = Config.Bind<bool>(SectionName,
      "FlightVerticalToggle",
      true,
      "Flight Vertical Continues UntilToggled: Saves the user's fingers by allowing the ship to continue to climb or descend without needing to hold the button");
    FlightHasRudderOnly = Config.Bind<bool>(SectionName,
      "FlightHasRudderOnly",
      false,
      "Flight allows for different rudder speeds. Use rudder speed only. Do not use sail speed.");
    

    WheelDeadZone = Config.Bind(SectionName,
      "WheelDeadZone",
      0.02f,
      ConfigHelpers.CreateConfigDescription(
        "Plus or minus deadzone of the wheel when turning. Setting this to 0 will disable this feature. This will zero out the rudder if the user attempts to navigate with a value lower than this threshold range",
        false, true, new AcceptableValueRange<float>(0f, 0.1f)));

    DefaultPhysicsMode.SettingChanged +=
      (sender, args) =>
        VehicleMovementController.SetPhysicsSyncTarget(
          DefaultPhysicsMode
            .Value);
    AllowFlight.SettingChanged += VehicleShip.OnAllowFlight;
    // setters that must be called on init
    VehicleMovementController.SetPhysicsSyncTarget(
      DefaultPhysicsMode.Value);
  }
}