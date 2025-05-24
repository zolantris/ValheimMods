// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.Modules;

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
    public static float FuelCapacityDefault = 100f;
    public static float FuelEfficiencyDefault = 1f;
    public static float FuelConsumptionRateDefault = 1f;
    public static float FuelEnergyYieldDefault = 10f;
    private static float MaxOutputEnergy = 100f;

    // efficiency variants
    public static float CoalFuelEfficiency = 1f;
    public static float SurtlingCoreFuelEfficiency = 3f;
    public static float EitrFuelEfficiency = 12f;

    public override bool IsActive => true;

    // when the player adds this it will not be added until next simulation is called, meaning the added fuel is conserved.
    public float PendingFuel = 0f;

    public float Fuel = 0;
    public float FuelCapacity = FuelCapacityDefault;
    public float OutputRate = OutputRateDefault;

    public float FuelConsumptionRate = FuelConsumptionRateDefault;
    public float FuelEnergyYield = FuelEnergyYieldDefault;
    public float FuelEfficiency = 1f;
    public FuelType fuelType = FuelType.Eitr;


    public float BaseFuelEfficiency = FuelEfficiencyDefault;
    public bool isRunning = false;

    public float LastProducedEnergy;

    public PowerSourceData()
    {
      OnPropertiesUpdate();
    }

    /// <summary>
    /// For refreshing properties when config changes.
    /// </summary>
    public void OnPropertiesUpdate()
    {
      FuelEfficiency = GetFuelEfficiency(fuelType);
      FuelConsumptionRate = MathX.Max(0f, FuelConsumptionRate);
      OutputRate = MathX.Max(0f, OutputRateDefault);
      FuelCapacity = FuelCapacityDefault;
    }

    public bool CanProducePower(float deltaTime, float remainingDemand)
    {
      if (Fuel <= 0f || remainingDemand <= 0f)
        return false;

      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return maxEnergyFromFuel > 0f;
    }

    public void ConsolidateFuel()
    {
      if (PendingFuel <= 0f) return;
      Fuel += MathX.Clamp(PendingFuel, 0f, FuelCapacity - Fuel);
      PendingFuel = 0f;
    }

    public float GetMaxPotentialOutput(float deltaTime)
    {
      if (Fuel <= 0f) return 0f;

      // clamp to never exceed fuel-amount
      var maxFuelUsable = MathX.Min(FuelConsumptionRate * deltaTime, Fuel);

      // max amount of energy produce per amount burned.
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return MathX.Min(OutputRate * deltaTime, maxEnergyFromFuel);
    }

    /// <summary>
    /// This assumes we already ran a request for fuel.
    /// </summary>
    /// <param name="energyUsed"></param>
    public void CommitEnergyUsed(float energyUsed)
    {
      ConsolidateFuel();

      var requiredFuel = energyUsed / (FuelEnergyYield * FuelEfficiency);
      Fuel = MathX.Max(0f, Fuel - requiredFuel);
      if (Fuel <= 0.0001f)
      {
        Fuel = 0f;
      }
    }

    public float ProducePower(float requestedEnergy)
    {
      var maxDeliverable = Fuel * FuelEnergyYield * FuelEfficiency;
      var actual = MathX.Min(requestedEnergy, maxDeliverable);

      var requiredFuel = actual / (FuelEnergyYield * FuelEfficiency);
      Fuel = MathX.Max(0f, Fuel - requiredFuel);

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

    public float GetFuelEfficiency(FuelType val)
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

      var maxFuelBurn = MathX.Min(FuelConsumptionRate * deltaTime, snapshotFuel);
      var maxEnergy = maxFuelBurn * energyPerFuel;
      return MathX.Min(OutputRate * deltaTime, MathX.Min(remainingDemand, maxEnergy));
    }


    public float RequestAvailablePower(float deltaTime, float remainingDemand, bool isDemanding)
    {
      if (!IsActive)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      if (!isDemanding || remainingDemand <= 0f || Fuel <= 0.0001f)
      {
        Fuel = 0f;
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      if (!isRunning)
        SetRunning(true);

      var maxEnergy = MaxOutputEnergy * deltaTime;
      var energyToProduce = MathX.Min(remainingDemand, maxEnergy);

      // Limit based on fuel consumption rate
      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      // Cap energy to available fuel and consumption rate
      energyToProduce = MathX.Min(energyToProduce, maxEnergyFromFuel);

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
      var space = FuelCapacity - Fuel;
      var toAdd = MathX.Min(space, amount);
      PendingFuel += toAdd;
    }

    public void SetFuel(float val)
    {
      Fuel = MathX.Clamp(val, 0f, FuelCapacity);
    }
  }
}