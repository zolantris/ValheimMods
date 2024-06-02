using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class RamPrefabs : IRegisterPrefab
{
  public static readonly RamPrefabs Instance = new();


  public struct RamBladeVariant
  {
    public string prefabName;
    public GameObject asset;
  }

  private const float minimumHitInterval = 0.5f;

  /// <summary>
  /// @todo make this a Config JSON or Value in Vehicles config 
  /// </summary>
  private const float hitInterval = 2.0f;

  private const float hitRadius = 10;

  private Aoe SetupAoeComponent(GameObject colliderObj)
  {
    var aoe = colliderObj.AddComponent<Aoe>();
    aoe.m_blockable = false;
    aoe.m_dodgeable = false;
    aoe.m_hitTerrain = true;
    aoe.m_hitCharacters = true;
    aoe.m_hitFriendly = true;
    aoe.m_hitEnemy = true;
    aoe.m_hitParent = false;
    aoe.m_hitInterval = Mathf.Max(minimumHitInterval, hitInterval);
    // todo need to tweak this
    aoe.m_damageSelf = 0;
    // aoe.m_scaleDamageByDistance = true;
    aoe.m_toolTier = 100;
    aoe.m_attackForce = 5;
    aoe.m_radius = hitRadius;
    aoe.m_useTriggers = true;
    aoe.m_triggerEnterOnly = false;
    aoe.m_useCollider = null;
    aoe.m_useAttackSettings = true;
    aoe.m_ttl = 0;
    aoe.m_canRaiseSkill = true;
    aoe.m_skill = Skills.SkillType.None;
    aoe.m_backstabBonus = 1;
    // aoe.m_ttlMax = 20;
    return aoe;
  }

  private void RegisterRamNose()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.RamNose,
        LoadValheimVehicleAssets.RamNose);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.RamNose, prefab);

    var noseColliderObj = prefab.transform.Find("damage_colliders")?.gameObject;
    PrefabRegistryHelpers.SetWearNTear(prefab, 3);

    // just a safety check should always work unless and update breaks things
    if (noseColliderObj != null)
    {
      var aoe = SetupAoeComponent(noseColliderObj);
      aoe.m_damage = new HitData.DamageTypes()
      {
        m_blunt = 50,
        m_pickaxe = 200,
      };
    }

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 5,
          Item = "Bronze",
          Recover = true,
        }
      ]
    }));
  }

  private void RegisterRamBlade()
  {
    RamBladeVariant[] loadedAssets =
    [
      new RamBladeVariant()
      {
        asset = LoadValheimVehicleAssets.RamBladeTop,
        prefabName = "top",
      },
      new RamBladeVariant()
      {
        asset = LoadValheimVehicleAssets.RamBladeBottom,
        prefabName = "bottom",
      },
      new RamBladeVariant()
      {
        asset = LoadValheimVehicleAssets.RamBladeLeft,
        prefabName = "left",
      },
      new RamBladeVariant()
      {
        asset = LoadValheimVehicleAssets.RamBladeRight,
        prefabName = "right",
      },
    ];

    foreach (var variant in loadedAssets)
    {
      var prefabFullName = PrefabNames.GetRamBladeName(variant.prefabName);
      var prefab = PrefabManager.Instance.CreateClonedPrefab(prefabFullName, variant.asset);
      PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
      PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
      var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
      PrefabRegistryHelpers.AddPieceForPrefab(prefabFullName, prefab);
      var bladeColliderObj = prefab.transform.FindDeepChild("collider")?.gameObject;

      // just a safety check should always work unless and update breaks things
      if (bladeColliderObj != null)
      {
        var aoe = SetupAoeComponent(bladeColliderObj);
        aoe.m_damage = new HitData.DamageTypes()
        {
          m_slash = 200,
          m_pickaxe = 50,
        };
      }

      PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, new PieceConfig
      {
        PieceTable = "Hammer",
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 5,
            Item = "Bronze",
            Recover = true,
          }
        ]
      }));
    }
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterRamNose();
    RegisterRamBlade();
  }
}