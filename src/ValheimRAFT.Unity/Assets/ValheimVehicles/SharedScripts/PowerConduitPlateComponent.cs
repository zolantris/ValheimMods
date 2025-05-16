// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConduitPlateComponent : PowerNodeComponentBase
  {
    public enum EnergyPlateMode
    {
      Charging,
      Draining
    }

    public EnergyPlateMode mode;
    public static float chargeRate = 1f;
    public static float drainRate = 10f;
    public static float eitrToFuelRatio = 40f;
    public float fuelStored = 0f;
    public float maxFuelCapacity = 100f;

    public Func<float> GetPlayerEitr = () => 0f;
    public Action<float> AddPlayerEitr = _ => {};
    public Action<float> SubtractPlayerEitr = _ => {};

    private bool m_hasPlayerInRange;
    public bool HasPlayerInRange => m_hasPlayerInRange;

    public bool IsDemanding => mode == EnergyPlateMode.Charging && HasPlayerInRange;

    public float RequestPower(float deltaTime)
    {
      if (mode != EnergyPlateMode.Charging || !HasPlayerInRange) return 0f;
      return chargeRate * deltaTime;
    }

    public void SetHasPlayerInRange(bool val)
    {
      m_hasPlayerInRange = val;
    }

    public float SupplyPower(float deltaTime)
    {
      if (mode != EnergyPlateMode.Draining || !HasPlayerInRange) return 0f;

      var availableEitr = GetPlayerEitr();
      if (availableEitr <= 0f || fuelStored >= maxFuelCapacity) return 0f;

      var eitrToDrain = drainRate * deltaTime;
      var actualDrain = Mathf.Min(eitrToDrain, availableEitr);
      SubtractPlayerEitr(actualDrain);

      var fuelGained = actualDrain / eitrToFuelRatio;
      fuelStored = Mathf.Min(fuelStored + fuelGained, maxFuelCapacity);

      return fuelGained * chargeRate; // Optional: convert to power units
    }
    public override bool IsActive
    {
      get;
    } = true;
  }
}