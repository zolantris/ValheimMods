using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Immutable draw data for one mesh/submesh/material/render-state combination.
/// Matrix chunks are prepared at build time to avoid per-frame allocations.
/// </summary>
public sealed class InstancedClusterBatch
{
  private readonly Matrix4x4[][] _matrixChunks;

  public Mesh Mesh { get; }
  public int SubMeshIndex { get; }
  public Material SourceMaterial { get; }
  public Material InstancedMaterial { get; }
  public int Layer { get; }
  public ShadowCastingMode ShadowCastingMode { get; }
  public bool ReceiveShadows { get; }
  public LightProbeUsage LightProbeUsage { get; }

  public int DrawCallCount => _matrixChunks.Length;

  public InstancedClusterBatch(
    Mesh mesh,
    int subMeshIndex,
    Material sourceMaterial,
    Material instancedMaterial,
    int layer,
    ShadowCastingMode shadowCastingMode,
    bool receiveShadows,
    LightProbeUsage lightProbeUsage,
    List<Matrix4x4> matrices)
  {
    Mesh = mesh;
    SubMeshIndex = subMeshIndex;
    SourceMaterial = sourceMaterial;
    InstancedMaterial = instancedMaterial;
    Layer = layer;
    ShadowCastingMode = shadowCastingMode;
    ReceiveShadows = receiveShadows;
    LightProbeUsage = lightProbeUsage;

    var chunkCount =
      Mathf.CeilToInt(
        matrices.Count /
        (float) SectorMeshClusterSettings.MaxInstancedMatricesPerDraw);

    _matrixChunks = new Matrix4x4[chunkCount][];

    for (var chunkIndex = 0;
         chunkIndex < chunkCount;
         ++chunkIndex)
    {
      var start =
        chunkIndex *
        SectorMeshClusterSettings.MaxInstancedMatricesPerDraw;

      var count = Mathf.Min(
        SectorMeshClusterSettings.MaxInstancedMatricesPerDraw,
        matrices.Count - start);

      var chunk = new Matrix4x4[count];

      for (var index = 0;
           index < count;
           ++index)
      {
        chunk[index] =
          matrices[start + index];
      }

      _matrixChunks[chunkIndex] = chunk;
    }
  }

  /// <summary>
  /// Returns false when Unity rejects the instanced material/shader combination.
  /// The caller can then restore originals and blacklist the material from instancing.
  /// </summary>
  public bool Draw()
  {
    if (!Mesh ||
        !InstancedMaterial ||
        _matrixChunks.Length == 0)
    {
      return false;
    }

    try
    {
#pragma warning disable 0618
      foreach (var matrices in _matrixChunks)
      {
        Graphics.DrawMeshInstanced(
          Mesh,
          SubMeshIndex,
          InstancedMaterial,
          matrices,
          matrices.Length,
          null,
          ShadowCastingMode,
          ReceiveShadows,
          Layer,
          null,
          LightProbeUsage,
          null);
      }
#pragma warning restore 0618

      return true;
    }
    catch (Exception exception)
    {
      Debug.LogWarning(
        $"[ValheimScalability] GPU instancing failed for mesh '{Mesh.name}', material '{SourceMaterial?.name}'. Falling back to original renderers. {exception.Message}");

      return false;
    }
  }
}
