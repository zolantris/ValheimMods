using System;
using System.Linq;
using Eldritch.Core;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;
using Zolantris.Shared;
namespace Eldritch.Valheim;

public static class EldritchPrefabRegistry
{
  public static AssetBundle assetBundle;
  public static SpriteAtlas Sprites;
  public const string droneAssetName = "xenomorph-drone-v1";
  public const string droneConfigName = "eldritch_xeno_drone";
  private const string XenoAdultName = "Eldritch_XenoAdult_Creature";

  public static Heightmap.Biome DefaultAlwaysSpawnInBiomes = Heightmap.Biome.AshLands | Heightmap.Biome.DeepNorth | Heightmap.Biome.Mistlands;
  public static Heightmap.Biome DefaultBiomesAfterGlobalKey = Heightmap.Biome.BlackForest | Heightmap.Biome.Swamp;


  public static SpawnConfig xenoGlobalModifierSpawnConfig = new()
  {
    Name = droneConfigName,
    Biome = DefaultBiomesAfterGlobalKey,
    SpawnChance = 5f,
    MinGroupSize = 1,
    MaxGroupSize = 3,
    RequiredGlobalKey = nameof(GlobalKeys.defeated_goblinking),
    SpawnInterval = 600f,
    SpawnAtDay = true,
    SpawnAtNight = true,
    MaxAltitude = 1000f,
    MinAltitude = 0f,
    HuntPlayer = true,
    MaxLevel = 10 // todo add config for level scaling.
  };

  public static SpawnConfig xenoNoModifierSpawnConfig = new()
  {
    Name = droneConfigName,
    Biome = DefaultAlwaysSpawnInBiomes,
    SpawnChance = 5f,
    MinGroupSize = 1,
    MaxGroupSize = 5,
    SpawnInterval = 600f,
    SpawnAtDay = true,
    SpawnAtNight = true,
    MaxAltitude = 10000f,
    MinAltitude = 0f,
    HuntPlayer = true,
    MaxLevel = 10
  };


  public static void UpdateSpawnConfig()
  {
    xenoGlobalModifierSpawnConfig = new SpawnConfig
    {
      Name = droneConfigName,
      Biome = EldritchBepinExXenoDroneConfig.BiomeSpawn.Value,
      SpawnChance = EldritchBepinExXenoDroneConfig.SpawnChance.Value,
      MinGroupSize = EldritchBepinExXenoDroneConfig.MinGroupSize.Value,
      MaxGroupSize = EldritchBepinExXenoDroneConfig.MaxGroupSize.Value,
      SpawnInterval = EldritchBepinExXenoDroneConfig.SpawnInterval.Value,
      SpawnAtDay = EldritchBepinExXenoDroneConfig.SpawnAtDay.Value,
      SpawnAtNight = EldritchBepinExXenoDroneConfig.SpawnAtNight.Value,
      MaxAltitude = EldritchBepinExXenoDroneConfig.MaxAltitude.Value,
      MinAltitude = EldritchBepinExXenoDroneConfig.MinAltitude.Value,
      HuntPlayer = EldritchBepinExXenoDroneConfig.HuntPlayer.Value,
      MaxLevel = EldritchBepinExXenoDroneConfig.MaxLevel.Value
    };

    xenoNoModifierSpawnConfig = new SpawnConfig
    {
      Name = droneConfigName,
      Biome = EldritchBepinExXenoDroneConfig.BiomesAlwaysSpawn.Value,
      SpawnChance = EldritchBepinExXenoDroneConfig.SpawnChance.Value,
      MinGroupSize = EldritchBepinExXenoDroneConfig.MinGroupSize.Value,
      MaxGroupSize = EldritchBepinExXenoDroneConfig.MaxGroupSize.Value,
      SpawnInterval = EldritchBepinExXenoDroneConfig.SpawnInterval.Value,
      SpawnAtDay = EldritchBepinExXenoDroneConfig.SpawnAtDay.Value,
      SpawnAtNight = EldritchBepinExXenoDroneConfig.SpawnAtNight.Value,
      MaxAltitude = EldritchBepinExXenoDroneConfig.MaxAltitude.Value,
      MinAltitude = EldritchBepinExXenoDroneConfig.MinAltitude.Value,
      HuntPlayer = EldritchBepinExXenoDroneConfig.HuntPlayer.Value,
      MaxLevel = EldritchBepinExXenoDroneConfig.MaxLevel.Value
    };
  }

  /// <summary>
  /// Loads from eldritch core. (this might need to be done here so eldritch.Core gameobjects are resolved with eldritch core scripts)
  /// </summary>
  public static void LoadAssemblies()
  {
    if (!assetBundle)
    {
      assetBundle = Entry.LoadAssembly();
    }
    if (assetBundle == null) return;
    Sprites = assetBundle.LoadAsset<SpriteAtlas>("icons");
  }

  public static void RegisterPlaceableAlien()
  {
    if (assetBundle == null) return;
    LoggerProvider.LogDebug($"Starting InjectAlienPrefab");
    const string XenoAdultPieceName = "Eldritch_XenoAdult_Piece";

    var prefabAsset = assetBundle.LoadAsset<GameObject>(droneAssetName);
    if (prefabAsset == null) return;

    var originalComponents = prefabAsset.GetComponents<Component>();
    var clonedPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultPieceName, prefabAsset);

    if (!clonedPrefab) return;

    clonedPrefab.AddComponent<ZNetView>();
    clonedPrefab.AddComponent<XenoDroneSpawnHandler>();
    var piece = clonedPrefab.AddComponent<Piece>();
    piece.m_name = XenoAdultPieceName;
    piece.m_icon = Sprites.GetSprite("anchor");

    var components = clonedPrefab.GetComponents<Component>();
    foreach (var component in components)
    {
      if (component == null)
      {
        LoggerProvider.LogError("Got null component");
        continue;
      }
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    foreach (var component in originalComponents)
    {
      if (component == null)
      {
        LoggerProvider.LogError("Got null component");
        continue;
      }
      LoggerProvider.LogDebug(component.GetType().ToString());
    }

    PieceManager.Instance.AddPiece(new CustomPiece(clonedPrefab, true,
      new PieceConfig
      {
        PieceTable = "Hammer", // for now.
        Name = "XENO",
        Description = "Xenos....they're everywhere...",
        Icon = Sprites.GetSprite("anchor"), // for testing.
        Category = "Eldritch",
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 1,
            Item = "Wood",
            Recover = true
          }
        ]
      }));

    LoggerProvider.LogDebug($"Added {piece}");
  }

  public static void OnConfigUpdateSpawnSettings()
  {
    UpdateSpawnConfig();
    var xenoPrefab = PrefabManager.Instance.GetPrefab(droneConfigName);
  }

  public static void RegisterXenoSpawn()
  {
    UpdateSpawnConfig();

    // Load prefab from ZNetScene or an AssetBundle
    var xenoAsset = assetBundle.LoadAsset<GameObject>(droneAssetName);
    if (!xenoAsset)
    {
      LoggerProvider.LogError("Xeno prefab not found — spawn registration skipped.");
      return;
    }

    var seekerCreature = CreatureManager.Instance.GetCreature("Seeker");
    var seekerPrefab = seekerCreature != null ? seekerCreature.Prefab : null;

    if (seekerCreature == null)
    {
      seekerPrefab = CreatureManager.Instance.GetCreaturePrefab("Seeker");
    }
    // var xenoPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, asset);
    if (seekerPrefab == null)
    {
      return;
    }

    // var xenoInstance = UnityEngine.Object.Instantiate(xenoAsset);
    var xenoPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, xenoAsset);

    // Merge in seeker top-level gameplay components (only missing ones)
    // if (!XenoFromSeekerMinimalMerge.MergeIntoXeno(xenoInstance, seekerPrefab))
    // {
    //   LoggerProvider.LogError("Xeno merge failed.");
    //   return;
    // }
    if (xenoPrefab == null) return;

    var seekerHumanoid = seekerPrefab.GetComponent<Humanoid>();

    var nv = xenoPrefab.GetOrAddComponent<ZNetView>();
    nv.m_type = ZDO.ObjectType.Default;
    nv.m_persistent = false;

    var humanoid = xenoPrefab.GetOrAddComponent<Humanoid>();

    // bind effects from seeker to xeno
    humanoid.m_deathEffects = seekerHumanoid.m_deathEffects;
    humanoid.m_onDeath = seekerHumanoid.m_onDeath;
    humanoid.m_dropEffects = seekerHumanoid.m_dropEffects;
    humanoid.m_waterEffects = seekerHumanoid.m_waterEffects;
    humanoid.m_critHitEffects = seekerHumanoid.m_critHitEffects;

    var zSyncTransform = xenoPrefab.GetOrAddComponent<ZSyncTransform>();
    var zSyncAnimation = xenoPrefab.GetOrAddComponent<ZSyncAnimation>();
    var xenoDroneMonsterAI = xenoPrefab.GetOrAddComponent<XenoDrone_MonsterAI>();

    var animator = xenoPrefab.GetComponentInChildren<Animator>();
    var animEvent = animator.gameObject.GetOrAddComponent<CharacterAnimEvent>();

    var animationController = xenoPrefab.GetComponentInChildren<XenoAnimationController>();


    // todo determine if this works.
    if (EldritchBepinExXenoDroneConfig.IsTameable.Value)
    {
      var tameable = xenoPrefab.GetOrAddComponent<Tameable>();
      tameable.m_commandable = true;
      tameable.m_monsterAI = xenoDroneMonsterAI;
      humanoid.m_tameableMonsterAI = xenoDroneMonsterAI;
    }

    humanoid.m_health = EldritchBepinExXenoDroneConfig.XenoHealth.Value;
    humanoid.m_runSpeed = EldritchBepinExXenoDroneConfig.RunSpeed.Value;
    humanoid.m_swimSpeed = EldritchBepinExXenoDroneConfig.XenoSwimSpeed.Value;

    humanoid.m_eye = xenoPrefab.transform.Find("EyePos");
    humanoid.m_animEvent = animEvent;

    animEvent.m_animator = animator;
    animEvent.m_nview = nv;
    animEvent.m_head = animationController.neckPivot;
    animEvent.m_eyes = [animationController.neckPivot, animationController.neckPivot];
    animEvent.m_feets = animationController.footColliders.Select(x => new CharacterAnimEvent.Foot(x.transform, x.transform.parent.name.Contains("_l_") ? AvatarIKGoal.LeftFoot : AvatarIKGoal.RightFoot)).ToArray();

    humanoid.m_animator = animator;
    zSyncAnimation.m_animator = animator;


    xenoDroneMonsterAI.m_huntPlayer = true;
    xenoDroneMonsterAI.m_enableHuntPlayer = true;

    // AddWeaponItemsToXenoInventory(humanoid);

    // if (!XenoFromSeekerBuilder.BuildXenoFromSeeker(xenoPrefab, xenoAsset.transform.Find("Visual").gameObject, xenoAsset))
    // {
    //   LoggerProvider.LogError("Failed to swap Seeker clone to Xeno.");
    //   return;
    // }

    // var humanoid = xenoPrefab.GetOrAddComponent<Humanoid>();
    // var xenoAnimationController = xenoPrefab.GetComponentInChildren<XenoAnimationController>();

    // if (humanoid && xenoAnimationController)
    // {
    //   humanoid.m_eye = xenoAnimationController.neckPivot;
    //   humanoid.m_head = xenoAnimationController.neckPivot;
    // }

    // Do not register spawn configs if disabled.
    SpawnConfig[] spawnConfigs = EldritchBepinExXenoDroneConfig.Enabled.Value ? [xenoGlobalModifierSpawnConfig, xenoNoModifierSpawnConfig] : [];

    var creatureConfig = new CreatureConfig
    {
      Name = droneConfigName,
      Consumables = [],
      SpawnConfigs = spawnConfigs,
      Faction = Character.Faction.SeaMonsters // or forests or demon is glitchy,
    };

    // Create CustomCreature
    var customXeno = new CustomCreature(xenoPrefab, true, creatureConfig);

    CreatureManager.Instance.AddCreature(customXeno);

    // Add a spawn config (vanilla world spawner)
  }

  public static HitData.DamageTypes XenoDroneArmDamage = new()
  {
    m_slash = 30f,
    m_poison = 5f
  };

  public static HitData.DamageTypes XenoDroneBloodDamage = new()
  {
    m_poison = 30f
  };

  public static HitData.DamageTypes XenoDroneTailDamage = new()
  {
    m_damage = 50f,
    m_pierce = 50f,
    m_poison = 5f
  };

  public static void UpdateDamage()
  {
    XenoDroneArmDamage = new HitData.DamageTypes
    {
      m_slash = EldritchBepinExXenoDroneConfig.attackDamageArmsSlash.Value,
      m_pierce = EldritchBepinExXenoDroneConfig.attackDamageArmsPierce.Value
    };

    XenoDroneBloodDamage = new HitData.DamageTypes
    {
      m_poison = EldritchBepinExXenoDroneConfig.attackDamageBloodAcid.Value
    };

    XenoDroneTailDamage = new HitData.DamageTypes
    {
      m_pierce = EldritchBepinExXenoDroneConfig.attackDamageTailPierce.Value,
      m_slash = EldritchBepinExXenoDroneConfig.attackDamageTailSlash.Value,
      m_poison = EldritchBepinExXenoDroneConfig.attackDamageTailAcid.Value
    };
  }

  public static void AddWeaponItemsToXenoInventory(Humanoid humanoid)
  {
    var handWeaponAttack = new Attack
    {
      m_attackAnimation = null,
      m_speedFactor = 1,
      m_attackStamina = 20f,
      m_attackRange = 2f
    };

    var xenoArmsItemDrop = new GameObject("XenoArmsPlaceholderDropPrefab");
    var nv1 = xenoArmsItemDrop.AddComponent<ZNetView>();
    nv1.m_persistent = false;

    var xenoTailItemDrop = new GameObject("XenoTailDropPrefab");
    var nv2 = xenoTailItemDrop.AddComponent<ZNetView>();
    nv2.m_persistent = false;

    var armItemDrop = xenoArmsItemDrop.AddComponent<ItemDrop>();
    var tailItemDrop = xenoTailItemDrop.AddComponent<ItemDrop>();

    var tailAttack = new Attack
    {
      m_attackAnimation = null,
      m_attackStamina = 20f,
      m_attackRange = 2f
    };

    var handWeapon = new ItemDrop.ItemData
    {
      m_dropPrefab = armItemDrop.gameObject,
      m_shared = new ItemDrop.ItemData.SharedData
      {
        m_name = "xeno_arms_weapon",
        m_attack = handWeaponAttack,
        m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon,
        m_maxStackSize = 1,
        m_icons = [Sprites.GetSprite("anchor")],
        m_skillType = Skills.SkillType.Swords,
        m_damages = XenoDroneArmDamage,
        m_equipDuration = 0,
        m_attackForce = 10f,
        m_useDurability = false,
        m_aiTargetType = ItemDrop.ItemData.AiTarget.Enemy,
        m_aiAttackMaxAngle = 25f
      }
    };
    var tailWeapon = new ItemDrop.ItemData
    {
      m_dropPrefab = tailItemDrop.gameObject,
      m_shared = new ItemDrop.ItemData.SharedData
      {
        m_name = "xeno_tail_weapon",
        m_icons = [Sprites.GetSprite("anchor")],
        m_attack = tailAttack,
        m_maxStackSize = 1,
        m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon,
        m_equipDuration = 0,
        m_skillType = Skills.SkillType.Swords,
        m_damages = XenoDroneTailDamage,
        m_attackForce = 10f,
        m_useDurability = false,
        m_aiTargetType = ItemDrop.ItemData.AiTarget.Enemy,
        m_aiAttackMaxAngle = 25f
      }
    };

    humanoid.m_inventory.AddItem(handWeapon);
    humanoid.m_inventory.AddItem(tailWeapon);
  }

  public static void RegisterAllPrefabs()
  {
#if DEBUG
    // for now do not even register placeable xeno.
    return;
    LoadAssemblies();
    RegisterPlaceableAlien();
#endif
  }
  public static void RegisterAllCreatures()
  {
    LoadAssemblies();
    RegisterXenoSpawn();
  }
}