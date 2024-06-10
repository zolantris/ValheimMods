using BepInEx.Configuration;
using ComfyLib;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Config;

public static class RamConfig
{
  public static ConfigFile Config { get; private set; }

  public static ConfigEntry<float> HitRadius { get; set; }
  public static ConfigEntry<float> RamBaseSlashDamage { get; set; }
  public static ConfigEntry<float> RamBaseBluntDamage { get; set; }
  public static ConfigEntry<float> RamBaseChopDamage { get; set; }
  public static ConfigEntry<float> RamBasePickAxeDamage { get; set; }
  public static ConfigEntry<float> RamBasePierceDamage { get; set; }
  public static ConfigEntry<float> RamBaseMaximumDamage { get; set; }
  public static ConfigEntry<bool> HasMaximumDamageCap { get; set; }
  public static ConfigEntry<float> PercentageDamageToSelf { get; set; }
  public static ConfigEntry<bool> AllowContinuousDamage { get; set; }
  public static ConfigEntry<bool> CanHitCharacters { get; set; }
  public static ConfigEntry<bool> CanHitFriendly { get; set; }
  public static ConfigEntry<bool> CanDamageSelf { get; set; }
  public static ConfigEntry<bool> CanHitEnemies { get; set; }
  public static ConfigEntry<float> RamHitInterval { get; set; }
  public static ConfigEntry<bool> CanHitEnvironmentOrTerrain { get; set; }
  public static ConfigEntry<float> minimumVelocityToTriggerHit { get; set; }
  public static ConfigEntry<float> MaxVelocityMultiplier { get; set; }
  public static ConfigEntry<float> ShipMassMaxMultiplier { get; set; }
  public static ConfigEntry<int> RamDamageToolTier { get; set; }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    var damageDescription =
      "the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.";

    // damage
    RamBaseMaximumDamage = config.Bind("Rams", "maximumDamage", 200f,
      ConfigHelpers.CreateConfigDescription(
        "Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values",
        true, true));

    // Very important for preventing configuration or values from 
    HasMaximumDamageCap = config.Bind("Rams", "maxDamageCap", true,
      ConfigHelpers.CreateConfigDescription(
        "enable damage caps",
        true, true));

    RamBaseSlashDamage = config.Bind("Rams", "slashDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"slashDamage for Ram Blades. {damageDescription}",
        true, true));
    RamBaseBluntDamage = config.Bind("Rams", "bluntDamage", 10f,
      ConfigHelpers.CreateConfigDescription(
        $"bluntDamage {damageDescription}",
        true, true));
    RamBaseChopDamage = config.Bind("Rams", "chopDamage", 5f,
      ConfigHelpers.CreateConfigDescription(
        $"chopDamage for Ram Blades excludes Ram Stakes. {damageDescription}. Will damage trees dependending on tool tier settings",
        true, true));
    RamBasePickAxeDamage = config.Bind("Rams", "pickaxeDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"pickDamage {damageDescription} Will damage rocks as well as other entities",
        true, true));
    RamBasePierceDamage = config.Bind("Rams", "pierceDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"Pierce damage for Ram Stakes. {damageDescription} Will damage rocks as well as other entities",
        true, true));
    PercentageDamageToSelf = config.Bind("Rams", "percentageDamageToSelf", 1f,
      ConfigHelpers.CreateConfigDescription(
        $"Percentage Damage applied to the Ram piece per hit.",
        true, true));

    AllowContinuousDamage = config.Bind("Rams", "AllowContinuousDamage", true,
      ConfigHelpers.CreateConfigDescription(
        "Rams will continue to apply damage based on their velocity even after the initial impact",
        true, true));

    RamDamageToolTier = config.Bind("Rams", "RamDamageToolTier", 100,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to damage both rocks, ores, and higher tier trees and/or prefabs. Tier 1 is bronze. Setting to 100 will allow damage to all types of materials",
        true, true));

    // Hit Config
    CanHitCharacters = config.Bind("Rams", "CanHitCharacters", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit characters/entities",
        true, true));
    CanHitEnemies = config.Bind("Rams", "CanHitEnemies", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit enemies",
        true, true));
    CanHitFriendly = config.Bind("Rams", "CanHitFriendly", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));
    CanDamageSelf = config.Bind("Rams", "CanDamageSelf", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to be damaged. The values set for the damage will be calculated",
        true, true));

    CanHitEnvironmentOrTerrain = config.Bind("Rams", "CanHitEnvironmentOrTerrain", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));

    HitRadius = config.Bind("Rams", "HitRadius", 5f,
      ConfigHelpers.CreateConfigDescription(
        "The base ram hit radius area. Stakes are always half the size, this will hit all pieces within this radius, capped between 0.1 and 10 for balance and framerate stability",
        true, true));
    RamHitInterval = config.Bind("Rams", "RamHitInterval", 0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Every X seconds, the ram will apply this damage",
        true, true));

    // Physics damage multiplier Config
    minimumVelocityToTriggerHit = config.Bind("Rams", "minimumVelocityToTriggerHit", 0.5f,
      ConfigHelpers.CreateConfigDescription(
        "Minimum velocity required to activate the ram's damage",
        true, true));
    MaxVelocityMultiplier = config.Bind("Rams", "RamMaxVelocityMultiplier", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
        true, true));
    ;
    ShipMassMaxMultiplier = config.Bind("Rams", "ShipMassMaxMultiplier", 0.01f,
      ConfigHelpers.CreateConfigDescription(
        "Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
        true, true));


    // Must re-initialize config called in Awake
    HitRadius.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    PercentageDamageToSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    AllowContinuousDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitCharacters.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanDamageSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitFriendly.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnemies.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamHitInterval.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnvironmentOrTerrain.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    minimumVelocityToTriggerHit.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    MaxVelocityMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    ShipMassMaxMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamDamageToolTier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;


    // Must update damage values only
    RamBaseSlashDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBasePierceDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBaseBluntDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBaseChopDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBasePickAxeDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
  }
}