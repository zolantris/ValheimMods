// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public abstract class PowerDataBase
  {
    public string NetworkId;
    public int PrefabHash;
  }

  public class PowerSourceData : PowerDataBase
  {
    public float Fuel;
    public float MaxFuel;
    public float OutputRate;

    public float FuelConsumptionRate = 1f;
    public float FuelEnergyYield = 10f;
    public float FuelEfficiency = 10f;

    public float LastProducedEnergy;

    public bool CanProducePower(float deltaTime, float remainingDemand)
    {
      if (Fuel <= 0f || remainingDemand <= 0f)
        return false;

      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return maxEnergyFromFuel > 0f;
    }

    public float GetMaxPotentialOutput(float deltaTime)
    {
      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;
      return Mathf.Min(OutputRate * deltaTime, maxEnergyFromFuel);
    }

    public float ProducePower(float requestedEnergy)
    {
      var maxDeliverable = Fuel * FuelEnergyYield * FuelEfficiency;
      var actual = Mathf.Min(requestedEnergy, maxDeliverable);

      var requiredFuel = actual / (FuelEnergyYield * FuelEfficiency);
      Fuel = Mathf.Max(0f, Fuel - requiredFuel);

      LastProducedEnergy = actual;
      return actual;
    }

    public float EstimateFuelCost(float energy)
    {
      return energy / (FuelEnergyYield * FuelEfficiency);
    }

    public void SetFuel(float val)
    {
      Fuel = Mathf.Clamp(val, 0f, MaxFuel);
    }
  }

  public class PowerStorageData : PowerDataBase
  {
    public float StoredEnergy;
    public float MaxCapacity;

    public float EstimateAvailableEnergy()
    {
      return Mathf.Clamp(StoredEnergy, 0f, MaxCapacity);
    }

    public float DrainEnergy(float requested)
    {
      var used = Mathf.Min(requested, StoredEnergy);
      StoredEnergy -= used;
      return used;
    }

    public float AddEnergy(float amount)
    {
      var availableSpace = Mathf.Max(0f, MaxCapacity - StoredEnergy);
      var accepted = Mathf.Min(availableSpace, amount);
      StoredEnergy += accepted;
      return accepted;
    }

    public bool NeedsCharging()
    {
      return StoredEnergy < MaxCapacity;
    }

    public void SetStoredEnergy(float val)
    {
      StoredEnergy = Mathf.Clamp(val, 0f, MaxCapacity);
    }
  }

  public class PowerPylonData : PowerDataBase
  {
    public float Range;

    public PowerPylonData(string networkId, float range, int prefabHash)
    {
      NetworkId = networkId;
      Range = range;
      PrefabHash = prefabHash;
    }
  }

  public class PowerNetworkSimData
  {
    public readonly List<(PowerSourceData source, ZDO zdo)> Sources = new();
    public readonly List<(PowerStorageData storage, ZDO zdo)> Storages = new();
    public readonly List<(PowerConduitData conduit, ZDO zdo)> Conduits = new();
    public readonly List<(PowerPylonData pylon, ZDO zdo)> Pylons = new();
  }
}