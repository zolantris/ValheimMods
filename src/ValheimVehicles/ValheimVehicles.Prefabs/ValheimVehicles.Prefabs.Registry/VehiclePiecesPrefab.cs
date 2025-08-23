using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;


namespace Registry;

public class VehiclePiecesPrefab : RegisterPrefab<VehiclePiecesPrefab>
{
  public override void OnRegister()
  {
    RegisterPiecesContainer();
  }
  public static GameObject VehiclePiecesContainer = null!;

  /// <summary>
  /// PiecesContainer is an object that has a rigidbody and a FixedJoint that attaches to the MovementController's Rigidbody. This allows all pieces to be in sync and also able to use a ZsyncTransform without having issues
  /// </summary>
  public void RegisterPiecesContainer()
  {
    VehiclePiecesContainer = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VehiclePiecesContainer,
      LoadValheimVehicleAssets.VehiclePiecesAsset);
  }
}