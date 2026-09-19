namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Renderer optimization strategy selected after all structural and user filters.
/// </summary>
public enum ClusterRenderStrategy
{
  /// <summary>
  /// Keep the original MeshRenderer.
  /// </summary>
  Original = 0,

  /// <summary>
  /// Bake static geometry into a generated cell MeshRenderer.
  /// </summary>
  CombinedMesh = 1,

  /// <summary>
  /// Keep per-object transforms and render repeated mesh/material combinations
  /// through GPU instancing. Intended for vegetation/wind shaders.
  /// </summary>
  Instanced = 2
}
