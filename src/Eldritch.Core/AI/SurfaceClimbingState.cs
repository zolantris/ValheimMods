// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Core
{
  [Serializable]
  public class SurfaceClimbingState
  {
    [Header("Detection")]
    [SerializeField] private LayerMask climbMask = 0; // set in Init
    [SerializeField] private float nearWallDistance = 0.85f;
    [SerializeField] private float probeForward = 1.4f;
    [SerializeField] private float bodyRadius = 0.35f; // will be auto-tuned from colliders
    [SerializeField] private float minLedge = 0.4f;
    [SerializeField] private float maxLedge = 3.5f;
    [SerializeField] private float maxWallAngle = 25f; // reject sloped ground

    [Header("Traversal")]
    [SerializeField] private float climbUp = 2.2f;
    [SerializeField] private float stepOver = 0.30f;
    [SerializeField] private float climbDuration = 0.8f;
    [SerializeField] private float cooldownAfter = 1.0f;

    // state
    private MonoBehaviour _mb;
    private Rigidbody _rb;
    private Transform _tr;
    private CoroutineHandle _climbHandle;
    private float _nextClimbAllowedAt;
    private bool _gravityOverridden;

    public bool IsClimbing => _climbHandle is { IsRunning: true };

    [SerializeField] private float minAscent = 0.25f; // must end at least this much above current feet
    [SerializeField] private float underpassSearch = 3.0f; // how far we’ll try to slip under
    [SerializeField] private float clearanceHead = 0.1f; // extra headroom margin
    [SerializeField] private float crouchFactor = 0.75f; // allowed % of bodyHeight when passing under

    private CoroutineHandle _mantleHandle;
    private CoroutineHandle _underpassHandle;
    private float _bodyHeight = 1.8f;
    private float _bodyRadius = 0.35f;
    private bool _gravityOwned;

    public void Init(MonoBehaviour owner, Rigidbody rb, Transform root)
    {
      _mb = owner;
      _rb = rb;
      _tr = root;
      if (climbMask.value == 0) climbMask = LayerHelpers.GroundLayers;
      _mantleHandle = new CoroutineHandle(owner);
      _underpassHandle = new CoroutineHandle(owner);
      RecalcBodyMetrics();
    }

    public bool TryWallClimbWhenBlocked(Vector3 moveDir)
    {
      if (_mantleHandle.IsRunning || _underpassHandle.IsRunning) return false;
      if (Time.time < _nextClimbAllowedAt) return false;

      moveDir.y = 0;
      if (moveDir.sqrMagnitude < 1e-4f) return false;
      var fwd = moveDir.normalized;

      RecalcBodyMetrics();
      var feetY = GetFeetY();

      // Must be nose-to-wall and actually a wall, not a slope
      if (!IsNearClimbableWall(fwd, out var faceHit, out var wallN)) return false;

      // QUICK ceiling/overhang test: is the space right in front of HEAD blocked?
      var headPos = new Vector3(_tr.position.x, feetY + _bodyHeight, _tr.position.z);
      var headBlocked = Physics.SphereCast(headPos, _bodyRadius * 0.8f, fwd, out _, 0.6f, climbMask, QueryTriggerInteraction.Ignore);

      // Plan a mantle first
      if (TryPlanMantle(faceHit, wallN, feetY, out var mantleEnd))
      {
        // Only accept if it truly goes UP (prevents "sinking")
        if (mantleEnd.y >= feetY + minAscent && !headBlocked)
        {
          _mantleHandle.Start(TraverseMantle(_tr.position, mantleEnd, wallN), true);
          _nextClimbAllowedAt = Time.time + cooldownAfter;
          return true;
        }
      }

      // If mantle is invalid or we have an overhang in front of the head, attempt underpass
      if (TryFindUnderpass(fwd, feetY, out var passEnd))
      {
        _underpassHandle.Start(TraverseUnderpass(passEnd), true);
        _nextClimbAllowedAt = Time.time + 0.35f;
        return true;
      }

      return false;
    }

    private bool TryFindUnderpass(Vector3 forward, float feetY, out Vector3 passEnd)
    {
      passEnd = default;

      // We’ll accept a crouched height
      var minHeadroom = Mathf.Max(_bodyHeight * crouchFactor, _bodyHeight * 0.6f);

      var step = 0.25f;
      for (var d = 0.5f; d <= underpassSearch; d += step)
      {
        var probe = _tr.position + forward * d;

        // 1) there must be ground under the probe
        if (!RaycastDown(probe + Vector3.up * 1.5f, out var ground)) continue;

        // 2) vertical clearance between ground and ceiling >= minHeadroom
        // Find ceiling directly above ground (or above our head, whichever is lower)
        var ceilingFrom = ground + Vector3.up * (_bodyHeight + clearanceHead);
        if (Physics.Raycast(ceilingFrom, Vector3.up, out var upHit, 0.5f, climbMask, QueryTriggerInteraction.Ignore))
          continue; // something pokes into our headroom from below – reject

        // Find the nearest ceiling down from a higher point (to handle thick overhangs)
        var ceilingRayTop = ground + Vector3.up * (minHeadroom + 1.0f);
        if (!Physics.Raycast(ceilingRayTop, Vector3.down, out var ceilingHit, minHeadroom + 1.0f, climbMask, QueryTriggerInteraction.Ignore))
          continue;

        var clearance = ceilingHit.point.y - ground.y;
        if (clearance < minHeadroom) continue;

        // 3) forward capsule path to this point must be free for a crouched capsule
        if (CapsuleBlockedAlong(_tr.position, new Vector3(probe.x, ground.y + _bodyHeight * 0.5f, probe.z), _bodyRadius * 0.9f))
          continue;

        passEnd = new Vector3(probe.x, ground.y, probe.z);
        return true;
      }

      return false;
    }
    private IEnumerator TraverseMantle(Vector3 startPos, Vector3 endPos, Vector3 wallNormal)
    {
      var savedUseGravity = _rb.useGravity;
      var savedMode = _rb.collisionDetectionMode;
      _rb.useGravity = false;
      _gravityOwned = true;
      _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
      _rb.velocity = Vector3.zero;

      var startRot = _tr.rotation;
      var faceWall = Quaternion.LookRotation(-wallNormal, Vector3.up);

      var apex = Vector3.Lerp(startPos, endPos, 0.35f) + Vector3.up * Mathf.Min(0.8f, _bodyHeight * 0.35f);
      var t = 0f;
      var dur = Mathf.Max(0.05f, climbDuration);

      while (t < 1f)
      {
        t += Time.fixedDeltaTime / dur;
        var ab = Vector3.Lerp(startPos, apex, t);
        var bc = Vector3.Lerp(apex, endPos, t);
        var p = Vector3.Lerp(ab, bc, t);

        _rb.MovePosition(p);
        _rb.MoveRotation(Quaternion.Slerp(startRot, faceWall, Mathf.SmoothStep(0, 1, t * 0.8f)));
        _rb.velocity = Vector3.zero;
        yield return new WaitForFixedUpdate();
      }

      _rb.MovePosition(endPos + Vector3.up * 0.02f);
      _rb.velocity = Vector3.zero;
      yield return new WaitForFixedUpdate();

      _rb.useGravity = savedUseGravity;
      _gravityOwned = false;
      _rb.collisionDetectionMode = savedMode;
    }

    private void RecalcBodyMetrics()
    {
      // height: feet to head
      var feetY = GetFeetY();
      var maxY = feetY + 1.8f;
      foreach (var c in _tr.GetComponentsInChildren<Collider>())
      {
        if (!c.enabled) continue;
        maxY = Mathf.Max(maxY, c.bounds.max.y);
      }
      _bodyHeight = Mathf.Clamp(maxY - feetY, 1.0f, 3.2f);

      // radius: conservative from colliders
      var r = 0.3f;
      var any = false;
      foreach (var c in _tr.GetComponentsInChildren<Collider>())
      {
        if (!c.enabled) continue;
        var e = c.bounds.extents;
        var guess = Mathf.Max(0.2f, Mathf.Min(e.x, e.z) * 0.65f);
        r = any ? Mathf.Max(r, guess) : guess;
        any = true;
      }
      _bodyRadius = Mathf.Clamp(any ? r : _bodyRadius, 0.2f, 0.7f);
    }

    private IEnumerator TraverseUnderpass(Vector3 passEnd)
    {
      // keep gravity ON; just guide forward on ground to the passEnd
      var dur = Mathf.Clamp(underpassSearch / 2f, 0.25f, 1.2f);
      var t = 0f;
      var start = _tr.position;

      while (t < 1f)
      {
        t += Time.fixedDeltaTime / dur;
        var along = Vector3.Lerp(start, passEnd, t);

        // Snap to ground each step so we never sink or hover
        if (RaycastDown(along + Vector3.up * 1.5f, out var onGround))
          along = onGround;

        _rb.MovePosition(along);
        yield return new WaitForFixedUpdate();
      }
    }

    private bool CapsuleBlockedAlong(Vector3 from, Vector3 to, float radius)
    {
      var dir = to - from;
      var len = dir.magnitude;
      if (len < 1e-3f) return false;
      dir /= len;

      var p0 = new Vector3(from.x, from.y + radius, from.z);
      var p1 = new Vector3(from.x, from.y + radius + _bodyHeight * crouchFactor, from.z);
      return Physics.CapsuleCast(p0, p1, radius, dir, out _, len, climbMask, QueryTriggerInteraction.Ignore);
    }

    private bool TryPlanMantle(RaycastHit faceHit, Vector3 wallNormal, float feetY, out Vector3 landing)
    {
      landing = default;

      // Take a step "over" the ledge and cast down
      var stepOff = faceHit.point + Vector3.up * Mathf.Min(climbUp, maxLedge) + wallNormal * stepOver;
      if (!RaycastDown(stepOff + Vector3.up * 0.25f, out var down))
        return false;

      // Reject if landing is below current feet (prevents sinking)
      if (down.y < feetY + minAscent)
        return false;

      landing = down;
      return true;
    }

    // -------- detection helpers --------
    private bool IsNearClimbableWall(Vector3 forward, out RaycastHit hit, out Vector3 wallN)
    {
      hit = default;
      wallN = Vector3.zero;

      // chest-ish sample, but root-agnostic
      var feetY = GetFeetY();
      var chest = new Vector3(_tr.position.x, feetY + Mathf.Max(1.0f, bodyRadius * 3f), _tr.position.z);

      // gate 1: must be very close to something in front
      if (!Physics.CapsuleCast(chest, chest + Vector3.up * 0.01f, bodyRadius * 0.9f, forward, out hit, nearWallDistance, climbMask, QueryTriggerInteraction.Ignore))
        return false;

      // gate 2: confirm a real wall (not floor slope)
      wallN = hit.normal;
      var wallSlopeFromUp = Vector3.Angle(wallN, Vector3.up);
      var isWall = wallSlopeFromUp > 90f - maxWallAngle; // near vertical
      if (!isWall) return false;

      // gate 3: make sure there IS still wall slightly farther out (prevents spam)
      var secondCheckOrigin = chest + forward * Mathf.Min(hit.distance + 0.1f, probeForward);
      if (!Physics.SphereCast(secondCheckOrigin + Vector3.up * 0.2f, bodyRadius * 0.9f, -wallN, out _, 0.25f, climbMask, QueryTriggerInteraction.Ignore))
        return false;

      return true;
    }

    private bool RaycastDown(Vector3 from, out Vector3 point)
    {
      if (Physics.Raycast(from, Vector3.down, out var downHit, 4f, climbMask, QueryTriggerInteraction.Ignore))
      {
        // reject steep landings
        if (Vector3.Angle(downHit.normal, Vector3.up) <= 45f)
        {
          point = downHit.point + Vector3.up * 0.02f;
          return true;
        }
      }
      point = default;
      return false;
    }

    private float GetFeetY()
    {
      var minY = float.PositiveInfinity;
      foreach (var c in _tr.GetComponentsInChildren<Collider>())
      {
        if (!c.enabled) continue;
        minY = Mathf.Min(minY, c.bounds.min.y);
      }
      return float.IsInfinity(minY) ? _tr.position.y : minY;
    }

    private void RecalcBodyRadius()
    {
      var r = 0.3f;
      var any = false;
      foreach (var c in _tr.GetComponentsInChildren<Collider>())
      {
        if (!c.enabled) continue;
        var e = c.bounds.extents;
        var guess = Mathf.Max(Mathf.Min(e.x, e.z) * 0.8f, 0.2f);
        r = any ? Mathf.Max(r, guess) : guess;
        any = true;
      }
      bodyRadius = Mathf.Clamp(any ? r : bodyRadius, 0.2f, 0.7f);
    }

    // -------- traversal (FixedUpdate cadence, physics-friendly) --------
    private IEnumerator TraverseClimb(Vector3 startPos, Vector3 endPos, Vector3 wallNormal)
    {
      // exclusive: zero out motor while we climb
      var savedUseGravity = _rb.useGravity;
      var savedMode = _rb.collisionDetectionMode;

      _rb.useGravity = false;
      _gravityOverridden = true;
      _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
      _rb.velocity = Vector3.zero;
      _rb.isKinematic = true;

      var startRot = _tr.rotation;
      var faceWall = Quaternion.LookRotation(-wallNormal, Vector3.up);
      var apex = Vector3.Lerp(startPos, endPos, 0.35f) + Vector3.up * Mathf.Min(0.8f, (endPos - startPos).magnitude * 0.25f);

      var t = 0f;
      var dur = Mathf.Max(0.05f, climbDuration);
      while (t < 1f)
      {
        t += Time.fixedDeltaTime / dur;

        // quadratic bezier
        var ab = Vector3.Lerp(startPos, apex, t);
        var bc = Vector3.Lerp(apex, endPos, t);
        var p = Vector3.Lerp(ab, bc, t);

        _rb.MovePosition(p);
        _rb.MoveRotation(Quaternion.Slerp(startRot, faceWall, Mathf.SmoothStep(0, 1, t * 0.8f)));
        _rb.velocity = Vector3.zero;

        yield return new WaitForFixedUpdate();
      }

      // small settle and restore physics
      _rb.MovePosition(endPos);
      _rb.velocity = Vector3.zero;
      yield return new WaitForFixedUpdate();

      _rb.isKinematic = false;
      _rb.useGravity = savedUseGravity;
      _gravityOverridden = false;
      _rb.collisionDetectionMode = savedMode;
    }

    // Allow the motor to know if gravity is currently overridden by a climb
    public bool IsGravityOverridden()
    {
      return _gravityOverridden;
    }
  }
}