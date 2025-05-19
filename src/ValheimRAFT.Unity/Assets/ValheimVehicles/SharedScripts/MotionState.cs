// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  public enum MotionState
  {
    AtStart, // Not animating, already at original position.
    ToStart, // From Target to Return point
    AtTarget, // Not animating, already at full target rotation or offset
    ToTarget // From return to Target.
  }
}