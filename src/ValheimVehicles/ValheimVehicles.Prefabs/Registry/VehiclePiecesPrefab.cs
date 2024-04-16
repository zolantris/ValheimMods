using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;

namespace Registry;

public class VehiclePiecesPrefab : IRegisterPrefab
{
  public static readonly VehiclePiecesPrefab Instance = new();

  public static GameObject VehiclePiecesContainer =>
    PrefabManager.Instance.GetPrefab(PrefabNames.VehiclePiecesContainer);

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    prefabManager.CreateClonedPrefab(PrefabNames.VehiclePiecesContainer,
      LoadValheimVehicleAssets.VehiclePiecesAsset);
  }
}