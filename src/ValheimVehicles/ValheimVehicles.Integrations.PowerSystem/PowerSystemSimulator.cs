// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public static class PowerSystemSimulator
  {
    public static void Simulate(PowerSimulationData SimulationData)
    {
      var deltaTime = SimulationData.DeltaTime;

      // Step 1: Calculate total consumer and conduit demand
      var totalDemand = SimulationData.Consumers.Sum(c => c.GetRequestedEnergy(deltaTime)) +
                        SimulationData.Conduits.Sum(c => c.EstimateTotalDemand(deltaTime));
      var originalTotalDemand = totalDemand;

      // Step 1.5: Calculate additional demand from storages
      var storageDemand = SimulationData.Storages.Sum(s => Mathf.Clamp(s.EnergyCapacity - s.Energy, 0f, s.EnergyCapacity));
      totalDemand += storageDemand;

      // Step 2: Peek supply from conduits
      var suppliedFromConduits = 0f;

      foreach (var conduit in SimulationData.Conduits)
      {
        if (totalDemand <= 0f) break;
        var energyUsed = conduit.SimulateConduit(totalDemand, deltaTime);
        totalDemand -= energyUsed;
      }

      // Step 2.5: Peek discharge from storage
      var storageDischargeMap = new List<(PowerStorageData, float)>();
      var suppliedFromStorage = 0f;
      var remainingDemand = totalDemand;

      foreach (var storage in SimulationData.Storages)
      {
        if (remainingDemand <= 0f) break;

        var peekedAmount = storage.PeekDischarge(remainingDemand);
        if (peekedAmount > 0f)
        {
          storageDischargeMap.Add((storage, peekedAmount));
          suppliedFromStorage += peekedAmount;
          remainingDemand -= peekedAmount;
        }
      }

      // Step 3: Peek supply from sources
      var sourceOfferMap = new List<(PowerSourceData, float)>();
      var offeredFromSources = 0f;
      var hasChargeableStorage = SimulationData.Storages.Any(s => s.NeedsCharging());

      if (remainingDemand > 0f || hasChargeableStorage)
      {
        foreach (var source in SimulationData.Sources)
        {
          var offered = source.RequestAvailablePower(deltaTime, offeredFromSources, totalDemand, totalDemand > 0f);
          if (offered <= 0f) continue;

          sourceOfferMap.Add((source, offered));
          offeredFromSources += offered;

          if (offeredFromSources + suppliedFromStorage + suppliedFromConduits >= totalDemand && !hasChargeableStorage)
            break;
        }
      }

      var totalAvailable = offeredFromSources + suppliedFromStorage + suppliedFromConduits;

      // Step 4: Apply power to consumers
      foreach (var consumer in SimulationData.Consumers)
      {
        var needed = consumer.GetRequestedEnergy(deltaTime);
        if (needed > totalAvailable)
          break;
        totalAvailable -= needed;
      }

      // Step 4.5: Commit discharge from storage
      var totalUsedPower = offeredFromSources + suppliedFromStorage + suppliedFromConduits - totalAvailable;
      var usedFromDischarge = Mathf.Clamp(totalUsedPower - offeredFromSources - suppliedFromConduits, 0f, suppliedFromStorage);

      foreach (var storageDischargeOffer in storageDischargeMap)
      {
        var (storageData, offered) = storageDischargeOffer;

        var commitAmount = Mathf.Min(offered, usedFromDischarge);
        storageData.CommitDischarge(commitAmount);
        usedFromDischarge -= commitAmount;
        if (usedFromDischarge <= 0f) break;
      }

      // Avoid recharging from own discharge
      totalAvailable -= suppliedFromStorage;
      totalAvailable = Mathf.Max(0f, totalAvailable);

      var storageDemandAfterConduitRecharge = 0f;

      // Step 5: Recharge storages with surplus
      foreach (var storage in SimulationData.Storages)
      {
        if (totalAvailable <= 0f) break;
        if (storage.EnergyCapacityRemaining <= 0f) continue;

        var accepted = storage.AddEnergy(totalAvailable);
        totalAvailable -= accepted;
        storageDemandAfterConduitRecharge += storage.EnergyCapacityRemaining;
      }

      // Step 6: Commit fuel burn from sources
      var usedFromSources = Mathf.Clamp(storageDemandAfterConduitRecharge + totalUsedPower - suppliedFromStorage - suppliedFromConduits, 0f, offeredFromSources);

      // this will be used to send power into the batteries. It is generated
      var powerGeneratedFromSources = 0f;

      foreach (var sourceOffer in sourceOfferMap)
      {
        var (sourceData, offered) = sourceOffer;
        var commitAmount = Mathf.Min(offered, usedFromSources);
        sourceData.CommitEnergyUsed(commitAmount);

#if TEST
        TestContext.Progress.WriteLine($"SourceOffered {offered}");
#endif

        usedFromSources -= commitAmount;
        powerGeneratedFromSources += commitAmount;
        if (usedFromSources <= 0f) break;
      }



      // Step 7: Recharge storages with fuel burnt.
      foreach (var storage in SimulationData.Storages)
      {
        if (powerGeneratedFromSources <= 0f) break;
        if (storage.EnergyCapacityRemaining <= 0f) continue;

        var accepted = storage.AddEnergy(totalAvailable);
        usedFromSources -= accepted;
      }
    }
  }
}