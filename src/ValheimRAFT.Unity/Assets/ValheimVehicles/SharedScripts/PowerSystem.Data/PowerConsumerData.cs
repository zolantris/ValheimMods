// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Shared.Constants;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public partial class PowerConsumerData : PowerSystemComputeData
  {
    private float _basePowerConsumption;
    private float powerNone = 0f;
    private float powerLow = 1f;
    private float powerMedium = 10f;
    private float powerHigh = 20f;
    private PowerIntensityLevel powerIntensityLevel = PowerIntensityLevel.Low;

    public PowerIntensityLevel PowerIntensityLevel => powerIntensityLevel;
    public bool IsDemanding = true;
    public bool _isActive = false;
    public override bool IsActive => _isActive;
    public bool IsPowerDenied => !IsActive && IsDemanding;

    // method meant for client when pressing activators to prevent activation
    public Func<float, bool> CanRunConsumerForDeltaTime = (_) => true;

    public PowerConsumerData() {}
    public void UpdatePowerConsumptionValues(float val)
    {
      powerHigh = val * 4;
      powerMedium = val * 2;
      powerLow = val;
      powerNone = 0f;
    }

    public float GetRequestedEnergy(float deltaTime)
    {
      if (!IsDemanding) return 0f;
      if (PowerIntensityLevel.None == powerIntensityLevel && !IsActive)
      {
        SetActive(true);
      }
      return GetWattsForLevel(powerIntensityLevel) * deltaTime;
    }

    // todo this might need to control SetActive/Running states.
    public void ApplyPower(float joules, float deltaTime)
    {
    }

    public void SetPowerMode(PowerIntensityLevel level)
    {
      if (powerIntensityLevel == level) return;
      MarkDirty(VehicleZdoVars.PowerSystem_Intensity_Level);
      powerIntensityLevel = level;
    }

    public float BasePowerConsumption
    {
      get => _basePowerConsumption;
      set => SetBasePowerConsumption(value);
    }

    public void SetBasePowerConsumption(float value)
    {
      _basePowerConsumption = value;
      UpdatePowerConsumptionValues(value);
      if (!Mathf.Approximately(_basePowerConsumption, value))
      {
        MarkDirty(VehicleZdoVars.PowerSystem_BasePowerConsumption);
      }
    }

    public void SetDemandState(bool val)
    {
      IsDemanding = val;
      MarkDirty(VehicleZdoVars.PowerSystem_IsDemanding);
    }

    private float GetWattsForLevel(PowerIntensityLevel level)
    {
      return level switch
      {
        PowerIntensityLevel.Low => powerLow,
        PowerIntensityLevel.Medium => powerMedium,
        PowerIntensityLevel.High => powerHigh,
        _ => powerNone
      };
    }
  }
}