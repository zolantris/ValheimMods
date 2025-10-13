using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Controllers;

public class VehicleChunkController : MonoBehaviour
{
  private int chunkSize = 1;

  public int ChunkSize => chunkSize;

  public int GetChunkSizeFromPrefabName(string prefabName)
  {
    if (prefabName.Contains(PrefabNames.ShipChunkBoundary1x1x1))
    {
      return 1;
    }
    if (prefabName.Contains(PrefabNames.ShipChunkBoundary4x4x4))
    {
      return 4;
    }
    if (prefabName.Contains(PrefabNames.ShipChunkBoundary8x8x8))
    {
      return 8;
    }
    if (prefabName.Contains(PrefabNames.ShipChunkBoundary16x16x16))
    {
      return 16;
    }

    throw new System.Exception($"Unknown chunk size for prefab name: {prefabName}");
  }

  public void Awake()
  {
    chunkSize = GetChunkSizeFromPrefabName(gameObject.name);
  }
}