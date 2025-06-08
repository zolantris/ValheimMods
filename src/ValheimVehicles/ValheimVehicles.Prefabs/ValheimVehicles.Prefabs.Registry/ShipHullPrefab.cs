using System;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.SharedScripts.Prefabs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public static Shader MaskShader = null!;

  /// <summary>
  /// Main method for all hull registry. This method is not efficient for iteration however, it keeps all hull variants together instead of spacing them everywhere.
  /// </summary>
  /// <param name="prefabManager"></param>
  /// <param name="pieceManager"></param>
  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var sizeVariants = new[]
    {
      PrefabNames.PrefabSizeVariant.TwoByTwo,
      PrefabNames.PrefabSizeVariant.FourByFour
    };
    var hullMaterialTypes = new[]
      { HullMaterial.Wood, HullMaterial.Iron };

    PrefabNames.DirectionVariant[] ribDirections =
    [
      PrefabNames.DirectionVariant.Left,
      PrefabNames.DirectionVariant.Right
    ];

    RegisterWindowWallPorthole2x2Iron();
    RegisterWindowWallPorthole4x4Iron();
    RegisterWindowWallPorthole8x4Iron();
    RegisterWindowFloorPorthole4x4Iron();

    RegisterCornerWindow("corner_windows");
    RegisterCornerWindow("corner_windows_wide");
    RegisterCornerWindow("corner_windows_narrow");

    RegisterDoubleHullProw();
    RegisterDoubleHullWall();

    foreach (var hullMaterialType in hullMaterialTypes)
      RegisterHull(PrefabNames.GetShipHullCenterName(hullMaterialType), hullMaterialType,
        16 + 16 + 4,
        PrefabNames.PrefabSizeVariant.FourByEight);

    // does not have a size variant
    foreach (var hullMaterialType in hullMaterialTypes)
    {
      RegisterHullRibProw(hullMaterialType, PrefabNames.PrefabSizeVariant.TwoByTwo);
      RegisterHullRib(PrefabNames.GetHullRibName(hullMaterialType), hullMaterialType);
      RegisterHullRibCorner(hullMaterialType);

      foreach (var ribDirection in ribDirections)
      {
        RegisterHullRibProwCorner(hullMaterialType, ribDirection);
        RegisterHullRibCornerFloor(hullMaterialType, ribDirection);
      }
    }

    foreach (var hullMaterialType in hullMaterialTypes)
    foreach (var sizeVariant in sizeVariants)
    {
      var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);
      RegisterHull(
        PrefabNames.GetHullSlabName(hullMaterialType, sizeVariant),
        hullMaterialType,
        materialCount,
        sizeVariant);
    }

    foreach (var hullMaterialType in hullMaterialTypes)
    foreach (var sizeVariant in sizeVariants)
    {
      var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);
      RegisterHull(
        PrefabNames.GetHullWallName(hullMaterialType, sizeVariant),
        hullMaterialType,
        materialCount,
        sizeVariant);
    }
  }

  public static void RegisterWindowPortholeIronStandalone()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.WindowPortholeStandalonePrefab,
        LoadValheimVehicleAssets.ShipWindowPortholeStandalone);

    SetupHullPrefab(prefab, PrefabNames.WindowPortholeStandalonePrefab,
      HullMaterial.Iron, 1, prefab.transform.FindDeepChild("trim"));
  }

  public static void RegisterWindowWallPorthole2x2Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.WindowWallPorthole2x2Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall2x2);

    SetupHullPrefab(prefab, PrefabNames.WindowWallPorthole2x2Prefab,
      HullMaterial.Iron, 3);
  }

  public static void RegisterWindowWallPorthole4x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.WindowWallPorthole4x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall4x4);

    SetupHullPrefab(prefab, PrefabNames.WindowWallPorthole4x4Prefab,
      HullMaterial.Iron, 6);
  }

  public static void RegisterWindowWallPorthole8x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.WindowWallPorthole8x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeWall8x4);

    SetupHullPrefab(prefab, PrefabNames.WindowWallPorthole8x4Prefab,
      HullMaterial.Iron, 8);
  }

  public static void RegisterWindowFloorPorthole4x4Iron()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.WindowFloorPorthole4x4Prefab,
        LoadValheimVehicleAssets.ShipWindowPortholeFloor4x4);

    SetupHullPrefab(prefab, PrefabNames.WindowFloorPorthole4x4Prefab,
      HullMaterial.Iron, 6);
    var wnt = prefab.GetComponent<WearNTear>();

    // windows are half health
    wnt.m_health /= 2f;
  }

  public static RequirementConfig[] GetRequirements(string material,
    int materialCount)
  {
    RequirementConfig[] requirements = [];
    return material switch
    {
      HullMaterial.Iron =>
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
      HullMaterial.Wood =>
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
    wnt.m_roof = wnt.gameObject;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect =
      LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_burnable = hullMaterial != HullMaterial.Iron;
    wnt.m_ashDamageImmune = hullMaterial == HullMaterial.Iron;
  }

  /// <summary>
  /// Experimental, will likely not be used in production without a feature flag flip
  /// </summary>
  /// TODO if this is kept it needs to update the retreived piece data with IsInverse = true and inline the prefab registry.
  /// 
  /// <param name="hullMaterial"></param>
  /// <param name="directionVariant"></param>
  public void RegisterInverseHullRibCorner(string hullMaterial,
    PrefabNames.DirectionVariant directionVariant)
  {
    // var prefabName = PrefabNames.GetHullRibCornerName(hullMaterial);
    // var prefabInverseName = $"{prefabName}_inverse";
    // // must get the opposite IE if left get right for the flipped mesh to align
    // var prefabAsset =
    //   LoadValheimVehicleAssets.GetShipHullRibCorner(hullMaterial);
    // var prefabInverse =
    //   PrefabManager.Instance.CreateClonedPrefab(
    //     prefabInverseName, prefabAsset);
    //
    // // must flip this mesh upside down and then rotate 180 on X.
    // prefabInverse.transform.FindDeepChild("mesh").rotation =
    //   Quaternion.Euler(180f, 180f, 0);
    //
    // // does not need prefabInverseName
    // // todo might need to add a inverse description
    // SetupHullPrefab(prefabInverse, prefabName,
    //   hullMaterial, 1, prefabInverse.transform.FindDeepChild("mesh"),
    //   ["mesh"]);
  }

  public void RegisterHullRibProwCorner(
    string hullMaterial, PrefabNames.DirectionVariant directionVariant)
  {
    var prefabName = PrefabNames.GetHullRibCornerProwName(hullMaterial, directionVariant);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibCornerProw(hullMaterial, directionVariant);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial, 4, prefab.transform.FindDeepChild("mesh"));
  }


  public void RegisterHullRibCorner(
    string hullMaterial, bool hasInverse = true)
  {
    var prefabName = PrefabNames.GetHullRibCornerName(hullMaterial);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibCorner(hullMaterial);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial, 4, prefab.transform.FindDeepChild("mesh"));

    // if (hasInverse)
    //   RegisterInverseHullRibCorner(hullMaterial,
    //     directionVariant);
  }

  public PrefabNames.DirectionVariant GetInverseDirection(PrefabNames.DirectionVariant variant)
  {
    return variant == PrefabNames.DirectionVariant.Left
      ? PrefabNames.DirectionVariant.Right
      : PrefabNames.DirectionVariant.Left;
  }


  /// <summary>
  /// Registers all Hull-corner-floors (seals a hull rib)
  /// </summary>
  public void RegisterHullRibCornerFloor(
    string hullMaterial,
    PrefabNames.DirectionVariant directionVariant)
  {
    var prefabName = PrefabNames.GetHullRibCornerFloorName(hullMaterial,
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

  public void RegisterCornerWindow(string assetName)
  {
    var prefabAsset = PrefabRegistryController.vehicleAssetBundle.LoadAsset<GameObject>($"{assetName}.prefab");
    if (!prefabAsset)
    {
      LoggerProvider.LogError($"Failed to load {assetName}.prefab");
      return;
    }

    var prefabName = $"{PrefabNames.ValheimVehiclesPrefix}_{assetName}";
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.FourByEight);

    // placeholder.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = $"Window {assetName}",
      Description = "A premium window",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Iron,
      materialCount);

    LoggerProvider.LogDebug("Successfully registered double hull prow");
  }

  public void RegisterDoubleHullWall()
  {
    var prefabName = $"{PrefabNames.GetHullWallName(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.FourByEight)}_double_hull_wall";
    var prefabAsset =
      LoadValheimVehicleAssets.HullDoubleWall;
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);
    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.FourByEight);
    // placeholder.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "double wall",
      Description = "Flamemetal wall",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Iron,
      materialCount);

    LoggerProvider.LogDebug("Successfully registered double hull prow");
  }

  public void RegisterDoubleHullProw()
  {
    var prefabName = $"{PrefabNames.GetHullProwVariants(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.FourByEight)}_double";
    var prefabAsset =
      LoadValheimVehicleAssets.HullDoubleProw;
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.FourByEight);

    // placeholder.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_storage_eitr",
      Description = "$valheim_vehicles_mechanism_power_storage_eitr_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Iron,
      materialCount);

    LoggerProvider.LogDebug("Successfully registered double hull prow");
  }

  public void RegisterHullRibProw(
    string hullMaterial,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var prefabName = PrefabNames.GetHullProwVariants(hullMaterial, sizeVariant);
    var prefabAsset =
      LoadValheimVehicleAssets.GetShipHullRibProw(hullMaterial, sizeVariant);
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      materialCount);
  }

  private static void SetupHullPrefab(
    GameObject prefab,
    string prefabName,
    string hullMaterial,
    int materialCount,
    Transform? hoistParent = null, string[]? hoistFilters = null)
  {
    try
    {
      PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
        hoistParent ?? prefab.transform,
        hoistFilters);

      var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
      PrefabRegistryHelpers.SetWearNTearSupport(wnt,
        WearNTear.MaterialType.Iron);

      SetHullWnt(wnt, hullMaterial);

      ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, materialCount);
      PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

      PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
      var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);
      piece.m_clipGround = true;
      piece.m_allowRotatedOverlap = true;
      piece.m_noClipping = false;

      PieceManager.Instance.AddPiece(new CustomPiece(prefab, false,
        new PieceConfig
        {
          PieceTable = PrefabRegistryController.GetPieceTableName(),
          Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
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
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    if (prefabName.Contains(PrefabNames.HullWall))
    {
      if (sizeVariant == PrefabNames.PrefabSizeVariant.FourByFour)
        return hullMaterial.Equals(HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullWall4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullWall4X4WoodAsset;

      return hullMaterial.Equals(HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullWall2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullWall2X2WoodAsset;
    }

    if (prefabName.Contains(PrefabNames.HullSlab))
    {
      if (sizeVariant == PrefabNames.PrefabSizeVariant.FourByFour)
        return hullMaterial.Equals(HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullSlab4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullSlab4X4WoodAsset;

      return hullMaterial.Equals(HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullSlab2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullSlab2X2WoodAsset;
    }

    return hullMaterial.Equals(HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullIronAsset
      : LoadValheimVehicleAssets.ShipHullWoodAsset;
  }

  private static void RegisterHull(
    string prefabName,
    string hullMaterial,
    int materialCount,
    PrefabNames.PrefabSizeVariant prefabSizeVariant)
  {
    var prefabClone =
      GetShipHullAssetByMaterial(prefabName, hullMaterial, prefabSizeVariant);

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabClone);

    var hoistParents = new[] { "new", "snappoints" };

    if (prefabName.Contains(PrefabNames.ShipHullPrefabName))
      hoistParents.AddItem("hull_slab_new_shared");

    var wntNewParent = prefab.transform.Find("new");

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      materialCount,
      wntNewParent,
      hoistParents
    );
  }
}