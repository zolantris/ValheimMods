// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

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
      if (ZNet.instance.IsServer())
      {
        SyncNetworkState(pair.Value);
      }
      else
      {
        SyncNetworkStateClient(pair.Value);
      }

      SimulateNetwork(pair.Value, pair.Key);
    }
  }
}