// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerStorageComponent : PowerNodeComponentBase
  {
    [SerializeField] private float capacity = 500f;
    [SerializeField] private float storedPower;
    [SerializeField] private Transform energyLevel;

    public override bool IsActive => true;

    public void Update()
    {
      if (Input.GetKeyDown(KeyCode.T))
      {
        energyLevel.localScale = new Vector3(1f, 0.1f, 1f);
        Debug.Log("Manual scale test: " + energyLevel.localScale);
      }
    }

    private void FixedUpdate()
    {
      if (!energyLevel)
        energyLevel = transform.Find("energy_level");
      if (!energyLevel || capacity <= 0f) return;

      float percent = Mathf.Clamp01(storedPower / capacity);
      var scale = energyLevel.localScale;
      scale.y = percent;
      energyLevel.localScale = scale;
    }

    public float Charge(float amount)
    {
      var space = capacity - storedPower;
      var toCharge = Mathf.Min(space, amount);
      storedPower += toCharge;
      return toCharge;
    }

    public float Discharge(float amount)
    {
      var toDischarge = Mathf.Min(storedPower, amount);
      storedPower -= toDischarge;
      return toDischarge;
    }
  }
}