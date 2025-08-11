// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Core.Abilities
{
  public class DodgeAbility
  {
    public DodgeAbilityConfig config;
    private readonly MonoBehaviour _host;
    private readonly Transform _self;
    private readonly Rigidbody _rb;

    private readonly CoroutineHandle _routineHandler;
    private float _cooldownReadyAt;

    // Cached estimate of our horizontal half-extent (radius on XZ from COM to farthest collider corner)
    private float _selfHalfExtentXZ = -1f;

    public bool IsDodging => _routineHandler.IsRunning;
    public bool CanDodge => Time.time >= _cooldownReadyAt && !IsDodging;

    public DodgeAbility(MonoBehaviour host, DodgeAbilityConfig cfg, Transform self, Rigidbody rb)
    {
      _routineHandler = new CoroutineHandle(host);
      _host = host;
      config = cfg;
      _self = self;
      _rb = rb;

      // Cache our extent once; call RecomputeSelfExtent() later if your hull changes
      RecomputeSelfExtent();
    }

    /// Call if your collider setup changes at runtime (e.g., enabling/disabling hulls).
    public void RecomputeSelfExtent()
    {
      _selfHalfExtentXZ = ComputeSelfHalfExtentXZ(_rb);
    }

    private static Vector3 GetTargetCenter(Rigidbody targetRb, Collider[] colsIfNoRb, Vector3 fallbackNear)
    {
      if (targetRb != null) return targetRb.worldCenterOfMass;

      if (colsIfNoRb != null && colsIfNoRb.Length > 0)
      {
        var b = new Bounds(colsIfNoRb[0].bounds.center, Vector3.zero);
        for (var i = 0; i < colsIfNoRb.Length; i++)
        {
          var c = colsIfNoRb[i];
          if (!c || !c.enabled) continue;
          b.Encapsulate(c.bounds);
        }
        return b.center;
      }

      return fallbackNear; // last resort
    }

    // -------------------- Public API --------------------

    public bool TryDodge(Vector2 input)
    {
      if (!CanDodge) return false;

      Vector3 dir;
      float dist;

      if (Mathf.Abs(input.y) >= Mathf.Abs(input.x))
      {
        var sign = Mathf.Sign(input.y);
        dir = sign > 0f ? _self.forward : -_self.forward;
        dist = sign > 0f ? config.forwardDistance : config.backwardDistance;
      }
      else
      {
        var sign = Mathf.Sign(input.x);
        dir = sign > 0f ? _self.right : -_self.right;
        dist = config.sideDistance;
      }

      var start = _rb.position;
      var end = start + dir.normalized * dist;
      return StartDodge(start, end, config.dodgeDuration, config.jumpHeight);
    }

    /// <summary>
    /// Leap toward a target RB (fast path) or its colliders (fallback) and land with a safe gap.
    /// Caller passes ONLY our Rigidbody; we use cached self extent.
    /// </summary>
    public bool TryLeapAt(
      Rigidbody targetRb,
      Rigidbody selfRb)
    {
      if (!CanDodge || selfRb == null) return false;

      var from = selfRb.position;

      // Fast closest point for landing offset math
      var targetClosest = targetRb.ClosestPointOnBounds(from);
      var fromClosest = selfRb.ClosestPointOnBounds(targetClosest);

      var toEnemy = targetClosest - fromClosest;
      toEnemy.y = 0f;
      if (toEnemy.sqrMagnitude < 1e-6f) return false; // we’re literally on top; let caller decide

      var toward = toEnemy.normalized;

      var distance = Vector3.Distance(fromClosest, targetClosest);

      // Travel strictly forward toward enemy, stopping short of its bounds by "backoff"
      var travel = Mathf.Min(config.forwardDistance, distance);
      var end = from + toward * Mathf.Max(0.05f, travel);
      end.y = selfRb.position.y;

      // Safety: avoid landing inside level geo
      var probe = Mathf.Max(0.05f, config.landingClearanceProbe);
      if (Physics.CheckSphere(end, probe, LayerHelpers.GroundLayers, QueryTriggerInteraction.Ignore))
      {
        end -= toward * 0.25f;
      }

      return StartDodge(from, end, config.dodgeDuration, Mathf.Max(config.jumpHeight, 0.6f));
    }

    /// <summary>
    /// Leap toward a world-space point; stop short so our bounds don’t collide with that point.
    /// </summary>
    public bool TryLeapAt(Vector3 targetPoint, Rigidbody selfRb, float? minGapOverride = null)
    {
      if (!CanDodge || selfRb == null) return false;

      var from = selfRb.position;
      var dir = targetPoint - from;
      dir.y = 0f;
      if (dir.sqrMagnitude < 1e-6f) return false;

      var toward = dir.normalized;
      var halfExtent = _selfHalfExtentXZ > 0f ? _selfHalfExtentXZ : 0.4f;
      var gap = Mathf.Max(0.05f, minGapOverride ?? config.minGapFromTarget);

      var landingCenter = targetPoint - toward * (halfExtent + gap);
      landingCenter.y = selfRb.position.y;

      var probe = Mathf.Max(0.05f, config.landingClearanceProbe);
      if (Physics.CheckSphere(landingCenter, probe, Physics.AllLayers, QueryTriggerInteraction.Ignore))
      {
        landingCenter -= toward * 0.25f;
      }

      var toLanding = landingCenter - from;
      var maxDist = Mathf.Max(0.1f, config.forwardDistance);
      var end = toLanding.magnitude > maxDist
        ? from + toLanding.normalized * maxDist
        : landingCenter;

      return StartDodge(from, end, config.dodgeDuration, Mathf.Max(config.jumpHeight, 0.6f));
    }

    // -------------------- Core motion --------------------

    private bool StartDodge(Vector3 start, Vector3 end, float duration, float apexHeight)
    {
      if (!CanDodge) return false;
      _routineHandler.Start(DodgeRoutine(start, end, duration, apexHeight));
      _cooldownReadyAt = Time.time + Mathf.Max(config.cooldown, duration);
      return true;
    }

    private IEnumerator DodgeRoutine(Vector3 start, Vector3 end, float duration, float apexHeight)
    {
      var t0 = Time.time;
      var t1 = t0 + Mathf.Max(0.01f, duration);

      var wasKinematic = _rb.isKinematic;
      _rb.isKinematic = true;

      while (Time.time < t1)
      {
        var t = Mathf.InverseLerp(t0, t1, Time.time);
        var pos = Vector3.Lerp(start, end, t);
        pos.y += Mathf.Sin(t * Mathf.PI) * apexHeight;
        _rb.MovePosition(pos);
        yield return new WaitForFixedUpdate();
      }

      _rb.MovePosition(end);
      _rb.isKinematic = wasKinematic;
    }

    // -------------------- Helpers --------------------

    private static Vector3 ClosestPointOnTarget(
      Rigidbody targetRb,
      Collider[] targetColsIfNoRb,
      Vector3 to,
      bool preferRigidBodyBounds)
    {
      if (targetRb != null)
      {
        if (preferRigidBodyBounds)
        {
          // Fast: AABB-based closest point of the RB's attached colliders
          return targetRb.ClosestPointOnBounds(to);
        }

        // Accurate: iterate child colliders (allocation happens only when leap is triggered)
        var cols = targetRb.GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
          var best = to;
          var bestDist = float.PositiveInfinity;
          for (var i = 0; i < cols.Length; i++)
          {
            var c = cols[i];
            if (!c || !c.enabled) continue;
            var p = c.ClosestPoint(to);
            var d = (p - to).sqrMagnitude;
            if (d < bestDist)
            {
              bestDist = d;
              best = p;
            }
          }
          return best;
        }
      }

      // No Rigidbody: use provided colliders (only gathered by caller if RB is missing)
      if (targetColsIfNoRb != null && targetColsIfNoRb.Length > 0)
      {
        var best = to;
        var bestDist = float.PositiveInfinity;
        for (var i = 0; i < targetColsIfNoRb.Length; i++)
        {
          var c = targetColsIfNoRb[i];
          if (!c || !c.enabled) continue;
          var p = c.ClosestPoint(to);
          var d = (p - to).sqrMagnitude;
          if (d < bestDist)
          {
            bestDist = d;
            best = p;
          }
        }
        return best;
      }

      return to;
    }

    /// <summary>
    /// Conservative radius on XZ plane from COM to farthest collider corner.
    /// Computed once to avoid per-leap collider scans.
    /// </summary>
    private static float ComputeSelfHalfExtentXZ(Rigidbody selfRb)
    {
      if (selfRb == null) return 0.4f;

      var cols = selfRb.GetComponentsInChildren<Collider>();
      if (cols == null || cols.Length == 0) return 0.4f;

      var com = selfRb.worldCenterOfMass;
      var maxRadius = 0f;

      for (var i = 0; i < cols.Length; i++)
      {
        var c = cols[i];
        if (!c || !c.enabled) continue;

        var b = c.bounds;
        var min = b.min;
        var max = b.max;

        // project all 8 AABB corners on XZ; take farthest from COM
        for (var xi = 0; xi < 2; xi++)
        for (var yi = 0; yi < 2; yi++)
        for (var zi = 0; zi < 2; zi++)
        {
          var corner = new Vector3(
            xi == 0 ? min.x : max.x,
            yi == 0 ? min.y : max.y,
            zi == 0 ? min.z : max.z);

          var v = corner - com;
          v.y = 0f;
          var r = v.magnitude;
          if (r > maxRadius) maxRadius = r;
        }
      }

      // Floor to something reasonable to avoid underestimation
      return Mathf.Max(0.25f, maxRadius);
    }
  }
}