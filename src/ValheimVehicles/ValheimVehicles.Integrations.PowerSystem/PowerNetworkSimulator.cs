// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public static class PowerNetworkSimulator
  {
    public static void Simulate(PowerNetworkSimData simData)
    {
      if (simData == null || simData.DeltaTime <= 0f) return;

      var deltaTime = simData.DeltaTime;

      // Snapshot original values to ensure consistency during simulation
      var storageSnapshot = simData.Storages.ToDictionary(s => s.storage, s => s.storage.StoredEnergy);
      var sourceSnapshot = simData.Sources.ToDictionary(s => s.source, s => s.source.Fuel);

      var consumerDemand = ComputeConsumerDemand(simData, deltaTime);
      var conduitDemand = ComputeConduitDemand(simData);
      var storageDemand = ComputeStorageDemand(simData);

      var totalDemand = consumerDemand + conduitDemand + storageDemand;
      var isDemanding = totalDemand > 0f;

      var storageDischarge = SimulatePeekStorageDischarge(simData, totalDemand, storageSnapshot, out var storageDischargeList);
      var remainingDemand = totalDemand - storageDischarge;

      var sourceOffer = SimulateSourceOffer(simData, remainingDemand, isDemanding, sourceSnapshot, out var sourceOfferList);
      var totalAvailable = storageDischarge + sourceOffer;

      SimulateConduitEitrRecharge(simData, ref totalAvailable);
      SimulateStorageRecharge(simData, ref totalAvailable);
      SimulateStorageDischargeCommit(storageDischargeList, totalAvailable, sourceOffer);
      SimulateSourceBurn(sourceOfferList, totalAvailable);

      PersistSimState(simData, storageSnapshot, sourceSnapshot);
    }

    private static float ComputeConsumerDemand(PowerNetworkSimData simData, float deltaTime)
    {
      var total = 0f;
      foreach (var (consumer, _) in simData.Consumers)
      {
        total += consumer.RequestedPower(deltaTime);
      }
      return total;
    }

    private static float ComputeConduitDemand(PowerNetworkSimData simData)
    {
      var total = 0f;
      foreach (var (conduit, _) in simData.Conduits)
      {
        conduit.ResolvePlayersFromIds();
        total += PowerConduitData.GetAverageEitr(conduit.Players);
      }
      return total;
    }

    private static float ComputeStorageDemand(PowerNetworkSimData simData)
    {
      var total = 0f;
      foreach (var (storage, _) in simData.Storages)
      {
        total += Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
      }
      return total;
    }

    private static float SimulatePeekStorageDischarge(
      PowerNetworkSimData simData,
      float totalDemand,
      Dictionary<PowerStorageData, float> snapshot,
      out List<(PowerStorageData storage, float amount)> dischargeList)
    {
      dischargeList = new List<(PowerStorageData storage, float amount)>();
      var discharge = 0f;
      var remaining = totalDemand;

      foreach (var (storage, _) in simData.Storages)
      {
        if (remaining <= 0f) break;
        var peek = storage.PeekDischarge(remaining, snapshot[storage]);
        if (peek > 0f)
        {
          dischargeList.Add((storage, peek));
          discharge += peek;
          remaining -= peek;
        }
      }
      return discharge;
    }

    private static float SimulateSourceOffer(
      PowerNetworkSimData simData,
      float remainingDemand,
      bool isDemanding,
      Dictionary<PowerSourceData, float> snapshot,
      out List<(PowerSourceData source, float amount)> offerList)
    {
      offerList = new List<(PowerSourceData source, float amount)>();
      var total = 0f;
      foreach (var (source, _) in simData.Sources)
      {
        var offer = source.GetOfferEstimate(simData.DeltaTime, total, remainingDemand, isDemanding, snapshot[source]);
        if (offer > 0f)
        {
          offerList.Add((source, offer));
          total += offer;
        }
      }
      return total;
    }

    private static void SimulateConduitEitrRecharge(PowerNetworkSimData simData, ref float available)
    {
      foreach (var (conduit, zdo) in simData.Conduits)
      {
        if (available <= 0f || conduit.Players.Count == 0) continue;

        var perPlayer = available / conduit.Players.Count;
        var used = 0f;

        foreach (var player in conduit.Players)
        {
          if (player.m_eitr < player.m_maxEitr - 0.1f)
          {
            PowerConduitPlateComponentIntegration.Request_AddEitr(player, perPlayer);
            used += perPlayer;
          }
        }

        available -= used;
      }
    }

    private static void SimulateStorageRecharge(PowerNetworkSimData simData, ref float available)
    {
      foreach (var (storage, _) in simData.Storages)
      {
        if (available <= 0f) break;
        storage.SetStoredEnergy(available);
        available -= storage.StoredEnergy;
      }
    }

    private static void SimulateStorageDischargeCommit(List<(PowerStorageData storage, float amount)> dischargeList, float available, float sourceOffer)
    {
      var usedFromStorage = Mathf.Clamp(available - sourceOffer, 0f, available);
      foreach (var (storage, amount) in dischargeList)
      {
        if (usedFromStorage <= 0f) break;
        storage.CommitDischarge(amount);
        usedFromStorage -= amount;
      }
    }

    private static void SimulateSourceBurn(List<(PowerSourceData source, float amount)> offerList, float totalAvailable)
    {
      foreach (var (source, amount) in offerList)
      {
        if (totalAvailable <= 0f) break;

        var actualToProduce = Mathf.Min(amount, totalAvailable);
        var produced = source.ProducePower(actualToProduce); // Burns fuel
        totalAvailable -= produced;
      }
    }

    private static void PersistSimState(PowerNetworkSimData simData, Dictionary<PowerStorageData, float> storageSnapshot, Dictionary<PowerSourceData, float> sourceSnapshot)
    {
      foreach (var (source, _) in simData.Sources)
      {
        if (source.zdo != null && !Mathf.Approximately(sourceSnapshot[source], source.Fuel))
        {
          source.zdo.Set(VehicleZdoVars.Power_StoredFuel, source.Fuel);
        }
      }

      foreach (var (storage, _) in simData.Storages)
      {
        if (storage.zdo != null && !Mathf.Approximately(storageSnapshot[storage], storage.StoredEnergy))
        {
          storage.zdo.Set(VehicleZdoVars.Power_StoredEnergy, storage.StoredEnergy);
        }
      }
    }
  }
}