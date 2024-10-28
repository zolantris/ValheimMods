using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
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
    RegisterTestComponents();
  }

  public void RegisterWaterMaskCreator()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(
        PrefabNames.CustomWaterMaskCreator,
        false);

    var mesh = prefab.GetComponent<MeshRenderer>();
    var material = new Material(LoadValheimAssets.CustomPieceShader)
    {
      color = new Color(0.3f, 0.4f, 1)
    };
    mesh.material = material;
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

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

  public static void RegisterWaterMaskPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.CustomWaterMask);

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "Water Mask";
    piece.m_description =
      "Vehicle Water Mask component, this requires the water mask creator to work. You should not see this message unless using a mod to expose this prefab";
    piece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryHelpers.SetWearNTear(prefab, 1);

    WaterMaskComponent.WaterMaskMaterial =
      new Material(LoadValheimAssets.waterMaskShader);
    prefab.AddComponent<WaterMaskComponent>();
    PrefabManager.Instance.AddPrefab(prefab);
  }

  public void RegisterTestComponents()
  {
    var maskShader = LoadValheimAssets.waterMask.GetComponent<MeshRenderer>()
      .sharedMaterial.shader;
    var maskMaterial = new Material(maskShader);
    AddTestPrefab("Test1", maskMaterial);
    AddTestPrefab("Test2",
      new Material(LoadValheimVehicleAssets.WaterMaskShader));
    AddTestPrefab("Test3", LoadValheimVehicleAssets.WaterMaskMaterial);
    var renderqueueLower =
      new Material(LoadValheimVehicleAssets.WaterMaskMaterial)
      {
        renderQueue = 7
      };
    AddTestPrefab("Test4", renderqueueLower);
    // var waterLiquid = PrefabManager.Instance.GetPrefab("WaterSurface");
    // var waterLiquidMaterial =
    //   waterLiquid.GetComponent<MeshRenderer>().sharedMaterial;


    // AddTransparentWaterMaskPrefab("InverseMask",
    //   new Material(LoadValheimVehicleAssets.SelectiveMask),
    //   new Color(0f, 0f, 0f, 0f));
    // AddTransparentWaterMaskPrefab("InverseMask2",
    //   new Material(LoadValheimVehicleAssets.SelectiveMaskMat),
    //   greenish);
    // AddTransparentWaterMaskPrefab("PureMask", new Material(MaskShader),
    //   greenish);
    // AddTransparentWaterMaskPrefab("WaterPlane",
    //   new Material(waterLiquidMaterial),
    //   greenish);
    // AddTransparentWaterMaskPrefab("MaskWithWater",
    //   new Material(MaskShader),
    //   greenish);
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
      var specialCube = prefab.AddComponent<ScalableDoubleSidedCube>();
      specialCube.CubeMaskMaterial = LoadValheimVehicleAssets.WaterMaskMaterial;
      specialCube.CubeVisibleSurfaceMaterial =
        new Material(LoadValheimVehicleAssets.SelectiveMask);
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