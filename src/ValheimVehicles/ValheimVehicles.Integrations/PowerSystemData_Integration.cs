using System;
using System.Diagnostics.CodeAnalysis;
using ValheimVehicles.Config;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Structs;


// must be same namespace to override.
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

/// <summary>
/// All overrides for integrations of PowerSystemComputeData. This injects ZDO data into the loaders allowing for the data to be populated when calling Load()
/// </summary>
public abstract partial class PowerSystemComputeData
{
  public ZDO? zdo;

  public bool IsValid = false;

  /// <summary>
  /// Guard for validation so we do not continue a bad sync without evaluating some values again.
  /// </summary>
  /// short-circuits until an error occurs
  /// <returns></returns>
  public bool TryValidate([NotNullWhen(true)] out ZDO? validZdo)
  {
    validZdo = zdo;
    if (IsValid) return true;
    if (!IsValid)
    {
      IsValid = zdo != null && zdo.IsValid();
    }
    return IsValid;
  }

  public void WithIsValidCheck(Action<ZDO> action)
  {
    if (TryValidate(out var validZdo))
    {
      try
      {
        action.Invoke(validZdo);
      }
      catch (Exception e)
      {
#if DEBUG
        LoggerProvider.LogDebug($"Error when calling {action.Method.Name} on {validZdo.m_uid} \n {e.Message} \n {e.StackTrace}");
#endif
        IsValid = false;
      }
    }
  }

  /// <summary>
  /// Should be run inside the validator.
  /// </summary>
  /// <param name="isPylon"></param>
  public void OnSharedConfigSync(bool isPylon = false)
  {
    NetworkId = zdo!.GetString(VehicleZdoVars.Power_NetworkId, "");
    // config sync.
    Range = isPylon ? PowerSystemConfig.PowerPylonRange.Value : PowerSystemConfig.PowerMechanismRange.Value;
  }
}

/// ZDO integration
public partial class PowerPylonData : IPowerComputeZdoSync
{
  public PowerPylonData(ZDO zdo)
  {
    this.zdo = zdo;
    Range = PowerSystemConfig.PowerPylonRange.Value;
    PrefabHash = zdo.m_prefab;

    OnLoad += OnLoadZDOSync;
    Load();
  }

  public void OnLoadZDOSync()
  {
    // shared config
    OnSharedConfigSync(true);
  }
}

public partial class PowerConduitData : IPowerComputeZdoSync
{
  public PowerConduitData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;
    Mode = GetConduitVariant(PrefabHash);
    Load();
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();
    });
  }
}

public partial class PowerConsumerData : IPowerComputeZdoSync
{

  public PowerConsumerData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnLoad += OnLoadZDOSync;
    Load();
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();
      IsDemanding = validZdo.GetBool(VehicleZdoVars.Power_IsDemanding, true);
      var intensityInt = validZdo.GetInt(VehicleZdoVars.Power_Intensity_Level, 0);
      powerIntensityLevel = (PowerIntensityLevel)intensityInt;
    });
  }
}

public partial class PowerSourceData : IPowerComputeZdoSync
{
  public PowerSourceData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnLoad += OnLoadZDOSync;
    Load();
  }
  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();

      Fuel = validZdo.GetFloat(VehicleZdoVars.Power_StoredFuel);
      MaxFuel = validZdo.GetFloat(VehicleZdoVars.Power_StoredFuelCapacity, MaxFuelDefault);
      OutputRate = validZdo.GetFloat(VehicleZdoVars.Power_FuelOutputRate, OutputRateDefault);
    });
  }
}

/// <summary>
/// Integration level class.
/// </summary>
public partial class PowerStorageData : IPowerComputeZdoSync
{
  public PowerStorageData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnLoad += OnLoadZDOSync;
    Load();
  }

  // This is meant for integration
  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();
      NetworkId = validZdo.GetString(VehicleZdoVars.Power_NetworkId, "");
      StoredEnergy = validZdo.GetFloat(VehicleZdoVars.Power_StoredEnergy, 0);
      MaxCapacity = validZdo.GetFloat(VehicleZdoVars.Power_StoredEnergyCapacity, MaxCapacityDefault);
    });
  }
}