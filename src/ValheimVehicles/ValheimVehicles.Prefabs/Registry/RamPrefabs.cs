using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Helpers;
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


  private VehicleRamAoe SetupAoeComponent(GameObject colliderObj)
  {
    var aoe = colliderObj.AddComponent<VehicleRamAoe>();
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

    // Lazy selector for shared_ram_blade/damage_colliders, but it makes this code adaptive if the gameobject is renamed/extended in unity
    var noseColliderObj = prefab.transform.FindDeepChild("damage_colliders")?.gameObject;
    PrefabRegistryHelpers.SetWearNTear(prefab, 3);

    // just a safety check should always work unless and update breaks things
    if (noseColliderObj != null)
    {
      var aoe = SetupAoeComponent(noseColliderObj);
      aoe.SetBaseDamage(new HitData.DamageTypes()
      {
        m_blunt = 50,
        m_pickaxe = 200,
      });
    }
    else
    {
      Logger.LogError(
        "Could not found damage_colliders within ram prefab, this likely means that rams are broken");
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
      var bladeColliderObj = prefab.transform.FindDeepChild("damage_colliders")?.gameObject;

      // just a safety check should always work unless and update breaks things
      if (bladeColliderObj != null)
      {
        var aoe = SetupAoeComponent(bladeColliderObj);
        aoe.SetBaseDamage(new HitData.DamageTypes()
        {
          m_slash = 10,
          m_chop = 10,
          m_pickaxe = 10,
        });
      }
      else
      {
        Logger.LogError(
          "Could not found damage_colliders within ram prefab, this likely means that rams are broken");
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