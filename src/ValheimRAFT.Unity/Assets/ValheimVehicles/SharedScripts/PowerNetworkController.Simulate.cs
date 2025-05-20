// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

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
      if (networkId == null)
      {
        LoggerProvider.LogInfoDebounced("NetworkId is null");
        _data = null;
        return false;
      }

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
      powerNodeNetworks.Clear();
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


    public virtual void Host_SimulateNetwork(string networkId) {}

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