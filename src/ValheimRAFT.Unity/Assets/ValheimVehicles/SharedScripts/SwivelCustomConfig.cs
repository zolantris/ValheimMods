// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.SharedScripts
{
  // ReSharper disable once PartialTypeWithSinglePart
  public partial class SwivelCustomConfig : ISwivelConfig
  {
    public Vector3 MaxEuler
    {
      get;
      set;
    }

    public Vector3 MinEuler
    {
      get;
      set;
    }

    public Vector3 MovementOffset
    {
      get;
      set;
    }

    public SwivelMode Mode
    {
      get;
      set;
    } = SwivelMode.Move;

    public float InterpolationSpeed
    {
      get;
      set;
    } = 10f;
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
    public MotionState MotionState
    {
      get;
      set;
    }

    public void ApplyTo(ISwivelConfig component)
    {
      component.Mode = Mode;
      component.InterpolationSpeed = Mathf.Clamp(InterpolationSpeed, 1f, 100f);
      component.MinTrackingRange = MinTrackingRange;
      component.MaxTrackingRange = MaxTrackingRange;
      component.HingeAxes = HingeAxes;
      component.MaxEuler = MaxEuler;
      component.MinEuler = MinEuler;
      component.MovementOffset = MovementOffset;
      component.MotionState = MotionState;
    }

    public void ApplyFrom(ISwivelConfig component)
    {
      Mode = component.Mode;
      InterpolationSpeed = Mathf.Clamp(component.InterpolationSpeed, 1f, 100f);
      MinTrackingRange = component.MinTrackingRange;
      MaxTrackingRange = component.MaxTrackingRange;
      HingeAxes = component.HingeAxes;
      MaxEuler = component.MaxEuler;
      MinEuler = component.MinEuler;
      MovementOffset = component.MovementOffset;
      MotionState = component.MotionState;
    }

    public static MotionState GetCompleteMotionState(MotionState current)
    {
      return current switch
      {
        MotionState.AtStart => MotionState.AtStart,
        MotionState.AtTarget => MotionState.AtTarget,
        MotionState.ToStart => MotionState.AtStart,
        MotionState.ToTarget => MotionState.AtTarget,
        _ => MotionState.AtStart
      };
    }

    public static MotionState GetNextMotionState(MotionState current)
    {
      return current switch
      {
        MotionState.AtStart => MotionState.ToTarget,
        MotionState.AtTarget => MotionState.ToStart,
        MotionState.ToStart => MotionState.ToTarget,
        MotionState.ToTarget => MotionState.ToStart,
        _ => MotionState.AtStart
      };
    }
  }
}