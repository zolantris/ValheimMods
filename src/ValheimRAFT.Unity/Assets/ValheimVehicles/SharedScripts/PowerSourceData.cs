// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
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
    public float FuelEfficiency = FuelEfficiencyDefault;

    public float LastProducedEnergy;

    public float _peekedDischargeAmount = 0f;

    public PowerSourceData() {}

    public bool CanProducePower(float deltaTime, float remainingDemand)
    {
      if (Fuel <= 0f || remainingDemand <= 0f)
        return false;

      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return maxEnergyFromFuel > 0f;
    }

    public float PeekDischarge(float amount)
    {
      if (_peekedDischargeAmount > 0f)
      {
        _peekedDischargeAmount = 0f;
      }

      _peekedDischargeAmount = MathUtils.RoundToHundredth(Mathf.Min(storedEnergy, amount));
      return _peekedDischargeAmount;
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