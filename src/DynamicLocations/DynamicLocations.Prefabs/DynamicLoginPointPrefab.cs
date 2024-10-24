using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Prefabs;

/**
 * example registry of a prefab
 */
public class DynamicPointPrefab
{
  public static readonly DynamicPointPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateEmptyPrefab(PrefabNames.SpawnPoint, true);
    prefab.layer = LayerMask.NameToLayer("piece");
  }
}