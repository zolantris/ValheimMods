using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Patches;

/// <summary>
/// A much more efficient way to do EffectArea checks, b/c each effect is first looking for a partion instead of all bounds.
///
/// Spatial Partitioning: Implement a grid or a quadtree/octree structure to partition the space into manageable sections. Each EffectArea can be associated with one or more partitions based on its bounds.
/// </summary>
/// <param name="area"></param>
using System.Collections.Generic;
using UnityEngine;

public class SpatialPartitioningManager
{
  private Dictionary<Vector3Int, List<EffectArea>> grid =
    new Dictionary<Vector3Int, List<EffectArea>>();

  private int cellSize;

  public SpatialPartitioningManager(int cellSize)
  {
    this.cellSize = cellSize;
  }

  private Vector3Int GetCellKey(Vector3 position)
  {
    return new Vector3Int(
      Mathf.FloorToInt(position.x / cellSize),
      Mathf.FloorToInt(position.y / cellSize),
      Mathf.FloorToInt(position.z / cellSize)
    );
  }

  public void RegisterArea(EffectArea area)
  {
    var bounds = area.m_collider.bounds;
    var min = bounds.min;
    var max = bounds.max;

    // Calculate cells covered by the bounds
    Vector3Int minCell = GetCellKey(min);
    Vector3Int maxCell = GetCellKey(max);

    for (int x = minCell.x; x <= maxCell.x; x++)
    {
      for (int y = minCell.y; y <= maxCell.y; y++)
      {
        for (int z = minCell.z; z <= maxCell.z; z++)
        {
          Vector3Int cellKey = new Vector3Int(x, y, z);
          if (!grid.ContainsKey(cellKey))
          {
            grid[cellKey] = new List<EffectArea>();
          }

          grid[cellKey].Add(area);
        }
      }
    }
  }

  public void UnregisterArea(EffectArea area)
  {
    var bounds = area.m_collider.bounds;
    var min = bounds.min;
    var max = bounds.max;

    Vector3Int minCell = GetCellKey(min);
    Vector3Int maxCell = GetCellKey(max);

    for (int x = minCell.x; x <= maxCell.x; x++)
    {
      for (int y = minCell.y; y <= maxCell.y; y++)
      {
        for (int z = minCell.z; z <= maxCell.z; z++)
        {
          Vector3Int cellKey = new Vector3Int(x, y, z);
          if (grid.ContainsKey(cellKey))
          {
            grid[cellKey].Remove(area);
          }
        }
      }
    }
  }

  public EffectArea CheckPoint(Vector3 point)
  {
    Vector3Int cellKey = GetCellKey(point);
    if (grid.TryGetValue(cellKey, out var areas))
    {
      foreach (var area in areas)
      {
        if (area.m_collider.bounds.Contains(point))
        {
          return area;
        }
      }
    }

    return null;
  }
}

public static class EffectAreaManager
{
  // Checks if a point is within no-monster areas
  public static bool CheckPoint(Vector3 point, out EffectArea effectArea)
  {
    // Check if the point is already in the cache
    if (EffectsArea_VehiclePatches.cachedNoMonsterAreas.TryGetValue(point,
          out effectArea))
    {
      return true; // Point is inside an area
    }

    // Perform the check against existing areas
    foreach (var area in EffectArea.s_noMonsterAreas)
    {
      if (area.Key.Contains(point))
      {
        effectArea = area.Value;
        EffectsArea_VehiclePatches.cachedNoMonsterAreas[point] =
          effectArea; // Cache the result for future checks
        return true; // Found a matching area
      }
    }

    effectArea = null; // No area found
    return false; // No match
  }

  // Checks if a point is close to no-monster areas
  public static bool CheckPointCloseTo(Vector3 point, out EffectArea effectArea)
  {
    // Check proximity logic
    foreach (var area in EffectArea.s_noMonsterCloseToAreas)
    {
      if (area.Key.Contains(point))
      {
        effectArea = area.Value;
        EffectsArea_VehiclePatches.cachedCloseToMonsterAreas[point] =
          effectArea; // Cache for efficiency
        return true; // Found a matching area
      }
    }

    effectArea = null; // No area found
    return false; // No match
  }

  // Check if point is inside a specific type of area
  public static bool CheckPointInArea(Vector3 point, EffectArea.Type type,
    out EffectArea area)
  {
    // Logic to check against the area type
    foreach (var effectArea in EffectArea.s_allAreas)
    {
      if ((effectArea.m_type & type) != EffectArea.Type.None &&
          effectArea.m_collider.bounds.Contains(point))
      {
        area = effectArea;
        return true; // Found a matching area
      }
    }

    area = null; // No area found
    return false; // No match
  }

  // Check if point is inside a burning area
  public static bool CheckBurningArea(Vector3 point, out EffectArea area)
  {
    foreach (var burningArea in EffectArea.s_BurningAreas)
    {
      if (burningArea.Key.Contains(point))
      {
        area = burningArea.Value;
        return true; // Found a matching burning area
      }
    }

    area = null; // No area found
    return false; // No match
  }
}

[HarmonyPatch(typeof(EffectArea))]
public static class EffectsArea_VehiclePatches
{
  public static readonly Dictionary<Vector3, EffectArea> cachedNoMonsterAreas =
    new();

  public static readonly Dictionary<Vector3, EffectArea>
    cachedCloseToMonsterAreas = new();
}