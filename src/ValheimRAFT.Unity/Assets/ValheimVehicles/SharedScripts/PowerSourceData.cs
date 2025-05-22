// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public enum FuelType
  {
    Coal,
    SurtlingCore,
    Eitr
  }

  public partial class PowerSourceData : PowerSystemComputeData
  {
    public static float OutputRateDefault = 10f;
    public static float MaxFuelDefault = 100f;
    public static float FuelEfficiencyDefault = 10f;
    public static float FuelConsumptionRateDefault = 1f;
    public static float FuelEnergyYieldDefault = 10f;

    public override bool IsActive => true;

    public float Fuel = 0;
    public float MaxFuel = MaxFuelDefault;
    public float OutputRate = OutputRateDefault;

    public float FuelConsumptionRate = FuelConsumptionRateDefault;
    public float FuelEnergyYield = FuelEnergyYieldDefault;
    public float FuelEfficiency = GetFuelEfficiency(FuelType.Eitr);
    public FuelType fuelType = FuelType.Eitr;

    public static float BaseFuelEfficiency = 1f;
    public static float CoalFuelEfficiency = 1f;
    public static float SurtlingCoreFuelEfficiency = 3f;
    public static float EitrFuelEfficiency = 12f;
    public bool isRunning = false;
    private float maxOutputWatts = 100f;
    private static float MinimumFuelToBurn = 0.1f; // or whatever you prefer

    public float LastProducedEnergy;

    public PowerSourceData() {}

    /// <summary>
    /// For refreshing properties when config changes.
    /// </summary>
    public void OnPropertiesUpdate()
    {
      FuelEfficiency = GetFuelEfficiency(fuelType);
      FuelConsumptionRate = Mathf.Max(0f, FuelConsumptionRate);
      OutputRate = Mathf.Max(0f, OutputRateDefault);
      MaxFuel = MaxFuelDefault;
    }

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
      if (Fuel <= 0f) return 0f;

      // clamp to never exceed fuel-amount
      var maxFuelUsable = Mathf.Min(FuelConsumptionRate * deltaTime, Fuel);

      // max amount of energy produce per amount burned.
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return Mathf.Min(OutputRate * deltaTime, maxEnergyFromFuel);
    }

    /// <summary>
    /// This assumes we already ran a request for fuel.
    /// </summary>
    /// <param name="energyUsed"></param>
    public void CommitEnergyUsed(float energyUsed)
    {
      var requiredFuel = energyUsed / (FuelEnergyYield * FuelEfficiency);
      Fuel = Mathf.Max(0f, Fuel - requiredFuel);
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
    public void SetRunning(bool state)
    {
      isRunning = state;
    }
    private float _lastProducedEnergy = 0f;

    public static float GetFuelEfficiency(FuelType val)
    {
      return val switch
      {
        FuelType.Coal => BaseFuelEfficiency * CoalFuelEfficiency,
        FuelType.SurtlingCore => BaseFuelEfficiency * SurtlingCoreFuelEfficiency,
        FuelType.Eitr => BaseFuelEfficiency * EitrFuelEfficiency,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public float GetOfferEstimate(float deltaTime, float supplied, float totalDemand, bool isDemanding, float snapshotFuel)
    {
      if (!IsActive || !isDemanding) return 0f;

      var remainingDemand = totalDemand - supplied;
      if (remainingDemand <= 0f || snapshotFuel <= 0f)
        return 0f;

      var energyPerFuel = FuelEnergyYield * FuelEfficiency;
      if (energyPerFuel <= 0f) return 0f;

      var maxFuelBurn = Mathf.Min(FuelConsumptionRate * deltaTime, snapshotFuel);
      var maxEnergy = maxFuelBurn * energyPerFuel;
      return Mathf.Min(OutputRate * deltaTime, Mathf.Min(remainingDemand, maxEnergy));
    }


    public float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding)
    {
      if (!IsActive)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      var remainingDemand = totalDemand - supplyFromSources;
      if (!isDemanding || remainingDemand <= 0f)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      if (!isRunning)
        SetRunning(true);

      var maxEnergy = maxOutputWatts * deltaTime;
      var energyToProduce = Mathf.Min(remainingDemand, maxEnergy);

      // Limit based on fuel consumption rate
      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      // Cap energy to available fuel and consumption rate
      energyToProduce = Mathf.Min(energyToProduce, maxEnergyFromFuel);

      var requiredFuel = energyToProduce / (FuelEnergyYield * FuelEfficiency);
      if (Fuel < requiredFuel)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      _lastProducedEnergy = energyToProduce;
      return energyToProduce;
    }

    public void AddFuel(float amount)
    {
      var space = MaxFuel - Fuel;
      var toAdd = Mathf.Min(space, amount);
      Fuel += toAdd;
    }

    public void SetFuel(float val)
    {
      Fuel = Mathf.Clamp(val, 0f, MaxFuel);
    }
  }
}