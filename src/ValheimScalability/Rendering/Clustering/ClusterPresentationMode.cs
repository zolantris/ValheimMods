namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Controls how sector/cell optimized rendering is presented.
/// Damage/stale-geometry safety still takes precedence over every mode.
/// </summary>
public enum ClusterPresentationMode
{
  /// <summary>
  /// Do not build or present optimized cluster rendering.
  /// Original renderers remain active.
  /// Registration remains alive so the mode can be changed at runtime.
  /// </summary>
  Off = 0,

  /// <summary>
  /// Player proximity never restores originals.
  /// Useful for maximum rendering optimization and large builds.
  /// Damage/stale geometry can still temporarily restore originals until rebuilt.
  /// </summary>
  FullCluster = 1,

  /// <summary>
  /// Nearby cluster cells restore original renderers.
  /// Distant cells use optimized rendering.
  /// </summary>
  Adaptive = 2
}
