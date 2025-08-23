using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using Zolantris.Shared;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CustomMeshPrefabs : RegisterPrefab<CustomMeshPrefabs>
{
  public override void OnRegister()
  {
    RegisterWaterMaskCreator();
    RegisterWaterMaskPrefab();
    RegisterCustomFloatationPrefab();

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
    var material = new Material(LoadValheimAssets.CustomPieceShader)
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
    var material = new Material(LoadValheimAssets.CustomPieceShader)
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
}