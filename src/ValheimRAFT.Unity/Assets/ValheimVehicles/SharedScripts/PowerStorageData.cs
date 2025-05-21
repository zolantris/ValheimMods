// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  // Todo extend IPowerStorage
  public partial class PowerStorageData : PowerSystemComputeData
  {
    public static float MaxCapacityDefault = 800f;
    public float StoredEnergy;
    public float MaxCapacity = MaxCapacityDefault;
    public float _peekedDischargeAmount = 0f;

    public PowerStorageData() {}
    public float EstimateAvailableEnergy()
    {
      return Mathf.Clamp(StoredEnergy, 0f, MaxCapacity);
    }

    public float PeekDischarge(float amount)
    {
      _peekedDischargeAmount = MathUtils.RoundToHundredth(Mathf.Min(StoredEnergy, amount));
      return _peekedDischargeAmount;
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

    public void CommitDischarge()
    {
      var commit = MathUtils.RoundToHundredth(Mathf.Min(_peekedDischargeAmount, amount));

      var localEnergy = StoredEnergy - commit;
      SetStoredEnergy(localEnergy);

      _peekedDischargeAmount = 0f;
    }

    public bool NeedsCharging()
    {
      return StoredEnergy < MaxCapacity;
    }

    public void SetStoredEnergy(float val)
    {
      StoredEnergy = Mathf.Clamp(val, 0f, MaxCapacity);
    }
    public override bool IsActive
    {
      get;
    }
  }
}