using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class PlayerSpawnPrefab : IRegisterPrefab
{
  public static readonly PlayerSpawnPrefab Instance = new();

  private static GameObject CreateSpawnObject()
  {
    var go = new GameObject()
    {
      name = PrefabNames.PlayerSpawnControllerObj,
    };
    return go;
  }

  /// <summary>
  /// Registers the prefab initial object for players that do not yet have a matching spawner
  /// </summary>
  /// <param name="prefabManager"></param>
  /// <param name="pieceManager"></param>
  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(PrefabNames.PlayerSpawnControllerObj, CreateSpawnObject());
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.AddComponent<PlayerSpawnController>();
  }
}