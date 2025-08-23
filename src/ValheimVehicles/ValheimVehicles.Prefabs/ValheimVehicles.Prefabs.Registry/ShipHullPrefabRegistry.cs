using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.SharedScripts.Prefabs;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefabRegistry : RegisterPrefab<ShipHullPrefabRegistry>
{

  public override void OnRegister()
  {
    var sizeVariants = new[]
    {
      PrefabNames.PrefabSizeVariant.TwoByTwo,
      PrefabNames.PrefabSizeVariant.FourByFour
    };
    var hullMaterialTypes = new[]
      { HullMaterial.Wood, HullMaterial.Iron };


    RegisterWindowWallPorthole2x2Iron();
    RegisterWindowWallPorthole4x4Iron();
    RegisterWindowWallPorthole8x4Iron();
    RegisterWindowFloorPorthole4x4Iron();

    RegisterHullRib(HullMaterial.Wood, PrefabNames.PrefabSizeVariant.TwoByTwoByTwo);
    RegisterHullRib(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByTwo);

    RegisterHullRib(HullMaterial.Wood, PrefabNames.PrefabSizeVariant.TwoByOneByTwo);
    RegisterHullRib(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByOneByTwo);

    RegisterHullRib(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByOneByEight);


    // hull-rib-corner
    RegisterHullRibCorner(HullMaterial.Wood, null, PrefabNames.PrefabSizeVariant.TwoByTwoByTwo);
    RegisterHullRibCorner(HullMaterial.Iron, null, PrefabNames.PrefabSizeVariant.TwoByTwoByTwo);


    RegisterHullRibCorner(HullMaterial.Wood, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);
    RegisterHullRibCorner(HullMaterial.Wood, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);

    RegisterHullRibCorner(HullMaterial.Iron, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);
    RegisterHullRibCorner(HullMaterial.Iron, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);

    // for larger hulls 
    RegisterHullRibCorner(HullMaterial.Iron, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByOneByEight);
    RegisterHullRibCorner(HullMaterial.Iron, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByOneByEight);

    // hull-rib-corner-floor
    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByTwo);
    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByTwo);

    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByTwo);
    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByTwo);

    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByFour);
    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByFour);

    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByFour);
    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByFour);

    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByEight);
    RegisterHullCornerFloor(HullMaterial.Wood, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByEight);

    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Left, PrefabNames.PrefabSizeVariant.TwoByEight);
    RegisterHullCornerFloor(HullMaterial.Iron, PrefabNames.DirectionVariant.Right, PrefabNames.PrefabSizeVariant.TwoByEight);

    RegisterHullProw(HullMaterial.Wood, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);
    RegisterHullProw(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByFour);

    RegisterHullProwSeal();

    RegisterHullProwSpecialVariant(HullMaterial.Wood, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Left, "sleek");
    RegisterHullProwSpecialVariant(HullMaterial.Wood, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Right, "sleek");

    RegisterHullProwSpecialVariant(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Left, "cutter");
    RegisterHullProwSpecialVariant(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Right, "cutter");

    RegisterHullProwSpecialVariant(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Left, "sleek");
    RegisterHullProwSpecialVariant(HullMaterial.Iron, PrefabNames.PrefabSizeVariant.TwoByTwoByEight, PrefabNames.DirectionVariant.Right, "sleek");

    // todo remove iteration as it can be less stable when prefabs are updated with new variants.

    foreach (var hullMaterialType in hullMaterialTypes)
      RegisterHull(PrefabNames.GetShipHullCenterName(hullMaterialType), hullMaterialType,
        20,
        PrefabNames.PrefabSizeVariant.FourByEight);

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

    var v4Hulls = new List<string>
    {
      "hull_floor_4x4_wood",
      "hull_floor_4x4_iron",
      "hull_bow_center_wood",
      "hull_bow_curved_left_wood",
      "hull_bow_curved_right_wood",
      "hull_bow_curved_left_iron",
      "hull_bow_curved_right_iron",
      "hull_bow_tri_left_wood",
      "hull_bow_tri_right_wood",
      "hull_bow_tri_left_iron",
      "hull_bow_tri_right_iron",
      "hull_rib_aft_center_iron",
      "hull_rib_aft_center_wood",
      "hull_rib_aft_left_wood",
      "hull_rib_aft_right_wood",
      "hull_rib_aft_left_iron",
      "hull_rib_aft_right_iron",
      "hull_rib_expand_left_wood",
      "hull_rib_expand_right_wood",
      "hull_rib_expand_left_iron",
      "hull_rib_expand_right_iron",
      "hull_rib_wood",
      "hull_rib_iron",
      "hull_seal_bow_left_wood",
      "hull_seal_bow_right_wood",
      "hull_seal_bow_left_iron",
      "hull_seal_bow_right_iron",
      "hull_seal_corner_left_wood",
      "hull_seal_corner_right_wood",
      "hull_seal_corner_left_iron",
      "hull_seal_corner_right_iron",
      "hull_seal_expander_left_iron",
      "hull_seal_expander_right_iron",
      "hull_seal_expander_left_wood",
      "hull_seal_expander_right_wood",
      "hull_seal_tri_bow_left_wood",
      "hull_seal_tri_bow_right_wood",
      "hull_seal_tri_bow_left_iron",
      "hull_seal_tri_bow_right_iron",
      "hull_rail_straight_wood",
      "hull_rail_connector_wood",
      "hull_rail_25deg_wood",
      "hull_rail_45deg_wood",
      "hull_rail_corner_wood",
      "hull_rail_prow_corner_left_wood",
      "hull_rail_prow_corner_right_wood",
      "hull_rail_straight_iron",
      "hull_rail_connector_iron",
      "hull_rail_25deg_iron",
      "hull_rail_45deg_iron",
      "hull_rail_corner_iron",
      "hull_rail_prow_corner_left_iron",
      "hull_rail_prow_corner_right_iron"
    };

    v4Hulls.ForEach(x => RegisterHullV4Prefab(x, x.Contains("wood") ? "wood" : "iron", x.Contains("rail") ? PrefabNames.PrefabSizeVariant.TwoByTwo : PrefabNames.PrefabSizeVariant.FourByFour));
  }

  public static void RegisterHullProwSeal()
  {
    var materialName = HullMaterial.Iron.ToLower();
    var prefabName = $"{PrefabNames.HullRibProwSeal}_{materialName}";

    var prefabAssetName = $"hull_prow_seal_{materialName}";
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>(prefabAssetName);

    if (!prefabAsset)
    {
      LoggerProvider.LogWarning("Failed to load Valheim Prow Seal!");
      return;
    }

    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(prefabAssetName);

    if (!icon)
    {
      icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.ErrorIcon);
    }

    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = $"$valheim_vehicles_hull_prow_seal $valheim_vehicles_material_{materialName}",
      Description = $"$valheim_vehicles_hull_prow_seal_desc",
      Icon = icon
    });

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.TwoByEight);

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Iron,
      materialCount);
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
    var requirements = PrefabRecipeConfig.GetHullMaterialRecipeConfig(material, materialCount);

    if (requirements.Length > 0)
    {
      return requirements;
    }

    LoggerProvider.LogWarning("RequirementConfig is using deprecated material method. This means something is wrong with the base hull material config that can be set by the user.");

    // fallback for old approach if things fail.
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

  public void RegisterHullRibCorner(
    string materialVariant, PrefabNames.DirectionVariant? directionVariant, PrefabNames.PrefabSizeVariant sizeVariant, bool hasInverse = true)
  {
    var prefabName = PrefabNames.GetHullRibCornerName(materialVariant, directionVariant, sizeVariant);
    var prefabAssetString = LoadValheimVehicleAssets.GetShipHullRibCornerAssetName(materialVariant, directionVariant, sizeVariant);

    try
    {

      PrefabRegistryHelpers.RegisterHullRibCornerWall(materialVariant, directionVariant, sizeVariant);

      var prefabAsset =
        LoadValheimVehicleAssets.GetShipHullRibCorner(materialVariant, directionVariant, sizeVariant);

      if (!prefabAsset)
      {
        LoggerProvider.LogError($"Failed to load {prefabAssetString}");
        return;
      }

      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      SetupHullPrefab(prefab, prefabName,
        materialVariant, 4, prefab.transform.FindDeepChild("mesh"));

    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while registering for HullRibCorner {prefabName}, asset: {prefabAssetString}\n {e}");
    }
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
  public void RegisterHullCornerFloor(
    string materialVariant,
    PrefabNames.DirectionVariant directionVariant, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var prefabName = PrefabNames.GetHullRibCornerFloorName(materialVariant, directionVariant, sizeVariant);
    var prefabAssetName = PrefabNames.GetHullRibCornerFloorName(materialVariant, directionVariant, sizeVariant);
    try
    {
      // adds piece/icon related data.
      PrefabRegistryHelpers.RegisterHullRibCornerFloor(materialVariant, directionVariant, sizeVariant);

      var prefabAsset =
        LoadValheimVehicleAssets.GetShipHullCornerFloor(materialVariant,
          directionVariant, sizeVariant);

      if (!prefabAsset)
      {
        LoggerProvider.LogError($"Failed to load {prefabAssetName}");
        return;
      }

      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      SetupHullPrefab(prefab, prefabName,
        materialVariant, 1, prefab.transform.FindDeepChild("mesh"), ["mesh"]);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while registering for HullRibCorner {prefabName}, asset: {prefabAssetName}\n {e}");
    }
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

  public static ConvexHullCalculator convexHullCalculator = new();

  public void GenerateConvexHullMeshFromMeshFilters(GameObject prefab, GameObject meshObject)
  {
    var points = new List<Vector3>();
    var visualMeshes = meshObject.GetComponentsInChildren<MeshFilter>();
    foreach (var visualMesh in visualMeshes)
    {
      if (!visualMesh.sharedMesh) continue;
      points.AddRange(visualMesh.sharedMesh.vertices);
    }
    var prefabTransform = prefab.transform;

    if (points.Count <= 0) return;

    var localPoints = points
      .Select(x => prefabTransform.InverseTransformPoint(x)).ToList();

    // Prepare output containers
    var verts = new List<Vector3>();
    var tris = new List<int>();
    var normals = new List<Vector3>();

    // Generate convex hull and export the mesh
    // let this calculator garbage collect if the parentTransform is different.
    try
    {
      convexHullCalculator.GenerateHull(localPoints, false, ref verts,
        ref tris,
        ref normals, out var hasBailed);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error with generating convex hull for prefab hull name: <{prefab.name}> \n{e}");
    }

    GenerateMeshFromConvexOutput(meshObject, verts.ToArray(), tris.ToArray(), normals.ToArray());
  }

  public void GenerateMeshFromConvexOutput(GameObject meshObject, Vector3[] vertices, int[] triangles, Vector3[] normals)
  {
    if (meshObject == null) return;

    // First step is to either update previous mesh or generate a new one.
    var mesh = new Mesh
    {
      vertices = vertices,
      triangles = triangles,
      normals = normals,
      name =
        $"generated_mesh_{meshObject.transform.root.name}"
    };

    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.normals = normals;

    // always recalculate to avoid Physics issues. Low perf cost
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();

    // todo we get this twice. Possibly optimize this better.
    var meshCollider = meshObject.GetComponent<MeshCollider>();
    if (!meshCollider)
    {
      meshCollider = meshObject.AddComponent<MeshCollider>();
    }

    meshCollider.sharedMesh = mesh;
    // convex means it would be inaccurate the whole point of generating the mesh is so I can be an optimize concave collider.
    meshCollider.convex = false;
    // meshCollider.excludeLayers = LayerHelpers.BlockingColliderExcludeLayers;
    // meshCollider.includeLayers = LayerHelpers.PhysicalLayerMask;
  }

  public void RegisterHullV4Prefab(string assetName, string hullMaterial,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var prefabName = $"{PrefabNames.ValheimVehiclesPrefix}_{assetName}";
    try
    {
      var prefabAsset =
        LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>($"{assetName}.prefab");

      if (!prefabAsset)
      {
        LoggerProvider.LogWarning($"Failed to find prefab asset of assetName: {assetName} prefabName: {prefabName}");
        return;
      }

      // TODO After 3.8.0 remove this. It was added for beta compatibility to 3.7.x
      PrefabRegistryController.AddPrefabAlias(assetName, prefabName);

      var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(assetName);

      if (!icon)
      {
        icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ErrorIcon);
      }

      if (!PrefabRegistryHelpers.PieceDataDictionary.ContainsKey(prefabName))
      {
        PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
        {
          Name = $"{prefabName}",
          Description = $"{prefabName}",
          Icon = icon
        });
      }
      else
      {
        LoggerProvider.LogWarning($"Already registered {assetName} prefabName: {prefabName}");
        return;
      }

      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      var colliders = prefab.GetComponentsInChildren<Collider>();
      if (colliders == null || colliders.Length == 0)
      {
        var visual = prefab.transform.Find("Visual");
        if (!visual)
        {
          LoggerProvider.LogDebug("Failed to find visual to add collider to");
        }
        else
        {
          GenerateConvexHullMeshFromMeshFilters(prefab, visual.gameObject);
        }
      }

      var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);

      SetupHullPrefab(prefab, prefabName,
        hullMaterial,
        materialCount);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while registering for HullRibProw {assetName} prefabName: {prefabName} {e}");
    }
  }

  public void RegisterHullProwSpecialVariant(
    string hullMaterial,
    PrefabNames.PrefabSizeVariant sizeVariant, PrefabNames.DirectionVariant? directionVariant, string prowTypeVariant)
  {
    var prefabName = PrefabNames.GetHullProwRibVariants(hullMaterial, sizeVariant, directionVariant, prowTypeVariant);
    var assetName = LoadValheimVehicleAssets.GetShipProwRibSpecialVariantAssetName(hullMaterial, sizeVariant, directionVariant, prowTypeVariant);
    try
    {
      var prefabAsset =
        LoadValheimVehicleAssets.GetShipHullRibProwSpecialVariant(hullMaterial, sizeVariant, directionVariant, prowTypeVariant);

      if (!prefabAsset)
      {
        LoggerProvider.LogWarning($"Failed to find prefab asset of assetName: {assetName} prefabName: {prefabName}");
        return;
      }

      var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(assetName);

      if (!icon)
      {
        icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ErrorIcon);
      }

      if (!PrefabRegistryHelpers.PieceDataDictionary.ContainsKey(prefabName))
      {
        var sizeVariantString = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
        PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
        {
          Name = $"$valheim_vehicles_hull_rib_prow $valheim_vehicles_hull_rib_prow_variant_{prowTypeVariant} $valheim_vehicles_material_{hullMaterial.ToLower()} {sizeVariantString}",
          Description = $"$valheim_vehicles_hull_rib_prow_variant_{prowTypeVariant}_desc",
          Icon = icon
        });
      }
      else
      {
        LoggerProvider.LogWarning($"Already registered {assetName} prefabName: {prefabName}");
        return;
      }

      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);

      SetupHullPrefab(prefab, prefabName,
        hullMaterial,
        materialCount);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while registering for HullRibProw {assetName} prefabName: {prefabName} {e}");
    }
  }

  public void RegisterHullProw(
    string hullMaterial,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    var prefabName = PrefabNames.GetHullProwVariants(hullMaterial, sizeVariant);
    var assetName = LoadValheimVehicleAssets.GetShipProwAssetName(hullMaterial, sizeVariant);
    try
    {

      var prefabAsset =
        LoadValheimVehicleAssets.GetShipHullProw(hullMaterial, sizeVariant);

      var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(assetName);

      if (!icon)
      {
        icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ErrorIcon);
      }

      if (!PrefabRegistryHelpers.PieceDataDictionary.ContainsKey(prefabName))
      {
        PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
        {
          Name = $"$valheim_vehicles_hull_rib_prow $valheim_vehicles_material_{hullMaterial.ToLower()}",
          Description = $"$valheim_vehicles_hull_rib_prow_desc",
          Icon = icon
        });
      }
      else
      {
        LoggerProvider.LogWarning($"Already registered {assetName} prefabName: {prefabName}");
        return;
      }

      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      var materialCount = PrefabNames.GetPrefabSizeArea(sizeVariant);

      SetupHullPrefab(prefab, prefabName,
        hullMaterial,
        materialCount);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error while registering for HullRib assetName {assetName}, prefabName {prefabName} {e}");
    }
  }

  public void RegisterHullWallAngular45()
  {
    var prefabName = "Valheim_Vehicles_HullWallAngular_45";
    var prefabAsset =
      PrefabRegistryController.vehicleAssetBundle.LoadAsset<GameObject>($"hull_wall_angular_45.prefab");
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    // placeholder for wall component.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "Hull Wall Angular 45",
      Description = "A hull wall with a 45 degree angle",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.TwoByTwo);

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Wood,
      materialCount);
  }


  public void RegisterExperimentalHullPiece(string assetName)
  {
    try
    {

      var prefabAsset =
        PrefabRegistryController.vehicleAssetBundle.LoadAsset<GameObject>($"{assetName}.prefab");
      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          assetName, prefabAsset);

      var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite(assetName);
      if (!sprite)
      {
        sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ExperimentIcon);
      }

      // placeholder for wall component.
      PrefabRegistryHelpers.PieceDataDictionary.Add(assetName, new PrefabRegistryHelpers.PieceData
      {
        Name = $"{assetName}",
        Description = $"Experimental {assetName}",
        Icon = sprite
      });

      var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.TwoByTwo);

      SetupHullPrefab(prefab, assetName,
        HullMaterial.Wood,
        materialCount);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error registering \n {e}");
    }
  }

  public void RegisterHullWallAngular45Seal()
  {
    var prefabName = "Valheim_Vehicles_HullWallAngular_45_seal";
    var prefabAsset =
      PrefabRegistryController.vehicleAssetBundle.LoadAsset<GameObject>($"hull_wall_angular_45_seal.prefab");
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    // placeholder for wall component.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "Hull Wall Angular 45 Seal",
      Description = "A hull wall with a 45 degree angle Seal",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.TwoByTwo);

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Wood,
      materialCount);
  }

  public void RegisterHullWallAngular45SealInverse()
  {
    var prefabName = "Valheim_Vehicles_HullWallAngular_45_seal_inverse";
    var prefabAsset =
      PrefabRegistryController.vehicleAssetBundle.LoadAsset<GameObject>($"hull_wall_angular_45_seal_inverse.prefab");
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        prefabName, prefabAsset);

    // placeholder for wall component.
    PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "Hull Wall Angular 45 Seal (Inverse)",
      Description = "A hull wall with a 45 degree angle Seal",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });

    var materialCount = PrefabNames.GetPrefabSizeArea(PrefabNames.PrefabSizeVariant.TwoByTwo);

    SetupHullPrefab(prefab, prefabName,
      HullMaterial.Wood,
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

      PrefabRegistryController.AddPiece(new CustomPiece(prefab, false,
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
      LoggerProvider.LogError($"Failed to setupHullPrefab, prefab: {prefab} prefabName: {prefabName}, hullMaterial {hullMaterial} hoistParent: {hoistParent} hoistfilters {hoistFilters} \n{e}");
    }
  }


  /// <summary>
  /// Registers all hull ribs
  /// </summary>
  private static void RegisterHullRib(
    string hullMaterial, PrefabNames.PrefabSizeVariant sizeVariant)
  {
    try
    {
      var prefabName = PrefabNames.GetHullRibName(hullMaterial, sizeVariant);
      if (!PrefabRegistryHelpers.PieceDataDictionary.ContainsKey(prefabName))
      {
        var hullMaterialDescription =
          ShipHulls.GetHullMaterialDescription(hullMaterial);
        var sizeVariantString = PrefabNames.GetPrefabSizeVariantName(sizeVariant);
        var spriteAssetName = LoadValheimVehicleAssets.GetShipHullRibAssetName(hullMaterial, sizeVariant);
        PrefabRegistryHelpers.PieceDataDictionary.Add(prefabName
          , new PrefabRegistryHelpers.PieceData
          {
            Name =
              $"$valheim_vehicles_hull_rib_side {sizeVariantString} $valheim_vehicles_material_{hullMaterial.ToLower()}",
            Description =
              $"$valheim_vehicles_hull_rib_side_desc {hullMaterialDescription}",
            Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(spriteAssetName)
          });
      }
      else
      {
        LoggerProvider.LogDev($"RegisterHullRib Skipping registry of asset that already exists. Possible duplicate {prefabName}");
      }

      var prefabAsset = LoadValheimVehicleAssets.GetShipHullRib(hullMaterial, sizeVariant);

      if (!prefabAsset)
      {
        LoggerProvider.LogDev($"RegisterHullRib {prefabName} Skipping registry of asset that does not exist.");
        return;
      }


      var prefab =
        PrefabManager.Instance.CreateClonedPrefab(
          prefabName, prefabAsset);

      SetupHullPrefab(prefab, prefabName,
        hullMaterial,
        8,
        prefab.transform.Find("new") ?? prefab.transform);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error registering RegisterHullRib \n {e}");
    }
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