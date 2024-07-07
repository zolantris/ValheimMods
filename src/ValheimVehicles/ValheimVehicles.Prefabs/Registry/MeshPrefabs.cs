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
public class MeshPrefabs : IRegisterPrefab
{
  public static readonly MeshPrefabs Instance = new();

  public void RegisterWaterMeshMaskCreator()
  {
    var prefab = PrefabManager.Instance.CreateEmptyPrefab(PrefabNames.WaterMeshMaskCreator, false);
    PrefabRegistryHelpers.AddTempNetView(prefab);

    prefab.AddComponent<VehicleMeshMaskCreator>();

    // @todo make a custom icon that shows a ship hull without water
    var sailIcon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("customsail_tri");

    var piece = prefab.AddComponent<Piece>();
    piece.m_name = "$valheim_vehicles_water_mesh_mask";
    piece.m_description = $"$valheim_vehicles_water_mesh_mask_desc";
    piece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    var mesh = prefab.GetComponent<MeshRenderer>();
    var unlitColor = LoadValheimVehicleAssets.PieceShader;
    var material = new Material(unlitColor)
    {
      color = Color.green
    };
    mesh.sharedMaterial = material;
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    prefab.layer = LayerMask.NameToLayer("piece");
    PrefabRegistryHelpers.SetWearNTear(prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, new PieceConfig()
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Icon = sailIcon
    }));
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterMeshMaskCreator();
  }
}