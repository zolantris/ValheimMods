using System;
using BepInEx.Configuration;
using Jotunn.Configs;
using Zolantris.Shared;
namespace Eldritch.Valheim;

public struct EditableSpawnConfig
{
  public Heightmap.Biome Biome = Heightmap.Biome.Swamp;
  public float SpawnChance = 100f;
  public int MinGroupSize = 1;
  public int MaxGroupSize = 3;
  public float SpawnInterval = 200f;
  public bool SpawnAtDay = true;
  public bool SpawnAtNight = true;
  public float MaxAltitude = 1000f;
  public float MinAltitude = 0f;
  public bool HuntPlayer = true;
  public int MaxLevel = 10;

  public EditableSpawnConfig()
  {
  }

  public SpawnConfig ToSpawnConfig()
  {
    return new SpawnConfig
    {
      Name = EldritchPrefabRegistry.droneConfigName,
      Biome = Biome,
      SpawnChance = SpawnChance,
      MinGroupSize = MinGroupSize,
      MaxGroupSize = MaxGroupSize,
      SpawnInterval = SpawnInterval,
      SpawnAtDay = SpawnAtDay,
      SpawnAtNight = SpawnAtNight,
      MaxAltitude = MaxAltitude,
      MinAltitude = MinAltitude,
      HuntPlayer = HuntPlayer,
      MaxLevel = MaxLevel
    };
  }
}

public class EldritchXenoDroneConfig : BepInExBaseConfig<EldritchXenoDroneConfig>
{
  public static ConfigEntry<bool> Enabled;

  public static string prefabNameKey = "Xeno Drone";

  public static string sectionKey_spawnConfig = "xenoDrone - SpawnConfig";
  public static string sectionKey_characterConfig = "xenoDrone - CharacterConfig";


  public static ConfigEntry<float> XenoHealth;
  public static ConfigEntry<float> attackDamageTailAcid;
  public static ConfigEntry<float> attackDamageTailPierce;
  public static ConfigEntry<float> attackDamageTailSlash;
  
  public static ConfigEntry<float> attackDamageArmsAcid;
  public static ConfigEntry<float> attackDamageArmsPierce;
  public static ConfigEntry<float> attackDamageArmsSlash;
  
  public static ConfigEntry<float> attackDamageMouthPierce;
  
  public static ConfigEntry<float> attackDamageBloodAcid;
  
  public static ConfigEntry<float> XenoHealthMultiplier;

  // damage structs
  public static ConfigEntry<float> XenoAcidDamage;

  // Spawn Config
  // test only to see how json binding works
  public static ConfigEntry<string> XenoSpawnConfig;

  // individual properties
  public static ConfigEntry<Heightmap.Biome> Biome;
  public static ConfigEntry<Heightmap.Biome> BiomesWithoutWorldModifierRequirements;
  public static ConfigEntry<GlobalKeys> GlobalKeysConditionalSpawn;


  public static ConfigEntry<float> SpawnChance;
  public static ConfigEntry<int> MinGroupSize;
  public static ConfigEntry<int> MaxGroupSize;
  public static ConfigEntry<float> SpawnInterval;
  public static ConfigEntry<bool> SpawnAtDay;
  public static ConfigEntry<bool> SpawnAtNight;
  public static ConfigEntry<float> MaxAltitude;
  public static ConfigEntry<float> MinAltitude;
  public static ConfigEntry<bool> HuntPlayer;
  public static ConfigEntry<int> MaxLevel;

  public static void BindCharacterConfig(string prefabName, ConfigFile config)
  {
    XenoHealth = config.BindUnique(sectionKey_characterConfig, "maxHealth", 200f, ConfigHelpers.CreateConfigDescription("Configuration for Xeno Drone max base health"));
    XenoHealthMultiplier = config.BindUnique(sectionKey_characterConfig, "healthMultiplier", 1.0f, ConfigHelpers.CreateConfigDescription("Health multiplier for the Xeno Drone per level"));
  }

  public static void BindSpawnConfig(string prefabName, ConfigFile config)
  {
    Enabled = config.BindUnique(sectionKey_spawnConfig, "Enabled", true, ConfigHelpers.CreateConfigDescription($"Enable the {prefabName} for spawn"));

    XenoSpawnConfig = config.BindJson<EditableSpawnConfig>(sectionKey_spawnConfig, "xenoSpawnConfig", new EditableSpawnConfig(), "Configuration for Xeno spawning");

    Biome = config.BindUnique(sectionKey_spawnConfig, "Biome", Heightmap.Biome.All, ConfigHelpers.CreateConfigDescription("Enable Xenos for specific Biomes, multiple biomes can be used. This will allow spawning in all Biomes. However Xenos spawns will only occur if GlobalKey conditions are met (or none are set)"));
    BiomesWithoutWorldModifierRequirements = config.BindUnique(sectionKey_spawnConfig, "BiomesWithoutWorldModifierRequirements", Heightmap.Biome.AshLands | Heightmap.Biome.DeepNorth | Heightmap.Biome.Mistlands, ConfigHelpers.CreateConfigDescription($"Enable {prefabName} for specific Biomes without World Modifier requirements, multiple biomes can be used. This will allow spawning in these Biomes regardless of World Modifier conditions."));

    GlobalKeysConditionalSpawn = config.BindUnique(sectionKey_spawnConfig, "GlobalKeysConditionalSpawn", GlobalKeys.defeated_goblinking, ConfigHelpers.CreateConfigDescription("Conditionally enable spawning based on all global modifiers being active. This means all of them must be fulfilled. For more info on keys go here. https://valheimcheats.com/global-key"));

    SpawnChance = config.BindUnique(sectionKey_spawnConfig, "SpawnChance", 5f, ConfigHelpers.CreateConfigDescription("Chance to spawn each spawn interval (0-100)", new AcceptableValueRange<float>(0f, 100f)));

    MinGroupSize = config.BindUnique(sectionKey_spawnConfig, "MinGroupSize", 1, ConfigHelpers.CreateConfigDescription("Min Group Size Spawned"));

    MaxGroupSize = config.BindUnique(sectionKey_spawnConfig, "MaxGroupSize", 3, ConfigHelpers.CreateConfigDescription("Max Group Size Spawned"));

    SpawnInterval = config.BindUnique(sectionKey_spawnConfig, "SpawnInterval", 600f, ConfigHelpers.CreateConfigDescription("Spawning interval of the prefab"));

    SpawnAtDay = config.BindUnique(sectionKey_spawnConfig, "SpawnAtDay", true, ConfigHelpers.CreateConfigDescription("Enable spawning during the day."));

    SpawnAtNight = config.BindUnique(sectionKey_spawnConfig, "SpawnAtNight", true, ConfigHelpers.CreateConfigDescription("Enable spawning at night."));

    MaxAltitude = config.BindUnique(sectionKey_spawnConfig, "MaxAltitude", 1000f, ConfigHelpers.CreateConfigDescription("Allow spawning below this altitude.", new AcceptableValueRange<float>(20f, 10000f)));

    MinAltitude = config.BindUnique(sectionKey_spawnConfig, "MinAltitude", 0f, ConfigHelpers.CreateConfigDescription("Allow spawning above this altitude"));

    HuntPlayer = config.BindUnique(sectionKey_spawnConfig, "HuntPlayer", true, ConfigHelpers.CreateConfigDescription("Allow hunting players"));

    MaxLevel = config.BindUnique(sectionKey_spawnConfig, "MaxLevel", 10, ConfigHelpers.CreateConfigDescription($"Sets the max level of spawns"));
  }

  public override void OnBindConfig(ConfigFile config)
  {
    BindSpawnConfig(prefabNameKey, config);
    BindCharacterConfig(prefabNameKey, config);
  }
}