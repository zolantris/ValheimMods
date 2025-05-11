// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerSourceComponent : PowerNodeComponentBase
  {
    [SerializeField] private float maxOutputWatts = 100f;
    [SerializeField] private float fuelCapacity = 100f;
    [SerializeField] private float fuelConsumptionRate = 1f; // per second
    [SerializeField] private bool isRunning;

    [SerializeField] private float currentFuel;

    public override bool IsActive => isRunning && currentFuel > 0f;

    private void FixedUpdate()
    {
      if (!isRunning || currentFuel <= 0f) return;

      float fuelUsed = fuelConsumptionRate * Time.fixedDeltaTime;
      currentFuel = Mathf.Max(0f, currentFuel - fuelUsed);
    }

    public float RequestAvailablePower(float deltaTime)
    {
      return IsActive ? maxOutputWatts * deltaTime : 0f;
    }

    public float GetFuelLevel() => currentFuel;
    public float GetFuelCapacity() => fuelCapacity;

    public float Refuel(float amount)
    {
      var space = fuelCapacity - currentFuel;
      var toAdd = Mathf.Min(space, amount);
      currentFuel += toAdd;
      return toAdd;
    }

    public void SetRunning(bool state) => isRunning = state;
  }
}