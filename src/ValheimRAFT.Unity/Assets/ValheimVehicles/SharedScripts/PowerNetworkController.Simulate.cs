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

    public void SimulateNetwork(List<IPowerNode> nodes, string networkId)
    {
      var deltaTime = Time.fixedDeltaTime;

      _sources.Clear();
      _storage.Clear();
      _consumers.Clear();
      _conduits.Clear();

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

      // 1. Calculate total demand
      var totalDemand = 0f;

      foreach (var consumer in _consumers)
        totalDemand += consumer.RequestedPower(deltaTime);

      // this will be used to calculate the demand from non-conduits. Conduits do not show up on GUI.
      var totalConsumerDemand = totalDemand;

      foreach (var conduit in _conduits)
      {
        if (conduit.IsDemanding)
          totalDemand += conduit.RequestPower(deltaTime);
      }

      // Add demand from storage up to BatteryDemandPercentage
      var storageDemand = 0f;
      foreach (var storage in _storage)
      {
        var targetCapacity = storage.Capacity;
        if (storage.ChargeLevel < targetCapacity)
          storageDemand += Mathf.Clamp(targetCapacity - storage.ChargeLevel, 0f, storage.Capacity);
      }

      totalDemand += storageDemand;

      var isDemanding = totalDemand > 0f;

      // 2. Try supplying from storage/conduits first
      var suppliedFromStorage = 0f;
      var remainingDemand = totalDemand;

      foreach (var conduit in _conduits)
      {
        if (remainingDemand <= 0f) break;
        var supplied = conduit.SupplyPower(deltaTime);
        suppliedFromStorage += supplied;
        remainingDemand -= supplied;
      }

      foreach (var storage in _storage)
      {
        if (remainingDemand <= 0f) break;
        var supplied = storage.Discharge(remainingDemand);
        suppliedFromStorage += supplied;
        remainingDemand -= supplied;
      }

      // 3. Offer power from sources (even if no consumers, as long as storage isn't full)
      var offeredFromSources = 0f;
      var sourceOfferMap = new Dictionary<IPowerSource, float>();

      var hasChargeableStorage = false;
      foreach (var storage in _storage)
      {
        if (storage.ChargeLevel < storage.Capacity)
        {
          hasChargeableStorage = true;
          break;
        }
      }

      if (remainingDemand > 0f || hasChargeableStorage)
      {
        foreach (var source in _sources)
        {
          var offered = source.RequestAvailablePower(deltaTime, offeredFromSources, totalDemand, isDemanding);
          if (offered > 0f)
          {
            sourceOfferMap[source] = offered;
            offeredFromSources += offered;

            if (offeredFromSources + suppliedFromStorage >= totalDemand && !hasChargeableStorage)
              break;
          }
        }
      }

      var totalAvailable = offeredFromSources + suppliedFromStorage;

      // 4. Apply power to consumers
      foreach (var consumer in _consumers)
      {
        var needed = consumer.RequestedPower(deltaTime);
        var granted = Mathf.Min(needed, totalAvailable);
        totalAvailable -= granted;

        consumer.SetActive(granted > 0f);
        consumer.ApplyPower(granted, deltaTime);
      }

      // 5. Feed energy to storage (charge up to full 100%)
      var usedForStorage = 0f;
      foreach (var storage in _storage)
      {
        if (totalAvailable <= 0f) break;

        var remainingCapacity = Mathf.Clamp(storage.Capacity - storage.ChargeLevel, 0f, storage.Capacity);
        if (remainingCapacity <= 0f) continue;

        var accepted = storage.Charge(totalAvailable);
        usedForStorage += accepted;
        totalAvailable -= accepted;
      }

      // 6. Final fuel burn for used source power
      var totalUsed = offeredFromSources + suppliedFromStorage - totalAvailable;
      var usedFromSources = Mathf.Clamp(totalUsed - suppliedFromStorage, 0f, offeredFromSources);
      var toCommit = usedFromSources;

      foreach (var kvp in sourceOfferMap)
      {
        var offered = kvp.Value;
        var committed = Mathf.Min(offered, toCommit);
        kvp.Key.CommitEnergyUsed(committed);
        toCommit -= committed;

        if (toCommit <= 0f) break;
      }

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

      // 7. Lightning burst effect (visual only)
      if (lightningBurstCoroutine != null && !isDemanding)
      {
        StopCoroutine(lightningBurstCoroutine);
        lightningBurstCoroutine = null;
      }

      if (isDemanding)
      {
        // if (lightningBurstCoroutine != null)
        // {
        //   StopCoroutine(lightningBurstCoroutine);
        //   lightningBurstCoroutine = null;
        // }
        if (lightningBurstCoroutine != null) return;
        lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
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