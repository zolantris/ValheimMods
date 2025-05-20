// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerConsumerComponent : PowerNodeComponentBase
  {

    [SerializeField] private PowerIntensityLevel intensityLevel = PowerIntensityLevel.Medium;
    [SerializeField] private bool isActive;

    [Header("Power Consumption Settings")]
    [SerializeField] private float _basePowerConsumption;
    // values that are updated whenever BasePowerConsumption mutates.
    private float powerNone = 0f;
    private float powerLow = 10f;
    private float powerMedium = 20f;
    private float powerHigh = 30f;

    public float BasePowerConsumption
    {
      get => _basePowerConsumption;
      set
      {
        _basePowerConsumption = value;
        UpdatePowerConsumptionValues(value);
      }
    }

    public bool IsDemanding;

    public override bool IsActive => isActive;

    // for PowerConsumers.
    public bool IsPowerDenied => !IsActive && IsDemanding;

    public event Action<float>? OnPowerSupplied;
    public event Action? OnPowerDenied;

    protected override void Awake()
    {
      base.Awake();

      if (canSelfRegisterToNetwork)
      {
        PowerNetworkController.RegisterPowerComponent(this); // or RegisterNode(this)
      }
      // syncs computed values so they are not evalutated per fixedupdate.
      UpdatePowerConsumptionValues(_basePowerConsumption);
    }

    protected virtual void OnDestroy()
    {
      if (canSelfRegisterToNetwork)
      {
        PowerNetworkController.UnregisterPowerComponent(this);
      }
    }

    public virtual void OnCollisionEnter(Collision other) {}
    public virtual void OnCollisionExit(Collision other) {}

    public void SetDemandState(bool val)
    {
      IsDemanding = val;
    }

    public void UpdatePowerConsumptionValues(float val)
    {
      powerHigh = val * 4;
      powerMedium = val * 2;
      powerLow = val;
      powerNone = 0f;
    }

    public float RequestedPower(float deltaTime)
    {
      if (!IsDemanding) return 0f;
      return GetWattsForLevel(intensityLevel) * deltaTime;
    }

    public void SetActive(bool value)
    {
      isActive = value;
    }
    public void SetConsumptionLevel(PowerIntensityLevel level)
    {
      intensityLevel = level;
    }

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

    // todo this needs to be optimized so that it calls only downwards and not attempting to upgrade when iterating through power consumption levels.
    public void ApplyPowerWithDowngrade(float grantedJoules, float deltaTime)
    {
      if (!isActive || grantedJoules <= 0f)
      {
        intensityLevel = PowerIntensityLevel.None;
        OnPowerDenied?.Invoke();
        return;
      }

      foreach (PowerIntensityLevel level in Enum.GetValues(typeof(PowerIntensityLevel)))
      {
        var required = GetWattsForLevel(level) * deltaTime;
        if (grantedJoules >= required)
        {
          intensityLevel = level;
          OnPowerSupplied?.Invoke(grantedJoules);
          return;
        }
      }

      intensityLevel = PowerIntensityLevel.None;
      OnPowerDenied?.Invoke();
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