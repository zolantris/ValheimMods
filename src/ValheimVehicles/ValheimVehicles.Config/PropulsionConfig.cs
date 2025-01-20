using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Enums;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> FlightClimbingOffset { get; private set; } =
    null!;

  public static ConfigEntry<float> BallastClimbingOffset { get; private set; } =
    null!;

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

  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } =
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

    EnableLandVehicles = Config.Bind(SectionName, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles can now float on land. What is realism. Experimental only until wheels are invented. Must use rudder speeds to move forwards.",
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
      "BallastClimbingOffset",
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

    // setters that must be called on init
    VehicleMovementController.SetPhysicsSyncTarget(
      DefaultPhysicsMode.Value);
  }
}