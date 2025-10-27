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
  private const string droneAssetName = "xenomorph-drone-v1";
  private const string droneConfigName = "eldritch_xeno_drone";

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
    const string XenoAdultName = "Eldritch_XenoAdult";

    var prefabAsset = assetBundle.LoadAsset<GameObject>(droneAssetName);
    if (prefabAsset == null) return;

    var originalComponents = prefabAsset.GetComponents<Component>();
    var clonedPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, prefabAsset);

    if (!clonedPrefab) return;

    clonedPrefab.AddComponent<ZNetView>();
    clonedPrefab.AddComponent<XenoDroneSpawnHandler>();
    var piece = clonedPrefab.AddComponent<Piece>();
    piece.m_name = XenoAdultName;
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
        Enabled = true,
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

  public static void RegisterXenoSpawn()
  {
    // Load prefab from ZNetScene or an AssetBundle
    var xenoAsset = assetBundle.LoadAsset<GameObject>(droneAssetName);
    if (!xenoAsset)
    {
      LoggerProvider.LogError("Xeno prefab not found — spawn registration skipped.");
      return;
    }

    const string XenoAdultName = "Eldritch_XenoAdult_Creature";
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

    var nv = xenoPrefab.GetOrAddComponent<ZNetView>();
    nv.m_type = ZDO.ObjectType.Default;
    nv.m_persistent = false;

    var humanoid = xenoPrefab.GetOrAddComponent<Humanoid>();
    var zSyncTransform = xenoPrefab.GetOrAddComponent<ZSyncTransform>();
    var zSyncAnimation = xenoPrefab.GetOrAddComponent<ZSyncAnimation>();
    var xenoDroneMonsterAI = xenoPrefab.AddComponent<XenoDrone_MonsterAI>();

    var animator = xenoPrefab.GetComponentInChildren<Animator>();
    var animEvent = animator.gameObject.AddComponent<CharacterAnimEvent>();

    var animationController = xenoPrefab.GetComponentInChildren<XenoAnimationController>();

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

    var spawnConfig = new SpawnConfig
    {
      Name = droneConfigName,
      Biome = Heightmap.Biome.Swamp, // todo make this configurable
      // Biome = Heightmap.Biome.All, // for now all biomes can spawn alien. todo make this configurable
      SpawnChance = 100f, // 100% todo make this configurable
      MinGroupSize = 1, // todo make this configurable
      MaxGroupSize = 3, // todo make this configurable
      // SpawnInterval = 600f,
      SpawnInterval = 200f, // todo make this configurable
      SpawnAtDay = true,
      SpawnAtNight = true,
      MaxAltitude = 1000f,
      MinAltitude = 0f,
      HuntPlayer = true,
      MaxLevel = 10 // todo add config for level scaling.
    };

    var creatureConfig = new CreatureConfig
    {
      Name = droneConfigName,
      Consumables = [],
      SpawnConfigs = [spawnConfig],
      Faction = Character.Faction.Demon // or forests if demon is glitchy,
    };

    // Create CustomCreature
    var customXeno = new CustomCreature(xenoPrefab, true, creatureConfig);

    CreatureManager.Instance.AddCreature(customXeno);

    // Add a spawn config (vanilla world spawner)
  }

  public static void RegisterXenoSpawnOld()
  {

    // required for character to work...but not ideal for xeno.
    // var capsule = xenoPrefab.AddComponent<CapsuleCollider>();
    //
    // var visualTransform = xenoPrefab.transform.Find("Visual");
    // if (!visualTransform)
    // {
    //   var visualObj = new GameObject("Visual");
    //   visualObj.transform.SetParent(xenoPrefab.transform);
    // }

    // var nv = xenoPrefab.AddComponent<ZNetView>();
    // xenoPrefab.AddComponent<ZSyncTransform>();
    // xenoPrefab.AddComponent<ZSyncAnimation>();
    //
    // var animator = xenoPrefab.GetComponentInChildren<Animator>();
    // // must be on animator gameobject
    // var animEvent = animator.gameObject.AddComponent<CharacterAnimEvent>();
    //
    // animEvent.m_nview = nv;
    //
    // // todo might need to patch this significantly for alien AI...or use BaseAI.
    // // todo might have to add FootStep and VisEquipment
    //
    // var monsterAI = xenoPrefab.AddComponent<XenoDrone_MonsterAI>();
    // // todo determine if monsters that do not use human rigs can use humanoid
    // // var humanoid = xenoPrefab.AddComponent<Humanoid>();
    // var humanoid = xenoPrefab.AddComponent<Humanoid>();
    // humanoid.m_animator = animator;
    // var xenoAnimationController = xenoPrefab.GetComponentInChildren<XenoAnimationController>();
    //
    // monsterAI.m_huntPlayer = true;
    // monsterAI.m_enableHuntPlayer = true;
    //
    //
    // // todo setup character better
    // humanoid.m_health = 150f;
    //
    // var footStep = xenoPrefab.AddComponent<FootStep>();
    // footStep.m_animator = animator;
    //
    // monsterAI.m_pathAgentType = Pathfinding.AgentType.Humanoid;
    //
    // monsterAI.m_character = humanoid;
  }

  public static void RegisterAllPrefabs()
  {
    LoadAssemblies();
    RegisterPlaceableAlien();
  }
  public static void RegisterAllCreatures()
  {
    LoadAssemblies();
    RegisterXenoSpawn();
  }
}