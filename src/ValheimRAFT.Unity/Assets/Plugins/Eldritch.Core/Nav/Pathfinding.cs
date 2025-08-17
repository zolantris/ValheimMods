// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eldritch.Core.Nav
{
  /// <summary>
  /// Runtime-pluggable shim that hides the actual game API from Core.
  /// NOTE: agentType is an int on purpose to avoid leaking foreign enums.
  /// </summary>
  public interface IPathfindingShim
  {
    bool GetPath(
      Vector3 from,
      Vector3 to,
      List<Vector3> path,
      int agentType,
      bool requireFullPath = false,
      bool cleanup = true,
      bool havePath = false);

    bool TrySnapToNav(Vector3 center, int agentType, out Vector3 snapped);
    public bool FindValidPoint(out Vector3 point, Vector3 center, float range, int agentType);
    public bool HavePath(Vector3 from, Vector3 to, int agentType);
  }

  /// <summary>
  /// Static façade Core can call from anywhere. An integration registers the shim at runtime.
  /// </summary>
  public static class Pathfinding
  {
    private static IPathfindingShim _shim = new NullShim();

    public static bool IsValid()
    {
      return _shim != null;
    }

    /// <summary>Register the runtime integration shim (e.g., from Eldritch.Valheim).</summary>
    public static void Register(IPathfindingShim shim)
    {
      _shim = shim ?? throw new ArgumentNullException(nameof(shim));
    }

    /// <summary>
    /// Route to the runtime pathfinder. Mirrors the Valheim signature you use in XenoDroneAI.
    /// </summary>
    public static bool GetPath(
      Vector3 from,
      Vector3 to,
      List<Vector3> path,
      int agentType,
      bool requireFullPath = false,
      bool cleanup = true,
      bool havePath = false)
    {
      return _shim.GetPath(from, to, path, agentType, requireFullPath, cleanup, havePath);
    }

    /// <summary>
    /// Route to the runtime pathfinder's SnapToNavMesh via GetSettings(agentType).
    /// </summary>
    public static bool TrySnapToNav(Vector3 center, int agentType, out Vector3 snapped)
    {
      return _shim.TrySnapToNav(center, agentType, out snapped);
    }

    public static bool FindValidPoint(out Vector3 point, Vector3 center, float range, int agentType)
    {
      return _shim.FindValidPoint(out point, center, range, agentType);
    }

    public static bool HavePath(Vector3 from, Vector3 to, int agentType)
    {
      return _shim.HavePath(from, to, agentType);
    }


    private sealed class NullShim : IPathfindingShim
    {
      public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, int agentType, bool requireFullPath = false, bool cleanup = true, bool havePath = false)
      {
        path?.Clear();
        return false;
      }

      public bool TrySnapToNav(Vector3 center, int agentType, out Vector3 snapped)
      {
        snapped = center;
        return false;
      }

      public bool FindValidPoint(out Vector3 point, Vector3 center, float range, int agentType)
      {
        point = Vector3.zero;
        return false;
      }
      public bool HavePath(Vector3 from, Vector3 to, int agentType)
      {
        return false;
      }
    }
  }
}