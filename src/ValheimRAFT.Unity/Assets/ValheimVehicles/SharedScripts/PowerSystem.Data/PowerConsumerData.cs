// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public partial class PowerConsumerData : PowerSystemComputeData
  {

    public static float OutputRateDefault = 10f;
    public static float MaxFuelDefault = 100f;
    public static float FuelEfficiencyDefault = 10f;
    public static float FuelConsumptionRateDefault = 1f;
    public static float FuelEnergyYieldDefault = 10f;
    private float _basePowerConsumption;
    private float powerNone = 0f;
    private float powerLow = 10f;
    private float powerMedium = 20f;
    private float powerHigh = 30f;
    private PowerIntensityLevel powerIntensityLevel = PowerIntensityLevel.Low;

    public PowerIntensityLevel PowerIntensityLevel => powerIntensityLevel;
    public bool IsDemanding;
    public bool _isActive = false;
    public override bool IsActive => _isActive;
    public bool IsPowerDenied => !IsActive && IsDemanding;
    public PowerConsumerData() {}
    public void UpdatePowerConsumptionValues(float val)
    {
      powerHigh = val * 4;
      powerMedium = val * 2;
      powerLow = val;
      powerNone = 0f;
    }

    public float GetRequestedEnergy(float deltaTime)
    {
      if (!IsDemanding) return 0f;
      return GetWattsForLevel(powerIntensityLevel) * deltaTime;
    }

    // todo this might need to control SetActive/Running states.
    public void ApplyPower(float joules, float deltaTime)
    {
    }

    public void SetPowerMode(PowerIntensityLevel level)
    {
      powerIntensityLevel = level;
    }

    public float BasePowerConsumption
    {
      get => _basePowerConsumption;
      set
      {
        _basePowerConsumption = value;
        UpdatePowerConsumptionValues(value);
      }
    }


    public void SetDemandState(bool val)
    {
      IsDemanding = val;
    }

    public void SetActive(bool value)
    {
      _isActive = value;
    }

    private float GetWattsForLevel(PowerIntensityLevel level)
    {
      return level switch
      {
        PowerIntensityLevel.Low => powerLow,
        PowerIntensityLevel.Medium => powerMedium,
        PowerIntensityLevel.High => powerHigh,
        _ => powerNone
      };
    }
  }
}