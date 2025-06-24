// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.Storage.Serialization;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public partial class SwivelCustomConfig : ISerializableConfig<SwivelCustomConfig, ISwivelConfig>, ISwivelConfig
  {
    public const string Key_Mode = "swivel_mode";
    public const string Key_LerpSpeed = "swivel_lerpSpeed";
    public const string Key_TrackMin = "swivel_trackMin";
    public const string Key_TrackMax = "swivel_trackMax";
    public const string Key_HingeAxes = "swivel_hingeAxes";
    public const string Key_MaxX = "swivel_maxX";
    public const string Key_MaxY = "swivel_maxY";
    public const string Key_MaxZ = "swivel_maxZ";
    public const string Key_MotionState = "swivel_motionState";
    public const string Key_OffsetX = "swivel_offsetX";
    public const string Key_OffsetY = "swivel_offsetY";
    public const string Key_OffsetZ = "swivel_offsetZ";

    public int GetStableHashCode()
    {
      unchecked
      {
        var hash = 17;
        hash = hash * 31 + Mode.GetHashCode();
        hash = hash * 31 + InterpolationSpeed.GetHashCode();
        hash = hash * 31 + MinTrackingRange.GetHashCode();
        hash = hash * 31 + MaxTrackingRange.GetHashCode();
        hash = hash * 31 + HingeAxes.GetHashCode();
        hash = hash * 31 + MaxEuler.x.GetHashCode();
        hash = hash * 31 + MaxEuler.y.GetHashCode();
        hash = hash * 31 + MaxEuler.z.GetHashCode();
        hash = hash * 31 + MovementOffset.x.GetHashCode();
        hash = hash * 31 + MovementOffset.y.GetHashCode();
        hash = hash * 31 + MovementOffset.z.GetHashCode();
        hash = hash * 31 + MotionState.GetHashCode();
        return hash;
      }
    }

    public void Serialize(ZPackage pkg)
    {
      pkg.Write((int)Mode);
      pkg.Write(InterpolationSpeed);
      pkg.Write(MinTrackingRange);
      pkg.Write(MaxTrackingRange);
      pkg.Write((int)HingeAxes);
      pkg.Write(MaxEuler);
      pkg.Write(MovementOffset);
      pkg.Write((int)MotionState);
    }

    public SwivelCustomConfig Deserialize(ZPackage pkg)
    {
      pkg.SetPos(0); // Always reset read pointer otherwise we start at end and fail.

      return new SwivelCustomConfig
      {
        Mode = (SwivelMode)pkg.ReadInt(),
        InterpolationSpeed = pkg.ReadSingle(),
        MinTrackingRange = pkg.ReadSingle(),
        MaxTrackingRange = pkg.ReadSingle(),
        HingeAxes = (HingeAxis)pkg.ReadInt(),
        MaxEuler = pkg.ReadVector3(),
        MovementOffset = pkg.ReadVector3(),
        MotionState = (MotionState)pkg.ReadInt()
      };
    }

    public Vector3 CreateVector3(float x, float y, float z)
    {
      return new Vector3(
        x,
        y,
        x
      );
    }

    public void LoadByKey(ZDO zdo, ISwivelConfig config, string key)
    {
      switch (key)
      {
        case Key_Mode:
          config.Mode = (SwivelMode)zdo.GetInt(Key_Mode, (int)config.Mode);
          break;
        case Key_LerpSpeed:
          config.InterpolationSpeed = zdo.GetFloat(Key_LerpSpeed, config.InterpolationSpeed);
          break;
        case Key_TrackMin:
          config.MinTrackingRange = zdo.GetFloat(Key_TrackMin, config.MinTrackingRange);
          break;
        case Key_TrackMax:
          config.MaxTrackingRange = zdo.GetFloat(Key_TrackMax, config.MaxTrackingRange);
          break;
        case Key_HingeAxes:
          config.HingeAxes = (HingeAxis)zdo.GetInt(Key_HingeAxes, (int)config.HingeAxes);
          break;
        case Key_MaxX:
          config.MaxEuler = CreateVector3(zdo.GetFloat(Key_MaxX, config.MaxEuler.x), config.MaxEuler.y, config.MaxEuler.z);
          break;
        case Key_MaxY:
          config.MaxEuler = CreateVector3(config.MaxEuler.z, zdo.GetFloat(Key_MaxY, config.MaxEuler.y), config.MaxEuler.z);
          break;
        case Key_MaxZ:
          config.MaxEuler = CreateVector3(config.MaxEuler.z, config.MaxEuler.y, zdo.GetFloat(Key_MaxZ, config.MaxEuler.z));
          break;
        case Key_OffsetX:
          config.MovementOffset = CreateVector3(zdo.GetFloat(Key_OffsetX, config.MovementOffset.x), config.MovementOffset.y, config.MovementOffset.z);
          break;
        case Key_OffsetY:
          config.MovementOffset = CreateVector3(config.MovementOffset.z, zdo.GetFloat(Key_OffsetY, config.MovementOffset.y), config.MovementOffset.z);
          break;
        case Key_OffsetZ:
          config.MovementOffset = CreateVector3(config.MovementOffset.z, config.MovementOffset.y, zdo.GetFloat(Key_OffsetZ, config.MovementOffset.z));
          break;
        case Key_MotionState:
          config.MotionState = (MotionState)zdo.GetInt(Key_MotionState, (int)config.MotionState);
          break;
        default:
          LoggerProvider.LogDebug($"SwivelConfig: Unknown key: {key}");
          break;
      }
    }
    public void Save(ZDO zdo, SwivelCustomConfig config, string[]? filterKeys)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        SaveAll(zdo, config);
        return;
      }

      foreach (var filterKey in filterKeys)
      {
        SaveByKey(zdo, config, filterKey);
      }
    }

    public void SaveAll(ZDO zdo, SwivelCustomConfig config)
    {
      zdo.SetDelta(Key_Mode, (int)config.Mode);
      zdo.SetDelta(Key_LerpSpeed, config.InterpolationSpeed);
      zdo.SetDelta(Key_TrackMin, config.MinTrackingRange);
      zdo.SetDelta(Key_TrackMax, config.MaxTrackingRange);
      zdo.SetDelta(Key_HingeAxes, (int)config.HingeAxes);
      zdo.SetDelta(Key_MaxX, config.MaxEuler.x);
      zdo.SetDelta(Key_MaxY, config.MaxEuler.y);
      zdo.SetDelta(Key_MaxZ, config.MaxEuler.z);
      zdo.SetDelta(Key_OffsetX, config.MovementOffset.x);
      zdo.SetDelta(Key_OffsetY, config.MovementOffset.y);
      zdo.SetDelta(Key_OffsetZ, config.MovementOffset.z);
      zdo.SetDelta(Key_MotionState, (int)config.MotionState);
    }

    public void SaveByKey(ZDO zdo, ISwivelConfig config, string key)
    {
      switch (key)
      {
        case Key_Mode:
          zdo.Set(Key_Mode, (int)config.Mode);
          break;
        case Key_LerpSpeed:
          zdo.Set(Key_LerpSpeed, config.InterpolationSpeed);
          break;
        case Key_TrackMin:
          zdo.Set(Key_TrackMin, config.MinTrackingRange);
          break;
        case Key_TrackMax:
          zdo.Set(Key_TrackMax, config.MaxTrackingRange);
          break;
        case Key_HingeAxes:
          zdo.Set(Key_HingeAxes, (int)config.HingeAxes);
          break;
        case Key_MaxX:
          zdo.Set(Key_MaxX, config.MaxEuler.x);
          break;
        case Key_MaxY:
          zdo.Set(Key_MaxY, config.MaxEuler.y);
          break;
        case Key_MaxZ:
          zdo.Set(Key_MaxZ, config.MaxEuler.z);
          break;
        case Key_OffsetX:
          zdo.Set(Key_OffsetX, config.MovementOffset.x);
          break;
        case Key_OffsetY:
          zdo.Set(Key_OffsetY, config.MovementOffset.y);
          break;
        case Key_OffsetZ:
          zdo.Set(Key_OffsetZ, config.MovementOffset.z);
          break;
        case Key_MotionState:
          zdo.Set(Key_MotionState, (int)config.MotionState);
          break;
        default:
          LoggerProvider.LogDebug($"SwivelConfig: Unknown key: {key}");
          break;
      }
    }

    public SwivelCustomConfig LoadAll(ZDO zdo, ISwivelConfig configFromComponent)
    {
      var newConfig = new SwivelCustomConfig
      {
        Mode = (SwivelMode)zdo.GetInt(Key_Mode, (int)configFromComponent.Mode),
        InterpolationSpeed = zdo.GetFloat(Key_LerpSpeed, Mathf.Clamp(configFromComponent.InterpolationSpeed, 1f, 100f)),
        MinTrackingRange = zdo.GetFloat(Key_TrackMin, configFromComponent.MinTrackingRange),
        MaxTrackingRange = zdo.GetFloat(Key_TrackMax, configFromComponent.MaxTrackingRange),
        HingeAxes = (HingeAxis)zdo.GetInt(Key_HingeAxes, (int)configFromComponent.HingeAxes),
        MaxEuler = new Vector3(
          zdo.GetFloat(Key_MaxX, configFromComponent.MaxEuler.x),
          zdo.GetFloat(Key_MaxY, configFromComponent.MaxEuler.y),
          zdo.GetFloat(Key_MaxZ, configFromComponent.MaxEuler.z)
        ),
        MovementOffset = new Vector3(
          zdo.GetFloat(Key_OffsetX, configFromComponent.MovementOffset.x),
          zdo.GetFloat(Key_OffsetY, configFromComponent.MovementOffset.y),
          zdo.GetFloat(Key_OffsetZ, configFromComponent.MovementOffset.z)
        ),
        MotionState = (MotionState)zdo.GetInt(Key_MotionState, (int)configFromComponent.MotionState)
      };
      return newConfig;
    }

    public SwivelCustomConfig Load(ZDO zdo, ISwivelConfig configFromComponent, string[]? filterKeys = null)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        return LoadAll(zdo, configFromComponent);
      }
      return LoadByKeys(zdo, configFromComponent, filterKeys);
    }

    public SwivelCustomConfig LoadByKeys(ZDO zdo, ISwivelConfig configFromComponent, string[] filterKeys)
    {
      var swivelConfig = new SwivelCustomConfig();
      swivelConfig.ApplyFrom(configFromComponent);

      foreach (var key in filterKeys)
      {
        LoadByKey(zdo, swivelConfig, key);
      }

      return swivelConfig;
    }

    /// <summary>
    /// Serializes fields we allow for persistence when saving.
    /// </summary>
    /// <returns></returns>
    public StoredSwivelCustomConfig SerializeToJson()
    {
      var data = new StoredSwivelCustomConfig
      {
        Mode = Mode,
        InterpolationSpeed = InterpolationSpeed,
        HingeAxes = HingeAxes,
        MaxEuler = new SerializableVector3(MaxEuler.x, MaxEuler.y, MaxEuler.z),
        MovementOffset = new SerializableVector3(MovementOffset.x, MovementOffset.y, MovementOffset.z)
      };

      return data;
    }

    /// <summary>
    /// Deserializes and reads into the Config. Always call SaveAll and Load after deserialization so subscriptions are updated.
    /// </summary>
    /// <param name="data"></param>
    public void DeserializeFromJson(StoredSwivelCustomConfig data)
    {
      Mode = data.Mode;
      InterpolationSpeed = data.InterpolationSpeed;
      HingeAxes = data.HingeAxes;
      MaxEuler = data.MaxEuler.ToVector3();
      MovementOffset = data.MovementOffset.ToVector3();
    }
  }
}