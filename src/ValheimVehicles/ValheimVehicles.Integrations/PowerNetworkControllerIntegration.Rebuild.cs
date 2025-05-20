using System.Collections.Generic;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration
{
  public readonly Dictionary<string, PowerNetworkSimData> CachedSimulateData = new();

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

    LoggerProvider.LogDev($"[SIM] Rebuilding {networkId} with {zdos.Count} ZDOs");

    foreach (var (source, zdo) in simData.Sources)
    {
      LoggerProvider.LogDev($"[SIM] Source: {zdo.m_uid}, output = {source.OutputRate}");
    }

    foreach (var (storage, zdo) in simData.Storages)
    {
      LoggerProvider.LogDev($"[SIM] Storage: {zdo.m_uid}, stored = {storage.StoredEnergy}");
    }

    foreach (var (conduit, zdo) in simData.Conduits)
    {
      LoggerProvider.LogDev($"[SIM] Conduit: {zdo.m_uid}, players = {conduit.Players.Count}");
    }

    foreach (var zdo in zdos)
    {
      var prefab = zdo.GetPrefab();
      if (prefab == PrefabNameHashes.Mechanism_Power_Source_Coal ||
          prefab == PrefabNameHashes.Mechanism_Power_Source_Eitr)
      {
        if (PowerComputeFactory.TryCreateSource(zdo, out var source))
          simData.Sources.Add((source, zdo));
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
      {
        if (PowerComputeFactory.TryCreateStorage(zdo, out var storage))
          simData.Storages.Add((storage, zdo));
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Consumer_Swivel)
      {
        if (PowerComputeFactory.TryCreateConsumer(zdo, out var consumer))
        {
          simData.Consumers.Add((consumer, zdo));
        }
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate ||
               prefab == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate)
      {
        if (PowerComputeFactory.TryCreateConduit(zdo, out var conduit))
        {
          var zdoid = zdo.m_uid;
          conduit.PlayerIds.Clear();
          conduit.Players.Clear();

          if (PowerZDONetworkManager.TryGetData<PowerConduitData>(zdo, out var data))
          {
            conduit.PlayerIds.Clear();
            conduit.Players.Clear();

            foreach (var pid in data.PlayerIds)
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