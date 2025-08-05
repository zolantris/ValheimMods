// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core
{
  public class XenoDroneAI : MonoBehaviour
  {
    public enum XenoAIState
    {
      Idle,
      Roam,
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
    private CoroutineHandle _aiStateUpdateRoutine;
    private List<Func<bool>> _behaviorUpdaters = new();
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
      if (IsManualControlling)
      {
        return;
      }
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
      UpdateBehavior();
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
          movementController?.MoveFleeWithAllyBias(FindNearestAlly(), GetAllEnemies());
          break;
        case XenoAIState.Roam:
          if (PrimaryTarget)
            movementController?.MoveChaseTarget(
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
          else
            movementController?.MoveWander();
          break;
        case XenoAIState.Attack:
          break;
        case XenoAIState.Sleeping:
          animationController.PlaySleepingAnimation(CanSleepAnimate);
          break;
        case XenoAIState.Idle:
        default:
          movementController?.MoveWander();
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
      animationController.PlayAttack();
    }
    public void StopAttackBehavior()
    {
      if (CurrentState == XenoAIState.Dead) return;
      if (CurrentState == XenoAIState.Attack)
      {
        animationController?.StopAttack();
        CurrentState = XenoAIState.Roam;
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
      var target = GetClosestTargetDifferentPack();
      if (target)
      {
        PrimaryTarget = target.transform;
        CachedPrimaryTargetXeno = target;
      }
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
        Update_Roam,
        Update_AttackTargetBehavior,
        Update_SleepBehavior
      };
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
    public bool Update_Roam()
    {
      // if (!PrimaryTarget)
      // {
      // }
      CurrentState = XenoAIState.Roam;
      if (movementController.HasRoamTarget) return true;
      return movementController.TryUpdateCurrentWanderTarget();
    }

    public bool Update_AttackTargetBehavior()
    {
      if (!PrimaryTarget) return false;
      var isInAttackRange = DeltaPrimaryTarget < closeRange;
      if (isInAttackRange)
      {
        StartAttackBehavior();
      }
      else
      {
        StopAttackBehavior();
        // Stop_Attack();
      }
      return true;
    }

    public bool Update_SleepBehavior()
    {
      if (movementController.HasRoamTarget || PrimaryTarget != null && PrimaryTarget.gameObject.activeInHierarchy) return false;
      StartSleeping();
      return true;
    }

    public void UpdateBehavior()
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

    #endregion


    #region State Booleans

    #region State Booleans

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

    #endregion

  }
}