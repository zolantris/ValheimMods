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
    public float turnSpeed = 720f;
    public float closeTurnSpeed = 540f;
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

    public readonly HashSet<Collider> GroundContacts = new();
    public DodgeAbility dodgeAbility;
    private float dodgeElapsed;
    private Vector3 dodgeStart, dodgeEnd;

    private float lastDodgeTime = -Mathf.Infinity;

    private float nextWanderTime;
    public Rigidbody Rb => _rb;
    public float moveLerpVel { get; private set; }
    public bool HasRoamTarget => currentWanderTarget != Vector3.zero;

    public bool IsGrounded => GroundContacts.Count > 0;
    public void Awake()
    {
      if (!abilityManager) abilityManager = GetComponent<AbilityManager>();
      if (!_rb) _rb = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
      if (!OwnerAI) return;
      var vel = _rb.velocity;
      _rb.useGravity = !IsGrounded;


      GetGroundPoint();

      var isUnderground = false;

      foreach (var animationFootCollider in OwnerAI.animationController.footColliders)
      {
        if (animationFootCollider.bounds.min.y < GroundPoint.y)
        {
          isUnderground = true;
          vel.y = Mathf.Max(vel.y, GroundPoint.y - animationFootCollider.bounds.min.y, 0f);
        }
      }

      if (IsGrounded || isUnderground)
      {
        OwnerAI.lastTouchedLand = 0f;
      }

      _rb.velocity = vel;

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

    public void GetGroundPoint()
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

    public void MoveWander()
    {
      if (Time.time < nextWanderTime) return;
      if (!HasRoamTarget || Vector3.Distance(transform.position, currentWanderTarget) < 1.2f)
      {
        if (TryPickRandomWanderTarget(out currentWanderTarget))
        {
          nextWanderTime = Time.time + wanderCooldown;
        }
        else
        {
          nextWanderTime = Time.time + 2f;
          return;
        }
      }
      var toTarget = currentWanderTarget - transform.position;
      var distance = toTarget.magnitude;
      RotateTowardsDirection(toTarget, turnSpeed);

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

      moveLerpVel = Mathf.MoveTowards(moveLerpVel, wanderSpeed, AccelerationForceSpeed * 0.5f * Time.deltaTime);
      Rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);
    }

    public void BrakeHard()
    {
      var v = Rb.velocity;
      v.x = 0f;
      v.z = 0f;
      Rb.velocity = v;
      Rb.angularVelocity = Vector3.zero;
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
        if (Physics.Raycast(down, out var hit, 10f, LayerMask.GetMask("Default", "terrain")))
        {
          if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
          {
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
  }
}