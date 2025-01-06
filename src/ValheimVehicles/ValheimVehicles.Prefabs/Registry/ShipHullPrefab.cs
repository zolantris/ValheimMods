using System;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Vehicles.Components;
using static ValheimVehicles.Prefabs.PrefabNames;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public static Shader MaskShader = null!;

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var sizeVariants = new[]
    {
      PrefabSizeVariant.TwoByTwo,
      PrefabSizeVariant.FourByFour
    };
    var hullMaterialTypes = new[]
      { ShipHulls.HullMaterial.Wood, ShipHulls.HullMaterial.Iron };

    DirectionVariant[] ribDirections =
    [
      DirectionVariant.Left,
      DirectionVariant.Right
    ];

    RegisterWindowPortholeIronStandalone();
    RegisterWindowWallPorthole2x2Iron();
    RegisterWindowWallPorthole4x4Iron();
    RegisterWindowWallPorthole8x4Iron();
    RegisterWindowFloorPorthole4x4Iron();
    RegisterWindowWallSquareWood();
    RegisterWindowWallSquareIron();

    foreach (var hullMaterialType in hullMaterialTypes)
    {
      RegisterHull(GetShipHullCenterName(hullMaterialType), hullMaterialType,
        16 + 16 + 4,
        PrefabSizeVariant.FourByEight);

      RegisterHullRib(GetHullRibName(hullMaterialType), hullMaterialType);

      // does not have a size variant
      foreach (var ribDirection in ribDirections)
      {
        RegisterHullRibCorner(
          hullMaterialType,
          ribDirection);
        RegisterHullRibCornerFloor(hullMaterialType, ribDirection);
      }

      foreach (var sizeVariant in sizeVariants)
      {
        var materialCount = GetPrefabSizeArea(sizeVariant);
        RegisterHull(
          GetHullSlabName(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant);

        RegisterHull(
          GetHullWallName(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant);

        // hull-prow
        RegisterHullRibProw(hullMaterialType, sizeVariant);
      }
    }
  }

  public static void RegisterWindowPortholeIronStandalone()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowPortholeStandalonePrefab,
        LoadValheimVehicleAssets.ShipWindowPortholeStandalone);

    SetupHullPrefab(prefab, WindowPortholeStandalonePrefab,
      ShipHulls.HullMaterial.Iron, 1, prefab.transform.FindDeepChild("trim"));
  }

  public static void RegisterWindowWallPorthole2x2Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowWallPorthole2x2Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall2x2);

    SetupHullPrefab(prefab, WindowWallPorthole2x2Prefab,
      ShipHulls.HullMaterial.Iron, 3);
  }
  
  public static void RegisterWindowWallPorthole4x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowWallPorthole4x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall4x4);

    SetupHullPrefab(prefab, WindowWallPorthole4x4Prefab,
      ShipHulls.HullMaterial.Iron, 6);
  }

  public static void RegisterWindowWallPorthole8x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowWallPorthole8x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall8x4);

    SetupHullPrefab(prefab, WindowWallPorthole8x4Prefab,
      ShipHulls.HullMaterial.Iron, 8);
  }

  public static void RegisterWindowFloorPorthole4x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowFloorPorthole4x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeFloor4x4);

    SetupHullPrefab(prefab, WindowFloorPorthole4x4Prefab,
      ShipHulls.HullMaterial.Iron, 6);
  }

  public static void RegisterWindowWallSquareWood()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowWallSquareWoodPrefabName,
        LoadValheimVehicleAssets.ShipWindowSquareWallWood);

    SetupHullPrefab(prefab, WindowWallSquareWoodPrefabName,
      ShipHulls.HullMaterial.Wood, 3);
  }

  public static void RegisterWindowWallSquareIron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        WindowWallSquareIronPrefabName,
        LoadValheimVehicleAssets.ShipWindowSquareWallIron);

    SetupHullPrefab(prefab, WindowWallSquareIronPrefabName,
      ShipHulls.HullMaterial.Iron, 3);
  }

  public static RequirementConfig[] GetRequirements(string material,
    int materialCount)
  {
    RequirementConfig[] requirements = [];
    return material switch
    {
      ShipHulls.HullMaterial.Iron =>
      [
        new RequirementConfig
        {
          Amount = Mathf.RoundToInt(Mathf.Clamp(materialCount / 4f, 1, 10)),
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = Mathf.RoundToInt(Mathf.Clamp(materialCount / 4f, 1, 10)),
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2 * materialCount, Item = "BronzeNails", Recover = true
        },
        new RequirementConfig
          { Amount = 1 * materialCount, Item = "YggdrasilWood", Recover = true }
      ],
      ShipHulls.HullMaterial.Wood =>
      [
        new RequirementConfig
          { Amount = 2 * materialCount, Item = "Wood", Recover = true }
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
    wnt.m_switchEffect =
      LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_burnable = hullMaterial != ShipHulls.HullMaterial.Iron;
  }

  /// <summary>
  /// Experimental, will likely not be used in production without a feature flag flip
  /// </summary>
  /// <param name="hullMaterial"></param>
  /// <param name="directionVariant"></param>
  public void RegisterInverseHullRibCorner(string hullMaterial,
    DirectionVariant directionVariant)
  {
    var prefabName = GetHullRibCornerName(hullMaterial,
      directionVariant);
    var prefabInverseName = $"{prefabName}_inverse";
    var inverseDirection = GetInverseDirection(directionVariant);

    // must get the opposite IE if left get right for the flipped mesh to align
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibCorner(hullMaterial,
        inverseDirection);
    var prefabInverse =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabInverseName, prefabAsset);

    // must flip this mesh upside down and then rotate 180 on X.
    prefabInverse.transform.FindDeepChild("mesh").rotation =
      Quaternion.Euler(180f, 180f, 0);

    // does not need prefabInverseName
    // todo might need to add a inverse description
    SetupHullPrefab(prefabInverse, prefabName,
      hullMaterial, 1, prefabInverse.transform.FindDeepChild("mesh"),
      ["mesh"], true);
  }

  public void RegisterHullRibCorner(
    string hullMaterial,
    DirectionVariant directionVariant, bool hasInverse = true)
  {
    var prefabName = GetHullRibCornerName(hullMaterial,
      directionVariant);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibCorner(hullMaterial,
        directionVariant);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial, 4, prefab.transform.FindDeepChild("mesh"));

    if (hasInverse)
      RegisterInverseHullRibCorner(hullMaterial,
        directionVariant);
  }

  public DirectionVariant GetInverseDirection(DirectionVariant variant)
  {
    return variant == DirectionVariant.Left
      ? DirectionVariant.Right
      : DirectionVariant.Left;
  }


  /// <summary>
  /// Registers all Hull-corner-floors (seals a hull rib)
  /// </summary>
  public void RegisterHullRibCornerFloor(
    string hullMaterial,
    DirectionVariant directionVariant)
  {
    var prefabName = GetHullRibCornerFloorName(hullMaterial,
      directionVariant);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullCornerFloor(hullMaterial,
        directionVariant);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial, 1, prefab.transform.FindDeepChild("mesh"), ["mesh"]);
  }

  public void RegisterHullRibProw(
    string hullMaterial,
    PrefabSizeVariant sizeVariant)
  {
    var prefabName = GetHullProwVariants(hullMaterial, sizeVariant);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibProw(hullMaterial, sizeVariant);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    var materialCount = GetPrefabSizeArea(sizeVariant);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      materialCount);
  }

  private static void SetupHullPrefab(
    GameObject prefab,
    string prefabName,
    string hullMaterial,
    int materialCount,
    Transform? hoistParent = null, string[]? hoistFilters = null,
    bool isInverse = false)
  {
    try
    {
      var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
      PrefabRegistryHelpers.SetWearNTearSupport(wnt,
        WearNTear.MaterialType.Iron);

      SetHullWnt(wnt, hullMaterial);

      ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, materialCount);
      PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

      PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
      PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab, isInverse);

      PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
        hoistParent ?? prefab.transform,
        hoistFilters);

      PieceManager.Instance.AddPiece(new CustomPiece(prefab, false,
        new PieceConfig
        {
          PieceTable = "Hammer",
          Category = ValheimRaftMenuName,
          Enabled = true,
          Requirements = GetRequirements(hullMaterial, materialCount)
        }));
    }
    catch (Exception e)
    {
      Logger.LogError(e);
    }
  }


  /// <summary>
  /// Registers all hull ribs
  /// </summary>
  /// <param name="prefabName"></param>
  /// <param name="hullMaterial"></param>
  private static void RegisterHullRib(
    string prefabName,
    string hullMaterial)
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, LoadValheimVehicleAssets.GetShipHullRib(hullMaterial));

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      8,
      prefab.transform.Find("new") ?? prefab.transform);
  }


  /// <summary>
  /// TODO refactor for LoadAssetBundle dynamic string approach
  /// </summary>
  /// <param name="prefabName"></param>
  /// <param name="hullMaterial"></param>
  /// <param name="sizeVariant"></param>
  /// <returns></returns>
  private static GameObject GetShipHullAssetByMaterial(string prefabName,
    string hullMaterial,
    PrefabSizeVariant sizeVariant)
  {
    if (prefabName.Contains(HullWall))
    {
      if (sizeVariant == PrefabSizeVariant.FourByFour)
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullWall4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullWall4X4WoodAsset;

      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullWall2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullWall2X2WoodAsset;
    }

    if (prefabName.Contains(HullSlab))
    {
      if (sizeVariant == PrefabSizeVariant.FourByFour)
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullSlab4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullSlab4X4WoodAsset;

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
    PrefabSizeVariant prefabSizeVariant)
  {
    var prefabClone =
      GetShipHullAssetByMaterial(prefabName, hullMaterial, prefabSizeVariant);

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabClone);

    var hoistParents = new[] { "new" };

    if (prefabName.Contains(ShipHullPrefabName))
      hoistParents.AddItem("hull_slab_new_shared");

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      materialCount,
      prefab.transform.Find("new") ?? prefab.transform,
      hoistParents
    );
  }
}