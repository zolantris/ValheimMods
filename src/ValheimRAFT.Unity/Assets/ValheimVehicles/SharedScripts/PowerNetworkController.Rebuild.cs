// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController : SingletonBehaviour<PowerNetworkController>
  {
    private const float MaxNetworkJoinDistance = 16f;
    private const float MaxNetworkJoinDistanceSqr = MaxNetworkJoinDistance * MaxNetworkJoinDistance;

    private static readonly List<IPowerNode> _allNodes = new();
    private static readonly HashSet<IPowerNode> _unvisited = new();
    private static readonly Queue<IPowerNode> _pending = new();

    private readonly Stopwatch rebuildTimer = new();

    public IEnumerator RebuildPowerNetworkCoroutine()
    {
      yield return new WaitForSeconds(1f);

      // Step 1: Aggregate all valid power nodes
      _allNodes.Clear();
      PowerNetworkDataInstances.Clear();

      for (var i = 0; i < Conduits.Count; i++)
        if (Conduits.TryGetValidElement(ref i, out var conduit))
          _allNodes.Add(conduit);

      for (var i = 0; i < Pylons.Count; i++)
        if (Pylons.TryGetValidElement(ref i, out var pylon))
          _allNodes.Add(pylon);

      for (var i = 0; i < Sources.Count; i++)
        if (Sources.TryGetValidElement(ref i, out var source))
          _allNodes.Add(source);

      for (var i = 0; i < Consumers.Count; i++)
        if (Consumers.TryGetValidElement(ref i, out var consumer))
          _allNodes.Add(consumer);

      for (var i = 0; i < Storages.Count; i++)
        if (Storages.TryGetValidElement(ref i, out var storage))
          _allNodes.Add(storage);

      // Step 2: Build networks by proximity
      _unvisited.Clear();
      foreach (var node in _allNodes)
        _unvisited.Add(node);

      _networks.Clear();
      rebuildTimer.Restart();

      while (_unvisited.Count > 0)
      {
        if (rebuildTimer.ElapsedMilliseconds > 10)
        {
          rebuildTimer.Restart();
          yield return null;
        }

        var root = _unvisited.First();
        var networkId = Guid.NewGuid().ToString();
        var cluster = new List<IPowerNode>();

        _pending.Clear();
        _pending.Enqueue(root);
        _unvisited.Remove(root);

        while (_pending.Count > 0)
        {
          if (rebuildTimer.ElapsedMilliseconds > 10)
          {
            rebuildTimer.Restart();
            yield return null;
          }

          var current = _pending.Dequeue();
          current.SetNetworkId(networkId);
          cluster.Add(current);

          foreach (var other in _unvisited.ToList())
          {
            if ((current.Position - other.Position).sqrMagnitude <= MaxNetworkJoinDistanceSqr)
            {
              _pending.Enqueue(other);
              _unvisited.Remove(other);
            }
          }
        }

        _networks[networkId] = cluster;
      }

      rebuildTimer.Reset();
      _rebuildPylonNetworkRoutine = null;
      yield return new WaitForSeconds(1f);
    }
  }
}