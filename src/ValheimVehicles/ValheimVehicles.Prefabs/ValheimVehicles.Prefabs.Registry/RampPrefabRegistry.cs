using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class RampPrefabRegistry : RegisterPrefab<RampPrefabRegistry>
{
  public override void OnRegister()
  {
    RegisterBoardingRamp();
    RegisterBoardingRampWide();
  }

  private static void RegisterBoardingRamp()
  {
    var woodPole2PrefabPiece =
      PrefabManager.Instance.GetPrefab("wood_pole2").GetComponent<Piece>();

    var mbBoardingRamp =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.BoardingRamp,
        LoadValheimRaftAssets.boardingRampAsset);
    var floor = mbBoardingRamp.transform
      .Find("Ramp/Segment/SegmentAnchor/Floor").gameObject;
    var newFloor = Object.Instantiate(
      LoadValheimAssets.woodFloorPiece.transform
        .Find("New/_Combined Mesh [high]").gameObject,
      floor.transform.parent,
      false);
    Object.Destroy(floor);
    newFloor.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
    newFloor.transform.localScale = Vector3.one;
    newFloor.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

    var woodMat =
      woodPole2PrefabPiece.transform.Find("New").GetComponent<MeshRenderer>()
        .sharedMaterial;
    mbBoardingRamp.transform.Find("Winch1/Pole").GetComponent<MeshRenderer>()
        .sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Pole").GetComponent<MeshRenderer>()
        .sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole1")
      .GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole2")
      .GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Winch1/Cylinder")
        .GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Cylinder")
        .GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;

    var ropeMat = LoadValheimAssets.raftMast
      .GetComponentInChildren<LineRenderer>(true)
      .sharedMaterial;
    mbBoardingRamp.transform.Find("Rope1").GetComponent<LineRenderer>()
      .sharedMaterial = ropeMat;
    mbBoardingRamp.transform.Find("Rope2").GetComponent<LineRenderer>()
      .sharedMaterial = ropeMat;

    var mbBoardingRampPiece = mbBoardingRamp.AddComponent<Piece>();
    mbBoardingRampPiece.m_name = "$mb_boarding_ramp";
    mbBoardingRampPiece.m_description = "$mb_boarding_ramp_desc";
    mbBoardingRampPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryHelpers.AddNetViewWithPersistence(mbBoardingRamp);

    var boardingRamp2 = mbBoardingRamp.AddComponent<BoardingRampComponent>();
    boardingRamp2.m_stateChangeDuration = 0.3f;
    boardingRamp2.m_segments = 5;

    // previously was 1000f
    var mbBoardingRampWearNTear =
      PrefabRegistryHelpers.SetWearNTear(mbBoardingRamp, 1);
    mbBoardingRampWearNTear.m_supports = false;

    PrefabRegistryHelpers.FixCollisionLayers(mbBoardingRamp);

    PieceManager.Instance.AddPiece(new CustomPiece(mbBoardingRamp, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Description = "$mb_boarding_ramp_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .BoardingRamp),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      ]
    }));
  }

  /**
   * must be called after RegisterBoardingRamp
   */
  private static void RegisterBoardingRampWide()
  {
    var mbBoardingRampWide =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.BoardingRampWide,
        PrefabManager.Instance.GetPrefab(PrefabNames.BoardingRamp));
    var mbBoardingRampWidePiece = mbBoardingRampWide.GetComponent<Piece>();
    mbBoardingRampWidePiece.m_name = "$mb_boarding_ramp_wide";
    mbBoardingRampWidePiece.m_description = "$mb_boarding_ramp_wide_desc";
    mbBoardingRampWide.transform.localScale = new Vector3(2f, 1f, 1f);


    var boardingRamp = mbBoardingRampWide.GetComponent<BoardingRampComponent>();
    boardingRamp.m_stateChangeDuration = 0.3f;
    boardingRamp.m_segments = 5;

    PrefabRegistryHelpers.SetWearNTear(mbBoardingRampWide, 1);
    PrefabRegistryHelpers.FixSnapPoints(mbBoardingRampWide);


    PieceManager.Instance.AddPiece(new CustomPiece(mbBoardingRampWide, false,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Description = "$mb_boarding_ramp_wide_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .BoardingRamp),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 20,
            Item = "Wood",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = 8,
            Item = "IronNails",
            Recover = true
          }
        ]
      }));
  }

}