using System.Collections.Generic;
using Eldritch.Core.Abilities;
using Eldritch.Core.Nav;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{

  public class XenoAIMovementController : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private Rigidbody _rb;

    public Collider HeadCollider;
    public Transform HeadColliderTransform;
    public XenoDroneAI OwnerAI;

    [Header("Movement Tuning")]
    public float moveSpeed = 1f;
    public float closeMoveSpeed = 0.3f;
    public float AccelerationForceSpeed = 90f;
    public float closeAccelForce = 20f;
    public float closeRange = 3f;
    public float turnSpeed = 50f;
    public float closeTurnSpeed = 50f;
    public float wanderSpeed = 0.5f;
    public float maxJumpDistance = 4f;
    public float maxJumpHeightArc = 2.5f;
    public Vector3 maxRunRange = new(20f, 0f, 20f);

    // Wander state
    public float wanderRadius = 8f;
    public float wanderCooldown = 5f;
    public float wanderMinDistance = 3f;
    public Vector3 currentWanderTarget;
    public Vector3 GroundPoint = Vector3.zero;

    [SerializeField] public float CounterGravity = 0.1f;
    public AbilityManager abilityManager;

    public float arrivalThreshold = 5f;

    public readonly HashSet<Collider> GroundContacts = new();
    private XenoAnimationController animationController;
    public DodgeAbility dodgeAbility;
    private float dodgeElapsed;
    private Vector3 dodgeStart, dodgeEnd;

    private bool hasArrivedAtWanderPoint;

    private float lastDodgeTime = -Mathf.Infinity;

    private float nextWanderTime;
    public Rigidbody Rb => _rb;

    public float moveLerpVel { get; private set; }
    public bool HasRoamTarget => currentWanderTarget != Vector3.zero;
    public bool IsGrounded => GroundContacts.Count > 0;

    public bool HasMovedInFrame = false;

    [SerializeField] private SurfaceClimbingState climbingState = new();

    public void Awake()
    {
      if (!portalScanner)
      {
        portalScanner = gameObject.AddComponent<RuntimeLinkVisualizer>();
      }
      if (!animationController) animationController = GetComponentInChildren<XenoAnimationController>();
      if (!abilityManager) abilityManager = GetComponent<AbilityManager>();
      if (!_rb) _rb = GetComponent<Rigidbody>();

      climbingState.Init(this, _rb, transform);
    }

    public RuntimeLinkVisualizer portalScanner; // assign in inspector
    public float rescanInterval = 2f;
    private float _nextScanTime;

    public void FixedUpdate()
    {
      if (!OwnerAI) return;
      if (!Rb) return;

      // if (Time.time >= _nextScanTime)
      // {
      //   if (Pathfinding.IsValid())
      //   {
      //     var center = OwnerAI ? OwnerAI.transform.position : transform.position;
      //     // portalScanner.ScanFrom(center);
      //     _nextScanTime = Time.time + rescanInterval;
      //   }
      // }
      var vel = _rb.velocity;
      if (OwnerAI.IsManualControlling)
      {
        _rb.useGravity = !IsGrounded;
      }

      SyncVelocityWithMovementSpeed(vel);

      // animations desync rotation alot.
      if (!Rb.isKinematic)
      {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), Time.fixedTime);
      }

      UpdateGroundPoint();

      // var isUnderground = false;
      //
      // foreach (var animationFootCollider in OwnerAI.animationController.footColliders)
      // {
      //   if (animationFootCollider.bounds.min.y < GroundPoint.y)
      //   {
      //     isUnderground = true;
      //     vel.y = Mathf.Max(vel.y, GroundPoint.y - animationFootCollider.bounds.min.y, 0f);
      //   }
      // }
      //
      // if (IsGrounded || isUnderground)
      // {
      //   if (!Rb.isKinematic)
      //   {
      //     _rb.velocity = vel;
      //   }
      //   OwnerAI.lastTouchedLand = 0f;
      // }

      // if (IsGrounded)
      // {
      //     // vel.y = Mathf.Abs(Physics.gravity.y);
      //     vel.y = CounterGravity;
      //     _rb.velocity = vel;
      // }
    }

    public void OnTriggerEnter(Collider other)
    {
      if (other.gameObject.layer == LayerHelpers.TerrainLayer)
      {
        GroundContacts.Add(other);
      }
    }
    public void OnTriggerExit(Collider other)
    {
      if (other.gameObject.layer == LayerHelpers.TerrainLayer)
      {
        GroundContacts.Remove(other);
      }
    }

    private float GetMoveSpeedParam(float localForwardSpeed)
    {
      const float idleThreshold = 0.1f;
      const float creepThreshold = 0.25f;
      const float walkThreshold = 1.5f;
      const float runThreshold = 2f;

      if (localForwardSpeed < -idleThreshold)
        return Mathf.Lerp(localForwardSpeed, -1f, Time.fixedDeltaTime); // idle

      if (Mathf.Abs(localForwardSpeed) < idleThreshold)
        return Mathf.Lerp(localForwardSpeed, 0f, Time.fixedDeltaTime); // idle

      return Mathf.Clamp(localForwardSpeed, -1f, 2f);
      // if (Mathf.Abs(localForwardSpeed) < creepThreshold)
      //   return localForwardSpeed; // creep (unique animation)
      //
      // if (localForwardSpeed > creepThreshold && localForwardSpeed < walkThreshold)
      //   return 1f; // walk
      //
      // if (localForwardSpeed >= walkThreshold)
      //   return 2f; // run
      //
      // return 0f;
    }
    private float _smoothedAnimSpeed = 0f;
    [SerializeField] private float animatedMovementLerpSpeed = 12f;

    // todo coroutine or debounce animation setter (but this is done in SetMoveSpeed so it's just a call cost here.
    public void SyncVelocityWithMovementSpeed(Vector3 velocity)
    {
      if (Rb.isKinematic)
      {
        // animationController.SetMoveSpeed(0);
        return;
      }

      // Project velocity onto XZ plane
      var velocityXZ = new Vector3(velocity.x, 0f, velocity.z);
      var forward = transform.forward;
      forward.y = 0;
      forward.Normalize();

      var signedSpeed = Vector3.Dot(velocityXZ, forward);

      // 1. Apply deadzone to avoid shaking near zero
      const float idleDeadzone = 0.15f;
      if (Mathf.Abs(signedSpeed) < idleDeadzone)
        signedSpeed = 0f;

      // 2. Lerp animation speed for visual smoothness
      _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, signedSpeed, animatedMovementLerpSpeed * Time.fixedDeltaTime);

      // 3. Snap to animation states
      var outputSpeed = GetMoveSpeedParam(_smoothedAnimSpeed);

      animationController.SetMoveSpeed(outputSpeed);
      HasMovedInFrame = false;
    }

    public void UpdateGroundPoint()
    {
      if (!OwnerAI) return;
      if (Physics.Raycast(OwnerAI.xenoRoot.position, -OwnerAI.xenoRoot.up, out var hit, 40f, LayerHelpers.GroundLayers))
      {
        GroundPoint = hit.point;
      }
      else
      {
        GroundPoint = Vector3.negativeInfinity;
      }
    }

    // --- Core Movement ---
    public void MoveTowardsTarget(Vector3 targetPos, float speed, float accel, float turnSpeed)
    {
      var toTarget = targetPos - transform.position;
      RotateTowardsDirection(toTarget, turnSpeed);
      moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, accel * Time.deltaTime);
      Rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);

      HasMovedInFrame = true;
    }

    public void MoveInDirection(Vector3 direction, float speed, float lerpRate, float turnSpeed)
    {
      direction.y = 0;
      if (direction == Vector3.zero) return;

      RotateTowardsDirection(direction, turnSpeed);

      // Compute target velocity on XZ
      var targetVel = direction.normalized * speed;
      var currentVel = Rb.velocity;
      currentVel.y = 0; // Only care about XZ

      // Lerp velocity for smooth acceleration/deceleration
      var newVel = Vector3.Lerp(currentVel, targetVel, lerpRate * Time.fixedDeltaTime);

      // Compute the change needed to reach this velocity
      var velocityChange = newVel - currentVel;

      // Only apply to XZ, leave Y alone for gravity/jumps
      velocityChange.y = 0;

      Rb.AddForce(velocityChange, ForceMode.VelocityChange);

      HasMovedInFrame = true;
    }

    public void MoveAwayFromTarget(Vector3 awayFrom, float speed, float accel, float turnSpeed)
    {
      var awayDir = (transform.position - awayFrom).normalized;
      awayDir.y = 0f;
      var retreatTarget = transform.position + awayDir * speed * Time.deltaTime;

      // Face the target while backing away for maximum creep factor
      RotateTowardsDirection(awayFrom - transform.position, turnSpeed);
      MoveTowardsTarget(retreatTarget, speed, accel, turnSpeed);

      HasMovedInFrame = true;
    }

    public void MoveChaseTarget(Vector3 targetPos, Vector3? targetVelocity, float closeRange, float moveSpeed, float closeMoveSpeed, float accel, float closeAccel, float turnSpeed, float closeTurnSpeed)
    {
      var predictedTarget = targetPos + (targetVelocity ?? Vector3.zero) * 0.5f;
      var toTarget = predictedTarget - transform.position;
      var distance = toTarget.magnitude;

      RotateTowardsDirection(toTarget, turnSpeed);

      if (IsGapAhead(1.0f, 3f, 5f))
      {
        if (FindJumpableLanding(out var jumpTarget))
        {
          JumpTo(jumpTarget);
          return;
        }
        BrakeHard();
        return;
      }

      var targetAngle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
      var shouldMove = Mathf.Abs(targetAngle) < 120f;
      var slowDownStart = 6f;
      var speedFactor = Mathf.Clamp01(distance / slowDownStart);
      var targetSpeed = Mathf.Lerp(closeMoveSpeed, moveSpeed, speedFactor);
      var targetAccel = Mathf.Lerp(closeAccel, accel, speedFactor);

      if (shouldMove)
      {
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, targetSpeed, targetAccel * Time.deltaTime);
        Rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);
        HasMovedInFrame = true;
      }
      else
      {
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, 0f, targetAccel * 2f * Time.deltaTime);
      }
    }

    public void MoveAwayFromEnemies(HashSet<GameObject> enemySet, float maxRange)
    {
      var hostilePositions = new List<Vector3>();
      foreach (var enemyGO in enemySet)
      {
        if (enemyGO == null || enemyGO == gameObject) continue;
        var dist = Vector3.Distance(transform.position, enemyGO.transform.position);
        if (dist > maxRange) continue;
        hostilePositions.Add(enemyGO.transform.position);
      }
      if (hostilePositions.Count == 0) return;

      var bestScore = float.MinValue;
      var bestDir = Vector3.zero;
      var samples = 12;
      for (var i = 0; i < samples; i++)
      {
        var angle = 360f / samples * i;
        var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        var candidate = transform.position + dir * 5f;
        if (!IsGroundBelow(candidate, 1.5f)) continue;

        var score = 0f;
        foreach (var pos in hostilePositions)
          score += Vector3.Distance(candidate, pos);

        var localOffset = candidate - transform.position;
        if (Mathf.Abs(localOffset.x) > maxRunRange.x || Mathf.Abs(localOffset.z) > maxRunRange.z)
          score -= 10000f;

        if (score > bestScore)
        {
          bestScore = score;
          bestDir = dir;
        }
      }

      if (bestDir == Vector3.zero) return;
      MoveTowardsTarget(transform.position + bestDir * 4f, moveSpeed, AccelerationForceSpeed, turnSpeed);
    }

    public void MoveFleeWithAllyBias(XenoDroneAI friendly, HashSet<GameObject> enemies)
    {
      var hostilePositions = new List<Vector3>();
      foreach (var enemyGO in enemies)
      {
        if (enemyGO == null || enemyGO == gameObject) continue;
        hostilePositions.Add(enemyGO.transform.position);
      }
      var bestDir = Vector3.zero;
      var bestScore = float.MinValue;
      var samples = 12;
      for (var i = 0; i < samples; i++)
      {
        var angle = 360f / samples * i;
        var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        var allyBias = 0f;
        if (friendly != null)
        {
          var toAlly = (friendly.transform.position - transform.position).normalized;
          allyBias = Vector3.Dot(dir, toAlly) * 8f;
        }
        var candidate = transform.position + dir * 5f;
        var enemyPenalty = 0f;
        foreach (var hostile in hostilePositions)
        {
          var dist = Vector3.Distance(candidate, hostile);
          enemyPenalty -= 1f / Mathf.Max(dist, 0.1f);
        }
        if (!IsGroundBelow(candidate, 1.5f)) continue;
        var score = allyBias + enemyPenalty;
        if (score > bestScore)
        {
          bestScore = score;
          bestDir = dir;
        }
      }
      if (bestDir == Vector3.zero)
      {
        MoveAwayFromEnemies(enemies, 40f);
        return;
      }
      MoveTowardsTarget(transform.position + bestDir * 4f, moveSpeed * 1.25f, AccelerationForceSpeed * 1.1f, turnSpeed);
    }

    private float GetXZDistance(Vector3 a, Vector3 b)
    {
      return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    public void MoveWander()
    {
      // Don't move or rotate until wanderCooldown expires
      if (Time.time < nextWanderTime)
        return;
      // Compute distance to current wander target
      var distance = HasRoamTarget ? GetXZDistance(transform.position, currentWanderTarget) : Mathf.Infinity;

      // ARRIVAL: If we've arrived, or don't have a target, fully stop and do not rotate!
      if (!HasRoamTarget || distance < arrivalThreshold || hasArrivedAtWanderPoint)
      {
        if (!hasArrivedAtWanderPoint)
        {
          BrakeHard();
          moveLerpVel = 0f;
          hasArrivedAtWanderPoint = true;
        }
        // Do not rotate, do not apply movement!

        // Time to pick next point?
        if (Time.time >= nextWanderTime)
        {
          if (TryPickRandomWanderTarget(out currentWanderTarget))
          {
            nextWanderTime = Time.time + wanderCooldown;
            hasArrivedAtWanderPoint = false; // allow moving/rotating again
          }
          else
          {
            nextWanderTime = Time.time + 2f; // try again soon
          }
        }
        return;
      }

      // --- ONLY RUN BELOW IF WE HAVE NOT ARRIVED ---

      // Calculate movement vector
      var toTarget = currentWanderTarget - transform.position;

      // Only rotate if far enough from target (avoid chasing micro-deltas)
      if (distance > arrivalThreshold)
      {
        RotateTowardsDirection(toTarget, turnSpeed);

        // Optional: gap/jump check as before
        if (distance > 2f && IsGapAhead(1.0f, 3f, 5f))
        {
          if (FindJumpableLanding(out var jumpTarget))
          {
            JumpTo(jumpTarget);
            return;
          }
          BrakeHard();
          return;
        }

        // Rapid, realistic slowdown as we approach
        var slowDownDist = 5f; // start slowing 3 units out
        var approachT = Mathf.Clamp01(distance / slowDownDist);
        var targetSpeed = Mathf.Lerp(0f, wanderSpeed, approachT);
        var targetAccel = Mathf.Lerp(0f, AccelerationForceSpeed * 0.5f, approachT);

        moveLerpVel = Mathf.MoveTowards(moveLerpVel, targetSpeed, targetAccel * Time.deltaTime);

        Rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);

        HasMovedInFrame = true;
      }
    }


    public void BrakeHard()
    {
      var v = Rb.velocity;
      v.x = 0f;
      v.z = 0f;
      Rb.velocity = v;
      Rb.angularVelocity = Vector3.zero;

      HasMovedInFrame = false;
    }

    // --- Jumping & Grounding ---
    public bool IsGapAhead(float distance = 1.0f, float maxStepHeight = 2f, float maxDrop = 3.0f)
    {
      if (OwnerAI == null || !OwnerAI.CanJump || !OwnerAI.IsGrounded()) return false;
      // Use OwnerAI.GetFurthestToe() or equivalent foot transform
      // For demo: just check ahead
      var checkOrigin = transform.position + transform.forward * distance;
      if (Physics.Raycast(checkOrigin + Vector3.up, Vector3.down, out var hit, maxDrop + 1.2f, LayerMask.GetMask("Default", "terrain")))
      {
        var yDiff = checkOrigin.y - hit.point.y;
        if (yDiff < maxStepHeight) return false;
      }
      return true;
    }
    public bool IsGroundBelow(Vector3 position, float maxDrop)
    {
      var ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
      return Physics.Raycast(ray, out _, maxDrop + 0.5f, LayerMask.GetMask("Default", "terrain"));
    }
    private bool FindJumpableLanding(out Vector3 landingPoint, float maxDrop = 2.5f, float fanAngle = 60f, int numChecks = 7)
    {
      landingPoint = Vector3.zero;
      if (!IsGrounded) return false;

      var jumpOrigin = OwnerAI.animationController.GetFurthestToe().position;

      var bestDistance = 0f;
      var bestLanding = Vector3.zero;
      var found = false;

      for (var i = 0; i < numChecks; i++)
      {
        var angle = -fanAngle / 2f + fanAngle / (numChecks - 1) * i;
        var dir = Quaternion.Euler(0, angle, 0) * transform.forward;
        // Use the actual edge of the collider + a small fudge
        var checkStart = jumpOrigin + dir;

        for (var dist = 1.0f; dist <= maxJumpDistance; dist += 0.5f)
        {
          var rayOrigin = checkStart + dir * dist;
          Debug.DrawRay(rayOrigin, Vector3.down * (maxDrop + 0.5f), Color.cyan, 2f);

          var downRay = new Ray(rayOrigin, Vector3.down);
          if (Physics.Raycast(downRay, out var hit, maxDrop + 0.5f, LayerHelpers.GroundLayers))
          {
            var verticalDrop = jumpOrigin.y - hit.point.y;
            var slope = Vector3.Angle(hit.normal, Vector3.up);
            var landingDist = Vector3.Distance(jumpOrigin, hit.point);

            if (verticalDrop < maxDrop && slope < 40f && landingDist <= maxJumpDistance)
            {
              if (!found || landingDist > bestDistance)
              {
                bestDistance = landingDist;
                bestLanding = hit.point;
                found = true;
              }
              Debug.DrawLine(rayOrigin, hit.point, Color.magenta, 2f);
            }
          }
        }
      }

      if (found)
      {
        landingPoint = bestLanding;
        Debug.DrawLine(jumpOrigin + Vector3.up * 0.2f, bestLanding + Vector3.up * 0.2f, Color.green, 2f);
        return true;
      }
      return false;
    }
    public void JumpTo(Vector3 landingPoint)
    {
      OwnerAI.animationController.PlayJump();

      var origin = transform.position;
      var jumpVec = landingPoint - origin;
      var dist = jumpVec.magnitude;
      if (dist > maxJumpDistance)
      {
        jumpVec = jumpVec.normalized * maxJumpDistance;
        landingPoint = origin + jumpVec;
        dist = maxJumpDistance;
      }

      var gravity = Mathf.Abs(Physics.gravity.y);
      var timeToApex = Mathf.Sqrt(2 * maxJumpHeightArc / gravity);
      var vy = Mathf.Sqrt(2 * gravity * maxJumpHeightArc);
      var timeTotal = timeToApex + Mathf.Sqrt(2 * Mathf.Max(0, landingPoint.y - origin.y) / gravity);
      var horiz = jumpVec;
      horiz.y = 0;
      var vxz = horiz / Mathf.Max(0.01f, timeTotal);
      Rb.velocity = vxz + Vector3.up * vy;
    }

    public bool TryUpdateCurrentWanderTarget()
    {
      TryPickRandomWanderTarget(out currentWanderTarget);
      return HasRoamTarget;
    }

    // --- Wander Helpers ---
    public bool TryPickRandomWanderTarget(out Vector3 wanderTarget)
    {
      var origin = transform.position;
      for (var attempts = 0; attempts < 12; attempts++)
      {
        var angle = Random.Range(0, 360f);
        var dist = Random.Range(wanderMinDistance, wanderRadius);
        var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        var candidate = origin + dir * dist;
        var down = new Ray(candidate + Vector3.up * 3f, Vector3.down);
        if (Physics.Raycast(down, out var hit, 10f, LayerHelpers.GroundLayers))
        {
          if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
          {
            Debug.DrawRay(hit.point, Vector3.up, Color.blue, 10f);
            wanderTarget = hit.point;
            return true;
          }
        }
      }
      wanderTarget = Vector3.zero;
      return false;
    }

    // --- Rotation ---
    public void RotateTowardsDirection(Vector3 dir, float customTurnSpeed)
    {
      dir.y = 0;
      if (dir == Vector3.zero) return;
      var targetRotation = Quaternion.LookRotation(dir, Vector3.up);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, customTurnSpeed * Time.deltaTime);
    }

    #region Circling Logic

    private readonly List<Vector3> _orbitTmpPath = new();
    [SerializeField] private float orbitAheadDegrees = 45f;

    private enum OrbitState
    {
      Orbit,
      Detour
    }

    [SerializeField] private float orbitProbeLen = 2.0f;
    [SerializeField] private float orbitClearance = 0.35f;
    [SerializeField] private float orbitRejoinAheadDeg = 35f; // arc angle ahead to check for rejoin
    [SerializeField] private float orbitRejoinLoS = 6f; // LoS distance to rejoin point
    [SerializeField] private float orbitRadiusSlack = 0.6f; // how close to ring before rejoin
    [SerializeField] private float detourMaxSeconds = 2.0f; // safety exit

    private OrbitState _orbitState = OrbitState.Orbit;
    private Vector3 _detourLastNormal;
    private float _detourTimer;
    public void CircleAroundTarget(Transform target, int direction, float baseCircleSpeed)
    {
      if (!target) return;

      var toTarget = target.position - transform.position;
      toTarget.y = 0f;
      var dist = toTarget.magnitude;
      if (dist < 0.05f) return;

      // --- dynamic radius (your config) ---
      var cfg = OwnerAI.huntBehaviorConfig;
      var desiredRadius = Mathf.Clamp(dist * cfg.circleRadiusFactor, cfg.minCircleRadius, cfg.maxCircleRadius);

      var tangent = Quaternion.Euler(0, 90f * direction, 0) * toTarget.normalized;
      var radiusError = dist - desiredRadius;
      var correction = toTarget.normalized * Mathf.Clamp(radiusError * 2.0f, -baseCircleSpeed, baseCircleSpeed);

      var desiredVel = tangent * baseCircleSpeed + correction;
      var desiredDir = desiredVel.sqrMagnitude > 0.001f ? desiredVel.normalized : tangent;

      // Common probe
      var probeOrigin = transform.position + Vector3.up * 0.6f;
      var blocked = Physics.SphereCast(probeOrigin, orbitClearance, desiredDir,
        out var hit, orbitProbeLen, LayerHelpers.GroundLayers);

      // Compute a point on the ring ahead we want to get to (for rejoin checks)
      var aheadDir = Quaternion.Euler(0, orbitRejoinAheadDeg * direction, 0) * toTarget.normalized;
      var rejoinPoint = target.position + aheadDir * desiredRadius;

      switch (_orbitState)
      {
        case OrbitState.Orbit:
        {
          if (blocked)
          {
            // Enter detour: remember normal and reset timer
            _detourLastNormal = hit.normal;
            _detourTimer = 0f;
            _orbitState = OrbitState.Detour;
            // Fall through to Detour behavior this frame
          }
          else
          {
            ApplyDesired(desiredVel);
            return;
          }
          goto case OrbitState.Detour;
        }

        case OrbitState.Detour:
        {
          _detourTimer += Time.deltaTime;

          // Keep sampling; if we have a contact, use its normal. If not, keep last.
          if (blocked) _detourLastNormal = hit.normal;

          // Slide tangent to obstacle, but choose the sign that preserves orbit direction
          var slide = Vector3.Cross(_detourLastNormal, Vector3.up).normalized;
          if (Vector3.Dot(slide, tangent) < 0f) slide = -slide;

          // Stay roughly on the ring while sliding
          var detourVel = slide * baseCircleSpeed * 0.95f + correction * 0.6f;

          // Check if we can rejoin the circle **ahead**:
          // 1) we’re close to the ring,
          // 2) there’s line-of-sight from us to the rejoin point,
          // 3) not blocked in the desired tangent direction.
          var closeToRing = Mathf.Abs(radiusError) <= orbitRadiusSlack;

          var losToRejoin = !Physics.Linecast(
            transform.position + Vector3.up * 0.4f,
            rejoinPoint + Vector3.up * 0.4f,
            LayerHelpers.GroundLayers
          ) && Vector3.Distance(transform.position, rejoinPoint) <= orbitRejoinLoS;

          // also not blocked if we try to step toward the rejoin direction
          var rejoinStepDir = (rejoinPoint - transform.position).normalized;
          var rejoinBlocked = Physics.SphereCast(probeOrigin, orbitClearance, rejoinStepDir,
            out _, orbitProbeLen * 0.75f, LayerHelpers.GroundLayers);

          if (closeToRing && losToRejoin && !rejoinBlocked)
          {
            _orbitState = OrbitState.Orbit;
            // nudge toward rejoin point to lock back onto the arc
            var nudge = rejoinPoint - transform.position;
            nudge.y = 0f;
            var nudgeVel = nudge.normalized * baseCircleSpeed * 0.8f + correction * 0.5f;
            ApplyDesired(nudgeVel);
            return;
          }

          // Safety: don't get stuck forever — bail back to Orbit after timeout if no obstruction now
          if (_detourTimer > detourMaxSeconds && !blocked)
          {
            _orbitState = OrbitState.Orbit;
            ApplyDesired(desiredVel);
            return;
          }

          // Continue sliding around the obstacle
          ApplyDesired(detourVel);
          return;
        }
      }

      void ApplyDesired(Vector3 vel)
      {
        var flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        var delta = vel - flatVel;
        delta = Vector3.ClampMagnitude(delta, baseCircleSpeed * 0.7f);
        _rb.AddForce(delta, ForceMode.VelocityChange);
        HasMovedInFrame = true;

        if (delta.sqrMagnitude > 0.001f)
          RotateTowardsDirection(vel, turnSpeed * 0.6f);

        animationController?.PointHeadTowardTarget(transform, target);
      }
    }

    #endregion

    #region Climbing Movement

    public bool TryFindIngressToTargetFloor(
      Vector3 targetPos,
      out Vector3 ingressMovePoint,
      out Vector3 landingPoint,
      float searchRadius = 8f,
      float minDrop = 1.25f,
      float maxDrop = 6f,
      int directionSamples = 16,
      float step = 0.75f)
    {
      ingressMovePoint = Vector3.zero;
      landingPoint = Vector3.zero;

      if (!IsGrounded) return false;

      var selfPos = transform.position;
      var bestScore = float.NegativeInfinity;
      var found = false;

      // Helper: score candidate (bigger is better)
      float Score(Vector3 probe, Vector3 land)
      {
        // how much closer to the target's Y we get
        var beforeY = Mathf.Abs(selfPos.y - targetPos.y);
        var afterY = Mathf.Abs(land.y - targetPos.y);
        var yGain = beforeY - afterY; // positive = improvement

        // planar closeness to target after landing
        var planarAfter = Vector2.Distance(new Vector2(land.x, land.z), new Vector2(targetPos.x, targetPos.z));

        // prefer closer ingress points too
        var toProbe = Vector3.Distance(selfPos, probe);

        // weights tuned for sensible behavior
        return yGain * 2.0f - planarAfter * 0.12f - toProbe * 0.05f;
      }

      for (var i = 0; i < directionSamples; i++)
      {
        var yaw = 360f / directionSamples * i;
        var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        for (var dist = step; dist <= searchRadius; dist += step)
        {
          var probe = selfPos + dir * dist;

          // Ensure the probe is on navigable ground (don’t walk into nothing)
          if (!IsGroundBelow(probe, 1.5f)) continue;

          // Check for a drop (gap) beyond the probe (like stepping off the hole)
          var dropOrigin = probe + Vector3.up * 0.5f;
          if (!Physics.Raycast(dropOrigin, Vector3.down, out var hit, maxDrop + 0.6f, LayerHelpers.GroundLayers))
            continue;

          var drop = probe.y - hit.point.y;
          if (drop < minDrop || drop > maxDrop) continue;

          // Landing slope sanity
          if (Vector3.Angle(hit.normal, Vector3.up) > 45f) continue;

          // Optional: line-of-walk to probe (avoid walls right in front)
          var walkBlocked = Physics.SphereCast(
            selfPos + Vector3.up * 0.4f,
            0.25f,
            (probe - selfPos).normalized,
            out _,
            Mathf.Max(0.1f, dist - 0.1f),
            LayerHelpers.GroundLayers
          );
          if (walkBlocked) continue;

          // Score this ingress
          var s = Score(probe, hit.point);
          if (s > bestScore)
          {
            bestScore = s;
            ingressMovePoint = probe;
            landingPoint = hit.point;
            found = true;
          }
        }
      }

      return found;
    }

    // in XenoAIMovementController (helper you can reuse anywhere)
    public bool IsForwardBlocked(float checkDist = 0.9f, float radiusScale = 0.9f)
    {
      var feetY = OwnerAI ? OwnerAI.movementController.GroundPoint.y : transform.position.y;
      var chest = new Vector3(transform.position.x, feetY + 1.0f, transform.position.z);
      var r = Mathf.Max(0.25f, 0.9f * 0.35f);
      return Physics.CapsuleCast(chest, chest + Vector3.up * 0.01f, r * radiusScale, transform.forward,
        out _, checkDist, LayerHelpers.GroundLayers, QueryTriggerInteraction.Ignore);
    }

    public bool TryExecuteIngressJump(Vector3 ingressMovePoint, Vector3 landingPoint, float near = 0.75f)
    {
      // Close enough to the hole? Jump/drop.
      var planarSelf = new Vector2(transform.position.x, transform.position.z);
      var planarIngress = new Vector2(ingressMovePoint.x, ingressMovePoint.z);
      if (Vector2.Distance(planarSelf, planarIngress) > near) return false;

      // Use your existing JumpTo logic (works for both up and down arcs).
      JumpTo(landingPoint);
      return true;
    }

    // public bool TryWallClimbWhenBlocked(Vector3 moveDir)
    // {
    //   return climbingState.TryWallClimbWhenBlocked(moveDir);
    // }

    #endregion

  }
}