using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Controllers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class AnchorPrefabs : RegisterPrefab<AnchorPrefabs>
{
  public override void OnRegister()
  {
    RegisterAnchorWoodPrefab();
    RegisterAnchorDockingAttachment();
  }


  public static void RegisterAnchorDockingAttachment()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("anchor_dock.prefab");
    if (!asset)
    {
      LoggerProvider.LogError("error loading DockAnchor prefab");
      return;
    }

    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.DockAttachpoint,
        asset);

    var mbRopeAnchorPrefabPiece = prefab.AddComponent<Piece>();
    mbRopeAnchorPrefabPiece.m_name = "$valheim_vehicles_dock_anchor_point";
    mbRopeAnchorPrefabPiece.m_description = "$valheim_vehicles_dock_anchor_point_desc";

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var ropeAnchorComponent = prefab.AddComponent<RopeAnchorComponent>();
    var baseRope =
      LoadValheimAssets.raftMast.GetComponentInChildren<LineRenderer>(true);

    ropeAnchorComponent.m_rope = prefab.AddComponent<LineRenderer>();
    ropeAnchorComponent.m_rope.material = new Material(baseRope.material);
    ropeAnchorComponent.m_rope.widthMultiplier = 0.2f;
    ropeAnchorComponent.m_rope.enabled = false;

    var ropeAnchorComponentWearNTear =
      PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    ropeAnchorComponentWearNTear.m_supports = false;

    PrefabRegistryHelpers.FixCollisionLayers(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Name = "$valheim_vehicles_dock_anchor_point",
      Description = "$valheim_vehicles_dock_anchor_point_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.Dock),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 2,
          Item = "Iron",
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

  /// <summary>
  /// @todo ship the binary reference back to Unity so these values do not have to be assigned at runtime again
  /// </summary>
  public void RegisterAnchorWoodPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.ShipAnchorWood, LoadValheimVehicleAssets.ShipAnchorWood);

    var anchorMechanismController = prefab.AddComponent<VehicleAnchorMechanismController>();

    anchorMechanismController.rotationAnchorRopeAttachpoint =
      anchorMechanismController.transform.Find("attachpoint_rotational");
    anchorMechanismController.anchorRopeAttachmentPoint =
      anchorMechanismController.transform.Find(
        "anchor/attachpoint_anchor");

    anchorMechanismController.anchorRopeAttachStartPoint =
      anchorMechanismController.transform.Find("attachpoint_anchor_start");
    anchorMechanismController.anchorTransform =
      anchorMechanismController.transform.Find("anchor");

    anchorMechanismController.ropeLine =
      anchorMechanismController.transform.Find("rope_line").GetComponent<LineRenderer>();

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipAnchorWood,
        prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, false,
      new PieceConfig
      {
        Name = piece.name,
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Icon = piece.m_icon,
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 20,
            Item = "RoundLog",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = 3,
            Item = "Chain",
            Recover = true
          }
        ],
        Enabled = true
      }));
  }
}