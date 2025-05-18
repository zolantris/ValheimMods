// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Linq;
using Jotunn;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.Integrations;

public class PowerNetworkControllerIntegration : PowerNetworkController
{
  protected override void FixedUpdate()
  {
    if (!isActiveAndEnabled || !ZNet.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    foreach (var pair in _networks)
    {
      if (pair.Value.Count == 0) continue;
      var currentZone = ZoneSystem.GetZone(pair.Value[0].Position);

      // skip all unloaded network simulations.
      if (!ZoneSystem.instance.IsZoneLoaded(currentZone))
      {
        return;
      }

      // client + server (combo) and Dedicated server.
      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(pair.Value, pair.Key);
      }

      // Client only. Dedicated server should not run.
      if (!ZNet.instance.IsDedicated())
      {
        Client_SimulateNetwork(pair.Value, pair.Key);
      }

      if (ZNet.instance.IsServer())
      {
        SyncNetworkState(pair.Value);
      }
      else
      {
        SyncNetworkStateClient(pair.Value);
      }
    }

    // To be run after all loops. Todo if this made to run per network it might be better.
    if (ZNet.instance.IsClientInstance())
    {
      Client_SimulateNetworkPowerAnimations();
    }
  }
}