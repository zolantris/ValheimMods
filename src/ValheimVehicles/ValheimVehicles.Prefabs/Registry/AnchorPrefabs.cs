using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
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

  public void RegisterAnchorWoodPrefab()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.ShipAnchorWood,LoadValheimVehicleAssets.ShipAnchorWood);
    
    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    
    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipAnchorWood,
        prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
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