using System;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class RamPrefabRegistry : RegisterPrefab<RamPrefabRegistry>
{
  public enum RamType
  {
    Stake,
    Blade,
    WaterVehicle,
    LandVehicle
  }

  public struct RamVariant
  {
    public string prefabName;
    public GameObject asset;
    public string material;
    public int size;
  }

  public static bool IsRam(string objName)
  {
    return objName.StartsWith(PrefabNames.RamBladePrefix) ||
           objName.StartsWith(PrefabNames.RamStakePrefix);
  }


  private static VehicleRamAoe SetupAoeComponent(GameObject colliderObj)
  {
    var aoe = colliderObj.AddComponent<VehicleRamAoe>();
    return aoe;
  }

  private static void IgnoreNestedCollider(GameObject go)
  {
    var childColliders = go.GetComponentsInChildren<Collider>();
    var colliderFrontPhysicalOnly =
      childColliders.FirstOrDefault((collider) =>
        collider.name == "collider_front_physical");
    var damageCollider =
      childColliders.FirstOrDefault((collider) =>
        collider.name == "collider_front");
    if (!colliderFrontPhysicalOnly || !damageCollider) return;
    Physics.IgnoreCollision(damageCollider, colliderFrontPhysicalOnly, true);
  }

  private static void RegisterRamStake()
  {
    RamVariant[] loadedAssets =
    [
      new()
      {
        asset = LoadValheimVehicleAssets.RamStakeWood1X2,
        prefabName = "1x2",
        material = PrefabTiers.Tier1,
        size = 1
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamStakeWood2X4,
        prefabName = "2x4",
        material = PrefabTiers.Tier1,
        size = 2
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamStakeIron1X2,
        prefabName = "1x2",
        material = PrefabTiers.Tier3,
        size = 1
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamStakeIron2X4,
        prefabName = "2x4",
        material = PrefabTiers.Tier3,
        size = 2
      }
    ];


    foreach (var variant in loadedAssets)
    {
      var size = variant.size;
      var prefabFullName = PrefabNames.GetRamStakeName(variant.material, size);
      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(prefabFullName,
          variant.asset);
      prefab.name = prefabFullName;
      PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
      PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
      var wnt = PrefabRegistryHelpers.SetWearNTear(prefab,
        PrefabTiers.GetTierValue(variant.material));
      IgnoreNestedCollider(prefab.gameObject);

      PrefabRegistryHelpers.AddPieceForPrefab(prefabFullName, prefab);
      var bladeColliderObj = prefab.transform.FindDeepChild("damage_colliders")
        ?.gameObject;

      // just a safety check should always work unless and update breaks things
      if (bladeColliderObj != null)
      {
        var ramAoe = SetupAoeComponent(bladeColliderObj);
        ramAoe.materialTier = variant.material;
        ramAoe.m_RamType = RamType.Stake;
      }
      else
      {
        Logger.LogError(
          "Could not found damage_colliders within ram prefab, this likely means that rams are broken");
      }

      PrefabRegistryController.AddPiece(new CustomPiece(prefab, false,
        new PieceConfig
        {
          PieceTable = PrefabRegistryController.GetPieceTableName(),
          Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
          Enabled = true,
          Requirements =
          [
            new RequirementConfig
            {
              Amount = 3 * size,
              Item = "Iron",
              Recover = true
            },
            new RequirementConfig
            {
              Amount = 2 * size,
              Item = "RoundLog",
              Recover = true
            }
          ]
        }));
    }
  }

  private static void RegisterRamBlade()
  {
    RamVariant[] loadedAssets =
    [
      new()
      {
        asset = LoadValheimVehicleAssets.RamBladeTop,
        prefabName = "top"
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamBladeBottom,
        prefabName = "bottom"
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamBladeLeft,
        prefabName = "left"
      },
      new()
      {
        asset = LoadValheimVehicleAssets.RamBladeRight,
        prefabName = "right"
      }
    ];

    foreach (var variant in loadedAssets)
    {
      var prefabFullName = PrefabNames.GetRamBladeName(variant.prefabName);
      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(prefabFullName,
          variant.asset);
      PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
      PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
      var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
      PrefabRegistryHelpers.AddPieceForPrefab(prefabFullName, prefab);
      var bladeColliderObj = prefab.transform.FindDeepChild("damage_colliders")
        ?.gameObject;

      // just a safety check should always work unless and update breaks things
      if (bladeColliderObj != null)
      {
        var ramAoe = SetupAoeComponent(bladeColliderObj);
        ramAoe.m_RamType = RamType.Blade;
        ramAoe.materialTier = variant.material;
      }
      else
      {
        Logger.LogError(
          "Could not found damage_colliders within ram prefab, this likely means that rams are broken");
      }

      PrefabRegistryController.AddPiece(new CustomPiece(prefab, false,
        new PieceConfig
        {
          PieceTable = PrefabRegistryController.GetPieceTableName(),
          Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
          Enabled = true,
          Requirements =
          [
            new RequirementConfig
            {
              Amount = 10,
              Item = "Bronze",
              Recover = true
            },
            new RequirementConfig
            {
              Amount = 70,
              Item = "BronzeNails",
              Recover = true
            },
            new RequirementConfig
            {
              Amount = 2,
              Item = "Iron",
              Recover = true
            }
          ]
        }));
    }
  }

  public override void OnRegister()
  {
    RegisterRamStake();
    RegisterRamBlade();
  }
}