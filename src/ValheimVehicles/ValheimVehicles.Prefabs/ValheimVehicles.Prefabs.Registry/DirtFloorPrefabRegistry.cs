using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class DirtFloorPrefabRegistry : RegisterPrefab<DirtFloorPrefabRegistry>
{
  public override void OnRegister()
  {
    RegisterDirtFloor(1);
    RegisterDirtFloor(2);
  }

  private static void RegisterDirtFloor(int size)
  {
    var prefabSizeString = $"{size}x{size}";
    var prefabName = $"MBDirtFloor_{prefabSizeString}";
    var mbDirtFloorPrefab =
      PrefabManager.Instance.CreateClonedPrefab(prefabName,
        LoadValheimRaftAssets.dirtFloor);

    mbDirtFloorPrefab.transform.localScale = new Vector3(size, 1f, size);

    var mbDirtFloorPrefabPiece = mbDirtFloorPrefab.AddComponent<Piece>();
    mbDirtFloorPrefabPiece.m_placeEffect =
      LoadValheimAssets.stoneFloorPiece.m_placeEffect;
    mbDirtFloorPrefabPiece.m_allowedInDungeons = true;


    PrefabRegistryHelpers.AddNetViewWithPersistence(mbDirtFloorPrefab);

    var wnt = PrefabRegistryHelpers.SetWearNTear(mbDirtFloorPrefab, 2);
    wnt.m_haveRoof = false;

    // Makes the component cultivatable
    mbDirtFloorPrefab.AddComponent<CultivatableComponent>();

    PrefabRegistryHelpers.FixCollisionLayers(mbDirtFloorPrefab);
    PrefabRegistryHelpers.FixSnapPoints(mbDirtFloorPrefab);

    PieceManager.Instance.AddPiece(new CustomPiece(mbDirtFloorPrefab, false,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Name = $"$mb_dirt_floor_{prefabSizeString}",
        Description = $"$mb_dirt_floor_{prefabSizeString}_desc",
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .DirtFloor),
        Requirements =
        [
          new RequirementConfig
          {
            // this may cause issues it's just size^2 but Math.Pow returns a double
            Amount = (int)Math.Pow(size, 2),
            Item = "Stone",
            Recover = true
          }
        ]
      }));
  }
}