// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class ZoneUtil
  {
    public const int ZoneSize = 64;

    public static Vector3Int GetZoneCoord(Vector3 worldPosition)
    {
      int x = Mathf.FloorToInt(worldPosition.x / ZoneSize);
      int z = Mathf.FloorToInt(worldPosition.z / ZoneSize);
      return new Vector3Int(x, 0, z);
    }

    public static Vector3 GetZoneWorldOrigin(Vector3Int zoneCoord)
    {
      return new Vector3(zoneCoord.x * ZoneSize, 0f, zoneCoord.z * ZoneSize);
    }

    public static Vector3Int WorldToChunkCoord(Vector3 worldPosition, int chunkSize)
    {
      int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
      int y = Mathf.FloorToInt(worldPosition.y / chunkSize);
      int z = Mathf.FloorToInt(worldPosition.z / chunkSize);
      return new Vector3Int(x, y, z);
    }

    public static Vector3 GetChunkWorldOrigin(Vector3Int chunkCoord, int chunkSize)
    {
      return new Vector3(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, chunkCoord.z * chunkSize);
    }


    public static void ApplyMaterialValues(ZoneInstance from, ZoneTerrainInstanceController to)
    {
      to.cubeMaterial = from.cubeMaterial;
      to.fillerMaterial = from.fillerMaterial;
      to.terrainMaterial = from.terrainMaterial;
    }

    public static void ApplyMaterialValues(ZoneManager from, ZoneInstance to)
    {
      to.cubeMaterial = from.cubeMaterial;
      to.fillerMaterial = from.fillerMaterial;
      to.terrainMaterial = from.terrainMaterial;
    }
  }
}