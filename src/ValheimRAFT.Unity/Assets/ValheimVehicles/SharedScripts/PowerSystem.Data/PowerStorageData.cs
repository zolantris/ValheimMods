// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.SharedScripts.Modules;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  // Todo extend IPowerStorage
  public partial class PowerStorageData : PowerSystemComputeData
  {
    public static float EnergyCapacityDefault = 800f;
    public float Energy;
    public float EnergyCapacity = EnergyCapacityDefault;
    public float _peekedDischargeAmount = 0f;

    public float EnergyCapacityRemaining => MathX.Clamp(EnergyCapacity - Energy, 0f, EnergyCapacity);

    public PowerStorageData() {}
    public float EstimateAvailableEnergy()
    {
      return MathX.Clamp(Energy, 0f, EnergyCapacity);
    }

    // deprecated
    public float PeekDischarge(float amount)
    {
      _peekedDischargeAmount = MathUtils.RoundToHundredth(MathX.Min(Energy, amount));
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
      SetStoredEnergy(localEnergy);

      _peekedDischargeAmount = 0f;
    }

    public bool NeedsCharging()
    {
      return Energy < EnergyCapacity;
    }

    public void SetStoredEnergy(float val)
    {
      Energy = MathX.Clamp(val, 0f, EnergyCapacity);
    }
    public override bool IsActive
    {
      get;
    }
  }
}