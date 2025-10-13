using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Storage.Serialization;
using ValheimVehicles.ValheimVehicles.Structs;
namespace ValheimVehicles.Controllers;

public class VehicleChunkController
{
  private int chunkSize = 1;
  public static Vector3 eraserCubeSize = Vector3.one * 0.25f;

  public int ChunkSize => chunkSize;

  public static int GetChunkSizeFromPrefabName(string prefabName)
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

  /**
     * For testing if a piece is a ship chunk boundary piece.
     *
     * This is used to prevent vehicles from being nested within each other.
     */
  public static bool IsShipChunkBoundaryPiece(string name)
  {
    return name.StartsWith(PrefabNames.ShipChunkBoundary1x1x1) ||
           name.StartsWith(PrefabNames.ShipChunkBoundary4x4x4) ||
           name.StartsWith(PrefabNames.ShipChunkBoundary8x8x8) ||
           name.StartsWith(PrefabNames.ShipChunkBoundary16x16x16);
  }

  public static bool IsShipChunkBoundaryEraser(string name)
  {
    return name.StartsWith(PrefabNames.ShipChunkBoundaryEraser);
  }

  /**
   * Gets the scale for a boundary piece based on its name.
   */
  public static Vector3 GetBoundaryPieceScale(string name)
  {
    if (name.StartsWith(PrefabNames.ShipChunkBoundary1x1x1))
      return Vector3.one;
    if (name.StartsWith(PrefabNames.ShipChunkBoundary4x4x4))
      return Vector3.one * 4f;
    if (name.StartsWith(PrefabNames.ShipChunkBoundary8x8x8))
      return Vector3.one * 8f;
    if (name.StartsWith(PrefabNames.ShipChunkBoundary16x16x16))
      return Vector3.one * 16f;

    return Vector3.one;
  }

  public static VehicleChunkSizeData ToChunkSizeData(string name, Vector3 localPosition)
  {
    return new VehicleChunkSizeData
    {
      position = new SerializableVector3(localPosition),
      chunkSize = GetChunkSizeFromPrefabName(name)
    };
  }
}