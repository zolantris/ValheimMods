using BepInEx.Configuration;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts.Validation;
namespace ValheimVehicles.Config;

public class VehicleGlobalConfig
{
  // sounds for VehicleShip Effects
  public static ConfigEntry<bool> EnableShipWakeSounds = null!;
  public static ConfigEntry<bool> EnableShipInWaterSounds = null!;
  public static ConfigEntry<bool> EnableShipSailSounds = null!;

  // updaters
  public static ConfigEntry<float> ServerRaftUpdateZoneInterval = null!;
  public static ConfigEntry<bool> ForceShipOwnerUpdatePerFrame { get; set; }

  // section keys
  private const string VehicleGlobalBaseKey = "VehicleGlobal";
  private const string VehicleSoundKey = $"{VehicleGlobalBaseKey}:Sound";
  private const string VehicleGlobalUpdateKey = $"{VehicleGlobalBaseKey}:Updates";

  public static void BindConfig(ConfigFile config)
  {
    CreateSoundConfig(config);
    CreateVehicleUpdaterConfig(config);

    StaticFieldValidator.ValidateRequiredNonNullFields<VehicleGlobalConfig>();
  }

  private static void CreateVehicleUpdaterConfig(ConfigFile config)
  {
    ForceShipOwnerUpdatePerFrame = config.Bind("Rendering",
      "Force Ship Owner Piece Update Per Frame", false,
      ConfigHelpers.CreateConfigDescription(
        "Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.",
        true, true));


    ServerRaftUpdateZoneInterval = config.Bind(VehicleGlobalUpdateKey,
      "ServerRaftUpdateZoneInterval",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        true, true, new AcceptableValueRange<float>(1, 30f)));
  }

  private static void CreateSoundConfig(ConfigFile config)
  {
    EnableShipSailSounds = config.Bind(VehicleSoundKey, "Ship Sailing Sounds", true,
      "Toggles the ship sail sounds.");
    EnableShipWakeSounds = config.Bind(VehicleSoundKey, "Ship Wake Sounds", true,
      "Toggles Ship Wake sounds. Can be pretty loud");
    EnableShipInWaterSounds = config.Bind(VehicleSoundKey, "Ship In-Water Sounds",
      true,
      "Toggles ShipInWater Sounds, the sound of the hull hitting water");

    EnableShipSailSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
    EnableShipWakeSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
    EnableShipInWaterSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
  }
}