using Jotunn.Managers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class VehicleSwitchPrefab : IRegisterPrefab
{
  public static readonly VehicleSwitchPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    
  }
}