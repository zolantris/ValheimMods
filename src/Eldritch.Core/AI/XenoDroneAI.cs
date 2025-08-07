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

    [SerializeField]
    public XenoHuntBehaviorConfig huntBehaviorConfig = new();
    private readonly float _circleDecisionInterval = 3f;
    private CoroutineHandle _aiStateUpdateRoutine;
    private List<Func<bool>> _behaviorUpdaters = new();

    private float _circleStateTimer;
    private bool _isCreeping;
    private bool _isMovingAway;
    private bool _isPausingToTurn;
    private CoroutineHandle _sleepRoutine;
    private Vector3 cachedLowestPoint = Vector3.zero;

    private void Awake()
    {
      InitCoroutineHandlers();
      Instances.Add(this);
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
          UpdateHuntCircleMovement(); // Only moves, does not pick sub-state!
          break;
        case XenoAIState.Attack:
          if (!PrimaryTarget) return;
          movementController.MoveChaseTarget(
            PrimaryTarget.position,
            null,
            movementController.closeRange,
            movementController.moveSpeed,
            movementController.closeMoveSpeed,
            movementController.AccelerationForceSpeed,
            movementController.closeAccelForce,
            movementController.turnSpeed,
            movementController.closeTurnSpeed
          );
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
      DeactivateCamouflage();
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

    // --- Utility ---
    public void ActivateCamouflage()
    {
      animationController?.ActivateCamouflage();
    }
    public void DeactivateCamouflage()
    {
      animationController?.DeactivateCamouflage();
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

    public void UpdateHuntCircleMovement()
    {
      if (!PrimaryTarget) return;
      if (abilityManager.IsDodging) return;
      _circleSubStateTimer -= Time.fixedDeltaTime;
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

      switch (_circleSubState)
      {
        case HuntCircleSubState.MovingAway:
          if (!huntBehaviorConfig.enableRetreating) return;
          RetreatFromPrimaryTarget();
          return;

        case HuntCircleSubState.PausingToTurn:
          var toEnemy = PrimaryTarget.position - transform.position;
          toEnemy.y = 0f;
          movementController.RotateTowardsDirection(toEnemy, movementController.turnSpeed);
          movementController.BrakeHard();
          return;

        case HuntCircleSubState.Creeping:
          if (!huntBehaviorConfig.enableRetreating) return;
          var isInCloseRange = DeltaPrimaryTarget < closeRange;
          if (!isInCloseRange || !huntBehaviorConfig.enableAttack)
          {
            // Continue creeping toward target as normal
            var targetLookingAtMe = TargetingUtil.IsTargetLookingAtMe(PrimaryTarget, transform);
            var isNearCloseRange = DeltaPrimaryTarget - 2f < closeRange;
            var speedFluxCreep = Random.Range(0.75f, 1.25f);

            // retreat if closerange or being stared down.
            var creepDir = targetLookingAtMe || isNearCloseRange ? -1f : 1f;
            var creepVec = (PrimaryTarget.position - transform.position).normalized * creepDir;
            var creepSpeed = huntBehaviorConfig.creepingMoveSpeed * speedFluxCreep;

            movementController.RotateTowardsDirection((PrimaryTarget.position - transform.position).normalized, movementController.turnSpeed);
            movementController.MoveInDirection(creepVec, creepSpeed, movementController.AccelerationForceSpeed * 0.4f, movementController.turnSpeed);
          }
          else
          {
            // INSTANTLY EXIT CREEP/TRANSITION TO ATTACK
            // Option 1: Set state to attack and break out of switch
            _circleSubState = HuntCircleSubState.Circling; // or a real Attack state if you have one
            _circleSubStateTimer = 0f; // force immediate state pick
            // Optionally: Call your attack/charge logic here
            Update_AttackTargetBehavior();
          }
          return;

        case HuntCircleSubState.Circling:
        default:
          if (!huntBehaviorConfig.enableCircling) return;
          var speedFluxCircling = Random.Range(0.75f, 1.25f);
          movementController.CircleAroundTarget(PrimaryTarget, _circleDirection, huntBehaviorConfig.circleRadius, huntBehaviorConfig.circleMoveSpeed * speedFluxCircling);
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
      var isWithinHuntRange = IsWithinHuntingRange();
      if (!isWithinHuntRange) return false;

      if (CurrentState == XenoAIState.Attack)
      {
        StopAttackBehavior();
      }

      CurrentState = XenoAIState.Hunt;

      if (_circleSubStateTimer > 0)
      {
        return true;
      }

      GetNextHuntCircleBehavior();
      // 3. Check for leap opportunity
      // if (CanLeapAtTarget())
      // {
      //   StartLeapAttack(); // Play leap animation, set state, timers, etc.
      //   return true;
      // }

      // 4. After attack, maybe retreat (flavor/creepiness logic)
      // if (ShouldRetreatAfterAttack())
      // {
      //   StartRetreat(); // Run through or away from enemy
      //   return true;
      // }
      // RetreatFromPrimaryTarget();

      return true;
    }

    private enum HuntCircleSubState
    {
      Circling,
      MovingAway,
      PausingToTurn,
      Creeping
    }

    private HuntCircleSubState _circleSubState = HuntCircleSubState.Circling;
    private float _circleSubStateTimer;

// Circle parameters
    private int _circleDirection = 1;
    private const float _circleDecisionIntervalMin = 2.2f;
    private const float _circleDecisionIntervalMax = 3.4f;

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

    private void GetNextHuntCircleBehavior()
    {
      var rand = Random.value;
      if (huntBehaviorConfig.FORCE_Circling)
      {
        _circleDirection *= rand < 0.40f ? -1 : 1;
        _circleSubState = HuntCircleSubState.Circling;
        _circleSubStateTimer = Random.Range(_circleDecisionIntervalMin, _circleDecisionIntervalMax);
        return;
      }

      var cursor = 0f;

      if (rand < huntBehaviorConfig.probCamouflage)
      {
        ActivateCamouflage();
      }
      else
      {
        DeactivateCamouflage();
      }

      if (rand < (cursor += huntBehaviorConfig.probMovingAway))
      {
        _circleSubState = HuntCircleSubState.MovingAway;
        _circleSubStateTimer = Random.Range(huntBehaviorConfig.movingAwayTimeRange.x, huntBehaviorConfig.movingAwayTimeRange.y);
      }
      else if (rand < (cursor += huntBehaviorConfig.probPausingToTurn))
      {
        _circleSubState = HuntCircleSubState.PausingToTurn;
        _circleDirection *= -1;
        _circleSubStateTimer = Random.Range(huntBehaviorConfig.pausingToTurnTimeRange.x, huntBehaviorConfig.pausingToTurnTimeRange.y);
      }
      else if (rand < (cursor += huntBehaviorConfig.probCreeping))
      {
        _circleSubState = HuntCircleSubState.Creeping;
        _circleSubStateTimer = Random.Range(huntBehaviorConfig.creepingTimeRange.x, huntBehaviorConfig.creepingTimeRange.y);
      }
      else
      {
        _circleSubState = HuntCircleSubState.Circling;
        _circleSubStateTimer = Random.Range(huntBehaviorConfig.circlingTimeRange.x, huntBehaviorConfig.circlingTimeRange.y);
      }
    }

    public bool Update_AttackTargetBehavior()
    {
      if (!PrimaryTarget) return false;
      var isInAttackRange = IsInAttackRange();
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

  }
}