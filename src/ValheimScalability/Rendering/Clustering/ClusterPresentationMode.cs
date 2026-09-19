namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Controls how player proximity affects generated sector cluster meshes.
/// Damage/stale-geometry safety still takes precedence over every mode.
/// </summary>
public enum ClusterPresentationMode
{
  /// <summary>
  /// Nearby cluster cells restore original renderers.
  /// Distant cells use generated cluster meshes.
  /// </summary>
  Adaptive = 0,

  /// <summary>
  /// Player proximity never restores originals.
  /// Useful for testing visual/performance behavior and for very large builds.
  /// Damage/stale geometry can still temporarily restore originals until rebuilt.
  /// </summary>
  FullCluster = 1,

  /// <summary>
  /// Never present generated cluster meshes.
  /// Registration remains active so the mode can be changed at runtime.
  /// </summary>
  OriginalsOnly = 2
}
