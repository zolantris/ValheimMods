using System;
using System.Reflection;
using UnityEngine;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Stable identity for material batching.
///
/// Valheim can have separate Material objects with the same material contents/name.
/// Comparing Material references directly fragments those into separate combined meshes.
///
/// Unity's Material.ComputeCRC() is resolved by reflection so this remains source-compatible
/// if a target Unity API surface does not expose it. When unavailable/failing, we fall back
/// to Material instance identity, which is conservative and never merges unknown materials.
/// </summary>
public readonly struct MaterialBatchIdentity :
  IEquatable<MaterialBatchIdentity>
{
  private static readonly MethodInfo ComputeCrcMethod =
    typeof(Material).GetMethod(
      "ComputeCRC",
      BindingFlags.Instance | BindingFlags.Public,
      null,
      Type.EmptyTypes,
      null);

  public readonly int ShaderId;
  public readonly int MaterialContentId;
  public readonly int RenderQueue;
  public readonly bool UsesContentCrc;

  public MaterialBatchIdentity(
    int shaderId,
    int materialContentId,
    int renderQueue,
    bool usesContentCrc)
  {
    ShaderId = shaderId;
    MaterialContentId = materialContentId;
    RenderQueue = renderQueue;
    UsesContentCrc = usesContentCrc;
  }

  public static MaterialBatchIdentity From(Material material)
  {
    if (!material)
    {
      return default;
    }

    var shaderId =
      material.shader
        ? material.shader.GetInstanceID()
        : 0;

    if (TryComputeContentCrc(material, out var crc))
    {
      return new MaterialBatchIdentity(
        shaderId,
        crc,
        material.renderQueue,
        true);
    }

    // Safe fallback: do not merge separate material objects when we cannot
    // verify that their contents are equivalent.
    return new MaterialBatchIdentity(
      shaderId,
      material.GetInstanceID(),
      material.renderQueue,
      false);
  }

  private static bool TryComputeContentCrc(
    Material material,
    out int crc)
  {
    crc = 0;

    if (ComputeCrcMethod == null)
    {
      return false;
    }

    try
    {
      var value =
        ComputeCrcMethod.Invoke(
          material,
          null);

      if (value == null)
      {
        return false;
      }

      crc = unchecked(
        (int) Convert.ToUInt32(value));

      return true;
    }
    catch
    {
      return false;
    }
  }

  public bool Equals(
    MaterialBatchIdentity other)
  {
    return ShaderId == other.ShaderId &&
           MaterialContentId == other.MaterialContentId &&
           RenderQueue == other.RenderQueue;
  }

  public override bool Equals(object obj)
  {
    return obj is MaterialBatchIdentity other &&
           Equals(other);
  }

  public override int GetHashCode()
  {
    unchecked
    {
      var hash = ShaderId;
      hash = (hash * 397) ^ MaterialContentId;
      hash = (hash * 397) ^ RenderQueue;
      return hash;
    }
  }

  public override string ToString()
  {
    return $"{ShaderId:X8}:{MaterialContentId:X8}:Q{RenderQueue}";
  }
}
