using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class DodgeAbility
  {
    private readonly CoroutineHandle _dodgeCoroutine;
    private readonly Transform _owner;
    private readonly Rigidbody _rb;
    public readonly DodgeAbilityConfig config;
    private float _lastDodgeTime = -Mathf.Infinity;
    private bool _prevIsKinematic;
    private bool _prevUseGravity;

    private Vector3 _start, _end;

    public DodgeAbility(MonoBehaviour monoBehaviour, DodgeAbilityConfig config, Transform owner, Rigidbody rb)
    {
      if (rb == null || monoBehaviour == null || owner == null)
      {
        throw new Exception($"Invalid monoBehaviour {monoBehaviour} or owner object {owner}, or rigidbody {rb}");
      }

      this.config = config;
      _owner = owner;
      _rb = rb;
      _dodgeCoroutine = new CoroutineHandle(monoBehaviour);
    }

    public bool CanDodge => !IsDodging && !_rb.isKinematic && Time.time > _lastDodgeTime + config.cooldown;

    public bool IsDodging => _dodgeCoroutine.IsRunning;

    public bool TryDodge(Vector2 input, Action onDodgeComplete = null)
    {
      if (!CanDodge || input == Vector2.zero)
        return false;

      var worldDir = _owner.right * input.x + _owner.forward * input.y;
      if (worldDir.sqrMagnitude > 0.01f)
        worldDir.Normalize();

      var angle = Vector3.SignedAngle(_owner.forward, worldDir, Vector3.up);
      var dist = config.sideDistance;
      if (Mathf.Abs(angle) < 45f)
        dist = config.forwardDistance;
      else if (Mathf.Abs(angle) > 135f)
        dist = config.backwardDistance;

      _start = _owner.position;
      _end = _start + worldDir * dist;

      // Draw full arc
      var arcSegments = 20;
      var prevPoint = _start;
      for (var i = 1; i <= arcSegments; i++)
      {
        var t = i / (float)arcSegments;
        var arc = Mathf.Sin(Mathf.PI * t) * config.jumpHeight;
        var point = Vector3.Lerp(_start, _end, t);
        point.y += arc;
        Debug.DrawLine(prevPoint, point, Color.yellow, 10.0f);
        prevPoint = point;
      }

      _lastDodgeTime = Time.time;
      Debug.Log(
        $"[Dodge] input: {input}, worldDir: {worldDir}, forward: {_owner.forward}, right: {_owner.right}, angle: {angle}, dist: {dist}, _start: {_start}, _end: {_end}"
      );

      Debug.DrawRay(_start, _owner.forward * 2, Color.blue, 10.0f); // forward
      Debug.DrawRay(_start, _owner.right * 2, Color.red, 10.0f); // right
      Debug.DrawRay(_start, worldDir * dist, Color.yellow, 10.0f); // intended dodge

      // Debug visual
      Debug.DrawLine(_start, _end, Color.cyan, 10.0f);
      Debug.DrawRay(_start, Vector3.up * 0.5f, Color.green, 10.0f);
      Debug.DrawRay(_end, Vector3.up * 0.5f, Color.red, 10.0f);

      _dodgeCoroutine.Start(DodgeRoutine(_start, _end, onDodgeComplete));
      return true;
    }

    private IEnumerator DodgeRoutine(Vector3 start, Vector3 end, [CanBeNull] Action onDodgeComplete = null)
    {
      _prevUseGravity = _rb.useGravity;
      _prevIsKinematic = _rb.isKinematic;
      _rb.useGravity = false;
      _rb.isKinematic = true;

      var elapsed = 0f;
      var duration = config.dodgeDuration;

      while (elapsed < duration)
      {
        var t = elapsed / duration;
        var arc = Mathf.Sin(Mathf.PI * t) * config.jumpHeight;
        var basePos = Vector3.Lerp(start, end, t);
        basePos.y += arc;
        _rb.MovePosition(basePos);
        elapsed += Time.deltaTime;
        yield return null;
      }
      _rb.MovePosition(end);
      _rb.rotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);

      // Restore Rigidbody state
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
      _rb.velocity = Vector3.zero;

      // must zero out velocity and then do it again on fixed update to prevent any bounce when dodging.
      yield return new WaitForFixedUpdate();
      _rb.rotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);
      _rb.velocity = Vector3.zero;
      onDodgeComplete?.Invoke();
    }

    public void CancelDodge()
    {
      _dodgeCoroutine.Stop();
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
    }
    
    /// <summary>
    /// Leap toward a target RB/colliders and land with a safe gap so we don't collide with the target.
    /// </summary>
    public bool TryLeapAt(
      Rigidbody targetRb,
      IEnumerable<Collider> targetColliders,
      Rigidbody selfRb,
      IEnumerable<Collider> selfColliders,
      float? minGapOverride = null)
    {
      if (!CanDodge || selfRb == null) return false;

      // 1) Find the point on the target hull nearest to us
      var from = selfRb.position;
      var enemyClosest = ClosestPointOn(targetRb, targetColliders, from);

      // 2) Direction of travel toward the target on XZ
      var dir = enemyClosest - from;
      dir.y = 0f;
      if (dir.sqrMagnitude < 1e-6f) dir = _rb.transform.forward; // fallback
      var toward = dir.normalized;

      // 3) Estimate our half-extent along the travel direction, so our bounds don't overlap at landing
      var halfExtent = EstimateHalfExtentAlong(selfRb, selfColliders, toward);

      // 4) Choose landing center so that (our bounds) end up just in front of the enemy’s bounds
      var gap = Mathf.Max(0.05f, minGapOverride ?? (config?.minGapFromTarget ?? 0.35f));
      var landingCenter = enemyClosest - toward * (halfExtent + gap);

      // Keep height roughly level with current body center (tune if you prefer ground plane)
      landingCenter.y = selfRb.position.y;

      // 5) Safety: ensure we’re not trying to land inside static geometry; small nudge if occupied
      var probe = Mathf.Max(0.05f, config?.landingClearanceProbe ?? 0.4f);
      if (Physics.CheckSphere(landingCenter, probe, Physics.AllLayers, QueryTriggerInteraction.Ignore))
      {
        landingCenter -= toward * 0.25f; // nudge back slightly
      }

      // 6) Clamp leap distance to your forward dodge distance (short, snappy hop)
      var toLanding = landingCenter - from;
      var maxDist = Mathf.Max(0.1f, config.forwardDistance);
      var end = (toLanding.magnitude > maxDist)
        ? from + toLanding.normalized * maxDist
        : landingCenter;

      // 7) Fire the leap using your existing dodge runner (parabolic step)
      return StartDodge(from, end, config.dodgeDuration, Mathf.Max(config.jumpHeight, 0.6f));
    }

    /// <summary>
    /// Leap toward a world-space point and stop short so our bounds do not collide with that point’s hull.
    /// Useful if you already computed a "contact point" externally.
    /// </summary>
    public bool TryLeapAt(
      Vector3 targetPoint,
      Rigidbody selfRb,
      IEnumerable<Collider> selfColliders,
      float? minGapOverride = null)
    {
      if (!CanDodge || selfRb == null) return false;

      var from = selfRb.position;
      var dir = targetPoint - from;
      dir.y = 0f;
      if (dir.sqrMagnitude < 1e-6f) return false;
      var toward = dir.normalized;

      var halfExtent = EstimateHalfExtentAlong(selfRb, selfColliders, toward);
      var gap = Mathf.Max(0.05f, minGapOverride ?? (config?.minGapFromTarget ?? 0.35f));

      var landingCenter = targetPoint - toward * (halfExtent + gap);
      landingCenter.y = selfRb.position.y;

      var probe = Mathf.Max(0.05f, config?.landingClearanceProbe ?? 0.4f);
      if (Physics.CheckSphere(landingCenter, probe, Physics.AllLayers, QueryTriggerInteraction.Ignore))
      {
        landingCenter -= toward * 0.25f;
      }

      var toLanding = landingCenter - from;
      var maxDist = Mathf.Max(0.1f, config.forwardDistance);
      var end = (toLanding.magnitude > maxDist)
        ? from + toLanding.normalized * maxDist
        : landingCenter;

      return StartDodge(from, end, config.dodgeDuration, Mathf.Max(config.jumpHeight, 0.6f));
    }

    // ---------- helpers (safe to share with existing class) ----------

    private static Vector3 ClosestPointOn(Rigidbody rb, IEnumerable<Collider> cols, Vector3 to)
    {
      if (rb) return rb.ClosestPointOnBounds(to);
      var best = to;
      var bestDist = float.PositiveInfinity;
      if (cols != null)
      {
        foreach (var c in cols)
        {
          if (!c || !c.enabled) continue;
          var p = c.ClosestPoint(to);
          var d = (p - to).sqrMagnitude;
          if (d < bestDist) { bestDist = d; best = p; }
        }
      }
      return best;
    }

    private static float EstimateHalfExtentAlong(Rigidbody rb, IEnumerable<Collider> cols, Vector3 dir)
    {
      dir.y = 0f;
      if (dir.sqrMagnitude < 1e-6f) return 0.4f;
      dir.Normalize();

      var origin = rb ? rb.worldCenterOfMass : Vector3.zero;
      var minProj = float.PositiveInfinity;
      var maxProj = float.NegativeInfinity;
      var any = false;

      if (cols != null)
      {
        foreach (var c in cols)
        {
          if (!c || !c.enabled) continue;
          any = true;
          var b = c.bounds;
          var min = b.min; var max = b.max;

          // Project the 8 AABB corners along dir for a cheap directional size estimate
          for (int xi = 0; xi < 2; xi++)
          for (int yi = 0; yi < 2; yi++)
          for (int zi = 0; zi < 2; zi++)
          {
            var p = new Vector3(
              xi == 0 ? min.x : max.x,
              yi == 0 ? min.y : max.y,
              zi == 0 ? min.z : max.z);
            var v = p - origin;
            v.y = 0f;
            var proj = Vector3.Dot(v, dir);
            if (proj < minProj) minProj = proj;
            if (proj > maxProj) maxProj = proj;
          }
        }
      }

      if (!any) return 0.4f;
      return Mathf.Max(0.25f, (maxProj - minProj) * 0.5f);
    }
  }
}