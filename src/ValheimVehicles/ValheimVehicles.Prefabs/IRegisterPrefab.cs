using Jotunn.Managers;

namespace ValheimVehicles.Prefabs;

public interface IRegisterPrefab
{
  public void Register(PrefabManager prefabManager, PieceManager pieceManager);
}