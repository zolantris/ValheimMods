using Jotunn.Managers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class ExamplePrefab : IRegisterPrefab
{
  public static readonly ExamplePrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
  }
}