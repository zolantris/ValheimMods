// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConsumerComponent : PowerNodeComponentBase
  {
    [SerializeField] private PowerConsumptionLevel consumptionLevel = PowerConsumptionLevel.Medium;
    [SerializeField] private bool isActive;
    public float BasePowerConsumption = 10f;
    public bool IsDemanding;

    public override bool IsActive => isActive;

    public event Action<float>? OnPowerSupplied;
    public event Action? OnPowerDenied;

    public void SetDemandState(bool val)
    {
      IsDemanding = val;
    }

    public float RequestedPower(float deltaTime)
    {
      if (!isActive) return 0f;
      return GetWattsForLevel(consumptionLevel) * deltaTime;
    }

    public void SetActive(bool value) => isActive = value;
    public void SetConsumptionLevel(PowerConsumptionLevel level) => consumptionLevel = level;

    public void ApplyPower(float grantedJoules, float deltaTime)
    {
      var required = RequestedPower(deltaTime);
      if (grantedJoules >= required)
      {
        OnPowerSupplied?.Invoke(grantedJoules);
      }
      else
      {
        OnPowerDenied?.Invoke();
      }
    }

    public void ApplyPowerWithDowngrade(float grantedJoules, float deltaTime)
    {
      if (!isActive || grantedJoules <= 0f)
      {
        consumptionLevel = PowerConsumptionLevel.None;
        OnPowerDenied?.Invoke();
        return;
      }

      foreach (PowerConsumptionLevel level in Enum.GetValues(typeof(PowerConsumptionLevel)))
      {
        if (level > consumptionLevel) continue;

        var required = GetWattsForLevel(level) * deltaTime;
        if (grantedJoules >= required)
        {
          consumptionLevel = level;
          OnPowerSupplied?.Invoke(grantedJoules);
          return;
        }
      }

      consumptionLevel = PowerConsumptionLevel.None;
      OnPowerDenied?.Invoke();
    }

    private float GetWattsForLevel(PowerConsumptionLevel level)
    {
      return level switch
      {
        PowerConsumptionLevel.Low => BasePowerConsumption,
        PowerConsumptionLevel.Medium => BasePowerConsumption * 2,
        PowerConsumptionLevel.High => BasePowerConsumption * 4,
        _ => 0f
      };
    }
  }
}
