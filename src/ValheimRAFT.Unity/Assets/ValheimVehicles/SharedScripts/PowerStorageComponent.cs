// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.ComponentModel;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerStorageComponent : PowerNodeComponentBase
  {
    [Description("Visual representation of energy level")]
    [SerializeField] private Transform m_visualEnergyLevel;
    [SerializeField] public AnimatedMachineComponent powerRotator;
    [SerializeField] public Transform powerRotatorTransform;

    public Vector3 powerRotatorChargeDirection = Vector3.up;
    public Vector3 powerRotatorDischargeDirection = Vector3.down;

    [Description("Configurable Data")]
    [SerializeField] private PowerStorageData m_data = new();

    public override bool IsActive => Data.IsActive;
    public float EnergyCapacity => Data.EnergyCapacity;

    public float Energy => Data.Energy;
    public float CapacityRemaining => Data.EnergyCapacityRemaining;

    // local compute value.
    public bool IsCharging { get; set; }
    private float _previousEnergy = 0f;
    private float _lastChargeTime = 0f;
    private const float chargeCheckInterval = 0.5f;

    public PowerStorageData Data => m_data;

    private bool HasAttachedUpdateListener = false;

    public void SetData(PowerStorageData data)
    {
      if (m_data != data)
      {
        if (HasAttachedUpdateListener)
        {
          m_data.OnLoad -= OnDataUpdate;
        }
        HasAttachedUpdateListener = true;
        data.OnLoad += OnDataUpdate;
      }
      m_data = data;
    }

    public void OnEnable()
    {
      if (!HasAttachedUpdateListener && m_data != null && m_data.OnLoad != null)
      {
        m_data.OnLoad -= OnDataUpdate;
        HasAttachedUpdateListener = true;
      }
    }

    public void OnDisable()
    {
      if (HasAttachedUpdateListener)
      {
        m_data.OnLoad -= OnDataUpdate;
        HasAttachedUpdateListener = false;
      }
    }

    public void OnDataUpdate()
    {
      UpdatePowerVisualStates(_previousEnergy, Data.Energy);
    }

    public void UpdatePowerVisualStates(float current, float next)
    {
      var shouldCheckIfStayingInPlace = _lastChargeTime + chargeCheckInterval < Time.time;
      if (shouldCheckIfStayingInPlace)
      {
        _lastChargeTime = Time.time;
        _previousEnergy = next;
        IsCharging = next > current;
      }

      UpdatePowerAnimations();
      UpdateChargeScale();
    }

    protected override void Awake()
    {
      LoggerProvider.LogInfo($"[PowerStorageComponent] Awake on {name} ({gameObject.GetInstanceID()})");

      base.Awake();
      m_visualEnergyLevel = transform.Find("energy_level");
      powerRotatorTransform = transform.Find("meshes/power_rotator");

      if (!powerRotatorTransform)
      {
        throw new Exception("PowerStorageComponent: PowerRotatorTransform not found");
      }

      var rotator = powerRotatorTransform.GetComponent<AnimatedMachineComponent>();
      if (!rotator)
      {
        rotator = powerRotatorTransform.gameObject.AddComponent<AnimatedMachineComponent>();
      }

      rotator.HasRotation = true;
      powerRotator = rotator;
    }

    public void Start()
    {
      UpdatePowerVisualStates(_previousEnergy, Data.Energy);
    }

    public void FixedUpdate()
    {
      UpdatePowerVisualStates(_previousEnergy, Data.Energy);
    }


    /// <summary>
    /// To be run in a network manager or directly in the setter as a mutation
    /// </summary>
    private void UpdateChargeScale()
    {
      if (!Data.IsValid) return;
      if (!m_visualEnergyLevel || EnergyCapacity <= 0f) return;
      var percent = Mathf.Clamp01(Energy / EnergyCapacity);
      var scale = m_visualEnergyLevel.localScale;
      scale.y = percent;
      m_visualEnergyLevel.localScale = scale;
    }

    /// <summary>
    /// To be run in a network manager or directly in the setter as a mutation
    /// </summary>
    public void UpdatePowerAnimations()
    {
      // disable when at 0 or at capacity
      if (!IsActive && powerRotator.enabled)
      {
        powerRotator.enabled = false;
        return;
      }

      if (IsActive && !powerRotator.enabled)
      {
        powerRotator.enabled = true;
      }

      if (powerRotator.enabled)
      {
        powerRotator.RotationalVector = IsCharging ? powerRotatorChargeDirection : powerRotatorDischargeDirection;
      }
    }
  }
}