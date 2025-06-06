using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.BepInExConfig;

/// <summary>
/// A config syncing component for per-vehicle configs.
/// - This is not a BepInEx config component, there will be no BepInEx fields used.
/// </summary>
public class VehicleCustomConfig : ISerializableConfig<VehicleCustomConfig, IVehicleConfig>, IVehicleConfig
{
  // Constants for ZDO keys
  internal const string Key_Version = "vehicle_version";
  internal const string Key_TreadDistance = "vehicle_treadDistance";
  internal const string Key_TreadLength = "vehicle_treadLength";
  internal const string Key_TreadHeight = "vehicle_treadHeight";
  internal const string Key_TreadScaleX = "vehicle_treadScaleX";
  internal const string Key_HasCustomFloatationHeight = "vehicle_hasCustomFloatationHeight";
  internal const string Key_CustomFloatationHeight = "vehicle_customFloatationHeight";
  internal const string Key_CenterOfMassOffset = "vehicle_centerOfMassOffset";

  // todo integrate these keys.
  // unused keys.
  internal const string Key_AllowFlight = "vehicle_allowFlight";
  internal const string Key_AllowBallast = "vehicle_allowBallast";

  // todo support custom variant
  // private const string Key_VehicleVariant = "vehicle_variant";

  // Backing fields
  private string _version = ValheimRAFT_API.GetPluginVersion();

#if !UNITY_EDITOR
  private float _treadDistance = PrefabConfig.VehicleLandMaxTreadWidth.Value;
#else
  private float _treadDistance = 18f;
#endif

  private float _treadHeight = 0f;
#if !UNITY_EDITOR
  private float _treadLength = PrefabConfig.VehicleLandMaxTreadLength.Value;
#else
  private float _treadLength = 18f;
#endif

#if !UNITY_EDITOR
  private float _treadScaleX = PrefabConfig.ExperimentalTreadScaleX.Value;
#else
  private float _treadScaleX = 1f
#endif

  private bool _hasCustomFloatationHeight = false;
  private float _customFloatationHeight = 0f;
#if !UNITY_EDITOR
  private float _centerOfMassOffset = PhysicsConfig.VehicleCenterOfMassOffset.Value;
#else
  private float _centerOfMassOffset = 0f;
#endif
  // todo support custom variant
  // private float _vehicleVariant;


  public int GetStableHashCode()
  {
    unchecked
    {
      var hash = 17;
      hash = hash * 31 + Version.GetHashCode();
      hash = hash * 31 + TreadDistance.GetHashCode();
      hash = hash * 31 + TreadHeight.GetHashCode();
      hash = hash * 31 + TreadScaleX.GetHashCode();
      hash = hash * 31 + TreadLength.GetHashCode();
      hash = hash * 31 + HasCustomFloatationHeight.GetHashCode();
      hash = hash * 31 + CustomFloatationHeight.GetHashCode();
      hash = hash * 31 + CenterOfMassOffset.GetHashCode();
      return hash;
    }
  }

  // Properties
  public string Version
  {
    get => _version;
    set => _version = value;
  }

  public float TreadDistance
  {
    get => _treadDistance;
    set => _treadDistance = Mathf.Clamp(value, -5f, 20f);
  }

  public float TreadLength
  {
    get => _treadLength;
    set => _treadLength = Mathf.Clamp(value, 3f, 50f);
  }

  public float TreadHeight
  {
    get => _treadHeight;
    set => _treadHeight = Mathf.Clamp(value, 0.1f, 10f);
  }

  public float TreadScaleX
  {
    get => _treadScaleX;
    set => _treadScaleX = Mathf.Clamp(value, 1f, 10f);
  }

  public bool HasCustomFloatationHeight
  {
    get => _hasCustomFloatationHeight;
    set => _hasCustomFloatationHeight = value;
  }

  public float CustomFloatationHeight
  {
    get => _customFloatationHeight;
    set => _customFloatationHeight = Mathf.Clamp(value, -50f, 50f);
  }

  public float CenterOfMassOffset
  {
    get => _centerOfMassOffset;
    set => _centerOfMassOffset = value;
  }

  /// <summary>
  /// Write into ZPackage. Serialization is efficient but order dependent, so order matters when serializing and deserializing.
  /// </summary>
  public void Serialize(ZPackage pkg)
  {
    pkg.Write(_version);
    pkg.Write(_centerOfMassOffset);
    pkg.Write(_customFloatationHeight);
    pkg.Write(_hasCustomFloatationHeight);
    pkg.Write(_treadDistance);
    pkg.Write(_treadLength);
    pkg.Write(_treadHeight);
    pkg.Write(_treadScaleX);
  }

  public void Save(ZDO zdo, VehicleCustomConfig customConfig, string[]? filterKeys)
  {
#if DEBUG
    LoggerProvider.LogDebug("Saving vehicle config");
#endif
    zdo.SetDelta(Key_Version, customConfig.Version);
    zdo.SetDelta(Key_TreadDistance, customConfig.TreadDistance);
    zdo.SetDelta(Key_TreadLength, customConfig.TreadLength);
    zdo.SetDelta(Key_TreadHeight, customConfig.TreadHeight);
    zdo.SetDelta(Key_TreadScaleX, customConfig.TreadScaleX);
    zdo.SetDelta(Key_HasCustomFloatationHeight, customConfig.HasCustomFloatationHeight);
    zdo.SetDelta(Key_CustomFloatationHeight, customConfig.CustomFloatationHeight);
    zdo.SetDelta(Key_CenterOfMassOffset, customConfig.CenterOfMassOffset);
  }

  public VehicleCustomConfig Load(ZDO zdo, IVehicleConfig configFromComponent, string[]? filterKeys = null)
  {
    return new VehicleCustomConfig
    {
      Version = zdo.GetString(Key_Version, ValheimRAFT_API.GetPluginVersion()),
      TreadDistance = zdo.GetFloat(Key_TreadDistance, configFromComponent.TreadDistance),
      TreadLength = zdo.GetFloat(Key_TreadLength, configFromComponent.TreadLength),
      TreadHeight = zdo.GetFloat(Key_TreadHeight, PhysicsConfig.VehicleLandTreadVerticalOffset.Value),
      TreadScaleX = zdo.GetFloat(Key_TreadScaleX, PrefabConfig.ExperimentalTreadScaleX.Value),
      HasCustomFloatationHeight = zdo.GetBool(Key_HasCustomFloatationHeight, configFromComponent.HasCustomFloatationHeight),
      CustomFloatationHeight = zdo.GetFloat(Key_CustomFloatationHeight, configFromComponent.CustomFloatationHeight),
      CenterOfMassOffset = zdo.GetFloat(Key_CenterOfMassOffset, configFromComponent.CenterOfMassOffset)
    };
  }

  public void ApplyFrom(IVehicleConfig config)
  {
    Version = config.Version;
    TreadDistance = config.TreadDistance;
    TreadLength = config.TreadLength;
    TreadHeight = config.TreadHeight;
    TreadScaleX = config.TreadScaleX;
    HasCustomFloatationHeight = config.HasCustomFloatationHeight;
    CustomFloatationHeight = config.CustomFloatationHeight;
    CenterOfMassOffset = config.CenterOfMassOffset;
  }
  public void ApplyTo(IVehicleConfig config)
  {
    config.Version = Version;
    config.TreadDistance = Mathf.Clamp(TreadDistance, 0.2f, 20f);
    config.TreadLength = Mathf.Clamp(TreadLength, 3f, 50f);
    config.TreadHeight = Mathf.Clamp(TreadHeight, -5f, 20f);
    config.TreadScaleX = Mathf.Clamp(TreadScaleX, 1f, 10f);

    config.HasCustomFloatationHeight = HasCustomFloatationHeight;
    config.CustomFloatationHeight = Mathf.Clamp(CustomFloatationHeight, -50f, 50f);
    config.CenterOfMassOffset = CenterOfMassOffset;
  }

  public VehicleCustomConfig Deserialize(ZPackage pkg)
  {
    pkg.SetPos(0); // Always reset a read pointer, otherwise we start at end and fail.
    return new VehicleCustomConfig
    {
      Version = pkg.ReadString(),
      CenterOfMassOffset = pkg.ReadSingle(),
      CustomFloatationHeight = pkg.ReadSingle(),
      HasCustomFloatationHeight = pkg.ReadBool(),
      TreadDistance = pkg.ReadSingle(),
      TreadLength = pkg.ReadSingle(),
      TreadHeight = pkg.ReadSingle(),
      TreadScaleX = pkg.ReadSingle()
    };
  }
}