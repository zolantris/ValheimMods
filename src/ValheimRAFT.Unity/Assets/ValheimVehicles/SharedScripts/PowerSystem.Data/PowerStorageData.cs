// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts.Modules;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  // Todo extend IPowerStorage
  public class PowerStorageData : PowerSystemComputeData
  {
    public static float EnergyCapacityDefault = 800f;
    public float _peekedDischargeAmount;
    public float Energy;
    public float EnergyCapacity = EnergyCapacityDefault;

    public bool IsValid { get; set; }

    public float EnergyCapacityRemaining => MathX.Clamp(EnergyCapacity - Energy, 0f, EnergyCapacity);
    public float EstimateAvailableEnergy()
    {
      return MathX.Clamp(Energy, 0f, EnergyCapacity);
    }

    // deprecated
    public float PeekDischarge(float amount)
    {
      _peekedDischargeAmount = MathX.Min(Energy, amount);
      return _peekedDischargeAmount;
    }


    public float PeekDischarge(float amount, float snapshotValue)
    {
      _peekedDischargeAmount = MathUtils.RoundToHundredth(MathX.Min(snapshotValue, amount));
      return _peekedDischargeAmount;
    }

    public float DrainEnergy(float requested)
    {
      var used = MathX.Min(requested, Energy);
      Energy -= used;
      return used;
    }

    public float AddEnergy(float amount)
    {
      var availableSpace = MathX.Max(0f, EnergyCapacity - Energy);
      var accepted = MathX.Min(availableSpace, amount);
      Energy += accepted;
      return accepted;
    }

    public void CommitDischarge(float amount)
    {
      var commit = MathUtils.RoundToHundredth(MathX.Min(_peekedDischargeAmount, amount));

      var localEnergy = Energy - commit;
      SetEnergy(localEnergy);

      _peekedDischargeAmount = 0f;
    }

    public bool NeedsCharging()
    {
      return Energy < EnergyCapacity;
    }

    public void SetCapacity(float val)
    {
      if (Mathf.Approximately(EnergyCapacity, val)) return;
      EnergyCapacity = val;

      if (EnergyCapacity > Energy)
      {
        Energy = Mathf.Max(Energy, EnergyCapacity);
        MarkDirty(VehicleZdoVars.PowerSystem_Energy);
      }

      MarkDirty(VehicleZdoVars.PowerSystem_EnergyCapacity);
    }

    public void SetEnergy(float val)
    {
      var nextEnergy = MathX.Clamp(val, 0f, EnergyCapacity);

      if (!Mathf.Approximately(nextEnergy, EnergyCapacity))
      {
        Energy = nextEnergy;
        MarkDirty(VehicleZdoVars.PowerSystem_Energy);
      }
    }
  }
}