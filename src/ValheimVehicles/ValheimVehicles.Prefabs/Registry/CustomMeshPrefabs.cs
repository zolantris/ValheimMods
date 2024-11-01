using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.LayerUtils;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CustomMeshPrefabs : IRegisterPrefab
{
  public static readonly CustomMeshPrefabs Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterMaskCreator();
    RegisterWaterMaskPrefab();

    if (CustomMeshConfig.EnableCustomWaterMeshTestPrefabs.Value)
    {
      RegisterTestComponents();
    }
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
    mesh.material = material;
    prefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    var creatorComponent = prefab.AddComponent<CustomMeshCreatorComponent>();
    creatorComponent.SetCreatorType(CustomMeshCreatorComponent
      .MeshCreatorTypeEnum.WaterMask);

    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.CustomWaterMaskCreator,
      prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = "Hammer",
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
      }));
  }

  private static void RegisterWaterMaskPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.CustomWaterMask);

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
    // var waterMaskPrefab = new GameObject("WaterMaskPrefab")
    // {
    //   layer = LayerHelpers.NonSolidLayer
    // };

    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab($"{
        PrefabNames.CustomWaterMask}_{prefabName}");
    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var piece = prefab.AddComponent<Piece>();
    piece.m_name = $"WM Test {prefabName}";
    piece.m_description =
      $"TestComponent: {prefabName}";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    var waterMaskComponent = prefab.AddComponent<WaterZoneController>();
    waterMaskComponent.defaultScale = size;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        Name = piece.name,
        PieceTable = "Hammer",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterOpacityBucket),
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
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
    AddTestCube("Test2", Vector3.one * 4);
    AddTestCube("Test3", Vector3.one * 2);
    AddTestCube("Test4", Vector3.one * 8);
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

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        Name = "Vehicle Water Mask Test",
        PieceTable = "Hammer",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .WaterOpacityBucket),
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
      }));
  }
}