// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.SharedScripts.PowerSystem;

public partial class PowerNetworkController
{
  private readonly List<IPowerSource> _sources = new();
  private readonly List<IPowerStorage> _storage = new();
  private readonly List<IPowerConsumer> _consumers = new();
  private readonly List<IPowerConduit> _conduits = new();

  public static float BatteryDemandPercentage = 0.75f;

  public void SimulateNetwork(List<IPowerNode> nodes)
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

    foreach (var conduit in _conduits)
    {
      if (conduit.IsDemanding)
        totalDemand += conduit.RequestPower(deltaTime);
    }

    // Add demand from storage up to BatteryDemandPercentage
    var storageDemand = 0f;
    foreach (var storage in _storage)
    {
      var targetCapacity = storage.Capacity * BatteryDemandPercentage;
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

    // 7. Lightning burst effect (visual only)
    if (lightningBurstCoroutine != null && !isDemanding)
    {
      StopCoroutine(lightningBurstCoroutine);
      lightningBurstCoroutine = null;
    }

    if (isDemanding)
    {
      if (lightningBurstCoroutine != null)
      {
        StopCoroutine(lightningBurstCoroutine);
        lightningBurstCoroutine = null;
      }
      lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
    }
  }
}