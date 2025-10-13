using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using Zolantris.Shared;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CustomMeshPrefabs : RegisterPrefab<CustomMeshPrefabs>
{
  public static Color CachedBoundaryAdderColor = new(0, 1, 0, 0.25f);
  public static Color CachedBoundaryEraserColor = new(1, 0, 0, 0.25f);

  public override void OnRegister()
  {
    RegisterWaterMaskCreator();
    RegisterWaterMaskPrefab();
    RegisterCustomFloatationPrefab();
    RegisterShipChunkBoundary1x1();
    RegisterShipChunkBoundary4x4();
    RegisterShipChunkBoundary8x8();
    RegisterShipChunkBoundary16x16();
    RegisterShipChunkBoundaryEraser();

    if (CustomMeshConfig.EnableCustomWaterMeshTestPrefabs.Value)
    {
      RegisterTestComponents();
    }
  }

  // public void RegisterVehicleTreadsDistanceCreator()
  // {
  //   var prefab =
  //     PrefabManager.Instance.CreateEmptyPrefab(
  //       PrefabNames.CustomWaterMaskCreator,
  //       false);
  //
  //   var mesh = prefab.GetComponent<MeshRenderer>();
  //   var material = new Material(LoadValheimAssets.CustomPieceShader)
  //   {
  //     color = Color.green
  //   };
  //   var collider = prefab.GetComponent<BoxCollider>();
  //   prefab.layer = LayerMask.NameToLayer("piece_nonsolid");
  //   collider.excludeLayers = LayerHelpers.CustomRaftLayerMask;
  //   mesh.material = material;
  //   prefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
  //
  //   var creatorComponent = prefab.AddComponent<CustomMeshCreatorComponent>();
  //   creatorComponent.SetCreatorType(CustomMeshCreatorComponent
  //     .MeshCreatorTypeEnum.WaterMask);
  //
  //   var piece = PrefabRegistryHelpers.AddPieceForPrefab(
  //     PrefabNames.CustomTreadDistanceCreator,
  //     prefab);
  //   piece.m_canRotate = true;
  //
  //   PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
  //     new PieceConfig
  //     {
  //       PieceTable = PrefabRegistryController.GetPieceTableName(),
  //       Category = PrefabNames.ValheimRaftMenuName,
  //       Enabled = true
  //     }));
  // }

  // public void RegisterVehicleHeight()
  // {
  //   var prefab =
  //     PrefabManager.Instance.CreateEmptyPrefab(
  //       PrefabNames.CustomWaterMaskCreator,
  //       false);
  //
  //   var mesh = prefab.GetComponent<MeshRenderer>();
  //   var material = new Material(LoadValheimAssets.CustomPieceShader)
  //   {
  //     color = Color.green
  //   };
  //   var collider = prefab.GetComponent<BoxCollider>();
  //   prefab.layer = LayerMask.NameToLayer("piece_nonsolid");
  //   collider.excludeLayers = LayerHelpers.CustomRaftLayerMask;
  //   mesh.material = material;
  //   prefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
  //
  //   var creatorComponent = prefab.AddComponent<CustomMeshCreatorComponent>();
  //   creatorComponent.SetCreatorType(CustomMeshCreatorComponent
  //     .MeshCreatorTypeEnum.WaterMask);
  //
  //   var piece = PrefabRegistryHelpers.AddPieceForPrefab(
  //     PrefabNames.CustomTreadDistanceCreator,
  //     prefab);
  //   piece.m_canRotate = true;
  //
  //   PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
  //     new PieceConfig
  //     {
  //       PieceTable = PrefabRegistryController.GetPieceTableName(),
  //       Category = PrefabNames.ValheimRaftMenuName,
  //       Enabled = true
  //     }));
  // }
  private static void RegisterCustomFloatationPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.CustomWaterFloatation, false);
    var meshRenderer = prefab.GetComponent<MeshRenderer>();
    var material = new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
    {
      color = new Color(0.5f, 0.4f, 0.5f, 0.8f)
    };
    var collider = prefab.GetComponent<BoxCollider>();
    prefab.layer = LayerMask.NameToLayer("piece_nonsolid");
    collider.excludeLayers = LayerHelpers.CustomRaftLayerMask;
    meshRenderer.material = material;
    prefab.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);

    // No special-effects, etc. Should be completely empty area invisible.
    meshRenderer.lightProbeUsage = LightProbeUsage.Off;
    meshRenderer.receiveShadows = false;
    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    meshRenderer.rayTracingMode = RayTracingMode.Off;
    meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

    PrefabRegistryHelpers.AddTempNetView(prefab);

    PrefabRegistryHelpers.AddPieceForPrefab(
      PrefabNames.CustomWaterFloatation,
      prefab);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private void RegisterWaterMaskCreator()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(
        PrefabNames.CustomWaterMaskCreator,
        false);

    var mesh = prefab.GetComponent<MeshRenderer>();
    var material = new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
    {
      color = new Color(0.3f, 0.4f, 1, 0.8f)
    };
    var collider = prefab.GetComponent<BoxCollider>();
    prefab.layer = LayerMask.NameToLayer("piece_nonsolid");
    collider.excludeLayers = LayerHelpers.CustomRaftLayerMask;
    mesh.material = material;
    prefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    var creatorComponent = prefab.AddComponent<CustomMeshCreatorComponent>();
    creatorComponent.SetCreatorType(CustomMeshCreatorComponent
      .MeshCreatorTypeEnum.WaterMask);

    var piece = PrefabRegistryHelpers.AddPieceForPrefab(
      PrefabNames.CustomWaterMaskCreator,
      prefab);
    piece.m_canRotate = true;

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static void RegisterWaterMaskPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.CustomWaterMask);

    var meshRenderer = prefab.GetComponent<MeshRenderer>();

    // No special effects etc. Should be completely empty area invisible.
    meshRenderer.lightProbeUsage = LightProbeUsage.Off;
    meshRenderer.receiveShadows = false;
    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    meshRenderer.rayTracingMode = RayTracingMode.Off;
    meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Water Mask";
    piece.m_description =
      "Vehicle Water Mask component, this requires the water mask creator to work. You should not see this message unless using a mod to expose this prefab";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    prefab.AddComponent<WaterZoneController>();
    PrefabManager.Instance.AddPrefab(prefab);
  }

  public void AddTestCube(string prefabName, Vector3 size)
  {
    var waterMaskPrefab = new GameObject("WaterMaskPrefab")
    {
      layer = LayerHelpers.PieceNonSolidLayer
    };

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab($"{
        PrefabNames.CustomWaterMask}_{prefabName}", waterMaskPrefab);
    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var piece = prefab.AddComponent<Piece>();
    piece.m_name = $"WM Test {prefabName}";
    piece.m_description =
      $"TestComponent: {prefabName}";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    var waterMaskComponent = prefab.AddComponent<WaterZoneController>();
    waterMaskComponent.defaultScale = size;

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        Name = piece.name,
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterOpacityBucket),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  public void RegisterTestComponents()
  {
    var maskShader = LoadValheimAssets.waterMask.GetComponent<MeshRenderer>()
      .sharedMaterial.shader;
    var maskMaterial = new Material(maskShader);
    AddTestPrefab("Default_water_mask", maskMaterial);
    AddTestPrefab("Custom_watermask",
      LoadValheimVehicleAssets.TransparentDepthMaskMaterial);
    // AddTestCube("Test2", Vector3.one * 4);
    // AddTestCube("Test3", Vector3.one * 2);
    // AddTestCube("Test4", Vector3.one * 8);
  }

  public void AddTestPrefab(string prefabName,
    Material material, bool shouldAddCubeComponent = false)
  {
    var name = $"{PrefabNames.CustomWaterMask}_{prefabName}";
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(name);
    var piece = prefab.AddComponent<Piece>();

    piece.m_name = $"$valheim_vehicles_water_mask {prefabName}";
    piece.m_description = "$valheim_vehicles_water_mask_desc";
    piece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    piece.gameObject.layer = LayerMask.NameToLayer("piece_nonsolid");
    piece.m_allowRotatedOverlap = true;

    piece.m_clipEverything = true;

    var prefabMeshRenderer = prefab.GetComponent<MeshRenderer>();
    prefabMeshRenderer.transform.localScale = new Vector3(4f, 4f, 4f);
    prefabMeshRenderer.sharedMaterial = material;

    if (shouldAddCubeComponent)
    {
      prefab.AddComponent<ScalableDoubleSidedCube>();
    }

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        Name = "Vehicle Water Mask Test",
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterOpacityBucket),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static Material? cachedBoundaryMaterial = null;

  public static void ChunkPrefabSharedSetup(GameObject prefab, Vector3 scale, Color color)
  {
    var meshRenderer = prefab.GetComponent<MeshRenderer>();
    var piece = prefab.AddComponent<Piece>();

    prefab.transform.rotation = Quaternion.identity;


    prefab.AddComponent<DelayedSelfDeletingComponent>();
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    piece.m_canRotate = true;
    piece.m_allowRotatedOverlap = true;
    piece.m_noClipping = false;

    // injects snappoints per each transform and scales them properly.
    var convexHullBoundary = new ConvexHullBoundaryConstraint();
    convexHullBoundary.AddBoundaryPiecePoints(prefab.transform.localPosition, scale);
    var snappoints = convexHullBoundary.boundaryVertices;
    var count = 1;

    var collider = prefab.GetComponent<BoxCollider>();
    collider.includeLayers = LayerMask.GetMask("piece_nonsolid");

    prefab.transform.localScale = scale;

    foreach (var position in snappoints)
    {
      var go = new GameObject($"$hud_snappoint_corner ${count}")
      {
        transform =
        {
          position = position,
          parent = prefab.transform
        },
        tag = "snappoint",
        layer = LayerHelpers.PieceLayer
      };
      count++;
    }

    if (cachedBoundaryMaterial == null)
    {
      cachedBoundaryMaterial = new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
      {
        color = new Color(0.3f, 0.4f, 1, 0.8f)
      };
    }

    meshRenderer.sharedMaterial = cachedBoundaryMaterial;

    if (color != meshRenderer.material.color)
    {
      meshRenderer.material.color = color;
    }

    meshRenderer.material.renderQueue = 3000;

    meshRenderer.lightProbeUsage = LightProbeUsage.Off;
    meshRenderer.receiveShadows = false;
    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    meshRenderer.rayTracingMode = RayTracingMode.Off;
    meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
  }


  private static void RegisterShipChunkBoundary1x1()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.ShipChunkBoundary1x1x1);

    ChunkPrefabSharedSetup(prefab, Vector3.one, CachedBoundaryAdderColor);


    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Ship Chunk Boundary 1x1";
    piece.m_description =
      "Vehicle Ship Chunk Boundary 1x1 component, used to define the boundaries of the ship's collision mesh.";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .VehicleBorder),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static void RegisterShipChunkBoundary4x4()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.ShipChunkBoundary4x4x4);

    ChunkPrefabSharedSetup(prefab, Vector3.one * 4, CachedBoundaryAdderColor);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Ship Chunk Boundary 4x4";
    piece.m_description =
      "Vehicle Ship Chunk Boundary 4x4 component, used to define the boundaries of the ship's collision mesh.";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .VehicleBorder),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static void RegisterShipChunkBoundary8x8()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.ShipChunkBoundary8x8x8);

    ChunkPrefabSharedSetup(prefab, Vector3.one * 8, CachedBoundaryAdderColor);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Ship Chunk Boundary 8x8";
    piece.m_description =
      "Vehicle Ship Chunk Boundary 8x8 component, used to define the boundaries of the ship's collision mesh.";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .VehicleBorder),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static void RegisterShipChunkBoundary16x16()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.ShipChunkBoundary16x16x16);

    ChunkPrefabSharedSetup(prefab, Vector3.one * 16, CachedBoundaryAdderColor);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Ship Chunk Boundary 16x16";
    piece.m_description =
      "Vehicle Ship Chunk Boundary 16x16 component, used to define the boundaries of the ship's collision mesh.";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .VehicleBorder),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }

  private static void RegisterShipChunkBoundaryEraser()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.ShipChunkBoundaryEraser);

    ChunkPrefabSharedSetup(prefab, Vector3.one, CachedBoundaryEraserColor);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Chunk (Eraser)";
    piece.m_description =
      "Vehicle Ship Chunk (Eraser), placing this will delete any chunk overlapping chunks in the area.";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    piece.m_canRotate = true;
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .ExperimentIcon),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true
      }));
  }
}