// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{

  public abstract class PowerDataBase
  {
    public ZDO zdo;
    public const string NetworkIdUnassigned = "UNASSIGNED";
    public string NetworkId = NetworkIdUnassigned;
    public int PrefabHash;
    public abstract void Load();
  }

  public class PowerConsumerData : PowerDataBase
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
    public bool IsActive => _isActive;
    public bool IsPowerDenied => !IsActive && IsDemanding;

    public PowerConsumerData() {}

    public PowerConsumerData(ZDO zdo)
    {
      this.zdo = zdo;
      PrefabHash = zdo.m_prefab;
      Load();
    }

    public override sealed void Load()
    {
      NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId, "");
      IsDemanding = zdo.GetBool(VehicleZdoVars.Power_IsDemanding, true);

      var intensityInt = zdo.GetInt(VehicleZdoVars.Power_Intensity_Level, 0);
      powerIntensityLevel = (PowerIntensityLevel)intensityInt;
    }

    public void UpdatePowerConsumptionValues(float val)
    {
      powerHigh = val * 4;
      powerMedium = val * 2;
      powerLow = val;
      powerNone = 0f;
    }

    public float RequestedPower(float deltaTime)
    {
      if (!IsDemanding) return 0f;
      return GetWattsForLevel(powerIntensityLevel) * deltaTime;
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

  public class PowerSourceData : PowerDataBase
  {
    public static float OutputRateDefault = 10f;
    public static float MaxFuelDefault = 100f;
    public static float FuelEfficiencyDefault = 10f;
    public static float FuelConsumptionRateDefault = 1f;
    public static float FuelEnergyYieldDefault = 10f;

    public float Fuel = 0;
    public float MaxFuel = MaxFuelDefault;
    public float OutputRate = OutputRateDefault;

    public float FuelConsumptionRate = FuelConsumptionRateDefault;
    public float FuelEnergyYield = FuelEnergyYieldDefault;
    public float FuelEfficiency = FuelEfficiencyDefault;

    public float LastProducedEnergy;

    public PowerSourceData() {}
    public PowerSourceData(ZDO zdo)
    {
      this.zdo = zdo;
      PrefabHash = zdo.m_prefab;
      Load();
    }

    public override sealed void Load()
    {
      NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId, "");
      Fuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuel);
      MaxFuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuelCapacity, MaxFuelDefault);
      OutputRate = zdo.GetFloat(VehicleZdoVars.Power_FuelOutputRate, OutputRateDefault);
    }

    public bool CanProducePower(float deltaTime, float remainingDemand)
    {
      if (Fuel <= 0f || remainingDemand <= 0f)
        return false;

      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;

      return maxEnergyFromFuel > 0f;
    }

    public float GetMaxPotentialOutput(float deltaTime)
    {
      var maxFuelUsable = FuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * FuelEnergyYield * FuelEfficiency;
      return Mathf.Min(OutputRate * deltaTime, maxEnergyFromFuel);
    }

    public float ProducePower(float requestedEnergy)
    {
      var maxDeliverable = Fuel * FuelEnergyYield * FuelEfficiency;
      var actual = Mathf.Min(requestedEnergy, maxDeliverable);

      var requiredFuel = actual / (FuelEnergyYield * FuelEfficiency);
      Fuel = Mathf.Max(0f, Fuel - requiredFuel);

      LastProducedEnergy = actual;
      return actual;
    }

    public float EstimateFuelCost(float energy)
    {
      return energy / (FuelEnergyYield * FuelEfficiency);
    }

    public void SetFuel(float val)
    {
      Fuel = Mathf.Clamp(val, 0f, MaxFuel);
    }
  }

  public class PowerStorageData : PowerDataBase
  {
    public static float MaxCapacityDefault = 800f;
    public float StoredEnergy;
    public float MaxCapacity = MaxCapacityDefault;

    public PowerStorageData() {}
    public PowerStorageData(ZDO zdo)
    {
      this.zdo = zdo;
      PrefabHash = zdo.m_prefab;
      Load();
    }

    /// <summary>
    /// Meant to be called on all methods.
    /// </summary>
    public override sealed void Load()
    {
      NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId, "");
      StoredEnergy = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergy, 0);
      MaxCapacity = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergyCapacity, MaxCapacityDefault);
    }

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

  public class PowerPylonData : PowerDataBase
  {
    public float Range;

    public PowerPylonData() {}
    public PowerPylonData(ZDO zdo)
    {
      this.zdo = zdo;
      Range = PowerSystemConfig.PowerPylonRange.Value;
      PrefabHash = zdo.m_prefab;
      Load();
    }

    public override sealed void Load()
    {
      NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId, "");
      Range = PowerSystemConfig.PowerPylonRange.Value;
    }
  }

  public class PowerNetworkSimData
  {
    public readonly List<(PowerSourceData source, ZDO zdo)> Sources = new();
    public readonly List<(PowerStorageData storage, ZDO zdo)> Storages = new();
    public readonly List<(PowerConsumerData consumer, ZDO zdo)> Consumers = new();
    public readonly List<(PowerConduitData conduit, ZDO zdo)> Conduits = new();
    public readonly List<(PowerPylonData pylon, ZDO zdo)> Pylons = new();
  }
}