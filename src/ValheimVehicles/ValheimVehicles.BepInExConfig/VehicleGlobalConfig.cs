using BepInEx.Configuration;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.Validation;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class VehicleGlobalConfig : BepInExBaseConfig<VehicleGlobalConfig>
{
  // sounds for VehicleShip Effects
  public static ConfigEntry<bool> EnableShipWakeSounds = null!;
  public static ConfigEntry<bool> EnableShipInWaterSounds = null!;
  public static ConfigEntry<bool> EnableShipSailSounds = null!;
  // updaters
  public static ConfigEntry<float> ServerRaftUpdateZoneInterval = null!;
  public static ConfigEntry<bool> ForceShipOwnerUpdatePerFrame = null!;
  public static ConfigEntry<float> ForceVehicleOwnerShipTakeoverTime = null!;


  // shields
  public static ConfigEntry<float> VehicleShieldGeneratorMaxRadius = null!;
  public static ConfigEntry<float> VehicleShieldGeneratorMinRadius = null!;

  // section keys
  private const string VehicleGlobalBaseKey = "VehicleGlobal";
  private const string VehicleSoundKey = $"{VehicleGlobalBaseKey}:Sound";
  private const string VehicleGlobalUpdateKey = $"{VehicleGlobalBaseKey}:Updates";

  public override void OnBindConfig(ConfigFile config)
  {
    CreateSoundConfig(config);
    CreateVehicleUpdaterConfig(config);
    CreateVehicleShieldConfig(config);
  }

  private static void CreateVehicleUpdaterConfig(ConfigFile config)
  {
    ForceShipOwnerUpdatePerFrame = config.BindUnique("Rendering",
      "Force Ship Owner Piece Update Per Frame", false,
      ConfigHelpers.CreateConfigDescription(
        "Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.",
        true, true));


    ServerRaftUpdateZoneInterval = config.BindUnique(VehicleGlobalUpdateKey,
      "ServerRaftUpdateZoneInterval",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        true, true, new AcceptableValueRange<float>(1, 30f)));

    ForceVehicleOwnerShipTakeoverTime = config.BindUnique(VehicleGlobalUpdateKey,
      "ForceVehicleOwnerShipTakeoverTime",
      0.2f,
      ConfigHelpers.CreateConfigDescription(
        "If the previous owner does not respond in this time in seconds, the vehicle will be transferred over to the new physics and controls owner immediately after this timer. Lower time has the risk of duplicate clients attempting to run physics, but the server will only reconcile the owner it detects on the server side.",
        true, true, new AcceptableValueRange<float>(0.01f, 5f)));
  }

  private static void CreateVehicleShieldConfig(ConfigFile config)
  {
    VehicleShieldGeneratorMaxRadius = config.BindUnique("Shield",
      "Shield MaxRadius", 100f,
      ConfigHelpers.CreateConfigDescription(
        "The maximum radius a ShieldGenerator placed on a vehicle can expand to", true, true, new AcceptableValueRange<float>(30f, 200f)));

    VehicleShieldGeneratorMinRadius = config.BindUnique("Shield",
      "Shield MinRadius", 5f,
      ConfigHelpers.CreateConfigDescription(
        "The minimum radius a ShieldGenerator placed on a vehicle can expand to", true, true, new AcceptableValueRange<float>(5f, 200f)));
  }

  private static void CreateSoundConfig(ConfigFile config)
  {
    EnableShipSailSounds = config.BindUnique(VehicleSoundKey, "Ship Sailing Sounds", true,
      "Toggles the ship sail sounds.");
    EnableShipWakeSounds = config.BindUnique(VehicleSoundKey, "Ship Wake Sounds", true,
      "Toggles Ship Wake sounds. Can be pretty loud");
    EnableShipInWaterSounds = config.BindUnique(VehicleSoundKey, "Ship In-Water Sounds",
      true,
      "Toggles ShipInWater Sounds, the sound of the hull hitting water");

    EnableShipSailSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipWakeSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipInWaterSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
  }
}