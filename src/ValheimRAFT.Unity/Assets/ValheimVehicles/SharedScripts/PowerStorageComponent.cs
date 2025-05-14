// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.ComponentModel;
using UnityEngine;
using ValheimVehicles.Interfaces;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerStorageComponent : PowerNodeComponentBase, IPowerStorage
  {
    [Description("Energy levels in Watts")]
    [SerializeField] public float energyCapacity = 800f;
    [SerializeField] public float storedEnergy;

    [Description("Visual representation of energy level")]
    [SerializeField] private Transform m_visualEnergyLevel;
    [SerializeField] public AnimatedMachineComponent powerRotator;
    [SerializeField] public Transform powerRotatorTransform;

    public Vector3 powerRotatorChargeDirection = Vector3.up;
    public Vector3 powerRotatorDischargeDirection = Vector3.down;

    public override bool IsActive => true;
    public float ChargeLevel => storedEnergy;

    public float Capacity => energyCapacity;
    public float CapacityRemaining => energyCapacity - storedEnergy;

    protected override void Awake()
    {
      base.Awake();
      m_visualEnergyLevel = transform.Find("energy_level");
      powerRotatorTransform = transform.Find("meshes/power_rotator");

      if (!powerRotatorTransform)
      {
        throw new System.Exception("PowerStorageComponent: PowerRotatorTransform not found");
      }

      var rotator = powerRotatorTransform.GetComponent<AnimatedMachineComponent>();
      if (!rotator)
      {
        rotator = powerRotatorTransform.gameObject.AddComponent<AnimatedMachineComponent>();
      }

      rotator.HasRotation = true;
      powerRotator = rotator;
    }

    protected void FixedUpdate()
    {
      UpdatePowerAnimations();
      UpdateChargeScale();
    }

    public float Charge(float amount)
    {
      var space = energyCapacity - storedEnergy;
      var toCharge = Mathf.Min(space, amount);
      storedEnergy += toCharge;
      IsCharging = true;
      return toCharge;
    }

    public float Discharge(float amount)
    {
      var toDischarge = Mathf.Min(storedEnergy, amount);
      storedEnergy -= toDischarge;
      IsCharging = false;
      return toDischarge;
    }

    public bool IsCharging { get; set; }

    private void UpdateChargeScale()
    {
      if (!m_visualEnergyLevel || energyCapacity <= 0f) return;
      var percent = Mathf.Clamp01(storedEnergy / energyCapacity);
      var scale = m_visualEnergyLevel.localScale;
      scale.y = percent;
      m_visualEnergyLevel.localScale = scale;
    }

    public void UpdatePowerAnimations()
    {
      // disable when at 0 or at capacity
      var isZeroOrAtCapacity = storedEnergy <= 0f || storedEnergy >= energyCapacity;
      if (powerRotator.enabled && isZeroOrAtCapacity)
      {
        powerRotator.enabled = false;
        return;
      }

      if (!isZeroOrAtCapacity && storedEnergy > 0f && !powerRotator.enabled)
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