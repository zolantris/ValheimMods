using BepInEx.Configuration;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Config;

public static class RamConfig
{
  public static ConfigFile Config { get; private set; }

  public static ConfigEntry<bool> RamDamageEnabled { get; set; } = null!;

  public static ConfigEntry<float>
    DamageIncreasePercentagePerTier { get; set; } = null!;

  public static ConfigEntry<float> HitRadius { get; set; } = null!;
  public static ConfigEntry<float> RamBaseSlashDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseBluntDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseChopDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBasePickAxeDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBasePierceDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseMaximumDamage { get; set; } = null!;
  public static ConfigEntry<bool> HasMaximumDamageCap { get; set; } = null!;
  public static ConfigEntry<float> PercentageDamageToSelf { get; set; } = null!;
  public static ConfigEntry<bool> AllowContinuousDamage { get; set; } = null!;
  public static ConfigEntry<bool> CanHitCharacters { get; set; } = null!;
  public static ConfigEntry<bool> CanHitFriendly { get; set; } = null!;
  public static ConfigEntry<bool> CanDamageSelf { get; set; } = null!;
  public static ConfigEntry<bool> CanHitEnemies { get; set; } = null!;
  public static ConfigEntry<float> RamHitInterval { get; set; } = null!;

  public static ConfigEntry<bool> CanRepairRams { get; set; } = null!;

  public static ConfigEntry<bool> CanHitEnvironmentOrTerrain { get; set; } =
    null!;

  public static ConfigEntry<float> minimumVelocityToTriggerHit { get; set; } =
    null!;

  public static ConfigEntry<float> MaxVelocityMultiplier { get; set; } = null!;

  /// <summary>
  /// TODO possibly enable this after initial release of rams
  /// </summary>
  // public static ConfigEntry<float> ShipMassMaxMultiplier { get; set; }  = null!;
  public static ConfigEntry<int> RamDamageToolTier { get; set; } = null!;

  private const string SectionName = "Rams";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    var damageDescription =
      "the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.";

    RamDamageEnabled = config.Bind(SectionName, "ramDamageEnabled", true,
      ConfigHelpers.CreateConfigDescription(
        "Will keep the prefab available for aethetics only, will not do any damage nor will it initialize anything related to damage. Alternatives are using the damage tweaks.",
        true, true));

    // damage
    RamBaseMaximumDamage = config.Bind(SectionName, "maximumDamage", 200f,
      ConfigHelpers.CreateConfigDescription(
        "Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values",
        true, true));

    // Very important for preventing configuration or values from 
    HasMaximumDamageCap = config.Bind(SectionName, "maxDamageCap", true,
      ConfigHelpers.CreateConfigDescription(
        "enable damage caps",
        true, true));

    RamBaseSlashDamage = config.Bind(SectionName, "slashDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"slashDamage for Ram Blades. {damageDescription}",
        true, true));
    RamBaseBluntDamage = config.Bind(SectionName, "bluntDamage", 10f,
      ConfigHelpers.CreateConfigDescription(
        $"bluntDamage {damageDescription}",
        true, true));
    RamBaseChopDamage = config.Bind(SectionName, "chopDamage", 5f,
      ConfigHelpers.CreateConfigDescription(
        $"chopDamage for Ram Blades excludes Ram Stakes. {damageDescription}. Will damage trees dependending on tool tier settings",
        true, true));
    RamBasePickAxeDamage = config.Bind(SectionName, "pickaxeDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"pickDamage {damageDescription} Will damage rocks as well as other entities",
        true, true));
    RamBasePierceDamage = config.Bind(SectionName, "pierceDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"Pierce damage for Ram Stakes. {damageDescription} Will damage rocks as well as other entities",
        true, true));
    PercentageDamageToSelf = config.Bind(SectionName, "percentageDamageToSelf",
      0.01f,
      ConfigHelpers.CreateConfigDescription(
        $"Percentage Damage applied to the Ram piece per hit. Number between 0-1.",
        true, true, new AcceptableValueRange<float>(0, 1)));

    AllowContinuousDamage = config.Bind(SectionName, "AllowContinuousDamage",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Rams will continue to apply damage based on their velocity even after the initial impact",
        true, true));

    RamDamageToolTier = config.Bind(SectionName, "RamDamageToolTier", 100,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to damage both rocks, ores, and higher tier trees and/or prefabs. Tier 1 is bronze. Setting to 100 will allow damage to all types of materials",
        true, true));

    // Hit Config
    CanHitCharacters = config.Bind(SectionName, "CanHitCharacters", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit characters/entities",
        true, true));
    CanHitEnemies = config.Bind(SectionName, "CanHitEnemies", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit enemies",
        true, true));
    CanHitFriendly = config.Bind(SectionName, "CanHitFriendly", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));
    CanDamageSelf = config.Bind(SectionName, "CanDamageSelf", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to be damaged. The values set for the damage will be calculated",
        true, true));

    CanHitEnvironmentOrTerrain = config.Bind(SectionName,
      "CanHitEnvironmentOrTerrain", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));

    HitRadius = config.Bind(SectionName, "HitRadius", 5f,
      ConfigHelpers.CreateConfigDescription(
        "The base ram hit radius area. Stakes are always half the size, this will hit all pieces within this radius, capped between 0.1 and 10 for balance and framerate stability",
        true, true));
    RamHitInterval = config.Bind(SectionName, "RamHitInterval", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Every X seconds, the ram will apply this damage",
        true, true, new AcceptableValueRange<float>(0.5f, 20f)));

    CanRepairRams = config.Bind(SectionName, "RamsCanBeRepaired", false,
      ConfigHelpers.CreateConfigDescription("Allows rams to be repaired",
        true));

    // Physics damage multiplier Config
    minimumVelocityToTriggerHit = config.Bind(SectionName,
      "minimumVelocityToTriggerHit", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Minimum velocity required to activate the ram's damage",
        true, true, new AcceptableValueRange<float>(0f, 100f)));
    MaxVelocityMultiplier = config.Bind(SectionName, "RamMaxVelocityMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
        true, true));
    // ShipMassMaxMultiplier = config.Bind(SectionName, "ShipMassMaxMultiplier", 0.01f,
    //   ConfigHelpers.CreateConfigDescription(
    //     "Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
    //     true, true));

    const int tierDiff = 2;
    const float defaultDamagePerTier = .25f;
    const int baseDamage = 1;
    DamageIncreasePercentagePerTier = config.Bind(SectionName,
      "DamageIncreasePercentagePerTier",
      0.25f,
      ConfigHelpers.CreateConfigDescription(
        $"Damage Multiplier per tier. So far only HardWood (Tier1) Iron (Tier3) available. With base value 1 a Tier 3 mult at 25% additive additional damage would be 1.5. IE ({baseDamage} * {defaultDamagePerTier} * {tierDiff} + {baseDamage}) = {baseDamage * defaultDamagePerTier * tierDiff + baseDamage}",
        true, true, new AcceptableValueRange<float>(0, 4f)));

    // Disables/Enables itself when this value updates
    RamDamageEnabled.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;

    // Must re-initialize config called in Awake
    DamageIncreasePercentagePerTier.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;
    HitRadius.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    PercentageDamageToSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    AllowContinuousDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitCharacters.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanDamageSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitFriendly.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnemies.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamHitInterval.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnvironmentOrTerrain.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;
    minimumVelocityToTriggerHit.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;
    MaxVelocityMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    // ShipMassMaxMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamDamageToolTier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;

    // Must update damage values only
    RamBaseSlashDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBasePierceDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBaseBluntDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBaseChopDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
    RamBasePickAxeDamage.SettingChanged += VehicleRamAoe.OnSettingsChanged;
  }
}