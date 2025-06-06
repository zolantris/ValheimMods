// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  public enum SwivelMode
  {
    None,
    Rotate,
    Move,
    TargetWind,
#if DEBUG
    // not ready for prod.
    TargetEnemy,
#endif
  }
}