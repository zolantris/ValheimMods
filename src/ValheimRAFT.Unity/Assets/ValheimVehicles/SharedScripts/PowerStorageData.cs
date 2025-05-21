// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public partial class PowerStorageData : PowerSystemComputeData, IPowerStorage
  {
    public static float MaxCapacityDefault = 800f;
    public float StoredEnergy;
    public float MaxCapacity = MaxCapacityDefault;

    public PowerStorageData() {}
    public float EstimateAvailableEnergy()
    {
      return Mathf.Clamp(StoredEnergy, 0f, MaxCapacity);
    }

    public float DrainEnergy(float requested)
    {
      var used = Mathf.Min(requested, StoredEnergy);
      StoredEnergy -= used;
      return used;
    }

    public float AddEnergy(float amount)
    {
      var availableSpace = Mathf.Max(0f, MaxCapacity - StoredEnergy);
      var accepted = Mathf.Min(availableSpace, amount);
      StoredEnergy += accepted;
      return accepted;
    }

    public bool NeedsCharging()
    {
      return StoredEnergy < MaxCapacity;
    }

    public void SetStoredEnergy(float val)
    {
      StoredEnergy = Mathf.Clamp(val, 0f, MaxCapacity);
    }
  }
}