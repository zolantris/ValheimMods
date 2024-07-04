using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using static ValheimVehicles.Prefabs.PrefabNames;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var sizeVariants = new[]
    {
      PrefabSizeVariant.TwoByTwo,
      PrefabSizeVariant.FourByFour
    };
    var hullMaterialTypes = new[] { ShipHulls.HullMaterial.Wood, ShipHulls.HullMaterial.Iron };

    DirectionVariant[] ribDirections =
    [
      DirectionVariant.Left,
      DirectionVariant.Right
    ];

    foreach (var hullMaterialType in hullMaterialTypes)
    {
      RegisterHull(GetShipHullCenterName(hullMaterialType), hullMaterialType, 8,
        PrefabSizeVariant.FourByFour,
        prefabManager, pieceManager);

      // does not have a size variant
      foreach (var ribDirection in ribDirections)
      {
        RegisterHullRibCorner(
          GetHullRibCornerName(ShipHulls.HullMaterial.Wood,
            DirectionVariant.Left),
          hullMaterialType,
          ribDirection,
          prefabManager,
          pieceManager);
      }

      foreach (var sizeVariant in sizeVariants)
      {
        var materialCount = GetPrefabSizeArea(sizeVariant);
        RegisterHull(
          GetHullSlabName(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant,
          prefabManager,
          pieceManager);

        RegisterHull(
          GetHullWallVariants(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant,
          prefabManager,
          pieceManager);

        // hull-prow
        RegisterHullRibProw(GetHullProwVariants(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant,
          prefabManager,
          pieceManager);
      }
    }

    // hulls 4x8

    RegisterHull(ShipHullCenterIronPrefabName, ShipHulls.HullMaterial.Iron,
      8, PrefabSizeVariant.FourByFour, prefabManager, pieceManager);


    // hull-ribs
    RegisterHullRib(ShipHullRibWoodPrefabName, ShipHulls.HullMaterial.Wood,
      prefabManager, pieceManager);
    RegisterHullRib(ShipHullRibIronPrefabName, ShipHulls.HullMaterial.Iron,
      prefabManager, pieceManager);
  }

  public static RequirementConfig[] GetRequirements(string material, int materialCount)
  {
    RequirementConfig[] requirements = [];
    return material switch
    {
      ShipHulls.HullMaterial.Iron =>
      [
        new RequirementConfig { Amount = materialCount, Item = "Iron", Recover = true },
        new RequirementConfig { Amount = materialCount, Item = "Bronze", Recover = true },
        new RequirementConfig
        {
          Amount = 10 * materialCount, Item = "BronzeNails", Recover = true
        }
      ],
      ShipHulls.HullMaterial.Wood =>
      [
        new RequirementConfig { Amount = 5 * materialCount, Item = "Wood", Recover = true }
      ],
      _ => requirements
    };
  }

  public static void SetHullWnt(WearNTear wnt, string hullMaterial)
  {
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_burnable = hullMaterial != ShipHulls.HullMaterial.Iron;
  }

  public void RegisterHullRibCorner(string prefabName,
    string hullMaterial,
    DirectionVariant directionVariant,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefabAsset = LoadValheimVehicleAssets.GetShipHullRibCorner(hullMaterial, directionVariant);
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      pieceManager);
  }

  public void RegisterHullRibProw(string prefabName,
    string hullMaterial,
    int materialCount,
    PrefabSizeVariant prefabSizeVariant,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefabAsset = LoadValheimVehicleAssets.GetShipHullRibProw(hullMaterial, prefabSizeVariant);
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      pieceManager);
  }

  private static void SetupHullPrefab(
    GameObject prefab,
    string prefabName,
    string hullMaterial,
    PieceManager pieceManager, Transform? hoistParent = null, string[]? hoistFilters = null)
  {
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.Iron);

    SetHullWnt(wnt, hullMaterial);

    ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, 9);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab, hoistParent ?? prefab.transform,
      hoistFilters);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = GetRequirements(hullMaterial, 4)
    }));
  }

  /// <summary>
  /// Experimental not ready
  /// </summary>
  private static void RegisterHullRib(
    string prefabName,
    string hullMaterial,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, LoadValheimVehicleAssets.GetShipHullRib(hullMaterial));

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      pieceManager,
      prefab.transform.Find("new") ?? prefab.transform,
      ["shared_hull_rib", "mesh"]);
  }

  /// <summary>
  /// TODO refactor for LoadAssetBundle dynamic string approach
  /// </summary>
  /// <param name="prefabName"></param>
  /// <param name="hullMaterial"></param>
  /// <param name="sizeVariant"></param>
  /// <returns></returns>
  private static GameObject GetShipHullAssetByMaterial(string prefabName, string hullMaterial,
    PrefabSizeVariant sizeVariant)
  {
    if (prefabName.Contains(HullWall))
    {
      if (sizeVariant == PrefabSizeVariant.FourByFour)
      {
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullWall4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullWall4X4WoodAsset;
      }

      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullWall2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullWall2X2WoodAsset;
    }

    if (prefabName.Contains(HullSlab))
    {
      if (sizeVariant == PrefabSizeVariant.FourByFour)
      {
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullSlab4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullSlab4X4WoodAsset;
      }

      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullSlab2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullSlab2X2WoodAsset;
    }

    return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullIronAsset
      : LoadValheimVehicleAssets.ShipHullWoodAsset;
  }

  private static void RegisterHull(
    string prefabName,
    string hullMaterial,
    int materialCount,
    PrefabSizeVariant prefabSizeVariant,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullAssetByMaterial(prefabName, hullMaterial, prefabSizeVariant));

    var hoistParents = new[] { "new" };

    if (prefabName.Contains(ShipHullPrefabName))
    {
      hoistParents.AddItem("hull_slab_new_shared");
    }

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      pieceManager,
      prefab.transform.Find("new") ?? prefab.transform,
      hoistParents
    );

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = GetRequirements(hullMaterial, materialCount)
    }));
  }
}