using System.Collections.Generic;
using Eldritch.Core.Abilities;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;

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
    [SerializeField] private float dodgeForwardDistance = 6f;
    [SerializeField] private float dodgeBackwardDistance = 3f;
    [SerializeField] private float dodgeSideDistance = 4.5f;
    [SerializeField] private float dodgeJumpHeight = 1f;
    [SerializeField] private float dodgeDuration = 0.18f; // time for the dodge movement
    [SerializeField] private float dodgeCooldown = 1f; // time between dodges

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

    public void Awake()
    {
      if (!animationController) animationController = GetComponentInChildren<XenoAnimationController>();
      if (!abilityManager) abilityManager = GetComponent<AbilityManager>();
      if (!_rb) _rb = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
      if (!OwnerAI) return;
      if (!Rb) return;
      var vel = _rb.velocity;
      _rb.useGravity = !IsGrounded;

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
        return -1f; // walk backward

      if (Mathf.Abs(localForwardSpeed) < idleThreshold)
        return 0f; // idle

      if (Mathf.Abs(localForwardSpeed) < creepThreshold)
        return 0.25f; // creep (unique animation)

      if (localForwardSpeed > creepThreshold && localForwardSpeed < walkThreshold)
        return 1f; // walk

      if (localForwardSpeed >= walkThreshold)
        return 2f; // run

      return 0f;
    }
    private float _smoothedAnimSpeed = 0f;
    [SerializeField] private float animatedMovementLerpSpeed = 12f;

    // todo coroutine or debounce animation setter (but this is done in SetMoveSpeed so it's just a call cost here.
    public void SyncVelocityWithMovementSpeed(Vector3 velocity)
    {
      if (Rb.isKinematic)
      {
        animationController.SetMoveSpeed(0);
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

    public void CircleAroundTarget(Transform target, int direction, float baseCircleSpeed)
    {
      if (!target) return;

      var toTarget = target.position - transform.position;
      toTarget.y = 0f;
      var dist = toTarget.magnitude;

      // --- Dynamic circle radius ---
      var config = OwnerAI.huntBehaviorConfig;
      var minRadius = config.minCircleRadius;
      var maxRadius = config.maxCircleRadius;
      var scaling = config.circleRadiusFactor;

      // As AI gets closer, shrink the radius, but don't let it go below min
      var dynamicRadius = Mathf.Clamp(dist * scaling, minRadius, maxRadius);

      // --- Rest of circle logic uses dynamicRadius ---
      var tangent = Quaternion.Euler(0, 90f * direction, 0) * toTarget.normalized;
      var radiusError = dist - dynamicRadius;
      var correction = toTarget.normalized * radiusError * 2.0f;
      correction = Vector3.ClampMagnitude(correction, baseCircleSpeed * 0.8f);

      var desiredVelocity = tangent * baseCircleSpeed + correction;

      var flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
      var velocityDelta = desiredVelocity - flatVel;
      velocityDelta = Vector3.ClampMagnitude(velocityDelta, baseCircleSpeed * 0.7f);

      _rb.AddForce(velocityDelta, ForceMode.VelocityChange);

      HasMovedInFrame = true;

      // Only rotate toward the tangent, NOT toward the target!
      if (velocityDelta.sqrMagnitude > 0.001f)
        RotateTowardsDirection(desiredVelocity, turnSpeed * 0.5f);

      // Head always looks at target
      animationController?.PointHeadTowardTarget(transform, target);
    }
    // public void CircleAroundTarget(Transform target, int direction, float circleRadius, float circleSpeed)
    // {
    //   if (!target) return;
    //
    //   var toTarget = target.position - transform.position;
    //   toTarget.y = 0f;
    //   var dist = toTarget.magnitude;
    //   if (dist < 0.01f) return;
    //
    //   var tangent = Quaternion.Euler(0, 90f * direction, 0) * toTarget.normalized;
    //   var radiusError = dist - circleRadius;
    //   var correction = toTarget.normalized * radiusError * 2.0f;
    //   correction = Vector3.ClampMagnitude(correction, circleSpeed * 0.8f);
    //
    //   var desiredVelocity = tangent * circleSpeed + correction;
    //
    //   var flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
    //   var velocityDelta = desiredVelocity - flatVel;
    //   velocityDelta = Vector3.ClampMagnitude(velocityDelta, circleSpeed * 0.7f);
    //
    //   _rb.AddForce(velocityDelta, ForceMode.VelocityChange);
    //
    //   HasMovedInFrame = true;
    //
    //   // Only rotate toward the tangent, NOT toward the target!
    //   if (velocityDelta.sqrMagnitude > 0.001f)
    //     RotateTowardsDirection(desiredVelocity, turnSpeed * 0.5f);
    //
    //   // Head always looks at target
    //   animationController?.PointHeadTowardTarget(transform, target);
    // }
  }
}