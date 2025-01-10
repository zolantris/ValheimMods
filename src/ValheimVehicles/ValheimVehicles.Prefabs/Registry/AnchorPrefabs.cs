using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles.Controllers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class AnchorPrefabs : IRegisterPrefab
{
  public static readonly AnchorPrefabs Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterAnchorWoodPrefab();
  }

  /// <summary>
  /// @todo ship the binary reference back to Unity so these values do not have to be assigned at runtime again
  /// </summary>
  public void RegisterAnchorWoodPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.ShipAnchorWood,LoadValheimVehicleAssets.ShipAnchorWood);

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
    
    var cogTransform =
      anchorMechanismController.transform.Find(
        "anchor_reel/rotational");
    
    anchorMechanismController.anchorCogRb =cogTransform.GetComponent<Rigidbody>();
    
    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    
    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipAnchorWood,
        prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, false,
      new PieceConfig
      {
        Name = piece.name,
        PieceTable = "Hammer",
        Icon = piece.m_icon,
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
      }));
  }
}