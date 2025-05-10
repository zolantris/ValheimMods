// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.Validation
{
  /// <summary>
  /// For any Controller that must not be null or access whiled in a bad state
  ///
  /// There are no setters as these properties should be coming from the VehicleManager.
  /// </summary>
  public interface IControllerValidator
  {

    /// <summary>
    /// Catch all for all values. Must be true for everything.
    /// </summary>
    internal bool IsControllerValid { get; }

    /// <summary>
    /// Can run all methods.
    /// </summary>
    internal bool IsInitialized { get; }

    /// <summary>
    /// should never run anything on this while in this state
    /// </summary>
    internal bool IsDestroying { get; }
  }
}