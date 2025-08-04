// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using Zolantris.Shared;

namespace Eldritch.Core
{
    public class XenoDroneAI : MonoBehaviour
    {
        public enum XenoAIState { Idle, Roam, Attack, Flee, Dead, Sleeping }
        public static readonly HashSet<XenoDroneAI> Instances = new();

        [Header("Controller References")]
        [SerializeField] public XenoAIMovementController Movement;
        [SerializeField] public XenoAIAnimationController Animation;
 
        #region Animation getters
        // Animator/Bones
        public Transform xenoAnimatorRoot => Animation?.xenoAnimatorRoot;
        public Transform xenoRoot => Animation?.xenoRoot;
        public Transform spine01 => Animation?.spine01;
        public Transform spine02 => Animation?.spine02;
        public Transform spine03 => Animation?.spine03;
        public Transform spineTop => Animation?.spineTop;
        public Transform neckUpDown => Animation?.neckUpDown;
        public Transform neckPivot => Animation?.neckPivot;
        public Transform leftHip => Animation?.leftHip;
        public Transform rightHip => Animation?.rightHip;
        public Transform tailRoot => Animation?.tailRoot;
        public Transform leftArm => Animation?.leftArm;
        public Transform rightArm => Animation?.rightArm;
        public Transform leftToeTransform => Animation?.leftToeTransform;
        public Transform rightToeTransform => Animation?.rightToeTransform;

// Collections
        public HashSet<Transform> leftArmJoints => Animation?.leftArmJoints;
        public HashSet<Transform> rightArmJoints => Animation?.rightArmJoints;
        public HashSet<Transform> tailJoints => Animation?.tailJoints;
        public HashSet<Collider> allColliders => Animation?.allColliders;
        public HashSet<Collider> attackColliders => Animation?.attackColliders;

// Optional: attack collider names
        public string ARMAttackObjName => Animation?.armAttackObjName;
        public string tailAttackObjName => Animation?.tailAttackObjName;
        #endregion
        // State
        public XenoAIState CurrentState = XenoAIState.Idle;
        public Transform PrimaryTarget;
        [CanBeNull] public XenoDroneAI CachedPrimaryTargetXeno;
        public int PackId;
        public float Health, MaxHealth = 100f;

        [Header("Timers")]
        public float TimeUntilSleep = 5f, TimeUntilWake = 50f;
        private CoroutineHandle _sleepRoutine;
        private CoroutineHandle _aiStateUpdateRoutine;

        public bool HasCamouflage;
        public bool CanSleepAnimate;
        public bool CanJump = true;


        public float DeltaPrimaryTarget;
        public bool IsHiding = true;

        // Grounding
        public float lastTouchedLand = 0f;
        public float lastLowestPointCheck;
        private Vector3 cachedLowestPoint = Vector3.zero;
        [SerializeField] public bool IsManualControlling;
        private List<Func<bool>> _behaviorUpdaters = new();
        
        public float closeRange = 1f;

        public void SetManualControls(bool isControlling)
        {
            IsManualControlling = isControlling;
        }
        
        private void Awake()
        {
            InitCoroutineHandlers();
            Instances.Add(this);
            if (!Movement) Movement = GetComponent<XenoAIMovementController>();
            if (!Animation) Animation = GetComponent<XenoAIAnimationController>();
            Health = MaxHealth;
            if (Movement) Movement.OwnerAI = this;
            
            allColliders.UnionWith(GetComponentsInChildren<Collider>());
            attackColliders.UnionWith(allColliders); // Can filter specific attack colliders here if needed

            BindBehaviors();
        }

        private void InitCoroutineHandlers()
        {
            _sleepRoutine ??= new CoroutineHandle(this);
            _aiStateUpdateRoutine ??= new CoroutineHandle(this);
        }

        private void OnEnable()
        {
            InitCoroutineHandlers();
            Instances.Add(this);
        }
        private void OnDisable() => Instances.Remove(this);

        private void Update()
        {
            Animation.ResetBloodCooldown();
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
            if (Movement.HasRoamTarget) return true;
            return Movement.TryUpdateCurrentWanderTarget();
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
            if (Movement.HasRoamTarget || PrimaryTarget != null && PrimaryTarget.gameObject.activeInHierarchy) return false;
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

        public void UpdateLastTouchGround(Collider otherCollider)
        {
            if (!otherCollider) return;
            if (LayerHelpers.IsContainedWithinLayerMask(LayerHelpers.GroundLayers, otherCollider.gameObject.layer))
            {
                lastTouchedLand = 0f;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            // UpdateLastTouchGround(other.collider);
        }
        private void OnCollisionStay(Collision other)
        {
            UpdateLastTouchGround(other.collider);
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
                    Movement?.MoveFleeWithAllyBias(FindNearestAlly(), GetAllEnemies());
                    break;
                case XenoAIState.Roam:
                    if (PrimaryTarget)
                        Movement?.MoveChaseTarget(
                            PrimaryTarget.position,
                            null,
                            Movement.closeRange,
                            Movement.moveSpeed,
                            Movement.closeMoveSpeed,
                            Movement.AccelerationForceSpeed,
                            Movement.closeAccelForce,
                            Movement.turnSpeed,
                            Movement.closeTurnSpeed
                        );
                    else
                        Movement?.MoveWander();
                    break;
                case XenoAIState.Attack:
                    break;
                case XenoAIState.Sleeping:
                    Animation.ScheduleSleepingAnimation(CanSleepAnimate);
                    break;
                case XenoAIState.Idle:
                default:
                    Movement?.MoveWander();
                    break;
            }
        }

        // --- State Setters ---
        public void SetDead()
        {
            if (CurrentState == XenoAIState.Dead) return;
            CurrentState = XenoAIState.Dead;
            Animation?.PlayDead();
            if (Movement && Movement.Rb) Movement.Rb.isKinematic = true;
        }
        public void StartSleeping()
        {
            if (CurrentState == XenoAIState.Sleeping) return;
            CurrentState = XenoAIState.Sleeping;
            if (_sleepRoutine.IsRunning) return;
            _sleepRoutine.Start(SleepCoroutine());
            if (Movement && Movement.Rb) Movement.Rb.isKinematic = true;
        }
        public void StopSleeping()
        {
            if (CurrentState != XenoAIState.Sleeping) return;
            if (_sleepRoutine.IsRunning)
            {
                _sleepRoutine.Stop();
            }
            CurrentState = XenoAIState.Idle;
            Animation.EnableAnimator();
            Animation.PlayAwake();
            if (Movement && Movement.Rb) Movement.Rb.isKinematic = false;
        }
        public void StartAttackBehavior()
        {
            if (CurrentState == XenoAIState.Dead) return;
            CurrentState = XenoAIState.Attack;
            Animation.PlayAttack();
        }
        public void StopAttackBehavior()
        {
            if (CurrentState == XenoAIState.Dead) return;
            if (CurrentState == XenoAIState.Attack)
            {
                Animation?.StopAttack();
                CurrentState = XenoAIState.Roam;
            }
        }
        public void ApplyDamage(float damage)
        {
            Animation?.PlayBloodEffect();
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
            float closestDist = float.MaxValue;
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
            float closestDist = float.MaxValue;
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
            Animation.PlaySleep();
            yield return new WaitForSeconds(2f);
            Animation.DisableAnimator();
            yield return new WaitForSeconds(TimeUntilWake);
            StopSleeping();
        }

        // --- Utility ---
        public void ActivateCamouflage() => Animation?.ActivateCamouflage();
        public void DeactivateCamouflage() => Animation?.DeactivateCamouflage();


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
