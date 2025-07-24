#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ZoneData
  {
    public Vector3Int Coord { get; set; } // World zone coordinate (used as key)
    public Vector3 ZoneOrigin => ZoneUtil.GetZoneWorldOrigin(Coord);
    public GameObject ZoneGO { get; set; } // Root GameObject of the zone
    public ZoneTerrainConfig TerrainConfig { get; set; } // Cloned config for this zone

    // Optional: Runtime info
    public bool IsActive { get; set; } // If the zone is loaded and actively updating
    public float LastUsedTime { get; set; } // Time last accessed (for GC/cleanup)
  }
}