using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.Components;
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
  private const string Key_Version = "vehicle_version";
  private const string Key_TreadDistance = "vehicle_treadDistance";
  private const string Key_TreadHeight = "vehicle_treadHeight";
  private const string Key_TreadScaleX = "vehicle_treadScaleX";
  private const string Key_HasCustomFloatationHeight = "vehicle_hasCustomFloatationHeight";
  private const string Key_CustomFloatationHeight = "vehicle_customFloatationHeight";
  private const string Key_CenterOfMassOffset = "vehicle_centerOfMassOffset";

  // todo support custom variant
  // private const string Key_VehicleVariant = "vehicle_variant";

  // Backing fields
  private string _version = ValheimRAFT_API.GetPluginVersion();
  private float _treadDistance;
  private float _treadHeight;
  private float _treadScaleX;
  private bool _hasCustomFloatationHeight;
  private float _customFloatationHeight;
  private float _centerOfMassOffset;

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
    set => _treadDistance = Mathf.Clamp(value, 0.5f, 5f);
  }

  public float TreadHeight
  {
    get => _treadHeight;
    set => _treadHeight = Mathf.Clamp(value, 0.1f, 10f);
  }

  public float TreadScaleX
  {
    get => _treadScaleX;
    set => _treadScaleX = Mathf.Clamp(value, 0.1f, 10f);
  }

  public bool HasCustomFloatationHeight
  {
    get => _hasCustomFloatationHeight;
    set => _hasCustomFloatationHeight = value;
  }

  public float CustomFloatationHeight
  {
    get => _customFloatationHeight;
    set => _customFloatationHeight = Mathf.Max(0f, value);
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
    pkg.Write(_treadHeight);
    pkg.Write(_treadScaleX);
  }

  public void Save(ZDO zdo, VehicleCustomConfig customConfig)
  {
    LoggerProvider.LogDebug("Saving vehicle config");

    if (zdo.GetString(Key_Version) != customConfig.Version)
      zdo.Set(Key_Version, customConfig.Version);

    if (!Mathf.Approximately(zdo.GetFloat(Key_TreadDistance), customConfig.TreadDistance))
      zdo.Set(Key_TreadDistance, customConfig.TreadDistance);

    if (!Mathf.Approximately(zdo.GetFloat(Key_TreadHeight), customConfig.TreadHeight))
      zdo.Set(Key_TreadHeight, customConfig.TreadHeight);

    if (!Mathf.Approximately(zdo.GetFloat(Key_TreadScaleX), customConfig.TreadScaleX))
      zdo.Set(Key_TreadScaleX, customConfig.TreadScaleX);

    if (zdo.GetBool(Key_HasCustomFloatationHeight) != customConfig.HasCustomFloatationHeight)
      zdo.Set(Key_HasCustomFloatationHeight, customConfig.HasCustomFloatationHeight);

    if (!Mathf.Approximately(zdo.GetFloat(Key_CustomFloatationHeight), customConfig.CustomFloatationHeight))
      zdo.Set(Key_CustomFloatationHeight, customConfig.CustomFloatationHeight);

    if (!Mathf.Approximately(zdo.GetFloat(Key_CenterOfMassOffset), customConfig.CenterOfMassOffset))
      zdo.Set(Key_CenterOfMassOffset, customConfig.CenterOfMassOffset);
  }


  public VehicleCustomConfig Load(ZDO zdo, IVehicleConfig configFromComponent)
  {
    return new VehicleCustomConfig
    {
      Version = zdo.GetString(Key_Version, ValheimRAFT_API.GetPluginVersion()),
      TreadDistance = Mathf.Clamp(zdo.GetFloat(Key_TreadDistance, 0.1f), 0.1f, 20f),
      TreadHeight = zdo.GetFloat(Key_TreadHeight, PhysicsConfig.VehicleLandTreadVerticalOffset.Value),
      TreadScaleX = zdo.GetFloat(Key_TreadScaleX, PrefabConfig.ExperimentalTreadScaleX.Value),
      HasCustomFloatationHeight = zdo.GetBool(Key_HasCustomFloatationHeight, configFromComponent.HasCustomFloatationHeight),
      CustomFloatationHeight = zdo.GetFloat(Key_CustomFloatationHeight, configFromComponent.CustomFloatationHeight),
      CenterOfMassOffset = zdo.GetFloat(Key_CenterOfMassOffset, configFromComponent.CenterOfMassOffset)
    };
  }

  public void ApplyFrom(IVehicleConfig config)
  {
    // throw new System.NotImplementedException();
  }
  public void ApplyTo(IVehicleConfig config)
  {
    // throw new System.NotImplementedException();
  }

  public VehicleCustomConfig Deserialize(ZPackage pkg)
  {
    pkg.SetPos(0); // Always reset read pointer otherwise we start at end and fail.
    return new VehicleCustomConfig
    {
      Version = pkg.ReadString(),
      CenterOfMassOffset = pkg.ReadSingle(),
      CustomFloatationHeight = pkg.ReadSingle(),
      HasCustomFloatationHeight = pkg.ReadBool(),
      TreadDistance = pkg.ReadSingle(),
      TreadHeight = pkg.ReadSingle(),
      TreadScaleX = pkg.ReadSingle()
    };
  }
}