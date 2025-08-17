// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using Eldritch.Core.Nav;

// Aliases make it explicit we bind to the game's runtime types.
using GamePathfinding = Pathfinding;
using GameAgentType = Pathfinding.AgentType;
using GameAgentSettings = Pathfinding.AgentSettings;

namespace Eldritch.Valheim
{
  /// <summary>
  /// Concrete shim that forwards to Valheim's global Pathfinding.instance.
  /// All foreign enums/classes are contained here, never in Eldritch.Core.
  /// </summary>
  public sealed class ValheimPathfindingShim : IPathfindingShim
  {
    private GamePathfinding Instance => GamePathfinding.instance;

    public bool GetPath(
      Vector3 from,
      Vector3 to,
      List<Vector3> path,
      int agentType,
      bool requireFullPath = false,
      bool cleanup = true,
      bool havePath = false)
    {
      var inst = Instance;
      if (inst == null) return false;

      // Cast to the runtime enum used by the game
      var gameType = (GameAgentType)agentType;

      // This calls the *actual* Valheim API from your attached source
      return inst.GetPath(from, to, path, gameType, requireFullPath, cleanup, havePath);
    }

    public bool TrySnapToNav(Vector3 center, int agentType, out Vector3 snapped)
    {
      snapped = Vector3.zero;

      var inst = Instance;
      if (inst == null) return false;

      var p = center;

      // Cast to runtime enum, then request the runtime's AgentSettings object
      var gameType = (GameAgentType)agentType;
      var settings = inst.GetSettings(gameType);

      // Exactly mirrors your requirement:
      // ok = instance.SnapToNavMesh(ref p, true, instance.GetSettings(agentType));
      var ok = inst.SnapToNavMesh(ref p, true, settings);
      snapped = p;
      return ok;
    }
    public bool FindValidPoint(out Vector3 point, Vector3 center, float range, int agentType)
    {
      point = Vector3.zero;
      if (Pathfinding.instance == null) return false;
      return Pathfinding.instance.FindValidPoint(out point, center, range, (GameAgentType)agentType);
    }

    public bool HavePath(Vector3 from, Vector3 to, int agentType)
    {
      return Pathfinding.instance.HavePath(from, to, (GameAgentType)agentType);
    }
  }
}