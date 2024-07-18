using System;
using BepInEx.Configuration;
using ComfyLib;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<float> VehicleFlightClimbingSpeed { get; private set; } = null!;

  public static ConfigEntry<float> TurnPowerNoRudder { get; private set; } = null!;
  public static ConfigEntry<float> TurnPowerWithRudder { get; private set; } = null!;
  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } = null!;
  public static ConfigEntry<bool> SlowAndReverseWithoutControls { get; private set; } = null!;

  public static ConfigEntry<bool> ShouldLiftAnchorOnSpeedChange { get; private set; } = null!;

  public static ConfigEntry<bool> AllowBaseGameSailRotation { get; private set; } = null!;

  public static ConfigEntry<bool> EnableExperimentalMultiplayerJitterFix { get; private set; } =
    null!;

  private const string SectionName = "Propulsion";

  /// <summary>
  /// Todo migrate ValheimRaftPlugin.CreatePropulsionConfig to here
  /// </summary>
  /// <param name="config"></param>
  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    EnableExperimentalMultiplayerJitterFix = Config.Bind(SectionName, "ExperimentalJitterFix", true,
      ConfigHelpers.CreateConfigDescription(
        "Makes the client owner of the ship physics the only client that force syncs the Vehicle Pieces. Makes all vehicle updates on non-physics owners utilize a kinematic rigidbody which makes the client appear much less janky and mostly removes camera shake especially during turning and high speeds. This config flag is expirimental so if your ship gets weird in multiplayer you may want to set this to false.",
        true));

    TurnPowerNoRudder = Config.Bind(SectionName, "turningPowerNoRudder", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the base turning power of the steering wheel", true));

    TurnPowerWithRudder = Config.Bind(SectionName, "turningPowerWithRudder", 6f,
      ConfigHelpers.CreateConfigDescription(
        "Set the turning power with a rudder", true));

    SlowAndReverseWithoutControls = Config.Bind(SectionName, "slowAndReverseWithoutControls", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles do not require controls while in slow and reverse with a person on them",
        true));

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

    VehicleFlightClimbingSpeed = Config.Bind(SectionName, "FlightClimbingSpeed", 5f,
      ConfigHelpers.CreateConfigDescription(
        "Ascent and Descent speed for the vehicle in the air. Numbers above 1 require turning the synced rigidbody for vehicle into another joint rigidbody.",
        true, true, new AcceptableValueRange<float>(1, 10)));

    EnableExperimentalMultiplayerJitterFix.SettingChanged +=
      (sender, args) =>
        VehicleMovementController.SetPhysicsSyncTarget(EnableExperimentalMultiplayerJitterFix
          .Value);

    // setters that must be called on init
    VehicleMovementController.SetPhysicsSyncTarget(EnableExperimentalMultiplayerJitterFix.Value);
  }
}