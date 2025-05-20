using System.Collections.Generic;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration
{
  private readonly Dictionary<string, PowerNetworkSimData> CachedSimulateData = new();

  public bool TryBuildPowerNetworkSimData(string networkId, out PowerNetworkSimData simData)
  {
    if (!CachedSimulateData.TryGetValue(networkId, out simData))
    {
      if (!PowerZDONetworkManager.Networks.TryGetValue(networkId, out var zdos) || zdos == null)
        return false;

      BuildPowerNetworkSimData(networkId, zdos);
      simData = CachedSimulateData[networkId]; // safe: just built
    }
    return simData != null;
  }

  public void BuildPowerNetworkSimData(string networkId, List<ZDO> zdos)
  {
    var simData = new PowerNetworkSimData();

    foreach (var zdo in zdos)
    {
      var prefab = zdo.GetPrefab();
      if (prefab == PrefabNameHashes.Mechanism_Power_Source_Coal ||
          prefab == PrefabNameHashes.Mechanism_Power_Source_Eitr)
      {
        if (PowerComputeFactory.TryCreateSource(zdo, prefab, out var source))
          simData.Sources.Add((source, zdo));
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
      {
        if (PowerComputeFactory.TryCreateStorage(zdo, prefab, out var storage))
          simData.Storages.Add((storage, zdo));
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate ||
               prefab == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate)
      {
        if (PowerComputeFactory.TryCreateConduit(zdo, prefab, out var conduit))
        {
          var zdoid = zdo.m_uid;
          conduit.PlayerIds.Clear();
          conduit.Players.Clear();

          if (PowerConduitStateTracker.TryGet(zdoid, out var state))
          {
            foreach (var pid in state.PlayerIds)
            {
              conduit.PlayerIds.Add(pid);
              var player = Player.GetPlayer(pid);
              if (player != null)
              {
                conduit.Players.Add(player);
              }
            }
          }

          simData.Conduits.Add((conduit, zdo));
        }
      }
    }

    CachedSimulateData[networkId] = simData;
  }
}