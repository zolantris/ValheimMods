// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.ComponentModel;
using UnityEngine;
using ValheimVehicles.Interfaces;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerStorageComponent : PowerNodeComponentBase, IPowerStorage
  {
    [Description("Energy levels in Watts")]
    [SerializeField] public float baseEnergyCapacity = 800f;
    [SerializeField] private float maxEnergy = 800f;
    [SerializeField] private float storedEnergy;

    [Description("Visual representation of energy level")]
    [SerializeField] private Transform m_visualEnergyLevel;
    [SerializeField] public AnimatedMachineComponent powerRotator;
    [SerializeField] public Transform powerRotatorTransform;

    public Vector3 powerRotatorChargeDirection = Vector3.up;
    public Vector3 powerRotatorDischargeDirection = Vector3.down;

    // will half capacity 
    [SerializeField] public bool IsSource = false;

    private bool isActive = false;
    public override bool IsActive => isActive;
    public float ChargeLevel => storedEnergy;

    public float Energy => maxEnergy;
    public float CapacityRemaining => maxEnergy - storedEnergy;
    private float _peekedDischargeAmount = 0f;
    public bool IsCharging { get; set; }

    public void SetStoredEnergy(float val)
    {
      // pre-calculation to get Charging/Active State.
      var nextEnergyVal = Mathf.Clamp(val, 0f, maxEnergy);
      UpdatePowerStates(storedEnergy, nextEnergyVal);

      // main setter.
      storedEnergy = nextEnergyVal;

      // side effects
      UpdatePowerAnimations();
      UpdateChargeScale();
    }

    private float _lastCharge = 0f;
    private float _lastChargeTime = 0f;
    private const float chargeCheckInterval = 0.5f;

    public void UpdatePowerStates(float current, float next)
    {
      var shouldCheckIfStayingInPlace = _lastChargeTime + chargeCheckInterval < Time.time;
      if (Mathf.Approximately(_lastCharge, next) || Mathf.Approximately(next, 0f) || Mathf.Approximately(next, maxEnergy))
      {
        isActive = false;
      }
      else
      {
        isActive = true;
      }

      if (shouldCheckIfStayingInPlace)
      {
        _lastChargeTime = Time.time;
        _lastCharge = next;
      }

      IsCharging = next > current;
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

    public virtual void Start()
    {
      IsSource = GetComponent<PowerSourceComponent>() != null;
      UpdateCapacity();
    }

    public void UpdateCapacity()
    {
      SetCapacity(baseEnergyCapacity);
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

    public void CommitDischarge(float amount)
    {
      var commit = MathUtils.RoundToHundredth(Mathf.Min(_peekedDischargeAmount, amount));

      var localEnergy = storedEnergy - commit;
      SetStoredEnergy(localEnergy);

      _peekedDischargeAmount = 0f;
    }

    public float Charge(float amount)
    {
      var space = maxEnergy - storedEnergy;
      var toCharge = MathUtils.RoundToHundredth(Mathf.Min(space, amount));

      var localEnergy = storedEnergy + toCharge;
      SetStoredEnergy(localEnergy);

      return toCharge;
    }

    public float Discharge(float amount)
    {
      var toDischarge = MathUtils.RoundToHundredth(Mathf.Min(storedEnergy, amount));

      var localEnergy = storedEnergy - toDischarge;
      SetStoredEnergy(localEnergy);

      return toDischarge;
    }

    public void SetCapacity(float val)
    {
      baseEnergyCapacity = val;
      maxEnergy = IsSource ? Mathf.Round(val * 0.5f) : val;
    }
    public void SetActive(bool value)
    {
      isActive = value;
    }

    /// <summary>
    /// To be run in a network manager or directly in the setter as a mutation
    /// </summary>
    private void UpdateChargeScale()
    {
      if (!m_visualEnergyLevel || maxEnergy <= 0f) return;
      var percent = Mathf.Clamp01(storedEnergy / maxEnergy);
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