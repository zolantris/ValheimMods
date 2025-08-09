// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Core
{
  [Serializable]
  public class SurfaceClimbingState
  {
    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;
    [SerializeField] private float replanCooldown = 0.5f;

    [Header("Body")]
    [SerializeField] private float bodyRadius = 0.35f;
    [SerializeField] private float bodyHeight = 1.8f;

    [Header("Mantle")]
    [SerializeField] private float minLedge = 0.35f;
    [SerializeField] private float maxLedge = 3.0f;
    [SerializeField] private float stepOver = 0.30f;
    [SerializeField] private float minAscent = 0.25f;

    [Header("Ingress (floor opening)")]
    [SerializeField] private float ingressSearchRadius = 10f;
    [SerializeField] private float ingressMinDrop = 1.0f;
    [SerializeField] private float ingressMaxDrop = 6.0f;
    [SerializeField] private int ingressSamples = 14;

    [Header("Queries")]
    [SerializeField] private LayerMask solidMask = 0; // set in Init
    [SerializeField] private float sameFloorEps = 0.9f;

    private Transform _tr;
    private float _nextPlanAt;
    private PlannedPath _cached;

    // --- public API: call every frame where you want the line shown ---
    public void Init(MonoBehaviour _, Rigidbody __, Transform root)
    {
      _tr = root;
      if (solidMask.value == 0) solidMask = LayerHelpers.GroundLayers;
      RecalcBodyFromColliders();
    }

    public void DebugPlanTo(Vector3 target)
    {
      if (!drawDebug) return;
      if (Time.time >= _nextPlanAt)
      {
        _cached = Plan(_tr.position, target);
        _nextPlanAt = Time.time + replanCooldown;
      }
      Draw(_cached);
    }

    // ---------------------------------------------------------------

    #region Planner

    private struct Portal
    {
      public Vector3 from; // on current surface/floor
      public Vector3 to; // result position after using portal
      public PortalType type;
    }

    private enum PortalType
    {
      Mantle,
      Ingress
    }

    private sealed class Node
    {
      public int id;
      public Vector3 p;
      public PortalType? via; // via!=null means arriving through this portal type
      public Node(int id, Vector3 p, PortalType? via = null)
      {
        this.id = id;
        this.p = p;
        this.via = via;
      }
    }

    private sealed class PlannedPath
    {
      public bool ok;
      public readonly List<(Vector3 a, Vector3 b, Color c, bool isWalk, List<Vector3> corners)> segs = new();
    }

    private PlannedPath Plan(Vector3 from, Vector3 to)
    {
      var path = new PlannedPath();
      var nodes = new List<Node>(12);
      var edges = new Dictionary<(int, int), (float, bool, PortalType?)>(); // cost, isWalk, portalType?

      // 0) build start/target nodes (feet‑snapped)
      var start = new Node(0, GroundSnap(from));
      var goal = new Node(1, GroundSnap(to));
      nodes.Add(start);
      nodes.Add(goal);

      // Early out: same floor & direct nav path
      if (SameFloor(start.p, goal.p) && TryGetNavCorners(start.p, goal.p, out var corners))
      {
        AddWalkSeg(path, corners);
        path.ok = true;
        return path;
      }

      // 1) discover portals near start and near goal (few, fast)
      var portals = _tmpPortals;
      portals.Clear();
      FindMantlePortalsAround(start.p, goal.p, portals, 6);
      FindIngressPortals(start.p, goal.p, portals, 6);

      // 2) turn portals into graph nodes (use both ends)
      for (var i = 0; i < portals.Count; i++)
      {
        var pid = 2 + i * 2;
        nodes.Add(new Node(pid, portals[i].from));
        nodes.Add(new Node(pid + 1, portals[i].to, portals[i].type));
      }

      // 3) connect walkable pairs on the same floor (cheap checks + HavePath)
      for (var i = 0; i < nodes.Count; i++)
      {
        for (var j = i + 1; j < nodes.Count; j++)
        {
          if (!SameFloor(nodes[i].p, nodes[j].p)) continue;
          if (!TryGetNavCorners(nodes[i].p, nodes[j].p, out var cs)) continue;
          var cost = CornersLength(cs);
          edges[(nodes[i].id, nodes[j].id)] = (cost, true, null);
          edges[(nodes[j].id, nodes[i].id)] = (cost, true, null);
          _walkCorners[(nodes[i].id, nodes[j].id)] = cs; // cache for drawing
          _walkCorners[(nodes[j].id, nodes[i].id)] = _revTmp(cs);
        }
      }

      // 4) connect each portal from->to with a fixed cost
      for (var pi = 0; pi < portals.Count; pi++)
      {
        var fid = 2 + pi * 2;
        var tid = fid + 1;
        var baseCost = Vector3.Distance(nodes[fid].p, nodes[tid].p);
        var add = portals[pi].type == PortalType.Mantle ? 2.5f : 1.5f; // mantle “costlier” than drop
        edges[(fid, tid)] = (baseCost + add, false, portals[pi].type);
      }

      // 5) A* over this tiny graph
      var came = AStar(nodes, edges, start.id, goal.id, out var routeIds);
      if (!came)
      {
        path.ok = false;
        return path;
      }

      // 6) materialize route into draw segments
      for (var k = 0; k < routeIds.Count - 1; k++)
      {
        var a = routeIds[k];
        var b = routeIds[k + 1];
        var key = (a, b);
        var (cost, isWalk, via) = edges[key];

        if (isWalk)
        {
          var cs = _walkCorners[key];
          AddWalkSeg(path, cs);
        }
        else
        {
          // portal: draw short colored stroke
          var pa = nodes.Find(n => n.id == a)!.p;
          var pb = nodes.Find(n => n.id == b)!.p;
          var col = via == PortalType.Mantle ? new Color(1f, 1f, 0.2f) : new Color(1f, 0f, 1f);
          path.segs.Add((pa, pb, col, false, null));
        }
      }

      path.ok = true;
      return path;
    }

    #endregion

    #region Queries / building blocks

    private readonly List<Portal> _tmpPortals = new(12);
    private readonly Dictionary<(int, int), List<Vector3>> _walkCorners = new();

    private bool SameFloor(Vector3 a, Vector3 b)
    {
      return Mathf.Abs(a.y - b.y) <= sameFloorEps;
    }

    private Vector3 GroundSnap(Vector3 p)
    {
      if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var h, 6f, solidMask, QueryTriggerInteraction.Ignore))
        return h.point;
      return p;
    }

    private void FindMantlePortalsAround(Vector3 from, Vector3 to, List<Portal> outList, int maxCount)
    {
      // Probe a small fan toward the target to find a wall face, then its top
      var dir = to - from;
      dir.y = 0;
      if (dir.sqrMagnitude < 0.01f) dir = _tr ? _tr.forward : Vector3.forward;
      dir.Normalize();

      var fan = new[] { -35f, -15f, 0f, 15f, 35f };
      var step = 0.9f;
      var maxDist = 5f;

      for (var a = 0; a < fan.Length && outList.Count < maxCount; a++)
      {
        var fwd = Quaternion.Euler(0, fan[a], 0) * dir;
        for (var d = step; d <= maxDist && outList.Count < maxCount; d += step)
        {
          var origin = from + fwd * d + Vector3.up * Mathf.Max(1.0f, bodyRadius * 3f);
          if (!Physics.SphereCast(origin, bodyRadius * 0.9f, fwd, out var face, 0.4f, solidMask)) continue;

          var wallN = face.normal;
          var feetY = from.y;
          var h = face.point.y - feetY;
          if (h < minLedge || h > maxLedge) break; // too low/high

          // step over top then down
          var stepOverPos = face.point + Vector3.up * Mathf.Min(maxLedge, h + 0.8f) + wallN * stepOver;
          if (!Physics.Raycast(stepOverPos + Vector3.up * 0.25f, Vector3.down, out var down, 4f, solidMask)) break;
          if (down.point.y < feetY + minAscent) break; // must go up

          // create portal
          outList.Add(new Portal
          {
            from = GroundSnap(origin - fwd * 0.35f), // just before wall
            to = down.point,
            type = PortalType.Mantle
          });
          break;
        }
      }
    }

    private void FindIngressPortals(Vector3 from, Vector3 target, List<Portal> outList, int maxCount)
    {
      var best = new List<(Portal p, float score)>(maxCount);
      for (var i = 0; i < ingressSamples; i++)
      {
        var yaw = 360f * (i / (float)ingressSamples);
        var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        for (var dist = 1f; dist <= ingressSearchRadius; dist += 1f)
        {
          var probe = from + dir * dist;
          if (!Physics.Raycast(probe + Vector3.up * 1.5f, Vector3.down, out var on, 4f, solidMask)) continue;

          if (!Physics.Raycast(on.point + Vector3.up * 0.2f, Vector3.down, out var drop, ingressMaxDrop + 0.6f, solidMask)) continue;
          var delta = on.point.y - drop.point.y;
          if (delta < ingressMinDrop || delta > ingressMaxDrop) continue;
          if (Vector3.Angle(drop.normal, Vector3.up) > 45f) continue;

          var yBefore = Mathf.Abs(from.y - target.y);
          var yAfter = Mathf.Abs(drop.point.y - target.y);
          var yGain = yBefore - yAfter;
          var planarAfter = Vector2.Distance(new Vector2(on.point.x, on.point.z), new Vector2(target.x, target.z));
          var toProbe = Vector3.Distance(from, on.point);

          var score = yGain * 2f - planarAfter * 0.12f - toProbe * 0.05f;
          var port = new Portal { from = on.point, to = drop.point, type = PortalType.Ingress };

          if (best.Count < maxCount) best.Add((port, score));
          else
          {
            var worstI = 0;
            var worst = best[0].score;
            for (var k = 1; k < best.Count; k++)
              if (best[k].score < worst)
              {
                worst = best[k].score;
                worstI = k;
              }
            if (score > worst) best[worstI] = (port, score);
          }
        }
      }
      foreach (var item in best) outList.Add(item.p);
    }

    private bool TryGetNavCorners(Vector3 a, Vector3 b, out List<Vector3> corners)
    {
      corners = _cornersPool;
      corners.Clear();
      var pf = ValheimPathfinding.instance;
      if (pf == null) return false;
      if (!pf.GetPath(a, b, corners, ValheimPathfinding.AgentType.HumanoidBig)) return false;
      if (corners.Count < 2) return false;
      return true;
    }
    private static float CornersLength(List<Vector3> cs)
    {
      float sum = 0;
      for (var i = 1; i < cs.Count; i++) sum += Vector3.Distance(cs[i - 1], cs[i]);
      return sum;
    }

    private readonly List<Vector3> _cornersPool = new(32);
    private static List<Vector3> _revTmp(List<Vector3> src)
    {
      var dst = new List<Vector3>(src.Count);
      for (var i = src.Count - 1; i >= 0; i--) dst.Add(src[i]);
      return dst;
    }

    private void RecalcBodyFromColliders()
    {
      float minY = float.PositiveInfinity, maxY = float.NegativeInfinity, r = 0.3f;
      var any = false;
      foreach (var c in _tr.GetComponentsInChildren<Collider>())
      {
        if (!c.enabled) continue;
        minY = Mathf.Min(minY, c.bounds.min.y);
        maxY = Mathf.Max(maxY, c.bounds.max.y);
        var e = c.bounds.extents;
        r = any ? Mathf.Max(r, Mathf.Min(e.x, e.z) * 0.65f) : Mathf.Min(e.x, e.z) * 0.65f;
        any = true;
      }
      if (any)
      {
        bodyHeight = Mathf.Clamp(maxY - minY, 1.0f, 3.2f);
        bodyRadius = Mathf.Clamp(r, 0.2f, 0.7f);
      }
    }

    #endregion

    #region A* over tiny graph

    private static bool AStar(List<Node> nodes,
      Dictionary<(int, int), (float, bool, PortalType?)> edges,
      int start, int goal, out List<int> route)
    {
      route = null;
      var open = new SortedList<float, int>(new DuplicateKeyComparer<float>());
      var g = new Dictionary<int, float>();
      var f = new Dictionary<int, float>();
      var prev = new Dictionary<int, int>();

      foreach (var n in nodes)
      {
        g[n.id] = float.PositiveInfinity;
        f[n.id] = float.PositiveInfinity;
      }
      g[start] = 0;
      f[start] = Heur(nodes, start, goal);
      open.Add(f[start], start);

      while (open.Count > 0)
      {
        var current = open.Values[0];
        open.RemoveAt(0);
        if (current == goal)
        {
          var outIds = new List<int>();
          var cur = goal;
          outIds.Add(cur);
          while (prev.TryGetValue(cur, out var p))
          {
            cur = p;
            outIds.Add(cur);
          }
          outIds.Reverse();
          route = outIds;
          return true;
        }

        // neighbors
        foreach (var kv in edges)
        {
          if (kv.Key.Item1 != current) continue;
          var neighbor = kv.Key.Item2;
          var cost = kv.Value.Item1;
          var alt = g[current] + cost;
          if (alt < g[neighbor])
          {
            g[neighbor] = alt;
            f[neighbor] = alt + Heur(nodes, neighbor, goal);
            prev[neighbor] = current;
            open.Add(f[neighbor], neighbor);
          }
        }
      }
      return false;
    }

    private static float Heur(List<Node> nodes, int a, int b)
    {
      var pa = nodes.Find(n => n.id == a)!.p;
      var pb = nodes.Find(n => n.id == b)!.p;
      var planar = Vector2.Distance(new Vector2(pa.x, pa.z), new Vector2(pb.x, pb.z));
      var yCost = Mathf.Abs(pa.y - pb.y) * 0.6f;
      return planar + yCost;
    }

    private sealed class DuplicateKeyComparer<T> : IComparer<T> where T : IComparable<T>
    {
      public int Compare(T x, T y)
      {
        var r = x.CompareTo(y);
        return r == 0 ? 1 : r;
      }
    }

    #endregion

    #region Drawing

    private static readonly Color WalkCol = new(0.2f, 1f, 1f); // cyan
    private static readonly Color MantleCol = new(1f, 1f, 0.2f); // yellow
    private static readonly Color IngressCol = new(1f, 0f, 1f); // magenta

    private void AddWalkSeg(PlannedPath path, List<Vector3> corners)
    {
      for (var i = 1; i < corners.Count; i++)
      {
        path.segs.Add((corners[i - 1], corners[i], WalkCol, true, null));
      }
    }

    private void Draw(PlannedPath plan)
    {
      if (plan == null) return;
      foreach (var s in plan.segs)
      {
        // walk: draw each segment; portal: draw single stroke
        Debug.DrawLine(s.a + Vector3.up * 0.05f, s.b + Vector3.up * 0.05f,
          s.c, 0, false);
        if (s.isWalk == false)
          Debug.DrawRay(s.b, Vector3.up * 0.2f, s.c, 0, false);
      }
    }

    #endregion

  }
}