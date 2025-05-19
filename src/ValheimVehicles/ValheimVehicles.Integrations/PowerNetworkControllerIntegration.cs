// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.Integrations;

public class PowerNetworkControllerIntegration : PowerNetworkController
{
  private readonly List<string> _networksToRemove = new();
  private readonly List<IPowerNode> _nodesToRemove = new();

  // DO nothing for fixed update. Hosts cannot run FixedUpdate on server I think...
  protected override void FixedUpdate() {}
  protected void Update()
  {
    SimulateOnClientAndServer();
  }

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    foreach (var pair in _networks)
    {
      var nodes = pair.Value;

      if (nodes == null || nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      _nodesToRemove.Clear();

      // Prune invalid nodes
      for (var i = 0; i < nodes.Count; i++)
      {
        var node = nodes[i];

        // Only prune if the node object is destroyed or transform is invalid
        if (node == null || node.transform == null)
        {
          _nodesToRemove.Add(node);
        }
      }

      if (_nodesToRemove.Count > 0)
      {
        foreach (var deadNode in _nodesToRemove)
          nodes.Remove(deadNode);
      }

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(pair.Key);
        continue;
      }

      var currentZone = ZoneSystem.GetZone(nodes[0].Position);
      if (!ZoneSystem.instance.IsZoneLoaded(currentZone))
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(nodes, pair.Key);
      }

      if (!ZNet.instance.IsDedicated())
      {
        Client_SimulateNetwork(nodes, pair.Key);
      }

      if (ZNet.instance.IsServer())
      {
        SyncNetworkState(nodes);
      }
      else
      {
        SyncNetworkStateClient(nodes);
      }
    }

    foreach (var key in _networksToRemove)
    {
      _networks.Remove(key);
    }

    _networksToRemove.Clear();
  }


}