using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

public class SailCreatorComponent : MonoBehaviour
{
  private static List<SailCreatorComponent> m_sailCreators = [];

  public static GameObject sailPrefab;
  public int m_sailSize;

  private const float MinimumPlaneAreaSqr = 0.000001f;
  private const float MinimumAxisSqr = 0.000001f;

  public void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      return;
    }

    if (m_sailCreators.Count > 4)
    {
      m_sailCreators.Clear();
      return;
    }

    if (m_sailCreators.ToList().Any(sailCreator => sailCreator == null))
    {
      m_sailCreators.Clear();
      return;
    }

    if (m_sailCreators.Count > 0 &&
        (m_sailCreators[0].transform.position - transform.position).sqrMagnitude >
        SailComponent.m_maxDistanceSqr)
    {
      Logger.LogDebug("Sail creator corner distance too far.");
      m_sailCreators.Clear();
    }

    m_sailCreators.Add(this);

    if (m_sailCreators.Count >= m_sailSize)
    {
      CreateSailFromCorners();
    }
  }

  public void CreateSailFromCorners()
  {
    if (m_sailCreators.Count < m_sailSize)
    {
      return;
    }

    if (!sailPrefab)
    {
      return;
    }

    if (m_sailSize is not (3 or 4))
    {
      Logger.LogError(
        $"Cannot create sail with {m_sailSize} corners. Expected 3 or 4.");

      m_sailCreators.Clear();
      return;
    }

    Logger.LogDebug(
      $"Creating new sail {m_sailCreators.Count}/{m_sailSize}");

    /*
     * Capture exactly the creators participating in this sail.
     *
     * From this point onward their placement order is irrelevant.
     */
    var creators = m_sailCreators
      .Take(m_sailSize)
      .ToList();

    var worldCorners = creators
      .Select(creator => creator.transform.position)
      .ToList();

    /*
     * The mast provides the preferred coordinate system for deciding:
     *
     *   - what "up" means
     *   - which side of the sail plane is forward
     *   - consequently what top-right/bottom-right/etc mean
     *
     * This makes the result deterministic even if the player clicks the
     * exact same four locations in a completely different sequence.
     */
    var parentMastComponent =
      creators[0].GetComponentInParent<MastComponent>();

    Transform referenceTransform = null;

    if (parentMastComponent)
    {
      referenceTransform = parentMastComponent.transform;
    }
    else
    {
      referenceTransform = creators[0].transform;
    }

    var center = CalculateCentroid(worldCorners);

    if (!TryCalculateStableSailRotation(
          worldCorners,
          referenceTransform,
          out var sailRotation))
    {
      Logger.LogError(
        "Unable to create sail because the supplied corners do not form a usable plane.");

      return;
    }

    /*
     * Convert arbitrary click order into canonical topology.
     *
     * Quad:
     *
     *   3 -------- 0
     *   |          |
     *   |          |
     *   2 -------- 1
     *
     *   0 = top-right
     *   1 = bottom-right
     *   2 = bottom-left
     *   3 = top-left
     *
     * This is the topology expected by SailComponent.CreateSailMesh().
     */
    var orderedWorldCorners =
      OrderCorners(
        worldCorners,
        center,
        sailRotation);

    var sailPrefabInstance =
      Instantiate(
        sailPrefab,
        center,
        sailRotation);

    var netView =
      sailPrefabInstance.GetComponent<ZNetView>();

    var sailComponent =
      sailPrefabInstance.GetComponent<SailComponent>();

    if (!netView || !sailComponent)
    {
      Logger.LogError(
        "Created sail prefab is missing ZNetView or SailComponent.");

      Destroy(sailPrefabInstance);
      return;
    }

    /*
     * IMPORTANT:
     *
     * Establish the FINAL transform hierarchy BEFORE generating the mesh
     * or asking MagicaCloth to build.
     *
     * Magica should never be constructed and then immediately moved into
     * another transform hierarchy.
     */
    if (parentMastComponent &&
        parentMastComponent.m_rotationTransform)
    {
      var parentTransform =
        parentMastComponent.m_rotationTransform;

      sailPrefabInstance.transform.SetParent(
        parentTransform,
        true);

      var parentNetView =
        parentMastComponent.GetComponent<ZNetView>();

      if (parentNetView &&
          parentNetView.GetZDO() != null &&
          netView.GetZDO() != null)
      {
        var persistentId =
          ZdoWatchController.Instance
            .GetOrCreatePersistentID(
              parentNetView.GetZDO());

        if (persistentId != 0)
        {
          var zdo = netView.GetZDO();

          zdo.Set(
            SailComponent.SailParentIdHash,
            persistentId);

          /*
           * Store position/rotation relative to the ACTUAL transform
           * WaitForSailParent() later uses as the parent.
           *
           * Previously these were calculated against
           * parentMastComponent.transform but later applied under
           * m_rotationTransform. Those spaces are not guaranteed to be
           * equivalent.
           */
          zdo.Set(
            SailComponent.SailParentPositionHash,
            sailPrefabInstance.transform.localPosition);

          zdo.Set(
            SailComponent.SailParentRotationHash,
            sailPrefabInstance.transform.localRotation.eulerAngles);
        }
      }
    }

    /*
     * Now that the sail has its FINAL world transform and parent,
     * translate the canonical world-space corners into sail-local space.
     *
     * This gives SailComponent/Magica a stable local mesh regardless of:
     *
     *   - placement order
     *   - vehicle rotation
     *   - mast rotation
     *   - world orientation
     */
    sailComponent.m_sailCorners = [];

    foreach (var worldCorner in orderedWorldCorners)
    {
      sailComponent.m_sailCorners.Add(
        sailPrefabInstance.transform.InverseTransformPoint(
          worldCorner));
    }

    LogCanonicalCorners(
      sailPrefabInstance.transform,
      orderedWorldCorners);

    /*
     * Material import happens after the final renderer transform exists.
     */
    sailComponent.LoadFromMaterial();

    /*
     * Build Magica LAST.
     *
     * SailComponent now owns the Magica lifecycle and will create/rebuild
     * the cloth against this final mesh/transform topology.
     */
    sailComponent.CreateSailMesh();

    /*
     * Save only after canonicalization.
     *
     * Future ZDO loads therefore receive exactly the same ordering:
     *
     *   quad: TR -> BR -> BL -> TL
     *
     * rather than preserving arbitrary player click order.
     */
    sailComponent.SaveZdo();

    var piece =
      sailPrefabInstance.GetComponent<Piece>();

    var sourcePiece =
      creators[0].GetComponent<Piece>();

    if (piece && sourcePiece)
    {
      var creatorIndex =
        sourcePiece.GetCreatorPlatformUserIdIndex();

      var playerHistory =
        ZNet.World.m_playerHistory;

      // Older pieces may have no platform author recorded
      // in the world's history.
      var creatorPlatformUserId =
        creatorIndex >= 0 &&
        creatorIndex < playerHistory.Count
          ? playerHistory[creatorIndex].m_id
          : Splatform.PlatformUserID.None;

      piece.SetCreator(
        sourcePiece.GetCreator(),
        creatorPlatformUserId);
    }

    AddToVehicle(netView);

    foreach (var creator in creators)
    {
      if (creator)
      {
        Destroy(creator.gameObject);
      }
    }

    m_sailCreators.Clear();
  }

  private static Vector3 CalculateCentroid(
    IReadOnlyList<Vector3> points)
  {
    var center = Vector3.zero;

    for (var i = 0; i < points.Count; i++)
    {
      center += points[i];
    }

    return center / points.Count;
  }

  private static bool TryCalculateStableSailRotation(
    IReadOnlyList<Vector3> points,
    Transform referenceTransform,
    out Quaternion rotation)
  {
    rotation = Quaternion.identity;

    if (points.Count < 3)
    {
      return false;
    }

    /*
     * Sort only for plane detection.
     *
     * This removes another subtle dependency on click order when two
     * triangle combinations happen to have nearly identical areas.
     */
    var deterministicPoints =
      points
        .OrderBy(point => point.x)
        .ThenBy(point => point.y)
        .ThenBy(point => point.z)
        .ToList();

    var bestNormal = Vector3.zero;
    var bestNormalSqr = 0f;

    /*
     * Find the largest non-degenerate triangle from the point set.
     *
     * This is substantially more stable than simply using:
     *
     *   Cross(p1 - p0, p2 - p0)
     *
     * because p0/p1/p2 previously depended directly on click order.
     */
    for (var i = 0; i < deterministicPoints.Count - 2; i++)
    {
      for (var j = i + 1; j < deterministicPoints.Count - 1; j++)
      {
        for (var k = j + 1; k < deterministicPoints.Count; k++)
        {
          var normal =
            Vector3.Cross(
              deterministicPoints[j] - deterministicPoints[i],
              deterministicPoints[k] - deterministicPoints[i]);

          var normalSqr =
            normal.sqrMagnitude;

          if (normalSqr <= bestNormalSqr)
          {
            continue;
          }

          bestNormal = normal;
          bestNormalSqr = normalSqr;
        }
      }
    }

    if (bestNormalSqr <= MinimumPlaneAreaSqr)
    {
      return false;
    }

    var planeNormal =
      bestNormal.normalized;

    /*
     * A plane has two mathematically equivalent normals.
     *
     * Choose ONE deterministically relative to the mast/reference
     * transform instead of letting point ordering choose it.
     */
    planeNormal =
      OrientNormalDeterministically(
        planeNormal,
        referenceTransform);

    /*
     * Prefer mast-up as sail-up.
     *
     * Project it onto the sail plane so LookRotation receives a proper
     * orthogonal up direction.
     */
    var upAxis =
      FindBestProjectedUpAxis(
        planeNormal,
        referenceTransform);

    if (upAxis.sqrMagnitude <= MinimumAxisSqr)
    {
      return false;
    }

    upAxis.Normalize();

    rotation =
      Quaternion.LookRotation(
        planeNormal,
        upAxis);

    return true;
  }

  private static Vector3 OrientNormalDeterministically(
    Vector3 normal,
    Transform referenceTransform)
  {
    Vector3[] referenceAxes;

    if (referenceTransform)
    {
      referenceAxes =
      [
        referenceTransform.forward,
        referenceTransform.right,
        referenceTransform.up
      ];
    }
    else
    {
      referenceAxes =
      [
        Vector3.forward,
        Vector3.right,
        Vector3.up
      ];
    }

    /*
     * Pick whichever reference axis is most parallel to the plane normal.
     *
     * Because one of three orthogonal axes must have a substantial dot
     * product with the normal, this avoids unstable sign decisions around
     * a nearly-perpendicular reference axis.
     */
    var bestDot = 0f;
    var bestAbsoluteDot = float.NegativeInfinity;

    foreach (var axis in referenceAxes)
    {
      var dot =
        Vector3.Dot(
          normal,
          axis.normalized);

      var absoluteDot =
        Mathf.Abs(dot);

      if (absoluteDot <= bestAbsoluteDot)
      {
        continue;
      }

      bestAbsoluteDot = absoluteDot;
      bestDot = dot;
    }

    if (bestDot < 0f)
    {
      normal = -normal;
    }

    return normal;
  }

  private static Vector3 FindBestProjectedUpAxis(
    Vector3 planeNormal,
    Transform referenceTransform)
  {
    Vector3[] candidates;

    if (referenceTransform)
    {
      candidates =
      [
        referenceTransform.up,
        referenceTransform.forward,
        referenceTransform.right
      ];
    }
    else
    {
      candidates =
      [
        Vector3.up,
        Vector3.forward,
        Vector3.right
      ];
    }

    var bestAxis = Vector3.zero;
    var bestAxisSqr = 0f;

    foreach (var candidate in candidates)
    {
      var projected =
        Vector3.ProjectOnPlane(
          candidate,
          planeNormal);

      var projectedSqr =
        projected.sqrMagnitude;

      if (projectedSqr <= bestAxisSqr)
      {
        continue;
      }

      bestAxis = projected;
      bestAxisSqr = projectedSqr;
    }

    return bestAxis;
  }

  private static List<Vector3> OrderCorners(
    IReadOnlyList<Vector3> worldCorners,
    Vector3 center,
    Quaternion sailRotation)
  {
    if (worldCorners.Count == 3)
    {
      return OrderTriangleCorners(
        worldCorners,
        center,
        sailRotation);
    }

    if (worldCorners.Count == 4)
    {
      return OrderQuadCorners(
        worldCorners,
        center,
        sailRotation);
    }

    return worldCorners.ToList();
  }

  private static List<Vector3> OrderQuadCorners(
    IReadOnlyList<Vector3> worldCorners,
    Vector3 center,
    Quaternion sailRotation)
  {
    var inverseRotation =
      Quaternion.Inverse(sailRotation);

    var points =
      worldCorners
        .Select(worldPoint =>
        {
          var localPoint =
            inverseRotation *
            (worldPoint - center);

          return new SailCornerSortData(
            worldPoint,
            localPoint);
        })
        .ToList();

    /*
     * Sort clockwise around the sail center when viewed along the
     * canonical sail normal.
     *
     * Example:
     *
     *   TL -> TR -> BR -> BL
     *
     * We rotate this sequence below so that TR becomes index zero.
     */
    points.Sort(
      (left, right) =>
      {
        var leftAngle =
          Mathf.Atan2(
            left.Local.y,
            left.Local.x);

        var rightAngle =
          Mathf.Atan2(
            right.Local.y,
            right.Local.x);

        return rightAngle.CompareTo(leftAngle);
      });

    var minX =
      points.Min(point => point.Local.x);

    var maxX =
      points.Max(point => point.Local.x);

    var minY =
      points.Min(point => point.Local.y);

    var maxY =
      points.Max(point => point.Local.y);

    var xRange =
      Mathf.Max(
        maxX - minX,
        0.0001f);

    var yRange =
      Mathf.Max(
        maxY - minY,
        0.0001f);

    /*
     * Find the point furthest toward normalized top-right.
     *
     * Normalizing the dimensions prevents a very wide sail from choosing
     * bottom-right simply because X is numerically much larger than Y.
     */
    var topRightIndex = 0;
    var bestTopRightScore = float.NegativeInfinity;

    for (var i = 0; i < points.Count; i++)
    {
      var normalizedX =
        (points[i].Local.x - minX) /
        xRange;

      var normalizedY =
        (points[i].Local.y - minY) /
        yRange;

      var score =
        normalizedX +
        normalizedY;

      if (score <= bestTopRightScore)
      {
        continue;
      }

      bestTopRightScore = score;
      topRightIndex = i;
    }

    /*
     * Rotate the cyclic list without changing its winding.
     *
     * Result:
     *
     *   0 = top-right
     *   1 = bottom-right
     *   2 = bottom-left
     *   3 = top-left
     */
    var ordered =
      new List<Vector3>(4);

    for (var i = 0; i < points.Count; i++)
    {
      var index =
        (topRightIndex + i) %
        points.Count;

      ordered.Add(
        points[index].World);
    }

    return ordered;
  }

  private static List<Vector3> OrderTriangleCorners(
    IReadOnlyList<Vector3> worldCorners,
    Vector3 center,
    Quaternion sailRotation)
  {
    var inverseRotation =
      Quaternion.Inverse(sailRotation);

    var points =
      worldCorners
        .Select(worldPoint =>
        {
          var localPoint =
            inverseRotation *
            (worldPoint - center);

          return new SailCornerSortData(
            worldPoint,
            localPoint);
        })
        .ToList();

    /*
     * Triangle topology used by SailComponent:
     *
     *             2
     *            / \
     *           /   \
     *          /     \
     *         0-------1
     *
     *   0 = bottom-left
     *   1 = bottom-right
     *   2 = top
     *
     * This also matches the triangle UV assignment:
     *
     *   0 -> (0, 0)
     *   1 -> (1, 0)
     *   2 -> (.5, 1)
     */
    var top =
      points
        .OrderByDescending(point => point.Local.y)
        .ThenByDescending(point => point.Local.x)
        .First();

    var bottom =
      points
        .Where(point => !ReferenceEquals(point, top))
        .OrderBy(point => point.Local.x)
        .ToList();

    /*
     * SailCornerSortData is a reference type specifically so the
     * ReferenceEquals exclusion above is exact.
     */
    return
    [
      bottom[0].World,
      bottom[1].World,
      top.World
    ];
  }

  private static void LogCanonicalCorners(
    Transform sailTransform,
    IReadOnlyList<Vector3> orderedWorldCorners)
  {
    if (orderedWorldCorners.Count == 4)
    {
      Logger.LogDebug(
        "Canonical sail corners: " +
        $"TR={sailTransform.InverseTransformPoint(orderedWorldCorners[0])}, " +
        $"BR={sailTransform.InverseTransformPoint(orderedWorldCorners[1])}, " +
        $"BL={sailTransform.InverseTransformPoint(orderedWorldCorners[2])}, " +
        $"TL={sailTransform.InverseTransformPoint(orderedWorldCorners[3])}");

      return;
    }

    if (orderedWorldCorners.Count == 3)
    {
      Logger.LogDebug(
        "Canonical triangular sail corners: " +
        $"BL={sailTransform.InverseTransformPoint(orderedWorldCorners[0])}, " +
        $"BR={sailTransform.InverseTransformPoint(orderedWorldCorners[1])}, " +
        $"TOP={sailTransform.InverseTransformPoint(orderedWorldCorners[2])}");
    }
  }

  /**
   * <description/> Delegates to the VehicleController that it is placed within.
   * - This avoids the additional check if possible.
   */
  public void AddToVehicle(ZNetView netView)
  {
    AddToBasicVehicle(netView);
  }

  private bool AddToBasicVehicle(ZNetView netView)
  {
    var baseVehicle =
      m_sailCreators[0].GetComponentInParent<IPieceController>();

    if (baseVehicle != null)
    {
      baseVehicle.AddNewPiece(netView);
      return true;
    }

    return false;
  }

  private sealed class SailCornerSortData
  {
    public readonly Vector3 World;
    public readonly Vector3 Local;

    public SailCornerSortData(
      Vector3 world,
      Vector3 local)
    {
      World = world;
      Local = local;
    }
  }
}