using System;
using System.Diagnostics.CodeAnalysis;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.Shared.Constants;

// must be same namespace to override.
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

/// <summary>
/// All overrides for integrations of PowerSystemComputeData. This injects ZDO data into the loaders allowing for the data to be populated when calling Load()
/// </summary>
public abstract partial class PowerSystemComputeData
{
  public ZDO? zdo;

  public bool IsValid = false;

  public PowerSystemComputeData(ZDO zdo)
  {
    OnNetworkIdChange += HandleNetworkIdUpdate;
  }

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

  public void HandleNetworkIdUpdate(string val)
  {
    if (!zdo.IsOwner())
    {
      zdo.SetOwner(ZDOMan.GetSessionID());
    }
    ValheimExtensions.SetDelta(zdo, VehicleZdoVars.PowerSystem_NetworkId, val);
  }

  /// <summary>
  /// Should be run inside the validator.
  /// </summary>
  /// <param name="isPylon"></param>
  public void OnSharedConfigSync(bool isPylon = false)
  {
    _isActive = zdo.GetBool(VehicleZdoVars.PowerSystem_IsActive, true);
    NetworkId = zdo.GetString(VehicleZdoVars.PowerSystem_NetworkId, "");
    // config sync.
    ConnectionRange = isPylon ? PowerSystemConfig.PowerPylonRange.Value : PowerSystemConfig.PowerMechanismRange.Value;
  }

  /// <summary>
  /// Must be run last in Save method.
  /// </summary>
  public void OnSharedConfigSave()
  {
    foreach (var dirtyField in _dirtyFields)
    {
      switch (dirtyField)
      {
        case VehicleZdoVars.PowerSystem_IsActive:
          ValheimExtensions.SetDelta(zdo, VehicleZdoVars.PowerSystem_IsActive, IsActive);
          break;
        case VehicleZdoVars.PowerSystem_NetworkId:
          if (!IsTempNetworkId)
          {
            ValheimExtensions.SetDelta(zdo, VehicleZdoVars.PowerSystem_NetworkId, NetworkId);
          }
          break;
      }
    }

    // always clears. Must be run last otherwise it will clear other config checks.
    ClearDirty();
  }
}