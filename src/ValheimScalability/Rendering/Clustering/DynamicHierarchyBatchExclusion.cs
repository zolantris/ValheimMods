using UnityEngine;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Rejects geometry that belongs to a moving hierarchy.
///
/// This intentionally has no compile-time dependency on ValheimRAFT /
/// ValheimVehicles. The two persisted ZDO keys below are treated as a small
/// compatibility contract: if either parent id is present, the object is not
/// world-static and must never be included in a sector combined mesh.
/// </summary>
internal static class DynamicHierarchyBatchExclusion
{
  private const string MBParentIdKey =
    "MBParentId";

  private const string SwivelParentIdKey =
    "SwivelParentId";

  private static readonly int MBParentIdHash =
    StringExtensionMethods.GetStableHashCode(
      MBParentIdKey);

  /// <summary>
  /// Checks persisted ownership before the spawned GameObject has necessarily
  /// been activated/reparented into its final moving hierarchy.
  /// </summary>
  public static bool HasPersistedMovingParent(
    ZDO zdo)
  {
    if (zdo == null)
    {
      return false;
    }

    // ValheimRAFT/ValheimVehicles stores these as persistent integer ids.
    // MBParentId uses the already-hashed key; SwivelParentId is a string key.
    return zdo.GetInt(
             MBParentIdHash,
             0) != 0 ||
           zdo.GetInt(
             SwivelParentIdKey,
             0) != 0;
  }

  /// <summary>
  /// Checks both persisted network ownership and the current Unity hierarchy.
  /// Any Rigidbody parent is rejected, even while kinematic: vehicles can be
  /// temporarily kinematic while anchored, loading, or transferring ownership.
  /// </summary>
  public static bool ShouldExclude(
    GameObject root)
  {
    if (!root)
    {
      return true;
    }

    var nview =
      root.GetComponent<ZNetView>() ??
      root.GetComponentInParent<ZNetView>();

    if (nview &&
        HasPersistedMovingParent(
          nview.GetZDO()))
    {
      return true;
    }

    return root.GetComponentInParent<Rigidbody>() != null;
  }

  /// <summary>
  /// Fast path for ZNetScene.AddInstance. This runs before vehicle activation
  /// can finish, so the ZDO check is the authoritative early-spawn guard.
  /// </summary>
  public static bool ShouldExclude(
    ZDO zdo,
    GameObject root)
  {
    if (HasPersistedMovingParent(zdo))
    {
      return true;
    }

    return root &&
           root.GetComponentInParent<Rigidbody>() != null;
  }

  /// <summary>
  /// Renderer-level protection for static roots that contain a nested physics
  /// hierarchy. A moving child must not be folded into the root's static batch.
  /// </summary>
  public static bool ShouldExcludeRenderer(
    MeshRenderer renderer)
  {
    return renderer &&
           renderer.GetComponentInParent<Rigidbody>() != null;
  }
}
