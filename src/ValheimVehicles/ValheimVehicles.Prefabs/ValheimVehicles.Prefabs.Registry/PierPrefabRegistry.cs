using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class PierPrefabRegistry : RegisterPrefab<PierPrefabRegistry>
{

  public override void OnRegister()
  {
    RegisterPierWall();
    RegisterPierPole();
  }


  private static void RegisterPierPole()
  {
    var woodPolePrefab = PrefabManager.Instance.GetPrefab("wood_pole_log_4");
    var mbPierPolePrefab =
      PrefabManager.Instance.CreateClonedPrefab("MBPier_Pole", woodPolePrefab);

    // Less complicated wnt so re-usable method is not used
    var pierPoleWearNTear = mbPierPolePrefab.GetComponent<WearNTear>();
    pierPoleWearNTear.m_noRoofWear = false;

    var pierPolePrefabPiece = mbPierPolePrefab.GetComponent<Piece>();
    pierPolePrefabPiece.m_waterPiece = true;

    var pierComponent = mbPierPolePrefab.AddComponent<PierComponent>();
    pierComponent.m_segmentObject =
      PrefabManager.Instance.CreateClonedPrefab("MBPier_Pole_Segment", woodPolePrefab);
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<ZNetView>());
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<Piece>());
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<WearNTear>());
    PrefabRegistryHelpers.FixSnapPoints(mbPierPolePrefab);

    var transforms2 =
      pierComponent.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var j = 0; j < transforms2.Length; j++)
      if ((bool)transforms2[j] && transforms2[j].CompareTag("snappoint"))
        Object.Destroy(transforms2[j]);

    pierComponent.m_segmentHeight = 4f;
    pierComponent.m_baseOffset = -1f;

    var customPiece = new CustomPiece(mbPierPolePrefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Name = "$mb_pier (" + pierPolePrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierPolePrefabPiece.m_description,
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Icon = pierPolePrefabPiece.m_icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      ]
    });

    PrefabRegistryController.AddPiece(customPiece);
  }

  private static void RegisterPierWall()
  {
    var stoneWallPrefab = PrefabManager.Instance.GetPrefab("stone_wall_4x2");
    var pierWallPrefab =
      PrefabManager.Instance.CreateClonedPrefab("MBPier_Stone", stoneWallPrefab);
    var pierWallPrefabPiece = pierWallPrefab.GetComponent<Piece>();
    pierWallPrefabPiece.m_waterPiece = true;

    var pier = pierWallPrefab.AddComponent<PierComponent>();
    pier.m_segmentObject =
      PrefabManager.Instance.CreateClonedPrefab("MBPier_Stone_Segment", stoneWallPrefab);
    Object.Destroy(pier.m_segmentObject.GetComponent<ZNetView>());
    Object.Destroy(pier.m_segmentObject.GetComponent<Piece>());
    Object.Destroy(pier.m_segmentObject.GetComponent<WearNTear>());
    PrefabRegistryHelpers.FixSnapPoints(pierWallPrefab);

    var transforms = pier.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var i = 0; i < transforms.Length; i++)
      if ((bool)transforms[i] && transforms[i].CompareTag("snappoint"))
        Object.Destroy(transforms[i]);

    pier.m_segmentHeight = 2f;
    pier.m_baseOffset = 0f;

    var customPiece = new CustomPiece(pierWallPrefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Name = "$mb_pier (" + pierWallPrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierWallPrefabPiece.m_description,
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Icon = pierWallPrefabPiece.m_icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      ]
    });

    PrefabRegistryController.AddPiece(customPiece);
  }
}