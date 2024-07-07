using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace Registry;

public class VehiclePiecesPrefab : IRegisterPrefab
{
  public static readonly VehiclePiecesPrefab Instance = new();

  public static GameObject VehiclePiecesContainer = null!;
  // PrefabManager.Instance.GetPrefab(PrefabNames.VehiclePiecesContainer);


  /// <summary>
  /// PiecesContainer is an object that has a rigidbody and a FixedJoint that attaches to the MovementController's Rigidbody. This allows all pieces to be in sync and also able to use a ZsyncTransform without having issues
  /// </summary>
  /// <param name="prefabManager"></param>
  public void RegisterPiecesContainer(PrefabManager prefabManager)
  {
    VehiclePiecesContainer = prefabManager.CreateClonedPrefab(PrefabNames.VehiclePiecesContainer,
      LoadValheimVehicleAssets.VehiclePiecesAsset);
    VehiclePiecesContainer.AddComponent<VehiclePiecesController>();
    VehiclePiecesContainer.AddComponent<VehicleMeshMaskManager>();
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterPiecesContainer(prefabManager);
    // todo to add a dynamic register for anything that needs to be a rigidbody added to this vehicle
    // RegisterStaticPiecesContainer(prefabManager);
  }
}