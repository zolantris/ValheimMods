// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Shared.Constants;
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
    private float _lastProducedEnergy = 0f;

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
    public bool IsRunning = false;

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
      Fuel = MathX.Clamp(Fuel + PendingFuel, 0f, FuelCapacity);
      PendingFuel = 0f;
      MarkDirty(VehicleZdoVars.PowerSystem_Fuel);
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

    public static bool IsNearlyZero(float value, float epsilon = 0.01f)
    {
      return Mathf.Abs(value) <= epsilon;
    }

    /// <summary>
    /// This assumes we already ran a request for fuel.
    /// </summary>
    /// <param name="energyUsed"></param>
    public void CommitEnergyUsed(float energyUsed)
    {
      ConsolidateFuel();

      var requiredFuel = energyUsed / (FuelEnergyYield * FuelEfficiency);
      var nextFuel = MathX.Max(0f, Fuel - MathX.Max(0, requiredFuel));

      if (!Mathf.Approximately(Fuel, nextFuel))
        MarkDirty(VehicleZdoVars.PowerSystem_Fuel);

      Fuel = nextFuel;

      if (IsNearlyZero(Fuel))
      {
        Fuel = 0f;
        MarkDirty(VehicleZdoVars.PowerSystem_Fuel);
      }
    }

#if DEBUG
    public float ProducePower(float requestedEnergy)
    {
      var maxDeliverable = Fuel * FuelEnergyYield * FuelEfficiency;
      var actual = MathX.Min(requestedEnergy, maxDeliverable);

      var requiredFuel = actual / (FuelEnergyYield * FuelEfficiency);
      Fuel = MathX.Max(0f, Fuel - requiredFuel);

      _lastProducedEnergy = actual;
      return actual;
    }

    public float EstimateFuelCost(float energy)
    {
      return energy / (FuelEnergyYield * FuelEfficiency);
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
#endif
    public void SetRunning(bool state)
    {
      IsRunning = state;
      MarkDirty(VehicleZdoVars.PowerSystem_IsRunning);
    }

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


    public float RequestAvailablePower(float deltaTime, float remainingDemand, bool isDemanding)
    {
      // if (!IsActive)
      // {
      //   SetRunning(false);
      //   _lastProducedEnergy = 0f;
      //   return 0f;
      // }

      var isVeryLowFuel = Fuel <= 0.0001f;

      if (!isDemanding || remainingDemand <= 0f || isVeryLowFuel)
      {
        if (isVeryLowFuel)
        {
          SetFuel(0f);
        }
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      if (!IsRunning)
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
        // Not enough fuel for full energyToProduce.
        // Instead, use up all remaining fuel:
        var possibleEnergy = Fuel * FuelEnergyYield * FuelEfficiency;

        // Only produce up to what's demanded and allowed by system
        possibleEnergy = MathX.Min(possibleEnergy, energyToProduce, remainingDemand);

        _lastProducedEnergy = possibleEnergy;
        return possibleEnergy;
      }

      _lastProducedEnergy = energyToProduce;
      return energyToProduce;
    }

    public void AddFuel(float amount)
    {
      var before = Fuel;

      var space = FuelCapacity - Fuel;
      var toAdd = MathX.Min(space, amount);
      PendingFuel += toAdd;

      if (!Mathf.Approximately(before, before + PendingFuel))
      {
        MarkDirty(VehicleZdoVars.PowerSystem_Fuel);
      }
    }

    public void SetFuel(float val)
    {
      if (Mathf.Approximately(val, Fuel)) return;
      Fuel = MathX.Clamp(val, 0f, FuelCapacity);
      MarkDirty(VehicleZdoVars.PowerSystem_Fuel);
    }
  }
}