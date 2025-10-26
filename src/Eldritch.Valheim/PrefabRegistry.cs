using System;
using Eldritch.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.U2D;
using Zolantris.Shared;
namespace Eldritch.Valheim;

public static class PrefabRegistry
{
  public static AssetBundle assetBundle;
  public static SpriteAtlas Sprites;
  private const string droneAssetName = "xenomorph-drone-v1";

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
    var asset = assetBundle.LoadAsset<GameObject>(droneAssetName);
    if (!asset)
    {
      LoggerProvider.LogError("Xeno prefab not found — spawn registration skipped.");
      return;
    }

    const string XenoAdultName = "Eldritch_XenoAdult_Creature";
    var xenoPrefab = PrefabManager.Instance.CreateClonedPrefab(XenoAdultName, asset);

    if (!xenoPrefab) return;

    // required for character to work...but not ideal for xeno.
    var capsule = xenoPrefab.AddComponent<CapsuleCollider>();

    var visualTransform = xenoPrefab.transform.Find("Visual");
    if (!visualTransform)
    {
      var visualObj = new GameObject("Visual");
      visualObj.transform.SetParent(xenoPrefab.transform);
    }

    var nv = xenoPrefab.AddComponent<ZNetView>();
    xenoPrefab.AddComponent<ZSyncTransform>();
    xenoPrefab.AddComponent<ZSyncAnimation>();

    var animator = xenoPrefab.GetComponentInChildren<Animator>();
    // must be on animator gameobject
    var animEvent = animator.gameObject.AddComponent<CharacterAnimEvent>();

    animEvent.m_nview = nv;

    // todo might need to patch this significantly for alien AI...or use BaseAI.
    // todo might have to add FootStep and VisEquipment

    var monsterAI = xenoPrefab.AddComponent<XenoDrone_MonsterAI>();
    // todo determine if monsters that do not use human rigs can use humanoid
    // var humanoid = xenoPrefab.AddComponent<Humanoid>();
    var humanoid = xenoPrefab.AddComponent<Humanoid>();
    humanoid.m_animator = animator;
    var xenoAnimationController = xenoPrefab.GetComponentInChildren<XenoAnimationController>();

    monsterAI.m_huntPlayer = true;

    humanoid.m_eye = xenoAnimationController.neckPivot;
    humanoid.m_head = xenoAnimationController.neckPivot;
    // humanoid.m_unarmedWeapon = xenoPrefab.AddComponent<ItemDrop>();

    // humanoid.m_unarmedWeapon.m_itemData = new ItemDrop.ItemData
    // {
    //   m_shared =
    //   {
    //     m_attack = new Attack
    //     {
    //       m_animEvent = animEvent,
    //       m_attackType = Attack.AttackType.Horizontal
    //     },
    //     m_secondaryAttack = new Attack
    //     {
    //       m_animEvent = animEvent,
    //       m_attackType = Attack.AttackType.Vertical
    //     }
    //   }
    // };


    // todo setup character better
    humanoid.m_health = 150f;
    monsterAI.m_pathAgentType = Pathfinding.AgentType.Humanoid;

    monsterAI.m_character = humanoid;

    var spawnConfig = new SpawnConfig
    {
      Name = "$eldritch_xeno_drone",
      Biome = Heightmap.Biome.All, // for now all biomes can spawn alien.
      SpawnChance = 100f, // 100%
      MinGroupSize = 1,
      MaxGroupSize = 3,
      SpawnInterval = 600f,
      SpawnAtDay = true,
      SpawnAtNight = true,
      MaxAltitude = 1000f,
      MinAltitude = 0f,
      HuntPlayer = true
    };

    var creatureConfig = new CreatureConfig
    {
      Name = "$eldritch_xeno_drone",
      Consumables = [],
      SpawnConfigs = [spawnConfig],
      Faction = Character.Faction.Demon // or forests if demon is glitchy,
    };

    // Create CustomCreature
    var customXeno = new CustomCreature(xenoPrefab, true, creatureConfig);

    CreatureManager.Instance.AddCreature(customXeno);

    // Add a spawn config (vanilla world spawner)
  }

  public static void RegisterAllPrefabs()
  {
    LoadAssemblies();
    RegisterPlaceableAlien();
    RegisterXenoSpawn();
  }
}