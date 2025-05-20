// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using ValheimVehicles.Structs;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    public static Dictionary<string, PowerNetworkData> PowerNetworkDataInstances = new();

    // todo add cut-off logic where batteries do not request above 0.75f for fuel sources only. But they can from drain plates and other renewable resources.
    public static float FuelSourcePercentageCap = 0.75f;
    private readonly List<IPowerConduit> _conduits = new();
    private readonly List<IPowerConsumer> _consumers = new();

    private readonly List<IPowerSource> _sources = new();
    private readonly List<IPowerStorage> _storage = new();

    public static bool TryNetworkPowerData(string networkId, [NotNullWhen(true)] out PowerNetworkData? _data)
    {
      if (PowerNetworkDataInstances.TryGetValue(networkId, out _data))
      {
        // if (_data.Cached_NetworkDataString == string.Empty)
        // {
        //   _data.Cached_NetworkDataString = GenerateNetworkDataString(networkId);
        //   PowerNetworkDataInstances[networkId] = _data;
        // }
        return true;
      }
      _data = null;
      return false;
    }

    public static void UpdateNetworkPowerData(string networkId, PowerNetworkData data)
    {
      PowerNetworkDataInstances[networkId] = data;
    }

    public static void ClearNetworkPowerData(string networkId)
    {
      PowerNetworkDataInstances.Clear();
    }

    public void Client_SimulateNetwork(List<IPowerNode> nodes, string networkId, bool canSkip = false)
    {
      if (!canSkip)
      {
        UpdateListData(nodes);
      }

      var totalConsumerDemand = GetTotalConsumerDemand(Time.fixedDeltaTime);

      var remainingStorage = 0f;
      var totalPowerCapacity = 0f;
      var totalFuel = 0f;
      var totalFuelCapacity = 0f;

      _storage.ForEach(x =>
      {
        if (x != null)
        {
          remainingStorage += x.ChargeLevel;
          totalPowerCapacity += x.Capacity;
        }
      });

      _sources.ForEach(x =>
      {
        totalFuel += x.GetFuelLevel();
        totalFuelCapacity += x.GetFuelCapacity();
      });

      var newData = new PowerNetworkData
      {
        NetworkConsumerPowerStatus = GetNetworkHealthStatus(_consumers),
        NetworkPowerSupply = MathUtils.RoundToHundredth(remainingStorage),
        NetworkPowerCapacity = MathUtils.RoundToHundredth(totalPowerCapacity),
        NetworkPowerDemand = MathUtils.RoundToHundredth(totalConsumerDemand),
        NetworkFuelSupply = MathUtils.RoundToHundredth(totalFuel),
        NetworkFuelCapacity = MathUtils.RoundToHundredth(totalFuelCapacity)
      };
      newData.Cached_NetworkDataString = GenerateNetworkDataString(networkId, newData);
      UpdateNetworkPowerData(networkId, newData);

      if (ZNet.instance.IsServer())
      {
        LoggerProvider.LogInfoDebounced($"Host Server updated network data with Fuel: {newData.NetworkFuelSupply} PowerSupply: {newData.NetworkPowerSupply}");
      }
    }

    public float GetTotalConsumerDemand(float deltaTime)
    {
      var totalDemand = 0f;

      foreach (var consumer in _consumers)
        totalDemand += consumer.RequestedPower(deltaTime);

      // this will be used to calculate the demand from non-conduits. Conduits do not show up on GUI.
      var totalConsumerDemand = totalDemand;
      return totalDemand;
    }

    public void ClearLocalListData()
    {
      _sources.Clear();
      _storage.Clear();
      _consumers.Clear();
      _conduits.Clear();
    }

    public void ClearAllSimulatedNetworkData()
    {
      ClearLocalListData();
      _networks.Clear();
    }

    public void UpdateListData(List<IPowerNode> nodes)
    {
      ClearLocalListData();

      // Categorize nodes
      foreach (var node in nodes)
      {
        switch (node)
        {
          case IPowerSource source:
            _sources.Add(source);
            break;
          case IPowerStorage storage:
            _storage.Add(storage);
            break;
          case IPowerConsumer consumer when consumer.IsDemanding:
            _consumers.Add(consumer);
            break;
          case IPowerConduit conduit:
            _conduits.Add(conduit);
            break;
        }
      }
    }

    public void Client_SimulateNetworkPowerAnimations()
    {

      // foreach (var powerNetworkDataInstance in PowerNetworkDataInstances)
      // {
      //   if (powerNetworkDataInstance.Value.NetworkConsumerPowerStatus == "Normal")
      //   {
      //     // run lightning burst for this network.
      //   }
      // }
      // 7. Lightning burst effect (visual only)
      // if (lightningBurstCoroutine != null && !isDemanding)
      // {
      //   StopCoroutine(lightningBurstCoroutine);
      //   lightningBurstCoroutine = null;
      // }

      // if (isDemanding)
      // {
      // if (lightningBurstCoroutine != null)
      // {
      //   StopCoroutine(lightningBurstCoroutine);
      //   lightningBurstCoroutine = null;
      // }
      // if (lightningBurstCoroutine != null) return;
      // lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
      // }
    }

    private readonly Dictionary<string, PowerNetworkSimData> _netSimDataCache = new();

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

      _netSimDataCache[networkId] = simData;
    }


    public void Host_SimulateNetworkZDOFull(string networkId)
    {
      if (!_netSimDataCache.TryGetValue(networkId, out var simData))
        return;

      var deltaTime = Time.fixedDeltaTime;
      var changedZDOs = new HashSet<ZDOID>();

      var totalDemand = 0f;

      // Step 1: Total demand
      foreach (var (storage, _) in simData.Storages)
      {
        totalDemand += Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
      }

      var conduitDemand = 0f;
      foreach (var (conduit, _) in simData.Conduits)
      {
        conduit.SanitizePlayers();
        conduitDemand += PowerConduitPlateComponentIntegration.GetAverageEitr(conduit.Players);
      }

      totalDemand += conduitDemand;
      var isDemanding = totalDemand > 0f;

      // Step 2: Peek storage discharge
      var suppliedFromStorage = 0f;
      var storageDischargeMap = new Dictionary<PowerStorageData, float>();

      var remainingDemand = totalDemand;
      foreach (var (storage, _) in simData.Storages)
      {
        if (remainingDemand <= 0f) break;

        var dischargeAmount = Mathf.Min(storage.StoredEnergy, remainingDemand);
        if (dischargeAmount > 0f)
        {
          storageDischargeMap[storage] = dischargeAmount;
          suppliedFromStorage += dischargeAmount;
          remainingDemand -= dischargeAmount;
        }
      }

      // Step 3: Offer from sources
      var offeredFromSources = 0f;
      var sourceOfferMap = new Dictionary<PowerSourceData, float>();

      const float fuelToEnergy = 10f;
      const float maxFuelRate = 0.1f;

      foreach (var (source, _) in simData.Sources)
      {
        if (remainingDemand <= 0f && !NeedsCharging(simData.Storages)) break;

        var burnable = Mathf.Min(source.Fuel, maxFuelRate * deltaTime);
        var potentialEnergy = burnable * fuelToEnergy;
        var clamped = Mathf.Min(potentialEnergy, source.OutputRate * deltaTime);

        if (clamped > 0f)
        {
          sourceOfferMap[source] = clamped;
          offeredFromSources += clamped;
          remainingDemand -= clamped;
        }
      }

      var totalAvailable = offeredFromSources + suppliedFromStorage;

      // Step 4: Apply to conduits (players needing Eitr)
      foreach (var (conduit, zdo) in simData.Conduits)
      {
        if (totalAvailable <= 0f || conduit.Players.Count == 0) continue;

        var perPlayer = totalAvailable / conduit.Players.Count;
        var used = 0f;

        foreach (var player in conduit.Players)
        {
          if (player.m_eitr < player.m_maxEitr - 0.1f)
          {
            PowerConduitPlateComponentIntegration.Request_AddEitr(player, perPlayer);
            used += perPlayer;
          }
        }

        totalAvailable -= used;
        changedZDOs.Add(zdo.m_uid);
      }

      // Step 5: Feed true surplus into storage
      foreach (var (storage, zdo) in simData.Storages)
      {
        if (totalAvailable <= 0f) break;

        var remainingCap = Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
        var accepted = Mathf.Min(remainingCap, totalAvailable);

        storage.StoredEnergy += accepted;
        totalAvailable -= accepted;

        changedZDOs.Add(zdo.m_uid);
      }

      // Step 6: Final fuel/discharge commit
      var totalUsed = offeredFromSources + suppliedFromStorage - totalAvailable;
      var usedFromStorage = Mathf.Clamp(totalUsed - offeredFromSources, 0f, suppliedFromStorage);
      var usedFromSources = Mathf.Clamp(totalUsed - usedFromStorage, 0f, offeredFromSources);

      // Discharge
      var toDischarge = usedFromStorage;
      foreach (var kvp in storageDischargeMap)
      {
        var commit = Mathf.Min(kvp.Value, toDischarge);
        kvp.Key.StoredEnergy -= commit;
        toDischarge -= commit;
        if (toDischarge <= 0f) break;
      }

      // Fuel burn
      var toBurn = usedFromSources / fuelToEnergy;
      foreach (var kvp in sourceOfferMap)
      {
        var maxFuel = kvp.Value / fuelToEnergy;
        var burn = Mathf.Min(toBurn, maxFuel);
        kvp.Key.Fuel -= burn;
        toBurn -= burn;
        if (toBurn <= 0f) break;
      }

      // Step 7: ZDO Set
      foreach (var (source, zdo) in simData.Sources)
      {
        zdo.Set(VehicleZdoVars.Power_StoredFuel, source.Fuel);
        changedZDOs.Add(zdo.m_uid);
      }

      foreach (var (storage, zdo) in simData.Storages)
      {
        zdo.Set(VehicleZdoVars.Power_StoredEnergy, storage.StoredEnergy);
        changedZDOs.Add(zdo.m_uid);
      }

      // Step 8: Fire one RPC
      if (changedZDOs.Count > 0)
      {
        var pos = GetNetworkFallbackPosition(simData);
        PowerSystemRPC.SendPowerZDOsChangedToNearbyPlayers(networkId, changedZDOs.ToList(), simData);
      }
    }


    private static bool NeedsCharging(List<(PowerStorageData storage, ZDO zdo)> storages)
    {
      foreach (var (storage, _) in storages)
      {
        if (storage.StoredEnergy < storage.MaxCapacity)
          return true;
      }
      return false;
    }

    private static Vector3 GetNetworkFallbackPosition(PowerNetworkSimData simData)
    {
      if (simData.Conduits.Count > 0)
        return simData.Conduits[0].Item2.GetPosition();
      if (simData.Storages.Count > 0)
        return simData.Storages[0].Item2.GetPosition();
      if (simData.Sources.Count > 0)
        return simData.Sources[0].Item2.GetPosition();
      return Vector3.zero;
    }
    /// <summary>
    /// Should only be run by the owner.
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="networkId"></param>
    public void Host_SimulateNetwork(List<IPowerNode> nodes, string networkId)
    {
      var deltaTime = Time.fixedDeltaTime;

      UpdateListData(nodes);

      // 1. Total demand from consumers and conduits
      var totalDemand = GetTotalConsumerDemand(deltaTime);

      foreach (var conduit in _conduits)
      {
        if (conduit.IsDemanding)
          totalDemand += conduit.RequestPower(deltaTime);
      }

      // 1.5. Add battery demand if not full
      var storageDemand = 0f;
      foreach (var storage in _storage)
      {
        if (storage.ChargeLevel < storage.Capacity)
          storageDemand += Mathf.Clamp(storage.Capacity - storage.ChargeLevel, 0f, storage.Capacity);
      }

      totalDemand += storageDemand;
      var isDemanding = totalDemand > 0f;

      // 2. Try supplying from conduits and storage (peek only)
      var suppliedFromConduits = 0f;
      foreach (var conduit in _conduits)
      {
        if (totalDemand <= 0f) break;
        var supplied = conduit.SupplyPower(deltaTime);
        suppliedFromConduits += supplied;
        totalDemand -= supplied;
      }

      var storageDischargeMap = new Dictionary<IPowerStorage, float>();
      var suppliedFromStorage = 0f;
      var remainingDemand = totalDemand;

      foreach (var storage in _storage)
      {
        if (remainingDemand <= 0f) break;

        var offered = storage.PeekDischarge(remainingDemand);
        if (offered > 0f)
        {
          storageDischargeMap[storage] = offered;
          suppliedFromStorage += offered;
          remainingDemand -= offered;
        }
      }

      // 3. Offer power from sources if demand still exists or storage needs charge
      var offeredFromSources = 0f;
      var sourceOfferMap = new Dictionary<IPowerSource, float>();

      var hasChargeableStorage = _storage.Exists(s => s.ChargeLevel < s.Capacity);

      if (remainingDemand > 0f || hasChargeableStorage)
      {
        foreach (var source in _sources)
        {
          var offered = source.RequestAvailablePower(deltaTime, offeredFromSources, totalDemand, isDemanding);
          if (offered > 0f)
          {
            sourceOfferMap[source] = offered;
            offeredFromSources += offered;

            if (offeredFromSources + suppliedFromStorage + suppliedFromConduits >= totalDemand && !hasChargeableStorage)
              break;
          }
        }
      }

      var totalAvailable = offeredFromSources + suppliedFromStorage + suppliedFromConduits;

      // 4. Apply power to consumers
      foreach (var consumer in _consumers)
      {
        var needed = consumer.RequestedPower(deltaTime);
        var granted = Mathf.Min(needed, totalAvailable);
        totalAvailable -= granted;

        consumer.SetActive(granted > 0f);
        consumer.ApplyPower(granted, deltaTime);
      }

      // 4.5. Commit Discharge for only the power that was actually used
      var totalUsedPower = offeredFromSources + suppliedFromStorage + suppliedFromConduits - totalAvailable;

      var usedFromDischarge = Mathf.Clamp(totalUsedPower - offeredFromSources - suppliedFromConduits, 0f, suppliedFromStorage);

      var toCommitDischarge = usedFromDischarge;
      foreach (var kvp in storageDischargeMap)
      {
        var offered = kvp.Value;
        var commitAmount = Mathf.Min(offered, toCommitDischarge);
        kvp.Key.CommitDischarge(commitAmount);
        toCommitDischarge -= commitAmount;
        if (toCommitDischarge <= 0f) break;
      }


// ⚠️ Prevent recharge from own discharge
      totalAvailable -= suppliedFromStorage;
      totalAvailable = Mathf.Max(0f, totalAvailable); // avoid negatives

// 5. Feed true surplus to charge storage
      foreach (var storage in _storage)
      {
        if (totalAvailable <= 0f) break;

        var remainingCapacity = Mathf.Clamp(storage.Capacity - storage.ChargeLevel, 0f, storage.Capacity);
        if (remainingCapacity <= 0f) continue;

        var accepted = storage.Charge(totalAvailable);
        totalAvailable -= accepted;
      }

      // 6. Final fuel burn (committed energy from sources)
      var usedFromSources = Mathf.Clamp(totalUsedPower - usedFromDischarge - suppliedFromConduits, 0f, offeredFromSources);
      var toCommitSources = usedFromSources;

      foreach (var kvp in sourceOfferMap)
      {
        var offered = kvp.Value;
        var commitAmount = Mathf.Min(offered, toCommitSources);
        kvp.Key.CommitEnergyUsed(commitAmount);
        toCommitSources -= commitAmount;
        if (toCommitSources <= 0f) break;
      }
    }


    public class PowerNetworkData
    {

      // Power status of the entire network. This is only computed 1 time per request.
      public string Cached_NetworkDataString = "";
      public string NetworkConsumerPowerStatus = "";
      public float NetworkFuelCapacity;

      public float NetworkFuelSupply;
      public float NetworkPowerCapacity;

      public float NetworkPowerDemand;
      public float NetworkPowerSupply;
    }
  }
}