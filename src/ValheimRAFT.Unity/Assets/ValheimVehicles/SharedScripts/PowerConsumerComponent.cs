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
    public enum ConsumerType
    {
      Engine
    }

    [SerializeField] private PowerConsumptionLevel consumptionLevel = PowerConsumptionLevel.Medium;
    [SerializeField] private bool isActive;

    [Header("Power Consumption Settings")]
    [SerializeField] private float _basePowerConsumption;
    // values that are updated whenever BasePowerConsumption mutates.
    private float powerNone = 0f;
    private float powerLow = 10f;
    private float powerMedium = 20f;
    private float powerHigh = 30f;

    public ConsumerType m_consumerType = ConsumerType.Engine;

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
      return GetWattsForLevel(consumptionLevel) * deltaTime;
    }

    public void SetActive(bool value)
    {
      isActive = value;
    }
    public void SetConsumptionLevel(PowerConsumptionLevel level)
    {
      consumptionLevel = level;
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
        consumptionLevel = PowerConsumptionLevel.None;
        OnPowerDenied?.Invoke();
        return;
      }

      foreach (PowerConsumptionLevel level in Enum.GetValues(typeof(PowerConsumptionLevel)))
      {
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
        PowerConsumptionLevel.Low => powerLow,
        PowerConsumptionLevel.Medium => powerMedium,
        PowerConsumptionLevel.High => powerHigh,
        _ => powerNone
      };
    }
  }
}