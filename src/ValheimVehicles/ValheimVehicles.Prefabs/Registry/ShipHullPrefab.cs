using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Vehicles.Components;
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
    var hullMaterialTypes = new[]
      { ShipHulls.HullMaterial.Wood, ShipHulls.HullMaterial.Iron };

    DirectionVariant[] ribDirections =
    [
      DirectionVariant.Left,
      DirectionVariant.Right
    ];
    var greenish = new Color(0.1f, 0.5f, 0.5f, 0.3f);
    var maskShader = LoadValheimAssets.waterMask.GetComponent<MeshRenderer>()
      .sharedMaterial.shader;
    var waterLiquid = PrefabManager.Instance.GetPrefab("WaterSurface");
    var waterLiquidMaterial =
      waterLiquid.GetComponent<MeshRenderer>().sharedMaterial;

    AddTransparentWaterMaskPrefab("InverseMask",
      new Material(LoadValheimVehicleAssets.InvertedWaterMask),
      greenish);
    AddTransparentWaterMaskPrefab("PureMask", new Material(maskShader),
      greenish);
    AddTransparentWaterMaskPrefab("MaskWithWater", waterLiquidMaterial,
      greenish);


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


  public static void AddTransparentWaterMaskPrefab(string prefabName,
    Material material, Color color)
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(
        $"{VehicleWaterMask}{prefabName}");

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = $"$valheim_vehicles_water_mask {prefabName}";
    piece.m_description = "$valheim_vehicles_water_mask_desc";
    piece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    piece.gameObject.layer = LayerMask.NameToLayer("piece_nonsolid");
    piece.m_allowRotatedOverlap = true;
    piece.m_clipEverything = true;

    var prefabMeshRenderer = prefab.GetComponent<MeshRenderer>();
    prefabMeshRenderer.transform.localScale = Vector3.one * 4;
    prefabMeshRenderer.sharedMaterial = material;

    if (material.shader.name == LoadValheimVehicleAssets.InvertedWaterMask.name)
    {
      var waterMaskDisplacement = prefab.AddComponent<WaterMaskDisplacement>();
      waterMaskDisplacement.waterColor = color;
    }

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        Name = VehicleWaterMask,
        PieceTable = "Hammer",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .BoardingRamp),
        Category = ValheimRaftMenuName,
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 1,
            Item = "Wood",
            Recover = true
          },
        ]
      }));
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

  public void RegisterHullRibCorner(
    string hullMaterial,
    DirectionVariant directionVariant)
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
      hullMaterial, 8);
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
    Transform? hoistParent = null, string[]? hoistFilters = null)
  {
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.Iron);

    SetHullWnt(wnt, hullMaterial);

    ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, materialCount);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    // prefab.layer = 0;
    // prefab.gameObject.layer = 0;
    PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

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

  /// <summary>
  /// Experimental not ready
  /// </summary>
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
    PrefabSizeVariant prefabSizeVariant)
  {
    var prefabClone =
      GetShipHullAssetByMaterial(prefabName, hullMaterial, prefabSizeVariant);

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabClone);

    var hoistParents = new[] { "new" };

    if (prefabName.Contains(ShipHullPrefabName))
    {
      hoistParents.AddItem("hull_slab_new_shared");
    }

    SetupHullPrefab(prefab, prefabName,
      hullMaterial,
      materialCount,
      prefab.transform.Find("new") ?? prefab.transform,
      hoistParents
    );
  }
}