using System.Collections.Generic;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration
{
  public static PowerNetworkSimData BuildSimDataForCluster(IEnumerable<ZDO> zdos)
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
          // Add players from tracker
          if (PowerConduitStateTracker.TryGet(zdo.m_uid, out var state))
          {
            foreach (var id in state.PlayerIds)
              conduit.AddPlayer(id);
          }

          simData.Conduits.Add((conduit, zdo));
        }
      }
      else if (prefab == PrefabNameHashes.Mechanism_Power_Pylon)
      {
        if (PowerComputeFactory.TryCreatePylon(zdo, prefab, out var pylon))
          simData.Pylons.Add((pylon, zdo));
      }
    }

    return simData;
  }
}