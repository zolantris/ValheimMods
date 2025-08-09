// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle


using System;
using UnityEngine;
using ValheimVehicles.Shared.Constants;

#if !TEST
using Zolantris.Shared;
#endif

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  // ReSharper disable once PartialTypeWithSinglePart
  public partial class PowerConsumerData : PowerSystemComputeData
  {
    public static class PowerConsumerBaseValues
    {
      public static float LandVehicleEngine = 1f;
      public static float WaterVehicleEngine = 1f;
      public static float AirVehicleEngine = 1f;
      public static float Swivel = 0.1f;
    }

    private float _basePowerConsumption;
    private float powerNone = 0f;
    private float powerLow = 1f;
    private float powerMedium = 10f;
    private float powerHigh = 20f;
    private PowerIntensityLevel powerIntensityLevel = PowerIntensityLevel.Low;

    public PowerIntensityLevel PowerIntensityLevel => powerIntensityLevel;

    public bool IsDemanding = false; // a value set when the consumer is requesting for power
    // method meant for client when pressing activators to prevent activation
    public Func<float, bool> CanRunConsumerForDeltaTime = (_) => true;

    public PowerConsumerData() {}

    public static float GetVariant(int prefabHash)
    {
      if (prefabHash == PrefabNameHashes.Mechanism_Power_Consumer_Swivel)
      {
        return PowerConsumerBaseValues.Swivel;
      }

      if (prefabHash == PrefabNameHashes.Mechanism_Power_Consumer_LandVehicle)
      {
        return PowerConsumerBaseValues.LandVehicleEngine;
      }

      if (prefabHash == PrefabNameHashes.Mechanism_Power_Consumer_WaterVehicle)
      {
        return PowerConsumerBaseValues.WaterVehicleEngine;
      }

      // uninitialized. We do not want to provide a value.
      if (prefabHash == 0)
      {
        return 0f;
      }

      // unexpected
      LoggerProvider.LogError($"Unknown PowerConsumerBaseValue: for prefab hash <{prefabHash}>");
      return 1f;
    }

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

    public static PowerIntensityLevel GetPowerIntensityFromPrefab(int powerIntensity)
    {
      return GetPowerIntensityFromPrefab((PowerIntensityLevel)powerIntensity);
    }
    public static PowerIntensityLevel GetPowerIntensityFromPrefab(PowerIntensityLevel powerIntensity)
    {
      if (powerIntensity == PowerIntensityLevel.None) return PowerIntensityLevel.Low;
      return powerIntensity;
    }

    public float BasePowerConsumption => _basePowerConsumption;

    public void SetPowerIntensity(PowerIntensityLevel level)
    {
      if (powerIntensityLevel == level) return;
      MarkDirty(VehicleZdoVars.PowerSystem_Intensity_Level);
      powerIntensityLevel = level;
    }

    public void UpdateBasePowerConsumption()
    {
      var previous = _basePowerConsumption;
      _basePowerConsumption = GetVariant(PrefabHash);
      if (!Mathf.Approximately(_basePowerConsumption, previous))
      {
        UpdatePowerConsumptionValues(_basePowerConsumption);
        MarkDirty(VehicleZdoVars.PowerSystem_BasePowerConsumption);
      }
    }

    public void SetDemandState(bool val)
    {
      IsDemanding = val;
      MarkDirty(VehicleZdoVars.PowerSystem_IsDemanding);
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