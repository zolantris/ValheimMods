// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
namespace ValheimVehicles.SharedScripts.UI
{
  public interface ISwivelConfig
  {
    public SwivelMode Mode { get; set; }
    public float InterpolationSpeed { get; set; }
    public float MinTrackingRange { get; set; }
    public float MaxTrackingRange { get; set; }
    public HingeAxis HingeAxes { get; set; }
    public Vector3 MaxEuler { get; set; }
    public Vector3 MovementOffset { get; set; }
    public MotionState MotionState { get; set; }
  }
}