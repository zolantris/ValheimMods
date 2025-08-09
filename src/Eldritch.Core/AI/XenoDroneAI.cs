// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;
namespace Eldritch.Core
{
  public class XenoDroneAI : MonoBehaviour
  {
    public enum XenoAIState
    {
      Idle,
      Roam,
      Hunt,
      Attack,
      Flee,
      Dead,
      Sleeping
    }

    public static readonly HashSet<XenoDroneAI> Instances = new();

    public static RigidbodyConstraints FreezeXZ = (RigidbodyConstraints)80;

    [Header("Controller References")]
    [SerializeField] public XenoAIMovementController movementController;
    [SerializeField] public XenoAnimationController animationController;
    // State
    public XenoAIState CurrentState = XenoAIState.Idle;
    public Transform PrimaryTarget;
    public Rigidbody PrimaryTargetRB;
    [CanBeNull] public XenoDroneAI CachedPrimaryTargetXeno;
    public int PackId;
    public float Health, MaxHealth = 100f;

    [Header("Timers")]
    public float TimeUntilSleep = 5f, TimeUntilWake = 50f;

    public bool HasCamouflage;
    public bool CanSleepAnimate;
    public bool CanJump = true;


    public float DeltaPrimaryTarget;
    public bool IsHiding = true;

    // Grounding
    public float lastTouchedLand;
    public float lastLowestPointCheck;
    [SerializeField] public bool IsManualControlling;

    public float closeRange = 1f;
    public AbilityManager abilityManager;

    [Header("Behavior State")]
    public XenoHuntBehaviorState huntBehaviorState = new();

    [Header("Behavior Configs")]
    [SerializeField]
    public XenoHuntBehaviorConfig huntBehaviorConfig = new();
    private readonly float _circleDecisionInterval = 3f;
    private CoroutineHandle _aiStateUpdateRoutine;
    private List<Func<bool>> _behaviorUpdaters = new();

    private CoroutineHandle _sleepRoutine;
    private Vector3 cachedLowestPoint = Vector3.zero;

    // Nav state
    private readonly List<Vector3> _navPath = new();
    private int _navIndex = 0;
    private float _nextRepathTime = 0f;

    // Expose this in inspector if you want different body sizes
    // --- Navigation (direct ValheimPathfinding) ---
    [SerializeField] private ValheimPathfinding.AgentType navAgentType = ValheimPathfinding.AgentType.HumanoidBig;
    private readonly PathRunner _nav = new();
    private readonly OrbitRunner _orbit = new();

    private void Awake()
    {
      InitCoroutineHandlers();
      Instances.Add(this);

      huntBehaviorState.behaviorConfig = huntBehaviorConfig;

      if (!abilityManager) abilityManager = GetComponent<AbilityManager>();
      if (!movementController) movementController = GetComponent<XenoAIMovementController>();
      if (!animationController) animationController = GetComponentInChildren<XenoAnimationController>();
      if (movementController) movementController.OwnerAI = this;
      if (animationController) animationController.OwnerAI = this;

      Health = MaxHealth;

      BindBehaviors();
    }

    private void Update()
    {
      animationController.ResetBloodCooldown();
    }

    private void FixedUpdate()
    {
      lastTouchedLand += Time.fixedDeltaTime;
      if (CurrentState == XenoAIState.Dead) return;
      if (!IsGrounded()) return;
      if (IsManualControlling)
      {
        return;
      }
      UpdateDeltaPrimaryTarget();
      TryUpdateAIState();
      UpdateAIMovement();
    }

    private void OnEnable()
    {
      InitCoroutineHandlers();
      Instances.Add(this);
    }
    private void OnDisable()
    {
      Instances.Remove(this);
    }

    private void OnCollisionEnter(Collision other)
    {
      // UpdateLastTouchGround(other.collider);
    }
    private void OnCollisionStay(Collision other)
    {
      UpdateLastTouchGround(other.collider);
    }

    public void SetManualControls(bool isControlling)
    {
      IsManualControlling = isControlling;
    }

    private void InitCoroutineHandlers()
    {
      huntBehaviorState.Setup(this);
      _sleepRoutine ??= new CoroutineHandle(this);
      _aiStateUpdateRoutine ??= new CoroutineHandle(this);
    }

    public void UpdateLastTouchGround(Collider otherCollider)
    {
      if (!otherCollider) return;
      if (LayerHelpers.IsContainedWithinLayerMask(LayerHelpers.GroundLayers, otherCollider.gameObject.layer))
      {
        lastTouchedLand = 0f;
      }
    }

    public bool IsGrounded()
    {
      return lastTouchedLand < 0.5f;
    }

    public void TryUpdateAIState()
    {
      if (!_aiStateUpdateRoutine.IsRunning)
      {
        _aiStateUpdateRoutine.Start(UpdateAIStateRoutine());
      }
    }
    public IEnumerator UpdateAIStateRoutine()
    {
      UpdateAllBehaviors();
      yield return new WaitForSeconds(0.5f);
    }

    // --- State Orchestration (unchanged from previous message) ---
    public void UpdateAIMovement()
    {
      if (IsManualControlling) return;
      switch (CurrentState)
      {
        case XenoAIState.Dead:
          break;
        case XenoAIState.Flee:
          movementController.MoveFleeWithAllyBias(FindNearestAlly(), GetAllEnemies());
          break;
        case XenoAIState.Roam:
          movementController.MoveWander();
          break;
        case XenoAIState.Hunt:
          Update_HuntMovement(); // Only moves, does not pick sub-state!
          break;
        case XenoAIState.Attack:
          if (!PrimaryTarget) return;
          FollowPathOrChase(PrimaryTarget.position);
          // movementController.MoveChaseTarget(
          //   PrimaryTarget.position,
          //   null,
          //   movementController.closeRange,
          //   movementController.moveSpeed,
          //   movementController.closeMoveSpeed,
          //   movementController.AccelerationForceSpeed,
          //   movementController.closeAccelForce,
          //   movementController.turnSpeed,
          //   movementController.closeTurnSpeed
          // );
          break;
        case XenoAIState.Sleeping:
          animationController.PlaySleepingAnimation(CanSleepAnimate);
          break;
        case XenoAIState.Idle:
          break;
        default:
          LoggerProvider.LogDebugDebounced("Invalid movement state");
          break;
      }
    }

    // --- State Setters ---
    public void SetDead()
    {
      if (CurrentState == XenoAIState.Dead) return;
      CurrentState = XenoAIState.Dead;
      animationController?.PlayDead();
      if (movementController && movementController.Rb) movementController.Rb.isKinematic = true;
    }
    public void StartSleeping()
    {
      CurrentState = XenoAIState.Sleeping;
      movementController.Rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void StopSleeping()
    {
      if (CurrentState != XenoAIState.Sleeping) return;
      if (_sleepRoutine.IsRunning)
      {
        _sleepRoutine.Stop();
      }
      CurrentState = XenoAIState.Idle;
      animationController.EnableAnimator();
      animationController.PlayAwake();
      if (movementController && movementController.Rb)
      {
        movementController.Rb.rotation = Quaternion.Euler(0, movementController.Rb.rotation.eulerAngles.y, 0);
        movementController.Rb.constraints = FreezeXZ;
        movementController.Rb.isKinematic = false;
      }
    }
    public void StartAttackBehavior()
    {
      if (CurrentState == XenoAIState.Dead) return;
      CurrentState = XenoAIState.Attack;
      abilityManager.camouflageAbility.Deactivate();
      animationController.PlayAttack();
    }
    public void StopAttackBehavior()
    {
      if (CurrentState == XenoAIState.Dead) return;
      if (CurrentState != XenoAIState.Attack)
      {
        animationController.StopAttack();
        return;
      }

      if (CurrentState == XenoAIState.Attack)
      {
        animationController.StopAttack();
        if (PrimaryTarget)
        {
          CurrentState = XenoAIState.Hunt;
        }
        else
        {
          CurrentState = XenoAIState.Roam;
        }
      }
    }
    public void ApplyDamage(float damage)
    {
      animationController?.PlayBloodEffect();
      Health = Mathf.Max(Health - damage, 0f);
      if (Health <= 0.1f) SetDead();
    }

    // --- Targeting helpers ---
    public void UpdatePrimaryTarget()
    {
      if (CachedPrimaryTargetXeno && CachedPrimaryTargetXeno.CurrentState == XenoAIState.Dead)
      {
        PrimaryTarget = null;
        CachedPrimaryTargetXeno = null;
      }
      if (!PrimaryTarget)
      {
        PrimaryTarget = null;
        CachedPrimaryTargetXeno = null;
      }

      if (!EnemyRegistry.TryGetClosestEnemy(transform, huntBehaviorConfig.maxTargetDistance, out var primaryTargetTransform))
      {
        return;
      }

      PrimaryTarget = primaryTargetTransform;
      PrimaryTargetRB = primaryTargetTransform.GetComponent<Rigidbody>();

      CurrentState = XenoAIState.Hunt;
      // var target = GetClosestTargetDifferentPack();
      // if (target)
      // {
      //   PrimaryTarget = target.transform;
      //   CachedPrimaryTargetXeno = target;
      // }
    }
    public XenoDroneAI GetClosestTargetDifferentPack()
    {
      XenoDroneAI closest = null;
      var closestDist = float.MaxValue;
      foreach (var xeno in Instances)
      {
        if (xeno == this || xeno.CurrentState == XenoAIState.Dead || xeno.PackId == PackId)
          continue;
        var dist = Vector3.Distance(transform.position, xeno.transform.position);
        if (dist < closestDist)
        {
          closest = xeno;
          closestDist = dist;
        }
      }
      return closest;
    }
    public XenoDroneAI FindNearestAlly()
    {
      XenoDroneAI closest = null;
      var closestDist = float.MaxValue;
      foreach (var xeno in Instances)
      {
        if (xeno == this || xeno.Health <= 0.1f || xeno.CurrentState == XenoAIState.Flee || xeno.PackId != PackId)
          continue;
        var dist = Vector3.Distance(transform.position, xeno.transform.position);
        if (dist < closestDist)
        {
          closest = xeno;
          closestDist = dist;
        }
      }
      return closest;
    }
    public HashSet<GameObject> GetAllEnemies()
    {
      var result = new HashSet<GameObject>();
      foreach (var xeno in Instances)
      {
        if (xeno != this && xeno.CurrentState != XenoAIState.Dead)
          result.Add(xeno.gameObject);
      }
      return result;
    }

    // --- Sleep Coroutine Logic ---
    private IEnumerator SleepCoroutine()
    {
      animationController.PlaySleep();
      yield return new WaitForSeconds(2f);
      animationController.DisableAnimator();
      yield return new WaitForSeconds(TimeUntilWake);
      StopSleeping();
    }

    private void UpdateDeltaPrimaryTarget()
    {
      if (!PrimaryTarget) return;
      var position = transform.position;
      if (PrimaryTargetRB)
      {
        var closestPrimaryTargetPoint = PrimaryTargetRB.ClosestPointOnBounds(position);
        var closestCurrentTargetPoint = movementController.Rb.ClosestPointOnBounds(closestPrimaryTargetPoint);
        DeltaPrimaryTarget = Vector3.Distance(closestCurrentTargetPoint, closestPrimaryTargetPoint);
      }
      else
      {
        var primaryTargetPosition = PrimaryTarget.position;
        var closestPointOnCurrentTransform = movementController.Rb.ClosestPointOnBounds(primaryTargetPosition);
        DeltaPrimaryTarget = Vector3.Distance(closestPointOnCurrentTransform, primaryTargetPosition);
      }
    }


    #region FixedUpdates / Per fixed frame logic

    public void Update_HuntMovement()
    {
      if (!PrimaryTarget) return;
      if (abilityManager.IsDodging) return;

      // Always point head at the target for creep factor
      animationController.PointHeadTowardTarget(transform, PrimaryTarget);

      if (IsOutOfHuntRange())
      {
        UpdateAllBehaviors();
        return;
      }

      // --- OVERRIDE: Retreat if in leap range and being looked at ---
      var inLeapRange = IsInLeapRange();

      var beingWatched = TargetingUtil.IsTargetLookingAtMe(PrimaryTarget, transform);


      // If within leap range and NOT being watched
      if (inLeapRange && huntBehaviorConfig.enableLeaping)
      {
        if (!beingWatched)
        {
          // 1. Lerp-rotate toward target (not snap!)
          var toTarget = PrimaryTarget.position - transform.position;
          toTarget.y = 0f;
          var angleToTarget = Vector3.Angle(transform.forward, toTarget.normalized);
          movementController.RotateTowardsDirection(toTarget, movementController.turnSpeed * 1.25f);

          // 2. Only leap if *almost* facing the target
          if (angleToTarget < _attackYawThreshold)
          {
            abilityManager?.RequestDodge(new Vector2(0, 1)); // Leap forward
          }
        }
        else
        {
          RetreatFromPrimaryTarget();
        }
        return;
      }

      switch (HuntBehaviorState)
      {
        case HuntBehaviorState.MovingAway:
          if (!huntBehaviorConfig.enableRetreating) return;
          RetreatFromPrimaryTarget();
          return;

        case HuntBehaviorState.Pausing:
          var toEnemy = PrimaryTarget.position - transform.position;
          toEnemy.y = 0f;
          movementController.RotateTowardsDirection(toEnemy, movementController.turnSpeed);
          movementController.BrakeHard();
          return;

        case HuntBehaviorState.Creeping:
          if (!huntBehaviorConfig.enableCreeping) return;

          var to = PrimaryTarget.position - transform.position;
          to.y = 0f;

          // movementController.DebugSimRouteTo(PrimaryTarget.position);

          // Only try to climb if **actually** blocked in the forward direction
          // if (movementController.IsForwardBlocked() &&
          //     movementController.TryWallClimbWhenBlocked(to))
          //   return;

          FollowPathOrChase(PrimaryTarget.position);

          return;

        case HuntBehaviorState.Circling:
        default:
          if (!huntBehaviorConfig.enableCircling) return;
          var cfg = huntBehaviorConfig;
          _orbit.agent = navAgentType;
          _orbit.Tick(
            transform,
            PrimaryTarget,
            huntBehaviorState.circleDirection,
            cfg.circleMoveSpeed,
            cfg.minCircleRadius,
            cfg.maxCircleRadius,
            cfg.circleRadiusFactor,
            (corner) => movementController.MoveTowardsTarget(
              corner,
              movementController.moveSpeed,
              movementController.AccelerationForceSpeed,
              movementController.turnSpeed),
            () =>
              movementController.CircleAroundTarget(PrimaryTarget, huntBehaviorState.circleDirection, cfg.circleMoveSpeed)
          );

          // var speedFluxCircling = Random.Range(0.75f, 1.25f);
          // movementController.CircleAroundTarget(PrimaryTarget, huntBehaviorState.circleDirection, huntBehaviorConfig.circleMoveSpeed * speedFluxCircling);
          return;
      }
    }

    #endregion

    #region Animation getters

    // Animator/Bones
    public Transform xenoAnimatorRoot => animationController?.xenoAnimatorRoot;
    public Transform xenoRoot => animationController?.xenoRoot;
    public Transform spine01 => animationController?.spine01;
    public Transform spine02 => animationController?.spine02;
    public Transform spine03 => animationController?.spine03;
    public Transform spineTop => animationController?.spineTop;
    public Transform neckUpDown => animationController?.neckUpDown;
    public Transform neckPivot => animationController?.neckPivot;
    public Transform leftHip => animationController?.leftHip;
    public Transform rightHip => animationController?.rightHip;
    public Transform tailRoot => animationController?.tailRoot;
    public Transform leftArm => animationController?.leftArm;
    public Transform rightArm => animationController?.rightArm;
    public Transform leftToeTransform => animationController?.leftToeTransform;
    public Transform rightToeTransform => animationController?.rightToeTransform;

// Collections
    public HashSet<Transform> leftArmJoints => animationController?.leftArmJoints;
    public HashSet<Transform> rightArmJoints => animationController?.rightArmJoints;
    public HashSet<Transform> tailJoints => animationController?.tailJoints;
    public HashSet<Collider> allColliders => animationController?.allColliders;

// Optional: attack collider names
    public string ARMAttackObjName => animationController?.armAttackObjName;
    public string tailAttackObjName => animationController?.tailAttackObjName;

    #endregion

    #region Behavior Updates

    private void BindBehaviors()
    {
      _behaviorUpdaters = new List<Func<bool>>
      {
        Update_Death,
        Update_Flee,
        Update_HuntBehavior,
        Update_AttackTargetBehavior,
        Update_Roam,
        Update_SleepBehavior
      };
    }

    public void UpdateAllBehaviors()
    {
      if (IsDead() || IsSleeping()) return;

      UpdatePrimaryTarget();

      // Call each updater until one returns true (bail).
      var hasBailed = false;
      foreach (var behaviorUpdater in _behaviorUpdaters)
      {
        var result = behaviorUpdater.Invoke();
        if (!result) continue;
        // (Optional) Log bailing for dev debugging
        LoggerProvider.LogDevDebounced($"Bailed on {behaviorUpdater.Method.Name}");
        hasBailed = true;
        break;
      }
      if (hasBailed) return;
    }


    public bool Update_Death()
    {
      if (Health <= 0.1f)
      {
        SetDead();
        return true;
      }

      return false;
    }
    public bool Update_Flee()
    {
      var isFleeing = IsFleeing();
      var shouldFlee = Health < 30f;
      if (shouldFlee)
      {
        if (!isFleeing)
        {
          CurrentState = XenoAIState.Flee;
        }
        // FleeTowardSafeAllyOrRunAway();
        return true;
      }

      if (isFleeing)
      {
        // StopFleeing();
      }

      return false;
    }

    public bool Update_HuntBehavior()
    {
      // early bail if we are in range and can attack.
      if (huntBehaviorConfig.enableAttack && IsInAttackRange())
      {
        var percentage = Random.value;
        if (percentage > huntBehaviorConfig.probAttackInRange) return false;
      }

      huntBehaviorState.SyncSharedData(new BehaviorStateSync
      {
        PrimaryTarget = PrimaryTarget,
        DeltaPrimaryTarget = DeltaPrimaryTarget,
        Self = transform
      });
      if (huntBehaviorState.DecisionTimer < 0)
      {
        TryTriggerCamouflage();
      }
      if (!huntBehaviorState.TryUpdateBehavior()) return false;
      // run other effects

      if (CurrentState == XenoAIState.Attack)
      {
        StopAttackBehavior();
      }
      CurrentState = XenoAIState.Hunt;
      return true;
    }

    private HuntBehaviorState HuntBehaviorState => huntBehaviorState.State;

    public void RetreatFromPrimaryTarget()
    {
      if (!PrimaryTarget) return;
      // Option 1: Back away while facing target
      movementController.MoveAwayFromTarget(
        PrimaryTarget.position,
        movementController.moveSpeed,
        movementController.AccelerationForceSpeed * 0.5f,
        movementController.turnSpeed
      );
      animationController.PointHeadTowardTarget(transform, PrimaryTarget);
    }

    public bool Update_Roam()
    {
      if (movementController.HasRoamTarget || movementController.TryUpdateCurrentWanderTarget())
      {
        CurrentState = XenoAIState.Roam;
        return true;
      }

      return false;
    }

    private readonly float _attackYawThreshold = 10f; // Degrees allowed for facing before leaping

    public void TryTriggerCamouflage()
    {
      if (!huntBehaviorConfig.enableRandomCamouflage) return;
      var rand = Random.value;
      if (rand < huntBehaviorConfig.probCamouflage)
      {
        abilityManager.camouflageAbility.Activate();
      }
    }

    public bool Update_AttackTargetBehavior()
    {
      if (!PrimaryTarget) return false;
      var isInAttackRange = IsInAttackRange();

      var chanceToAttack = Random.value;
      if (chanceToAttack < 0.25f)
      {
        return false;
      }

      if (isInAttackRange)
      {
        StartAttackBehavior();
      }
      else
      {
        StopAttackBehavior();
      }
      return true;
    }

    public bool Update_SleepBehavior()
    {
      if (movementController.HasRoamTarget || PrimaryTarget != null && PrimaryTarget.gameObject.activeInHierarchy) return false;
      StartSleeping();
      return true;
    }

    #endregion


    #region State Booleans

    public bool IsOutOfHuntRange()
    {
      return DeltaPrimaryTarget > huntBehaviorConfig.maxTargetDistance;
    }

    public bool IsWithinHuntingRange()
    {
      if (!PrimaryTarget) return false;
      return DeltaPrimaryTarget >= huntBehaviorConfig.minHuntDistance && DeltaPrimaryTarget <= huntBehaviorConfig.maxTargetDistance;
    }

    public bool IsInLeapRange()
    {
      var minDodgeDistance = abilityManager.dodgeAbility.config.forwardDistance / 3f;
      return DeltaPrimaryTarget < minDodgeDistance && DeltaPrimaryTarget < abilityManager.dodgeAbility.config.forwardDistance;
    }

    public bool IsInAttackRange()
    {
      if (PrimaryTarget == null) return false;
      if (!HasClearLOS(PrimaryTarget.position)) return false;
      return DeltaPrimaryTarget < closeRange;
    }

    public bool IsSleeping()
    {
      return CurrentState == XenoAIState.Sleeping;
    }

    public bool IsDead()
    {
      return CurrentState == XenoAIState.Dead;
    }

    public bool IsFleeing()
    {
      return CurrentState == XenoAIState.Flee;
    }
    public bool IsAttacking()
    {
      return CurrentState == XenoAIState.Attack;
    }

    #endregion

    // Put these inside XenoDroneAI (e.g., near bottom) to avoid new files.

    #region Nav Runners (direct ValheimPathfinding)

    private bool HasClearLOS(Vector3 to)
    {
      var from = transform.position + Vector3.up * 0.2f + transform.forward * 1f;

      var dir = to - from;
      var result = !Physics.Raycast(from, dir.normalized, out var hit, dir.magnitude,
        LayerMask.GetMask("Default", "static_solid", "piece", "terrain"));
      Debug.DrawLine(from, to, result ? Color.green : Color.red);

      return result;
    }

    private void FollowPathOrChase(Vector3 targetPos)
    {
      // 0) If we have line of sight, keep the fast chase
      if (HasClearLOS(targetPos))
      {
        movementController.MoveChaseTarget(
          targetPos,
          null,
          movementController.closeRange,
          movementController.moveSpeed,
          movementController.closeMoveSpeed,
          movementController.AccelerationForceSpeed,
          movementController.closeAccelForce,
          movementController.turnSpeed,
          movementController.closeTurnSpeed
        );
        _nav.Clear();
        return;
      }

      // 1) If there’s vertical separation, look for ingress to target’s floor
      var verticalSep = Mathf.Abs(transform.position.y - targetPos.y);
      if (verticalSep > 1.25f) // tweak threshold for your building heights
      {
        if (movementController.TryFindIngressToTargetFloor(
              targetPos,
              out var ingressMovePoint,
              out var landingPoint,
              10f, // slightly larger for buildings
              1.25f,
              6f,
              20,
              0.75f))
        {
          // Move to the opening
          movementController.MoveTowardsTarget(
            ingressMovePoint,
            movementController.moveSpeed,
            movementController.AccelerationForceSpeed,
            movementController.turnSpeed
          );

          // If we’re at the hole, perform the drop/jump
          if (movementController.TryExecuteIngressJump(ingressMovePoint, landingPoint))
            return;

          return; // keep driving toward ingress until we jump
        }
        // If we didn't find an ingress, keep going to pathing fallback below.
      }

      // 2) Short pathing toward target (same as before)
      var pathStart = transform.position;
      if (_nav.EnsurePath(pathStart, targetPos, navAgentType, 0.5f)
          && _nav.TryStep(transform.position, 0.75f, out var corner))
      {
        movementController.MoveTowardsTarget(
          corner,
          movementController.moveSpeed,
          movementController.AccelerationForceSpeed,
          movementController.turnSpeed
        );
      }
      else
      {
        movementController.BrakeHard(); // don’t ram walls when stuck
      }
    }

    private sealed class PathRunner
    {
      private readonly List<Vector3> _corners = new();
      private int _i;
      private float _nextRepathAt;

      public bool EnsurePath(Vector3 from, Vector3 to, ValheimPathfinding.AgentType agent, float cooldown = 0.5f)
      {
        if (Time.time < _nextRepathAt && _i < _corners.Count) return true;
        _corners.Clear();
        _i = 0;
        var ok = ValheimPathfinding.instance.GetPath(from, to, _corners, agent);
        _nextRepathAt = Time.time + cooldown;
        return ok && _corners.Count > 0;
      }

      public bool TryStep(Vector3 selfPos, float reach, out Vector3 corner)
      {
        corner = default;
        if (_i >= _corners.Count) return false;
        var here = new Vector3(selfPos.x, 0, selfPos.z);
        var next = new Vector3(_corners[_i].x, 0, _corners[_i].z);
        if ((here - next).sqrMagnitude <= reach * reach) _i++;
        if (_i >= _corners.Count) return false;
        corner = _corners[_i];
        return true;
      }

      public void Clear()
      {
        _corners.Clear();
        _i = 0;
      }
    }

    private sealed class OrbitRunner
    {
      private readonly List<Vector3> _path = new();
      private int _idx;
      private Vector3 _anchor;
      private bool _haveAnchor;
      private float _nextPlanAt;

      public ValheimPathfinding.AgentType agent = ValheimPathfinding.AgentType.Humanoid;
      public float cornerReach = 0.75f;
      public float arcDeg = 30f;
      public int samples = 8;

      public void Reset()
      {
        _path.Clear();
        _idx = 0;
        _haveAnchor = false;
      }

      public void Tick(Transform self, Transform target, int dir, float baseSpeed,
        float minR, float maxR, float scale,
        Action<Vector3> moveToCorner,
        Action fallbackMove)
      {
        if (!self || !target) return;

        var toT = target.position - self.position;
        toT.y = 0;
        var dist = toT.magnitude;
        var desiredR = Mathf.Clamp(dist * scale, minR, maxR);

        if (!_haveAnchor || Reached(self.position, _anchor, cornerReach))
        {
          if (!PickAnchor(self.position, target.position, desiredR, dir, out _anchor))
          {
            fallbackMove?.Invoke();
            return;
          }
          _haveAnchor = true;
          _path.Clear();
          _idx = 0;
        }

        if (_path.Count == 0 && Time.time >= _nextPlanAt)
        {
          _nextPlanAt = Time.time + 0.35f;
          if (!ValheimPathfinding.instance.GetPath(self.position, _anchor, _path, agent))
          {
            _haveAnchor = false;
            fallbackMove?.Invoke();
            return;
          }
        }

        if (_path.Count == 0)
        {
          fallbackMove?.Invoke();
          return;
        }

        var corner = _path[Mathf.Clamp(_idx, 0, _path.Count - 1)];
        if (Reached(self.position, corner, cornerReach))
        {
          _idx++;
          if (_idx >= _path.Count)
          {
            _haveAnchor = false;
            return;
          }
          corner = _path[_idx];
        }
        moveToCorner(corner);
      }

      private bool PickAnchor(Vector3 self, Vector3 tgt, float r, int dir, out Vector3 anchor)
      {
        var best = Vector3.zero;
        var bestScore = float.NegativeInfinity;

        var toT = tgt - self;
        toT.y = 0;
        var baseYaw = Mathf.Atan2(toT.z, toT.x) * Mathf.Rad2Deg;
        var step = arcDeg / Mathf.Max(1, samples - 1);

        for (var i = 0; i < samples; i++)
        {
          var yaw = baseYaw + dir * (arcDeg * 0.5f + i * step);
          var rad = yaw * Mathf.Deg2Rad;
          var raw = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * r + tgt;

          if (!ValheimPathfinding.instance.FindValidPoint(out var candidate, raw, 2.5f, agent))
            continue;

          var toA = candidate - tgt;
          toA.y = 0;
          var tangent = Quaternion.Euler(0, 90f * dir, 0) * toA.normalized;
          var progress = Vector3.Dot(tangent, (candidate - self).normalized);

          var reachable = ValheimPathfinding.instance.HavePath(self, candidate, agent);
          var reachBonus = reachable ? 1f : -2f;
          var distScore = -Vector3.Distance(self, candidate) * 0.1f;

          var score = progress * 2f + reachBonus + distScore;
          if (score > bestScore)
          {
            bestScore = score;
            best = candidate;
          }
        }

        anchor = best;
        return bestScore > float.NegativeInfinity;
      }

      private bool Reached(Vector3 a, Vector3 b, float r)
      {
        a.y = 0;
        b.y = 0;
        return (a - b).sqrMagnitude <= r * r;
      }
    }

    #endregion


  }
}