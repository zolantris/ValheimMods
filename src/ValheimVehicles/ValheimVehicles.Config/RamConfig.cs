using BepInEx.Configuration;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Config;

/// <summary>
/// Aggregation of all Ram and vehicle impact damages. These are all AOE impacts.
/// </summary>
public class RamConfig : BepInExBaseConfig<RamConfig>
{
  public static ConfigFile Config { get; private set; }

  public static ConfigEntry<bool> RamDamageEnabled { get; set; } = null!;

  public static ConfigEntry<float>
    DamageIncreasePercentagePerTier { get; set; } = null!;

  public static ConfigEntry<bool> WaterVehiclesAreRams { get; set; } = null!;
  public static ConfigEntry<bool> LandVehiclesAreRams { get; set; } = null!;
  public static ConfigEntry<float> HitRadius { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamHitRadius { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBaseSlashDamage { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBaseBluntDamage { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBaseChopDamage { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBasePickAxeDamage { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBasePierceDamage { get; set; } = null!;
  public static ConfigEntry<float> VehicleRamBaseMaximumDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseSlashDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseBluntDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseChopDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBasePickAxeDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBasePierceDamage { get; set; } = null!;
  public static ConfigEntry<float> RamBaseMaximumDamage { get; set; } = null!;
  public static ConfigEntry<bool> HasMaximumDamageCap { get; set; } = null!;
  public static ConfigEntry<float> PercentageDamageToSelf { get; set; } = null!;
  public static ConfigEntry<float> VehiclePercentageDamageToCollisionArea { get; set; } = null!;

  public static ConfigEntry<bool> AllowContinuousDamage { get; set; } = null!;
  public static ConfigEntry<bool> CanHitCharacters { get; set; } = null!;
  public static ConfigEntry<bool> CanHitFriendly { get; set; } = null!;
  public static ConfigEntry<bool> CanDamageSelf { get; set; } = null!;
  public static ConfigEntry<bool> CanHitEnemies { get; set; } = null!;
  public static ConfigEntry<bool> CanHitEnvironmentOrTerrain { get; set; } =
    null!;

  public static ConfigEntry<bool> VehicleRamCanHitCharacters { get; set; } = null!;
  public static ConfigEntry<bool> VehicleRamCanHitFriendly { get; set; } = null!;
  public static ConfigEntry<bool> VehicleRamCanDamageSelf { get; set; } = null!;
  public static ConfigEntry<bool> VehicleRamCanHitEnemies { get; set; } = null!;
  public static ConfigEntry<bool> VehicleRamCanHitEnvironmentOrTerrain { get; set; } =
    null!;

  public static ConfigEntry<bool> CanHitWhileHauling { get; set; } =
    null!;

  public static ConfigEntry<bool> CanHitSwivels { get; set; }

  public static ConfigEntry<float> RamHitInterval { get; set; } = null!;

  public static ConfigEntry<bool> CanRepairRams { get; set; } = null!;

  public static ConfigEntry<float> MinimumVelocityToTriggerHit { get; set; } =
    null!;

  public static ConfigEntry<float> VehicleMinimumVelocityToTriggerHit { get; set; } =
    null!;

  public static ConfigEntry<float> MaxVelocityMultiplier { get; set; } = null!;
  public static ConfigEntry<float> VehicleMaxVelocityMultiplier { get; set; } = null!;

  public static ConfigEntry<float>
    VehicleHullMassMultiplierDamage { get; set; } =
    null!;


  public static ConfigEntry<int> WaterVehicleRamToolTier { get; set; } =
    null!;
  public static ConfigEntry<int> LandVehicleRamToolTier { get; set; } =
    null!;

  /// <summary>
  /// TODO possibly enable this after initial release of rams
  /// </summary>
// public static ConfigEntry<float> ShipMassMaxMultiplier { get; set; }  = null!;
  public static ConfigEntry<int> RamDamageToolTier { get; set; } = null!;

  private const string RamSectionName = "Ram: Prefabs";
  private const string VehicleRamSectionName = "Ram: Vehicles";
  private static AcceptableValueRange<int> ToolTierRange = new(1, 1000);

  public override void OnBindConfig(ConfigFile config)
  {
    var damageDescription =
      "the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.";

    RamDamageEnabled = config.Bind(RamSectionName, "ramDamageEnabled", true,
      ConfigHelpers.CreateConfigDescription(
        "Will keep the prefab available for aethetics only, will not do any damage nor will it initialize anything related to damage. Alternatives are using the damage tweaks.",
        true, true));

    CanHitSwivels = config.Bind(VehicleRamSectionName, "CanHitSwivels", false,
      ConfigHelpers.CreateConfigDescription(
        "Allows the vehicle to smash into swivels and destroy their contents.",
        true, true));

    CanHitWhileHauling = config.Bind(VehicleRamSectionName, "CanHitWhileHauling", true,
      ConfigHelpers.CreateConfigDescription(
        "Allows the vehicle to continue hitting objects while it's being hauled/moved.",
        true, true));

    // damage
    RamBaseMaximumDamage = config.Bind(RamSectionName, "maximumDamage", 200f,
      ConfigHelpers.CreateConfigDescription(
        "Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values",
        true, true));

    VehicleRamBaseMaximumDamage = config.Bind(VehicleRamSectionName, "maximumDamage", 200f,
      ConfigHelpers.CreateConfigDescription(
        "Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values",
        true, true));

    // Very important for preventing configuration or values from 
    HasMaximumDamageCap = config.Bind(RamSectionName, "maxDamageCap", true,
      ConfigHelpers.CreateConfigDescription(
        "enable damage caps",
        true, true));

    // generic rams
    RamBaseSlashDamage = config.Bind(RamSectionName, "slashDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"slashDamage for Ram Blades. {damageDescription}",
        true, false));
    RamBaseBluntDamage = config.Bind(RamSectionName, "bluntDamage", 10f,
      ConfigHelpers.CreateConfigDescription(
        $"bluntDamage {damageDescription}",
        true, false));
    RamBaseChopDamage = config.Bind(RamSectionName, "chopDamage", 5f,
      ConfigHelpers.CreateConfigDescription(
        $"chopDamage for Ram Blades excludes Ram Stakes. {damageDescription}. Will damage trees dependending on tool tier settings",
        true, false));
    RamBasePickAxeDamage = config.Bind(RamSectionName, "pickaxeDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"pickDamage {damageDescription} Will damage rocks as well as other entities",
        true, false));
    RamBasePierceDamage = config.Bind(RamSectionName, "pierceDamage", 20f,
      ConfigHelpers.CreateConfigDescription(
        $"Pierce damage for Ram Stakes. {damageDescription} Will damage rocks as well as other entities",
        true, false));

    // vehicle rams
    VehicleRamBaseSlashDamage = config.Bind(VehicleRamSectionName, "slashDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"slashDamage for Ram Blades. {damageDescription}",
        true, false));
    VehicleRamBaseBluntDamage = config.Bind(VehicleRamSectionName, "bluntDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"bluntDamage {damageDescription}",
        true, false));
    VehicleRamBaseChopDamage = config.Bind(VehicleRamSectionName, "chopDamage", 100f,
      ConfigHelpers.CreateConfigDescription(
        $"chopDamage for Ram Blades excludes Ram Stakes. {damageDescription}. Will damage trees depending on tool tier settings",
        true, false));
    VehicleRamBasePickAxeDamage = config.Bind(VehicleRamSectionName, "pickaxeDamage", 100f,
      ConfigHelpers.CreateConfigDescription(
        $"pickDamage {damageDescription} Will damage rocks as well as other entities",
        true, false));
    VehicleRamBasePierceDamage = config.Bind(VehicleRamSectionName, "pierceDamage", 0f,
      ConfigHelpers.CreateConfigDescription(
        $"Pierce damage for Ram Stakes. {damageDescription} Will damage rocks as well as other entities",
        true, false));

    PercentageDamageToSelf = config.Bind(RamSectionName, "percentageDamageToSelf",
      0.01f,
      ConfigHelpers.CreateConfigDescription(
        $"Percentage Damage applied to the Ram piece per hit. Number between 0-1. This will damage the vehicle in the area hit.",
        true, true, new AcceptableValueRange<float>(0, 1)));

    VehiclePercentageDamageToCollisionArea = config.Bind(VehicleRamSectionName, "percentageDamageToSelf",
      0.01f,
      ConfigHelpers.CreateConfigDescription(
        $"Percentage Damage applied to the Vehicle pieces per hit. Number between 0-1. This will damage the vehicle in the area hit.",
        true, true, new AcceptableValueRange<float>(0, 1)));

    AllowContinuousDamage = config.Bind(RamSectionName, "AllowContinuousDamage",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Rams will continue to apply damage based on their velocity even after the initial impact",
        true, true));

    RamDamageToolTier = config.Bind(RamSectionName, "RamDamageToolTier", 100,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to damage both rocks, ores, and higher tier trees and/or prefabs. Tier 1 is bronze. Setting to 100 will allow damage to all types of materials",
        true, true, new AcceptableValueRange<int>(1, 100)));

    // Hit Config
    CanHitCharacters = config.Bind(RamSectionName, "CanHitCharacters", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit characters/entities",
        true, true));
    CanHitEnemies = config.Bind(RamSectionName, "CanHitEnemies", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit enemies",
        true, true));
    CanHitFriendly = config.Bind(RamSectionName, "CanHitFriendly", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));
    CanDamageSelf = config.Bind(RamSectionName, "CanDamageSelf", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to be damaged. The values set for the damage will be calculated",
        true, true));
    CanHitEnvironmentOrTerrain = config.Bind(RamSectionName,
      "CanHitEnvironmentOrTerrain", true,
      ConfigHelpers.CreateConfigDescription(
        "allows rams to hit friendlies",
        true, true));

    HitRadius = config.Bind(RamSectionName, "HitRadius", 5f,
      ConfigHelpers.CreateConfigDescription(
        "The base ram hit radius area. Stakes are always half the size, this will hit all pieces within this radius, capped between 5 and 10, but 50 is max. Stakes are half this value. Blades are equivalent to this value.",
        true, true, new AcceptableValueRange<float>(5f, 50f)));

    // VehicleRam Hit Config
    VehicleRamCanHitCharacters = config.Bind(VehicleRamSectionName, "CanHitCharacters", true,
      ConfigHelpers.CreateConfigDescription(
        "allows vehicle rams to hit characters/entities",
        true, true));
    VehicleRamCanHitEnemies = config.Bind(VehicleRamSectionName, "CanHitEnemies", true,
      ConfigHelpers.CreateConfigDescription(
        "allows vehicle rams to hit enemies",
        true, true));
    VehicleRamCanHitFriendly = config.Bind(VehicleRamSectionName, "CanHitFriendly", true,
      ConfigHelpers.CreateConfigDescription(
        "allows vehicle rams to hit friendlies",
        true, true));
    VehicleRamCanDamageSelf = config.Bind(VehicleRamSectionName, "CanDamageSelf", false,
      ConfigHelpers.CreateConfigDescription(
        "allows vehicle rams to be damaged. The values set for the damage will be calculated at same time hits are calculated. This config does not work yet so it's set to false currently on all releases",
        true, true, new AcceptableValueList<bool>(ModEnvironment.IsDebug)));
    VehicleRamCanHitEnvironmentOrTerrain = config.Bind(VehicleRamSectionName,
      "CanHitEnvironmentOrTerrain", true,
      ConfigHelpers.CreateConfigDescription(
        "allows vehicle rams to hit friendlies",
        true, true));

    VehicleRamHitRadius = config.Bind(VehicleRamSectionName, "HitRadius", 5f,
      ConfigHelpers.CreateConfigDescription(
        "The base hit radius of vehicle bodies. This will also effect self-damage to vehicle based on the radius.",
        true, true, new AcceptableValueRange<float>(5f, 50f)));
    RamHitInterval = config.Bind(RamSectionName, "RamHitInterval", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Every X seconds, the ram will apply this damage",
        true, true, new AcceptableValueRange<float>(0.5f, 20f)));

    CanRepairRams = config.Bind(RamSectionName, "RamsCanBeRepaired", false,
      ConfigHelpers.CreateConfigDescription("Allows rams to be repaired",
        true));

    // Physics damage multiplier Config
    MinimumVelocityToTriggerHit = config.Bind(RamSectionName,
      "minimumVelocityToTriggerHit", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Minimum velocity required to activate the ram's damage",
        true, true, new AcceptableValueRange<float>(0f, 100f)));
    MaxVelocityMultiplier = config.Bind(RamSectionName, "MaxVelocityMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
        true, true));

    // vehicle configs
    VehicleMinimumVelocityToTriggerHit = config.Bind(VehicleRamSectionName,
      "VehicleMinimumVelocityToTriggerHit", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Minimum velocity required to activate the vehicle hull's damage",
        true, true, new AcceptableValueRange<float>(0f, 100f)));
    VehicleMaxVelocityMultiplier = config.Bind(VehicleRamSectionName, "MaxVelocityMultiplier",
      1f,
      ConfigHelpers.CreateConfigDescription(
        "Damage of the vehicle hull is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.",
        true, true));
    VehicleHullMassMultiplierDamage = config.Bind(VehicleRamSectionName,
      "VehicleHullMassMultiplierDamage",
      0.1f,
      ConfigHelpers.CreateConfigDescription(
        $"Multiplier per each single point of mass the vehicle which adds additional damage. This value is multiplied by the velocity.",
        true, true));


    WaterVehicleRamToolTier = config.Bind(VehicleRamSectionName,
      "WaterVehicleRamToolTier",
      100,
      ConfigHelpers.CreateConfigDescription(
        "The tier damage a water vehicle can do to a rock or other object it hits. To be balanced this should be a lower value IE (1) bronze. But ashlands will require a higher tier to smash spires FYI.",
        true, true, ToolTierRange));
    LandVehicleRamToolTier = config.Bind(VehicleRamSectionName,
      "LandVehicleRamToolTier",
      100,
      ConfigHelpers.CreateConfigDescription(
        "The tier damage a Land vehicle can do to a rock or other object it hits. This should be set to maximum as land vehicles are black metal tier.",
        true, true, ToolTierRange));
    WaterVehiclesAreRams = config.Bind(VehicleRamSectionName,
      "WaterVehiclesAreRams",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds ram damage to a water vehicle's combined hull mesh. This affects all water vehicles vehicles. This will turn off all rams for water vehicles if set to false.",
        true, true));
    LandVehiclesAreRams = config.Bind(VehicleRamSectionName,
      "LandVehiclesAreRams",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Adds ram damage to a land vehicle's combined hull mesh. This affects all land vehicles vehicles. This will turn off all rams for land vehicles if set to false.",
        true, true));

    const int tierDiff = 2;
    const float defaultDamagePerTier = 0.25f;
    const int baseDamage = 1;
    DamageIncreasePercentagePerTier = config.Bind(RamSectionName,
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
    VehicleRamHitRadius.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    PercentageDamageToSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehiclePercentageDamageToCollisionArea.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    AllowContinuousDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitCharacters.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanDamageSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitFriendly.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnemies.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamHitInterval.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnvironmentOrTerrain.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;

    MinimumVelocityToTriggerHit.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;
    VehicleMinimumVelocityToTriggerHit.SettingChanged +=
      VehicleRamAoe.OnBaseSettingsChange;

    MaxVelocityMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehicleMaxVelocityMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    // ShipMassMaxMultiplier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamDamageToolTier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    WaterVehicleRamToolTier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    LandVehicleRamToolTier.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;


    HitRadius.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    // Must update damage values only
    RamBaseSlashDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamBasePierceDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamBaseBluntDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamBaseChopDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    RamBasePickAxeDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;

    // vehicle config updater section.
    CanHitCharacters.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanDamageSelf.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitFriendly.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    CanHitEnemies.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;

    VehicleRamBaseSlashDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehicleRamBasePierceDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehicleRamBaseBluntDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehicleRamBaseChopDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
    VehicleRamBasePickAxeDamage.SettingChanged += VehicleRamAoe.OnBaseSettingsChange;
  }
}