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

  private const string InvalidSailUserMessage =
    "Sail is too small or too narrow. Place the sail points farther apart and do not place them in a straight line.";

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

    var creators = m_sailCreators
      .Take(m_sailSize)
      .ToList();

    var worldCorners = creators
      .Select(creator => creator.transform.position)
      .ToList();

    /*
     * Reject impossible sails before a networked sail prefab is instantiated.
     * This catches tiny point clusters and nearly-collinear triangles/quads
     * that otherwise produce a paper-thin/line mesh which can be very difficult
     * or impossible to target with the hammer afterward.
     */
    if (!SailComponent.TryValidateSailGeometry(
          worldCorners,
          out var invalidGeometryReason))
    {
      RejectInvalidSailCreation(
        creators,
        invalidGeometryReason);

      return;
    }

    if (!TryResolveSharedParentMast(
          creators,
          out var parentMastComponent))
    {
      return;
    }

    /*
     * Never derive the ordering frame from creators[0]. The first creator is
     * literally the player's first click, so using its transform leaks click
     * order back into the supposedly canonical topology.
     *
     * Prefer the mast's rotational yard because that is also the final parent
     * coordinate system used by the generated sail. If there is no mast, the
     * ordering code falls back to world axes and remains click-order invariant.
     */
    var referenceTransform =
      parentMastComponent && parentMastComponent.m_rotationTransform
        ? parentMastComponent.m_rotationTransform
        : parentMastComponent
          ? parentMastComponent.transform
          : null;

    var center = CalculateCentroid(worldCorners);

    var orderedWorldCorners =
      OrderCorners(
        worldCorners,
        center,
        referenceTransform);

    if (orderedWorldCorners.Count != m_sailSize)
    {
      RejectInvalidSailCreation(
        creators,
        "Unable to determine a stable non-degenerate sail plane from the selected points.");

      return;
    }

    /*
     * Validate the canonical point set as well. The first check is deliberately
     * order-independent; this second check protects us if canonicalization ever
     * exposes a collapsed topology in a future ordering change.
     */
    if (!SailComponent.TryValidateSailGeometry(
          orderedWorldCorners,
          out invalidGeometryReason))
    {
      RejectInvalidSailCreation(
        creators,
        invalidGeometryReason);

      return;
    }

    /*
     * Keep the original world orientation behavior. Stability comes from a
     * canonical point order and final-parent-before-build invariant, not from
     * inventing a different sail-object rotation from click order.
     */
    var sailPrefabInstance =
      Instantiate(
        sailPrefab,
        center,
        Quaternion.identity);

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
     * Establish the FINAL hierarchy before converting corners to local space or
     * starting Magica. Building first and parenting afterward lets Magica cache
     * an initialization pose in the wrong coordinate system.
     */
    if (parentMastComponent &&
        parentMastComponent.m_rotationTransform)
    {
      sailPrefabInstance.transform.SetParent(
        parentMastComponent.m_rotationTransform,
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
           * WaitForSailParent() later reparents to m_rotationTransform, so save
           * the position/rotation in that exact local coordinate space.
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
     * Import material state after the final renderer/hierarchy exists. The
     * custom-material cache is refreshed inside LoadFromMaterial().
     */
    sailComponent.LoadFromMaterial();

    /*
     * Build last. SailComponent creates the skinned furl rig first and starts
     * Magica only after the source renderer has settled for one frame.
     */
    sailComponent.CreateSailMesh();
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

      // Older pieces may have no platform author recorded in the world's history.
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

  private static void RejectInvalidSailCreation(
    IReadOnlyList<SailCreatorComponent> creators,
    string reason)
  {
    Logger.LogWarning(
      $"Sail creation rejected: {reason}");

    if (Player.m_localPlayer)
    {
      Player.m_localPlayer.Message(
        MessageHud.MessageType.Center,
        InvalidSailUserMessage);
    }

    /*
     * These creator pieces exist only to collect the corner positions. Once the
     * set is known to be invalid, consume them exactly as a successful sail
     * creation would so the player is not left with orphaned point pieces.
     */
    foreach (var creator in creators)
    {
      if (creator)
      {
        Destroy(creator.gameObject);
      }
    }

    m_sailCreators.Clear();
  }

  private static bool TryResolveSharedParentMast(
    IReadOnlyList<SailCreatorComponent> creators,
    out MastComponent? mastComponent)
  {
    mastComponent = null;

    var masts = creators
      .Select(creator => creator.GetComponentInParent<MastComponent>())
      .Where(mast => mast)
      .Distinct()
      .ToList();

    if (masts.Count == 0)
    {
      return true;
    }

    if (masts.Count == 1)
    {
      mastComponent = masts[0];
      return true;
    }

    Logger.LogError(
      "Cannot create one sail from corners belonging to multiple mast hierarchies.");

    return false;
  }

  private static Vector3 CalculateCentroid(
    IReadOnlyList<Vector3> points)
  {
    var center = Vector3.zero;

    for (var i = 0;
         i < points.Count;
         i++)
    {
      center += points[i];
    }

    return center / points.Count;
  }

  private static List<Vector3> OrderCorners(
    IReadOnlyList<Vector3> worldCorners,
    Vector3 center,
    Transform? referenceTransform)
  {
    if (!TryBuildStablePlaneBasis(
          worldCorners,
          referenceTransform,
          out var right,
          out var up))
    {
      return [];
    }

    var projected =
      worldCorners
        .Select(point =>
        {
          var offset = point - center;

          return new SailCornerSortData(
            point,
            Vector3.Dot(offset, right),
            Vector3.Dot(offset, up));
        })
        .ToList();

    if (worldCorners.Count == 3)
    {
      var triangleTop = projected
        .OrderByDescending(point => point.Y)
        .ThenByDescending(point => point.X)
        .First();

      var triangleBottom = projected
        .Where(point => point != triangleTop)
        .OrderBy(point => point.X)
        .ToList();

      return
      [
        triangleBottom[0].World, // BL
        triangleBottom[1].World, // BR
        triangleTop.World // TOP
      ];
    }

    /*
     * Explicitly assign the four unordered points to the topology expected by
     * SailComponent instead of choosing an angular start index.
     *
     *   3 (TL) -------- 0 (TR)
     *     |              |
     *     |              |
     *   2 (BL) -------- 1 (BR)
     *
     * The top/bottom split and left/right split are both performed in the
     * deterministic sail-plane basis above, so all 24 click permutations of
     * the same physical quad produce the same [TR, BR, BL, TL] list.
     */
    var verticalOrder = projected
      .OrderByDescending(point => point.Y)
      .ThenByDescending(point => point.X)
      .ToList();

    var topRow = verticalOrder
      .Take(2)
      .OrderBy(point => point.X)
      .ToList();

    var bottomRow = verticalOrder
      .Skip(2)
      .Take(2)
      .OrderBy(point => point.X)
      .ToList();

    if (topRow.Count != 2 || bottomRow.Count != 2)
    {
      return [];
    }

    var topLeft = topRow[0];
    var topRight = topRow[1];
    var bottomLeft = bottomRow[0];
    var bottomRight = bottomRow[1];

    return
    [
      topRight.World, // 0 = TR
      bottomRight.World, // 1 = BR
      bottomLeft.World, // 2 = BL
      topLeft.World // 3 = TL
    ];
  }

  private static bool TryBuildStablePlaneBasis(
    IReadOnlyList<Vector3> points,
    Transform? referenceTransform,
    out Vector3 right,
    out Vector3 up)
  {
    right = Vector3.zero;
    up = Vector3.zero;

    if (points.Count < 3)
    {
      return false;
    }

    var deterministicPoints = points
      .OrderBy(point => point.x)
      .ThenBy(point => point.y)
      .ThenBy(point => point.z)
      .ToList();

    var bestNormal = Vector3.zero;
    var bestNormalSqr = 0f;

    for (var i = 0;
         i < deterministicPoints.Count - 2;
         i++)
    {
      for (var j = i + 1;
           j < deterministicPoints.Count - 1;
           j++)
      {
        for (var k = j + 1;
             k < deterministicPoints.Count;
             k++)
        {
          var normal = Vector3.Cross(
            deterministicPoints[j] - deterministicPoints[i],
            deterministicPoints[k] - deterministicPoints[i]);

          var normalSqr = normal.sqrMagnitude;

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

    var normalDirection =
      bestNormal.normalized;

    normalDirection = OrientNormalDeterministically(
      normalDirection,
      referenceTransform);

    up = Vector3.ProjectOnPlane(
      referenceTransform
        ? referenceTransform.up
        : Vector3.up,
      normalDirection);

    if (up.sqrMagnitude <= MinimumAxisSqr)
    {
      up = Vector3.ProjectOnPlane(
        referenceTransform
          ? referenceTransform.forward
          : Vector3.forward,
        normalDirection);
    }

    if (up.sqrMagnitude <= MinimumAxisSqr)
    {
      return false;
    }

    up.Normalize();

    right = Vector3.Cross(
      up,
      normalDirection);

    if (right.sqrMagnitude <= MinimumAxisSqr)
    {
      return false;
    }

    right.Normalize();
    return true;
  }

  private static Vector3 OrientNormalDeterministically(
    Vector3 normal,
    Transform? referenceTransform)
  {
    Vector3[] referenceAxes = referenceTransform
      ?
      [
        referenceTransform.forward,
        referenceTransform.right,
        referenceTransform.up
      ]
      :
      [
        Vector3.forward,
        Vector3.right,
        Vector3.up
      ];

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

    return bestDot < 0f
      ? -normal
      : normal;
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
    public readonly float X;
    public readonly float Y;

    public SailCornerSortData(
      Vector3 world,
      float x,
      float y)
    {
      World = world;
      X = x;
      Y = y;
    }
  }
}