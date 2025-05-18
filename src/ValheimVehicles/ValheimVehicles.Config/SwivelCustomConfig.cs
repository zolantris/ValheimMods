// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.Config
{
  public class SwivelCustomConfig : ISerializableConfig<SwivelCustomConfig, ISwivelConfig>, ISwivelConfig
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

    public void Serialize(ZPackage package)
    {
      package.Write((int)Mode);
      package.Write(InterpolationSpeed);
      package.Write(MinTrackingRange);
      package.Write(MaxTrackingRange);
      package.Write((int)HingeAxes);
      package.Write(MaxEuler);
      package.Write(MovementOffset);
      package.Write((int)MotionState);
    }

    public SwivelCustomConfig Deserialize(ZPackage package)
    {
      return new SwivelCustomConfig
      {
        Mode = (SwivelMode)package.ReadInt(),
        InterpolationSpeed = package.ReadSingle(),
        MinTrackingRange = package.ReadSingle(),
        MaxTrackingRange = package.ReadSingle(),
        HingeAxes = (HingeAxis)package.ReadInt(),
        MaxEuler = package.ReadVector3(),
        MovementOffset = package.ReadVector3(),
        MotionState = (MotionState)package.ReadInt()
      };
    }

    public void Save(ZDO zdo, SwivelCustomConfig config)
    {
      zdo.Set(Key_Mode, (int)config.Mode);
      zdo.Set(Key_LerpSpeed, config.InterpolationSpeed);
      zdo.Set(Key_TrackMin, config.MinTrackingRange);
      zdo.Set(Key_TrackMax, config.MaxTrackingRange);
      zdo.Set(Key_HingeAxes, (int)config.HingeAxes);
      zdo.Set(Key_MaxX, config.MaxEuler.x);
      zdo.Set(Key_MaxY, config.MaxEuler.y);
      zdo.Set(Key_MaxZ, config.MaxEuler.z);
      zdo.Set(Key_OffsetX, config.MovementOffset.x);
      zdo.Set(Key_OffsetY, config.MovementOffset.y);
      zdo.Set(Key_OffsetZ, config.MovementOffset.z);
      zdo.Set(Key_MotionState, (int)config.MotionState);
    }

    public SwivelCustomConfig Load(ZDO zdo, ISwivelConfig configFromComponent)
    {
      var newConfig = new SwivelCustomConfig
      {
        Mode = (SwivelMode)zdo.GetInt(Key_Mode, (int)configFromComponent.Mode),
        InterpolationSpeed = zdo.GetFloat(Key_LerpSpeed, configFromComponent.InterpolationSpeed),
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

      LoggerProvider.LogDebug($"Loaded new config: Mode: {newConfig.Mode} \nMotionState: {newConfig.MotionState}");

      return newConfig;
    }

    public void ApplyTo(ISwivelConfig component)
    {
      component.Mode = Mode;
      component.InterpolationSpeed = InterpolationSpeed;
      component.MinTrackingRange = MinTrackingRange;
      component.MaxTrackingRange = MaxTrackingRange;
      component.HingeAxes = HingeAxes;
      component.MaxEuler = MaxEuler;
      component.MovementOffset = MovementOffset;
      component.MotionState = MotionState;
      LoggerProvider.LogDebug("SwivelConfig: ApplyTo completed");
    }

    public void ApplyFrom(ISwivelConfig component)
    {
      Mode = component.Mode;
      InterpolationSpeed = component.InterpolationSpeed;
      MinTrackingRange = component.MinTrackingRange;
      MaxTrackingRange = component.MaxTrackingRange;
      HingeAxes = component.HingeAxes;
      MaxEuler = component.MaxEuler;
      MovementOffset = component.MovementOffset;
      MotionState = component.MotionState;

      LoggerProvider.LogDebug("SwivelConfig: ApplyingFrom config");
    }

    public SwivelMode Mode
    {
      get;
      set;
    }
    public float InterpolationSpeed
    {
      get;
      set;
    }
    public float MinTrackingRange
    {
      get;
      set;
    }
    public float MaxTrackingRange
    {
      get;
      set;
    }
    public HingeAxis HingeAxes
    {
      get;
      set;
    }
    public Vector3 MaxEuler
    {
      get;
      set;
    }
    public Vector3 MovementOffset
    {
      get;
      set;
    }
    public MotionState MotionState
    {
      get;
      set;
    }
  }
}