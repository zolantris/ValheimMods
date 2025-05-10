// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.Config
{
  public class SwivelCustomConfig : ISerializableConfig<SwivelCustomConfig, ISwivelConfig>, ISwivelConfig
  {
    private const string Key_Mode = "swivel_mode";
    private const string Key_LerpSpeed = "swivel_lerpSpeed";
    private const string Key_TrackMin = "swivel_trackMin";
    private const string Key_TrackMax = "swivel_trackMax";
    private const string Key_HingeAxes = "swivel_hingeAxes";
    private const string Key_MaxX = "swivel_maxX";
    private const string Key_MaxY = "swivel_maxY";
    private const string Key_MaxZ = "swivel_maxZ";
    private const string Key_MotionState = "swivel_motionState";
    private const string Key_OffsetX = "swivel_offsetX";
    private const string Key_OffsetY = "swivel_offsetY";
    private const string Key_OffsetZ = "swivel_offsetZ";

    public void Serialize(ZPackage package)
    {
      package.Write((int)Mode);
      package.Write(MovementLerpSpeed);
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
        MovementLerpSpeed = package.ReadSingle(),
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
      zdo.Set(Key_LerpSpeed, config.MovementLerpSpeed);
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
      return new SwivelCustomConfig
      {
        Mode = (SwivelMode)zdo.GetInt(Key_Mode, (int)configFromComponent.Mode),
        MovementLerpSpeed = zdo.GetFloat(Key_LerpSpeed, configFromComponent.MovementLerpSpeed),
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
    }

    public void ApplyTo(ISwivelConfig component)
    {
      component.Mode = Mode;
      component.MovementLerpSpeed = MovementLerpSpeed;
      component.MinTrackingRange = MinTrackingRange;
      component.MaxTrackingRange = MaxTrackingRange;
      component.HingeAxes = HingeAxes;
      component.MaxEuler = MaxEuler;
      component.MovementOffset = MovementOffset;
      component.MotionState = MotionState;
    }

    public void ApplyFrom(ISwivelConfig component)
    {
      Mode = component.Mode;
      MovementLerpSpeed = component.MovementLerpSpeed;
      MinTrackingRange = component.MinTrackingRange;
      MaxTrackingRange = component.MaxTrackingRange;
      HingeAxes = component.HingeAxes;
      MaxEuler = component.MaxEuler;
      MovementOffset = component.MovementOffset;
      MotionState = component.MotionState;
    }

    public SwivelMode Mode
    {
      get;
      set;
    }
    public float MovementLerpSpeed
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