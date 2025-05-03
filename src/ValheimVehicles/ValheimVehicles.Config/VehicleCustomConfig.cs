using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Config;

/// <summary>
/// A config syncing component for per-vehicle configs.
/// - This is not a BepInEx config component, there will be no BepInEx fields used.
/// </summary>
public class VehicleCustomConfig : ISerializableConfig<VehicleCustomConfig>
{
  // Constants for ZDO keys
  private const string Key_Version = "vehicle_version";
  private const string Key_TreadDistance = "vehicle_treadDistance";
  private const string Key_TreadHeight = "vehicle_treadHeight";
  private const string Key_TreadScaleX = "vehicle_treadScaleX";
  private const string Key_HasCustomFloatationHeight = "vehicle_hasCustomFloatationHeight";
  private const string Key_CustomFloatationHeight = "vehicle_customFloatationHeight";
  private const string Key_CenterOfMassOffset = "vehicle_centerOfMassOffset";

  // Backing fields
  private string _version = ValheimRAFT_API.GetPluginVersion();
  private float _treadDistance;
  private float _treadHeight;
  private float _treadScaleX;
  private bool _hasCustomFloatationHeight;
  private float _customFloatationHeight;
  private float _centerOfMassOffset;

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
  public void Serialize(ZPackage package)
  {
    package.Write(_version);
    package.Write(_centerOfMassOffset);
    package.Write(_customFloatationHeight);
    package.Write(_hasCustomFloatationHeight);
    package.Write(_treadDistance);
    package.Write(_treadHeight);
    package.Write(_treadScaleX);
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


  public VehicleCustomConfig Load(ZDO zdo)
  {
    return new VehicleCustomConfig
    {
      Version = zdo.GetString(Key_Version, ValheimRAFT_API.GetPluginVersion()),
      TreadDistance = zdo.GetFloat(Key_TreadDistance, 2f),
      TreadHeight = zdo.GetFloat(Key_TreadHeight, 0f),
      TreadScaleX = zdo.GetFloat(Key_TreadScaleX, 1f),
      HasCustomFloatationHeight = zdo.GetBool(Key_HasCustomFloatationHeight),
      CustomFloatationHeight = zdo.GetFloat(Key_CustomFloatationHeight, 0f),
      CenterOfMassOffset = zdo.GetFloat(Key_CenterOfMassOffset, 0f)
    };
  }

  public VehicleCustomConfig Deserialize(ZPackage package)
  {
    return new VehicleCustomConfig
    {
      Version = package.ReadString(),
      CenterOfMassOffset = package.ReadSingle(),
      CustomFloatationHeight = package.ReadSingle(),
      HasCustomFloatationHeight = package.ReadBool(),
      TreadDistance = package.ReadSingle(),
      TreadHeight = package.ReadSingle(),
      TreadScaleX = package.ReadSingle()
    };
  }
}