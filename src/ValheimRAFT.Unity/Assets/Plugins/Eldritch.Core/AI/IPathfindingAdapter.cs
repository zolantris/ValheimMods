using System.Collections.Generic;
using UnityEngine;
namespace Eldritch.Core
{
  public interface IPathfindingAdapter
  {
    // Return true and fill corners with navmesh corners (already smoothed enough for steering).
    bool TryGetPath(Vector3 start, Vector3 end, List<Vector3> corners, int agentType);

    // Cheap reachability check (line-of-path exists that the agent could follow).
    bool HavePath(Vector3 start, Vector3 end, int agentType);

    // Snap/validate a point near the navmesh for this agent.
    bool FindValidPoint(out Vector3 point, Vector3 near, float range, int agentType);
  }

  // Static access point the AI will use everywhere.
  public static class PathfindingAPI
  {
    public static IPathfindingAdapter Instance { get; set; }
  }
}