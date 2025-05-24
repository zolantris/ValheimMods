// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.SharedScripts.Modules;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  /// <summary>
  /// Meant to be run on a single thread.
  ///
  /// Todo consider running these computes on background task thread.
  /// </summary>
  public static class PowerSystemSimulator
  {
    // re-usable variables.

    public static float GetTotalCapacityFromStorages(List<PowerStorageData> storages)
    {
      return storages.Sum(s => s.EnergyCapacity);
    }

    public class PowerSystemDisplayData
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

    /// <summary>
    /// Demand Conduits consume the system energy by charging player eitr-diffused energy.
    /// </summary>
    public static void RunDemandConduitUpdate(PowerConduitData conduit, float deltaTime, ref float totalSupply, ref float totalDemand)
    {
      var energyUsed = conduit.SimulateConduit(totalSupply, deltaTime);

      // removes supply adds demand
      totalSupply -= energyUsed;
      totalDemand += energyUsed;
    }

    /// <summary>
    /// Supply Conduits supply the system by draining player eitr energy.
    /// </summary>
    public static void RunSupplyConduitUpdate(PowerConduitData conduit, float deltaTime, ref float totalSupply, ref float totalDemand)
    {
      var energyUsed = conduit.SimulateConduit(totalDemand, deltaTime);

      // adds supply removes demand
      totalSupply += energyUsed;
      totalDemand -= energyUsed;
    }

    public static void RunConsumerUpdate(PowerConsumerData consumer, float deltaTime, ref float totalSupply, ref float totalDemand)
    {
      var energyRequired = consumer.GetRequestedEnergy(deltaTime);
      var canRun = totalSupply >= energyRequired;
      consumer.SetActive(canRun);
      if (!canRun)
      {
        return;
      }

      totalSupply -= energyRequired;
    }

    public static void RunPowerDischarge(PowerStorageData storage, float deltaTime, ref float totalSupply, ref float totalDemand)
    {

    }

    public static void SortStoragesByEnergy(List<PowerStorageData> storages)
    {
      storages.Sort((x, y) => x.Energy > y.Energy ? -1 : 1);
    }

    /// <summary>
    /// This allows for lossless fuel additions while the simulation is running.
    /// </summary>
    /// <param name="sources"></param>
    public static void ConsolidateAllPendingFuel(List<PowerSourceData> sources)
    {
      foreach (var x in sources)
      {
        x.ConsolidateFuel();
      }
    }

    public static void Simulate(PowerSimulationData SimulationData)
    {
      var deltaTime = SimulationData.DeltaTime;
      SortStoragesByEnergy(SimulationData.Storages);
      ConsolidateAllPendingFuel(SimulationData.Sources);

      // Step 1: Calculate Supply and Demand
      var consumerDemand = SimulationData.Consumers.Sum(c => c.GetRequestedEnergy(deltaTime));
      var conduitDemand = SimulationData.Conduits.Sum(c => c.EstimateTotalDemand(deltaTime));
      var storageDemand = SimulationData.Storages.Sum(s => s.EnergyCapacityRemaining);

      var conduitSupply = SimulationData.Conduits.Sum(c => c.EstimateTotalSupply(deltaTime));
      var storageSupply = SimulationData.Storages.Sum(s => s.Energy);

      var demandConduits = SimulationData.Conduits.Where(c => c.Mode == PowerConduitMode.Charge).ToList();
      var supplyConduits = SimulationData.Conduits.Where(c => c.Mode == PowerConduitMode.Drain).ToList();

      // maximum amount of energy that can be stored. Total supply must not exceed this.
      var totalEnergyCapacity = GetTotalCapacityFromStorages(SimulationData.Storages);

      // demand must be zero before bailing on energy generation
      var totalDemand = consumerDemand + conduitDemand + storageDemand;

      // supply must be at max storage and fulfill demand before bailing on energy generation
      var totalSupply = conduitSupply + storageSupply;



      // Step 2: Supply power to players if there is supply.
      foreach (var conduit in demandConduits)
      {
        if (totalSupply <= 0f) break;
        RunDemandConduitUpdate(conduit, deltaTime, ref totalSupply, ref totalDemand);
      }

      // step 2.5: Supply power from conduits if there is demand.
      if (totalDemand > totalSupply)
      {
        // simulate every conduit. This will fulfill the demand / supply contracts of these conduits.
        foreach (var conduit in supplyConduits)
        {
          RunSupplyConduitUpdate(conduit, deltaTime, ref totalSupply, ref totalDemand);
        }
      }

      // Step 4: Apply power to consumers
      foreach (var consumer in SimulationData.Consumers)
      {
        RunConsumerUpdate(consumer, deltaTime, ref totalSupply, ref totalDemand);
      }


      // generate power to fulfill demand and target maximum storage capacity. 
      var hasChargeableStorageCapacity = totalEnergyCapacity > storageSupply;

      if (hasChargeableStorageCapacity)
      {
        foreach (var source in SimulationData.Sources)
        {
          if (totalSupply >= totalEnergyCapacity) break;
          var offered = source.RequestAvailablePower(deltaTime, totalDemand, totalDemand > 0f);
          if (offered <= 0f) continue;

          source.CommitEnergyUsed(offered);
          totalSupply += offered;
        }
      }

      foreach (var storage in SimulationData.Storages)
      {
        var energyToUse = MathX.Min(totalSupply, storage.EnergyCapacity);
        storage.SetStoredEnergy(energyToUse);
        totalSupply -= energyToUse;
        totalSupply = MathX.Max(totalSupply, 0f);
      }

      SimulationData.Sources.ForEach(x => x.Save());
      SimulationData.Storages.ForEach(x => x.Save());
      SimulationData.Conduits.ForEach(x => x.Save());
      SimulationData.Consumers.ForEach(x => x.Save());
    }
  }
}