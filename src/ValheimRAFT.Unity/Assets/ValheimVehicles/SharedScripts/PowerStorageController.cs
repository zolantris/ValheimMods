// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerStorageComponent : PowerNodeComponentBase
  {
    [SerializeField] public float capacity = 500f;
    [SerializeField] public float storedPower;
    [SerializeField] private Transform energyLevel;
    [SerializeField] public AnimatedMachineComponent powerRotator;
    [SerializeField] public Transform powerRotatorTransform;

    public bool IsCharging;

    public Vector3 powerRotatorChargeDirection = Vector3.up;

    public Vector3 powerRotatorDischargeDirection = Vector3.down;

    public override bool IsActive => true;

    protected override void Awake()
    {
      base.Awake();
      powerRotatorTransform = transform.Find("meshes/power_rotator");
      var movingObjectTester = powerRotatorTransform.GetComponent<AnimatedMachineComponent>();
      if (!movingObjectTester)
      {
        movingObjectTester = powerRotatorTransform.gameObject.AddComponent<AnimatedMachineComponent>();
      }
      movingObjectTester.HasRotation = true;
      powerRotator = movingObjectTester;
    }

    public void Update()
    {
      if (Input.GetKeyDown(KeyCode.T))
      {
        energyLevel.localScale = new Vector3(1f, 0.1f, 1f);
      }
    }

    private void FixedUpdate()
    {
      if (!energyLevel)
        energyLevel = transform.Find("energy_level");
      UpdatePowerAnimations();
      if (!energyLevel || capacity <= 0f) return;

      UpdateChargeScale();
    }

    public float CapacityRemaining => capacity - storedPower;


    public float Charge(float amount)
    {
      var space = capacity - storedPower;
      var toCharge = Mathf.Min(space, amount);
      storedPower += toCharge;
      IsCharging = true;
      return toCharge;
    }

    public void UpdateChargeScale()
    {
      var percent = Mathf.Clamp01(storedPower / capacity);
      var scale = energyLevel.localScale;
      scale.y = percent;
      energyLevel.localScale = scale;
    }

    public void UpdatePowerAnimations()
    {
      if (storedPower <= 0f && powerRotator.enabled)
      {
        powerRotator.enabled = false;
      }
      if (storedPower > 0f && !powerRotator.enabled)
      {
        powerRotator.enabled = true;
      }

      if (powerRotator.enabled)
      {
        powerRotator.RotationalVector = IsCharging ? powerRotatorChargeDirection : powerRotatorDischargeDirection;
      }
    }

    public float Discharge(float amount)
    {
      var toDischarge = Mathf.Min(storedPower, amount);
      storedPower -= toDischarge;
      IsCharging = false;
      return toDischarge;
    }
  }
}