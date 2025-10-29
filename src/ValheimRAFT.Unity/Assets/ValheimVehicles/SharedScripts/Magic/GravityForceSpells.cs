#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using JetBrains.Annotations;
  using UnityEngine;
  using Zolantris.Shared;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.Magic
  {
    /// <summary>
    /// Force / Gravity combat spells with:
    /// - Camera-center aim → player-to-aim sphere path targeting
    /// - Self-collider & self-rigidbody ignore
    /// - Gizmo visualization for aim/path + stop ring
    /// - Pull (hold): persistent lock set; PD-freeze at stop ring regardless of camera direction
    /// </summary>
    public class GravityForceSpells : MonoBehaviour
    {
      // ---------- Aim (Camera Center) ----------
      [Header("Aim")]
      [SerializeField] private float aimRayMaxDistance = 60f;
      [SerializeField] private float aimSphereRadius = 1.0f;
      [SerializeField] private LayerMask aimRayMask = ~0;
      [SerializeField] private LayerMask targetMask = ~0;
      [SerializeField] private QueryTriggerInteraction triggerQuery = QueryTriggerInteraction.Ignore;
      [SerializeField] private float playerAimHeightOffset = 1.0f;

      // ---------- Common ----------
      [Header("Common")]
      // todo make this much lower but for testing 5000f allows lifting boats.
      [SerializeField] private float maxAffectMass = 5000f;
      [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
      [SerializeField] private float lineOfSightPadding = 0.25f;

      // ---------- Spells ----------
      [Header("Blast (Q)")]
      [SerializeField] private float blastRadiusAtHit = 6f;
      [SerializeField] private float blastForce = 4000f;

      [Header("Hold (F)")]
      [SerializeField] private float holdForce = 500f;
      [SerializeField] private float holdDuration = 5.0f;

      [Header("Crush (C)")]
      [SerializeField] private float crushDownForce = 2800f;
      [SerializeField] private float crushStunSeconds = 1.0f;

      [Header("Pull (E - HOLD)")]
      [Tooltip("Base pull strength when far from stop ring.")]
      [SerializeField] private float pullForce = 1500f;

      [Tooltip("Extra clearance beyond player's max bounds radius where pull stops (ring radius offset).")]
      [SerializeField] private float pullStopBuffer = 0.5f;

      [Tooltip("Outer band beyond stop ring where pull ramps down before lock (for smooth arrival).")]
      [SerializeField] private float pullSoftZone = 2.0f;

      [Tooltip("PD spring strength to 'freeze' at the ring while held.")]
      [SerializeField] private float pullHoldKp = 40f;

      [Tooltip("PD damping to quickly bleed velocity near/at the ring while held.")]
      [SerializeField] private float pullHoldKd = 12f;

      [Tooltip("Clamp for PD acceleration (m/s^2) to prevent wild kicks.")]
      [SerializeField] private float pullMaxAccel = 60f;

      [Tooltip("Max number of targets to keep locked while pulling.")]
      [SerializeField] private int pullMaxLockedTargets = 6;

      [Tooltip("If a locked target gets farther than this from the player center, it is dropped. 0 = unlimited.")]
      [SerializeField] private float pullDropDistance = 0f;

      [Header("Push (R)")]
      [SerializeField] private float pushForce = 2000f;

      [Header("Throw (T)")]
      [SerializeField] private float throwUpForce = 2200f;

      // ---------- Private / Self ignore ----------
      private readonly List<Rigidbody> _buffer = new();
      private readonly HashSet<Rigidbody> _activePullSet = new();
      private readonly List<Rigidbody> _activeScratch = new(); // temp for safe iteration
      private Camera _cam;

      public HashSet<Collider> selfColliders = new();
      public Rigidbody body;

      private bool _pullHeldLast;

      private void Awake()
      {
        body = GetComponent<Rigidbody>();
        var cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) selfColliders.Add(c);
        _cam = GetGameCamera();

#if VALHEIM
        aimRayMask = LayerHelpers.PhysicalLayerMask;
        targetMask = LayerHelpers.PhysicalLayerMask;
#endif
      }

      private void Update()
      {
        if (GetBlastPressed()) ForceBlast();
        if (GetHoldPressed()) ForceHold();
        if (GetCrushPressed()) ForceCrush();

        // Pull: persistent while held
        var pulling = GetPullHeld();
        if (pulling) ForcePull();
        if (!pulling && _pullHeldLast)
        {
          // releasing pull clears the lock set; remaining momentum can carry them into the player
          _activePullSet.Clear();
        }
        _pullHeldLast = pulling;

        if (GetPushPressed()) ForcePush();
        if (GetThrowPressed()) ForceThrow();
      }

      // =====================================================================
      // Spells
      // =====================================================================

      public void ForceBlast()
      {
        if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var dist)) return;

        GatherAlongPath(playerPos, dir, dist, aimSphereRadius, _buffer);
        AddSplashAtPoint(aimPoint, blastRadiusAtHit, _buffer);

        foreach (var rb in _buffer)
        {
          if (!rb) continue;
          var from = aimPoint;
          var dirOut = (rb.worldCenterOfMass - from).normalized;
          rb.AddForce(dirOut * blastForce, forceMode);
        }
      }

      public void ForceHold()
      {
        if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var dist)) return;

        GatherAlongPath(playerPos, dir, dist, aimSphereRadius, _buffer);
        StartCoroutine(HoldRoutine(_buffer, playerPos, aimPoint, holdDuration));
      }

      public void ForceCrush()
      {
        if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var dist)) return;

        GatherAlongPath(playerPos, dir, dist, aimSphereRadius, _buffer);
        foreach (var rb in _buffer)
        {
          if (!rb) continue;
          rb.AddForce(Vector3.down * crushDownForce, forceMode);
        }
        OnCrushApplied(_buffer, crushStunSeconds);
      }

      /// <summary>
      /// PULL (hold): while held, add bodies along current path into a persistent lock set
      /// and PD-hold all locked bodies at the stop ring, regardless of where the camera points.
      /// </summary>
      public void ForcePull()
      {
        // 1) Optionally add newly aimed bodies to the lock set
        if (TryGetAimPath(out var playerPos, out var aimPoint, out var dirCam, out var distPath))
        {
          GatherAlongPath(playerPos, dirCam, distPath, aimSphereRadius, _buffer);
          AddToActivePullSet(_buffer);
        }
        else
        {
          playerPos = transform.position + Vector3.up * playerAimHeightOffset; // fallback for center/ring math
        }

        // 2) Ring math
        var playerCenter = GetPlayerCenter();
        var stopRadius = GetPlayerStopDistance();
        var ringOuter = stopRadius + pullSoftZone;

        // 3) Apply forces to ALL locked bodies, even if no longer aimed at
        _activeScratch.Clear();
        _activeScratch.AddRange(_activePullSet);

        foreach (var rb in _activeScratch)
        {
          if (!rb || rb.isKinematic)
          {
            _activePullSet.Remove(rb);
            continue;
          }

          // Drop by distance if configured
          if (pullDropDistance > 0f)
          {
            var far = Vector3.Distance(playerCenter, rb.worldCenterOfMass);
            if (far > pullDropDistance)
            {
              _activePullSet.Remove(rb);
              continue;
            }
          }

          var p = rb.worldCenterOfMass;
          var toCenter = playerCenter - p;
          var r = toCenter.magnitude;
          var dirToCenter = r > 1e-4f ? toCenter / r : (playerCenter - transform.position).normalized;
          var ringPoint = playerCenter - dirToCenter * stopRadius;

          if (r <= stopRadius)
          {
            // Inside ring: strong PD hold to keep it frozen at the ring
            ApplyPDHold(rb, ringPoint);
            continue;
          }

          if (r > ringOuter)
          {
            // Far: base pull
            rb.AddForce(dirToCenter * pullForce, forceMode);
          }
          else
          {
            // Soft band: blend base pull with PD toward ring
            var t = Mathf.InverseLerp(stopRadius, ringOuter, r);
            t = t * t; // ease-in
            rb.AddForce(dirToCenter * (pullForce * t), forceMode);
            ApplyPDHold(rb, ringPoint, 1f - t);
          }
        }
      }

      public void ForcePush()
      {
        if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var dist)) return;

        GatherAlongPath(playerPos, dir, dist, aimSphereRadius, _buffer);
        foreach (var rb in _buffer)
        {
          if (!rb) continue;
          var away = (rb.worldCenterOfMass - playerPos).normalized;
          var blended = (away + dir).normalized;
          rb.AddForce(blended * pushForce, forceMode);
        }
      }

      public void ForceThrow()
      {
        if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var dist)) return;

        GatherAlongPath(playerPos, dir, dist, aimSphereRadius, _buffer);
        foreach (var rb in _buffer)
        {
          if (!rb) continue;
          rb.AddForce(Vector3.up * throwUpForce, forceMode);
        }
      }

      // =====================================================================
      // Pull helpers: persistent lock set
      // =====================================================================

      private void AddToActivePullSet(List<Rigidbody> candidates)
      {
        if (candidates == null || candidates.Count == 0) return;
        foreach (var rb in candidates)
        {
          if (!rb || rb.isKinematic) continue;
          if (_activePullSet.Contains(rb)) continue;

          _activePullSet.Add(rb);
          // Cap max locks
          if (_activePullSet.Count > pullMaxLockedTargets)
          {
            // Remove oldest arbitrary (HashSet has no order; we just drop one)
            foreach (var rem in _activePullSet)
            {
              _activePullSet.Remove(rem);
              break;
            }
          }
        }
      }

      /// <summary>
      /// PD (spring-damper) toward a target point, clamped. Mass-independent via Acceleration.
      /// </summary>
      private void ApplyPDHold(Rigidbody rb, Vector3 targetPoint, float strengthScale = 1f)
      {
        if (strengthScale <= 0f) return;
        var x = rb.worldCenterOfMass;
        var v = rb.linearVelocity;
        var err = targetPoint - x;

        var kp = pullHoldKp * strengthScale;
        var kd = pullHoldKd * strengthScale;

        var accel = kp * err - kd * v;
        var mag = accel.magnitude;
        if (mag > pullMaxAccel) accel = accel * (pullMaxAccel / mag);

        rb.AddForce(accel, ForceMode.Acceleration);
      }

      // =====================================================================
      // Targeting Helpers
      // =====================================================================

      private bool TryGetAimPath(out Vector3 playerPos, out Vector3 aimPoint, out Vector3 dir, out float distance)
      {
        playerPos = transform.position + Vector3.up * playerAimHeightOffset;
        aimPoint = playerPos + transform.forward * 3f; // fallback
        dir = transform.forward;
        distance = 3f;

        var cam = _cam ? _cam : _cam = GetGameCamera();
        if (!cam) return false;

        var ray = GetAimRay(cam);

        if (Physics.Raycast(ray, out var hit, aimRayMaxDistance, aimRayMask, triggerQuery))
        {
          aimPoint = hit.point;
        }
        else
        {
          aimPoint = ray.origin + ray.direction * aimRayMaxDistance;
        }

        dir = aimPoint - playerPos;
        distance = dir.magnitude;
        if (distance < 0.01f) return false;
        dir /= distance;
        return true;
      }

      private void GatherAlongPath(Vector3 origin, Vector3 dir, float distance, float radius, List<Rigidbody> outList)
      {
        outList.Clear();
        var hits = Physics.SphereCastAll(origin, radius, dir, distance, targetMask, triggerQuery);

        for (var i = 0; i < hits.Length; i++)
        {
          var c = hits[i].collider;
          if (!TryGetBody(c, out var rb)) continue;
          if (rb.mass > maxAffectMass) continue;
          if (!outList.Contains(rb)) outList.Add(rb);
        }
      }

      private void AddSplashAtPoint(Vector3 point, float radius, List<Rigidbody> outList)
      {
        var cols = Physics.OverlapSphere(point, radius, targetMask, triggerQuery);
        foreach (var col in cols)
        {
          if (!TryGetBody(col, out var rb)) continue;
          if (rb.mass > maxAffectMass) continue;
          if (!outList.Contains(rb)) outList.Add(rb);
        }
      }

      private bool TryGetBody(Collider c, [NotNullWhen(true)] out Rigidbody rb)
      {
        // ignore own colliders
        if (selfColliders.Contains(c))
        {
          rb = null;
          return false;
        }

        rb = c.attachedRigidbody ? c.attachedRigidbody : c.GetComponentInParent<Rigidbody>();

        // ignore own body or kinematic
        if (rb != null && (rb == body || rb.isKinematic))
        {
          rb = null;
          return false;
        }

        return rb != null;
      }

      // =====================================================================
      // Player Geometry Helpers (stop ring)
      // =====================================================================

      private Bounds GetPlayerCombinedBounds()
      {
        var hasBounds = false;
        Bounds b = default;

        foreach (var c in selfColliders)
        {
          if (!c) continue;
          if (!hasBounds)
          {
            b = c.bounds;
            hasBounds = true;
          }
          else b.Encapsulate(c.bounds);
        }

        if (!hasBounds)
        {
          var p = transform.position;
          b = new Bounds(p, Vector3.one * 0.5f);
        }

        return b;
      }

      private Vector3 GetPlayerCenter()
      {
        return GetPlayerCombinedBounds().center;
      }

      private float GetPlayerStopDistance()
      {
        var b = GetPlayerCombinedBounds();
        var ext = b.extents;
        var maxExtent = ext.magnitude; // corner distance
        return maxExtent + Mathf.Max(0f, pullStopBuffer);
      }

      // =====================================================================
      // Hold Routine (for ForceHold spell)
      // =====================================================================

      private System.Collections.IEnumerator HoldRoutine(List<Rigidbody> bodies, Vector3 playerPos, Vector3 aimPoint, float duration)
      {
        var time = 0f;
        while (time < duration)
        {
          var dt = Time.deltaTime;
          var segA = playerPos;
          var segB = aimPoint;

          foreach (var rb in bodies)
          {
            if (!rb) continue;

            var p = rb.worldCenterOfMass;
            var target = NearestPointOnSegment(p, segA, segB);
            var toTarget = target - p;
            var dist = toTarget.magnitude + 0.001f;
            var dir = toTarget / dist;
            rb.AddForce(dir * holdForce * dt, ForceMode.Acceleration);
            rb.linearVelocity *= 0.96f;
          }

          time += dt;
          yield return null;
        }
      }

      private static Vector3 NearestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
      {
        var ab = b - a;
        var t = Vector3.Dot(p - a, ab) / (ab.sqrMagnitude + 1e-6f);
        t = Mathf.Clamp01(t);
        return a + ab * t;
      }

      // =====================================================================
      // Input (virtual) — override for Valheim
      // =====================================================================

      protected virtual bool GetBlastPressed()
      {
        return Input.GetKeyDown(KeyCode.Q);
      }
      protected virtual bool GetHoldPressed()
      {
        return Input.GetKeyDown(KeyCode.F);
      }
      protected virtual bool GetCrushPressed()
      {
        return Input.GetKeyDown(KeyCode.C);
      }

      /// <summary>Pull is HOLD-based now and persists regardless of camera direction.</summary>
      protected virtual bool GetPullHeld()
      {
        return Input.GetKey(KeyCode.E);
      }

      protected virtual bool GetPushPressed()
      {
        return Input.GetKeyDown(KeyCode.R);
      }
      protected virtual bool GetThrowPressed()
      {
        return Input.GetKeyDown(KeyCode.T);
      }

      // =====================================================================
      // Camera / Aim (virtual) — override for Valheim GameCamera
      // =====================================================================

      protected virtual Camera GetGameCamera()
      {
        return Camera.main;
      }

      protected virtual Ray GetAimRay(Camera cam)
      {
        var center = new Vector3(0.5f, 0.5f, 0f);
        return cam.ViewportPointToRay(center);
      }

      // =====================================================================
      // Hooks for integration
      // =====================================================================

      protected virtual void OnCrushApplied(List<Rigidbody> affected, float stunSeconds) {}

#if UNITY_EDITOR
    // =====================================================================
    // Gizmos: visualize camera ray + player->aim path + stop ring
    // =====================================================================
    [Header("Debug")]
    [SerializeField] private bool drawAimGizmos = true;

    private void OnDrawGizmos()
    {
      if (!drawAimGizmos) return;
      if (!_cam) _cam = GetGameCamera();
      if (!_cam) return;

      if (!Application.isPlaying)
      {
        _cam = Camera.main;
      }

      if (!TryGetAimPath(out var playerPos, out var aimPoint, out var dir, out var distance))
        return;

      // Camera ray (green)
      Gizmos.color = Color.green;
      Gizmos.DrawRay(_cam.transform.position, _cam.transform.forward * aimRayMaxDistance);

      // Player → Aim path (cyan)
      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(playerPos, aimPoint);

      // Aim intersection (yellow)
      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(aimPoint, 0.25f);

      // SphereCast endpoints (wire)
      Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
      Gizmos.DrawWireSphere(playerPos, aimSphereRadius);
      Gizmos.DrawWireSphere(aimPoint, aimSphereRadius);

      // Player combined bounds (magenta wire cube) + stop ring
      var b = GetPlayerCombinedBounds();
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireCube(b.center, b.size);

      var stop = GetPlayerStopDistance();
      Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
      DrawCircle(b.center, Vector3.up, stop);
      DrawCircle(b.center, Vector3.right, stop);
      DrawCircle(b.center, Vector3.forward, stop);
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments = 48)
    {
      var rot = Quaternion.FromToRotation(Vector3.up, normal.normalized);
      var prev = center + rot * (Vector3.forward * radius);
      for (var i = 1; i <= segments; i++)
      {
        var t = i / (float)segments * Mathf.PI * 2f;
        var next = center + rot * (new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * radius);
        Gizmos.DrawLine(prev, next);
        prev = next;
      }
    }
#endif
    }
  }