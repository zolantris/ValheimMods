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

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController : SingletonBehaviour<PowerNetworkController>
  {
    private const float MaxNetworkJoinDistance = 16f;
    private const float MaxNetworkJoinDistanceSqr = MaxNetworkJoinDistance * MaxNetworkJoinDistance;

    public static readonly List<IPowerNode> AllPowerNodes = new();
    private static readonly HashSet<IPowerNode> _unvisited = new();
    private static readonly Queue<IPowerNode> _pending = new();

    private readonly Stopwatch rebuildTimer = new();

    public static void RequestRebuildNetwork()
    {
      if (Instance == null || Instance._rebuildPylonNetworkRoutine != null) { return; }

      // do not run on dedicated servers.
      if (ZNet.instance.IsDedicated())
      {
        return;
      }

      Instance._rebuildPylonNetworkRoutine = Instance.StartCoroutine(Instance.RequestRebuildPowerNetworkCoroutine());
    }

    public static void UpdateAllPowerNodes()
    {
      AllPowerNodes.Clear();

      for (var i = 0; i < Conduits.Count; i++)
        if (Conduits.TryGetValidElement(ref i, out var conduit))
          AllPowerNodes.Add(conduit);

      for (var i = 0; i < Pylons.Count; i++)
        if (Pylons.TryGetValidElement(ref i, out var pylon))
          AllPowerNodes.Add(pylon);

      for (var i = 0; i < Sources.Count; i++)
        if (Sources.TryGetValidElement(ref i, out var source))
          AllPowerNodes.Add(source);

      for (var i = 0; i < Consumers.Count; i++)
        if (Consumers.TryGetValidElement(ref i, out var consumer))
          AllPowerNodes.Add(consumer);

      for (var i = 0; i < Storages.Count; i++)
        if (Storages.TryGetValidElement(ref i, out var storage))
          AllPowerNodes.Add(storage);
    }

    /// <summary>
    /// This likely will be overriden in integration layer or not called. Instead we would iterate off the game data objects (ZDOS) similar to server.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator RequestRebuildPowerNetworkCoroutine()
    {
      LoggerProvider.LogInfoDebounced("Called RebuildPowerNetworkCoroutine");
      yield return new WaitForSeconds(1f);

      // Step 1: Aggregate all valid power nodes
      PowerNetworkDataInstances.Clear();
      UpdateAllPowerNodes();

      // Step 2: Build networks by proximity
      _unvisited.Clear();
      foreach (var node in AllPowerNodes)
        _unvisited.Add(node);

      powerNodeNetworks.Clear();
      rebuildTimer.Restart();

      while (_unvisited.Count > 0)
      {
        if (rebuildTimer.ElapsedMilliseconds > 10 && !ZNet.instance.IsDedicated())
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
          if (rebuildTimer.ElapsedMilliseconds > 100 && !ZNet.instance.IsDedicated())
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

        powerNodeNetworks[networkId] = cluster;
      }
      LoggerProvider.LogInfoDebounced($"Rebuild with {powerNodeNetworks.Count}, scanned through {AllPowerNodes.Count} nodes in {rebuildTimer.ElapsedMilliseconds}ms");

      rebuildTimer.Reset();
      _rebuildPylonNetworkRoutine = null;
      LoggerProvider.LogInfoDebounced("At the near end of rebuild");

      yield return new WaitForSeconds(1f);
      LoggerProvider.LogInfoDebounced("At the end of rebuild");

      LoggerProvider.LogInfoDebounced($"_networks, {powerNodeNetworks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");
    }
  }
}