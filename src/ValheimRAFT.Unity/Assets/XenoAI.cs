#region
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class XenoDroneAI : MonoBehaviour
    {

        public enum XenoAIState { Idle, Hunt, Attack, Flee, Dead, Sleeping }

        public static readonly HashSet<XenoDroneAI> Instances = new();

        public static Quaternion SleepNeckZRotation = Quaternion.Euler(0, 0, 80f);

        public static readonly float IdleThresholdTime = 0.0001f;
        [Header("Movement Tuning")]
        public float moveSpeed = 1f;                // Normal approach speed
        public float closeMoveSpeed = 0.3f;         // Slower speed near target
        public float AccelerationForceSpeed = 90f;  // Normal acceleration
        public float closeAccelForce = 20f;         // Reduced acceleration near target
        public float closeRange = 3f;             // Distance for “slow down”

        [Header("Rotation Tuning")]
        public float turnSpeed = 720f;
        public float closeTurnSpeed = 540f;

        [Header("Targeting Controls")]
        public bool hasRandomTarget;
        public float cachedDeltaPrimaryTarget = -1;
        public Transform PrimaryTarget;
        [Header("Assign the tail root joint (e.g. XenosBiped_TailRoot_SHJnt)")]
        public Transform tailRoot;

        [SerializeField] public float tailMax;
        [SerializeField] public float tailMin = 5f;
        [SerializeField] public bool isHiding = true;

        [SerializeField] public bool canMove;

        [SerializeField] float GravityMultiplier = 5f;


        public float UpdatePauseTime = 2f;
        public float nextUpdateTime;
        public float cachedAttackMode;

        public ParticleSystem BloodEffects;

        public bool canPlayEffectOnFrame = true;

        [Header("Character Attributes")]
        public float health;
        public float maxHealth = 100f;
        public int packId; // Assign this per-Xeno, maybe via spawn system or prefab
        public bool hasCamouflage;

        public Vector3 maxRunRange = new Vector3(20f, 0f, 20f); // Editable in Inspector, e.g., limits to a 20x20 area
        public XenoAIState CurrentState = XenoAIState.Idle;

        [CanBeNull] public XenoDroneAI cachedPrimaryTargetXeno;

        [Header("Textures")] 
        public Material TransparentMaterial;

        public Collider HeadCollider;
        public Transform HeadColliderTransform;

        public float sleepMoveUpdateTilt;
        public float lastSleepMoveUpdate;


        [Header("AnimationTimers")]
        [SerializeField] public float timeUntilSleep = 5f;
        [SerializeField] public float timeUntilWake = 50f;

        public Quaternion SleepLeftArmRotation = Quaternion.Euler(36.1819992f,323.176208f,251.765549f);

        public bool canSleepAnimate;

        private readonly HashSet<Collider> allColliders = new();
        private readonly HashSet<Collider> attackColliders = new();

        private readonly float walkThreshold = 1.0f; // speed to trigger run (above this = run)
        private Animator _animator;
        private bool _lastCamouflageState;
        private Rigidbody _rb;

        public HashSet<(HashSet<Transform>,Transform)> allLists = new HashSet<(HashSet<Transform>,Transform)>();
        private Material BodyMaterial;

        public AnimatorStateInfo CurrentAnimation;
        private Material HeadMaterial;

        public HashSet<Transform> leftArmJoints = new HashSet<Transform>();
        public HashSet<Transform> leftLeftJoints = new HashSet<Transform>();

        private SkinnedMeshRenderer modelSkinRenderer;

        private float moveLerpVel;

        private float nextSleepUpdate;
        [CanBeNull] Rigidbody PrimaryTargetRB;
        public HashSet<Transform> rightArmJoints = new HashSet<Transform>();
        public HashSet<Transform> rightLegJoints = new HashSet<Transform>();
        private float runStart = 0.75f;     // start blending to run at this speed

        [Tooltip("List of all tail joints, from root to tip.")]
        public HashSet<Transform> tailJoints = new HashSet<Transform>();

        private float velocity;
        // getters
        public float DeltaPrimaryTarget => cachedDeltaPrimaryTarget;

        void Awake()
        {
            health = maxHealth;
            // find these easily with Gameobject -> Copy FullTransformPath from root.
            // You may need to adjust these paths to match your actual bone names/hierarchy


            BindUnOptimizedRoots();
            
            _animator = xenoAnimatorRoot.GetComponent<Animator>();
            _rb = GetComponent<Rigidbody>();
            _rb.maxLinearVelocity = 15f;
            _rb.maxAngularVelocity = 1f;

            BloodEffects = GetComponentInChildren<ParticleSystem>();

            allLists = new HashSet<(HashSet<Transform>, Transform)> { (rightArmJoints, rightArm), (leftArmJoints, leftArm), (leftLeftJoints, leftHip), (rightLegJoints, rightHip), (tailJoints, tailRoot) };

            var colliders = GetComponentsInChildren<Collider>();

            const string headColliderName = "head_collider";
            foreach (var col in colliders)
            {
                if (col.name == headColliderName)
                {
                    HeadCollider = col;
                    HeadColliderTransform = col.transform;
                }
            }

            if (!HeadColliderTransform) throw new Exception("No HeadColliderTransform");
            
            allColliders.AddRange(colliders);
            attackColliders.AddRange(colliders);

            modelSkinRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            var materials = modelSkinRenderer.materials;
            if (materials.Length == 2)
            {
                BodyMaterial = materials[0];
                HeadMaterial = materials[1];
            }
            
            

            // CollectAllBodyJoints();
            // AddCapsuleCollidersToAllJoints();
            // IgnoreAllColliders();
            
            // mainly for debugging but this will be useful as a coroutine later.
            InvokeRepeating(nameof(RegenerateHealthOverTime), 5f, 5f);

            if (hasRandomTarget)
            {
                PrimaryTarget = null;
            }

            if (PrimaryTarget)
            { 
                cachedPrimaryTargetXeno = PrimaryTarget.GetComponent<XenoDroneAI>();
            }

            InitCoroutineHandles();
        }

        void Update()
        {
            UpdateCurrentAnimationState();
        }

        public void FixedUpdate()
        {
            var rot = _rb.rotation.eulerAngles;
            rot.x = 0f;
            rot.z = 0f;
            _rb.rotation = Quaternion.Euler(rot);
            
            if (IsDead()) return;
            canPlayEffectOnFrame = !BloodEffects.isPlaying;
            UpdateTargetData();
            UpdateBehavior();
        }

        void LateUpdate()
        {
                // neckUpDown.localRotation = Quaternion.identity;
                // spineTop.localRotation = Quaternion.identity;
                // neckPivot.localRotation = Quaternion.identity;
            if (IsSleeping())
            {
                SleepingCustomAnimations();
            }
            else
            {
                PointHeadTowardTarget();
            }
            
            if (isHiding)
            {
                // Crouch the spine forward
                // spine01.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                // spine02.localRotation = Quaternion.Euler(-30f, 0f, 0f);

                // Fold the hips up to crouch legs
                leftHip.localRotation = QuaternionMerge(leftHip.localRotation, 50f);
                rightHip.localRotation = QuaternionMerge(rightHip.localRotation, 50f);

                
                var tailCurveIncrease = Random.Range(tailMin,tailMax);
                var baseIncrease = Random.Range(tailMin, tailMax);
                foreach (var tailJoint in tailJoints)
                {
                    tailCurveIncrease += baseIncrease * Random.Range(0.1f, 50f);
                    tailJoint.localRotation = Quaternion.Lerp(tailJoint.localRotation, Quaternion.Euler(tailCurveIncrease, 0, tailCurveIncrease), Time.deltaTime);
                }
            }
        }

        void OnEnable()
        {
            Instances.Add(this);
            EnemyRegistry.ActiveEnemies.Add(gameObject);
            InitCoroutineHandles();
        }

        void OnDisable()
        {
            Instances.Remove(this);
            EnemyRegistry.ActiveEnemies.Remove(gameObject);
        }

        // public void OnCollisionEnter(Collision other)
        // {
        //     if (IsDead()) return;
        //     if (!canPlayEffectOnFrame) return;
        //     if (other.body == _rb || other.transform.root == transform.root)
        //     {
        //         foreach (var otherContact in other.contacts)
        //         {
        //             Physics.IgnoreCollision(otherContact.otherCollider, otherContact.thisCollider, true);
        //         }
        //         return;
        //     }
        //     var layer =  other.gameObject.layer;
        //     if (layer != LayerHelpers.HitboxLayer) return;
        //     
        //     // var contactsHasHitbox = other.contacts.Any(x => x.otherCollider.gameObject.layer == LayerHelpers.HitboxLayer || x.thisCollider.gameObject.layer == LayerHelpers.HitboxLayer);
        //     if (layer == LayerHelpers.HitboxLayer)
        //     {
        //         LoggerProvider.LogDebugDebounced("Hit layer");
        //     }
        // }

        public void OnTriggerEnter(Collider other)
        {
            if (IsDead()) return;
            if (!canPlayEffectOnFrame) return;
            if (other.transform.root == transform.root)
            {
                LoggerProvider.LogDebugDebounced($"Hit self {other.transform}");
                return;
            }
            var layer =  other.gameObject.layer;
            if (layer != LayerHelpers.HitboxLayer) return;
            
            // var contactsHasHitbox = other.contacts.Any(x => x.otherCollider.gameObject.layer == LayerHelpers.HitboxLayer || x.thisCollider.gameObject.layer == LayerHelpers.HitboxLayer);
            if (layer == LayerHelpers.HitboxLayer)
            {
                LoggerProvider.LogDebugDebounced("Hit layer");
            }
            
            ApplyDamage(other);
        }

        public void BindOptimizedRoots()
        {
            xenoAnimatorRoot = transform.Find("model");
            xenoRoot = xenoAnimatorRoot;
            spine01 = xenoRoot.Find("XenosBiped_Spine_01SHJnt");
            spine02 = xenoRoot.Find("XenosBiped_Spine_02SHJnt");
            spine03 = xenoRoot.Find("XenosBiped_Spine_03SHJnt");
            spineTop = xenoRoot.Find("XenosBiped_Spine_TopSHJnt");
            neckUpDown = xenoRoot.Find("XenosBiped_Neck_01SHJnt");
            neckPivot = xenoRoot.Find("XenosBiped_Neck_TopSHJnt");
            
            tailRoot = xenoRoot.Find("XenosBiped_TailRoot_SHJnt");
            
            leftArm = xenoRoot.Find("XenosBiped_l_Arm_ShoulderSHJnt");
            rightArm = xenoRoot.Find("XenosBiped_r_Arm_ShoulderSHJnt");
        }

        public void BindUnOptimizedRoots()
        {
            xenoAnimatorRoot = transform.Find("model");
            xenoRoot = xenoAnimatorRoot.Find("alien_xenos_drone_SK_Xenos_Drone_skeleton/XenosBiped_TrajectorySHJnt/XenosBiped_ROOTSHJnt");
            spine01 = xenoRoot.Find("XenosBiped_Spine_01SHJnt");
            spine02 = spine01.Find("XenosBiped_Spine_02SHJnt");
            spine03 = spine02.Find("XenosBiped_Spine_03SHJnt");
            spineTop = spine03.Find("XenosBiped_Spine_TopSHJnt");
            neckUpDown = spineTop.Find("XenosBiped_Neck_01SHJnt");
            neckPivot = neckUpDown.Find("XenosBiped_Neck_TopSHJnt");
            
            leftHip = xenoRoot.Find("XenosBiped_l_Leg_HipSHJnt");
            rightHip = xenoRoot.Find("XenosBiped_r_Leg_HipSHJnt");
            
            tailRoot = xenoRoot.Find("XenosBiped_TailRoot_SHJnt");
            leftArm = spine03.Find("XenosBiped_l_Arm_ClavicleSHJnt/XenosBiped_l_Arm_ShoulderSHJnt");
            rightArm = spine03.Find("XenosBiped_r_Arm_ClavicleSHJnt/XenosBiped_r_Arm_ShoulderSHJnt");
        }

        public void PointHeadTowardTarget()
        {
            if (!PrimaryTarget) return;
            Vector3 toTarget = PrimaryTarget.position - neckPivot.position;
            toTarget.y = 0; // Flatten for Y-only tracking

            Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);

            // Calculate desired yaw (Y) and roll (Z)
            float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -40f, 40f);

            float roll = Mathf.Lerp(40f, 90f, Mathf.Abs(yaw / 40f));
            roll = Mathf.Clamp(roll, 40f, 90f);

            Vector3 currentEuler = neckPivot.localEulerAngles;

            // Normalize
            if (currentEuler.y > 180f) currentEuler.y -= 360f;
            if (currentEuler.z > 180f) currentEuler.z -= 360f;

            // Smoothly rotate toward look direction
            currentEuler.y = Mathf.MoveTowards(currentEuler.y, yaw, Time.deltaTime * 120f);
            currentEuler.z = Mathf.MoveTowards(currentEuler.z, roll, Time.deltaTime * 60f);

            neckPivot.localEulerAngles = currentEuler;
        }

        /// <summary>
/// Animations like rotating head or moving arms to better spot
/// </summary>
        public void SleepingCustomAnimations()
        {
            if (!canSleepAnimate) return;
            if (lastSleepMoveUpdate < Time.time)
            {
                var wasPositive = sleepMoveUpdateTilt > 0;
                var baseChange = wasPositive ? -2f : 2f;
                sleepMoveUpdateTilt = Random.Range(-10f, 10f) + baseChange;
                lastSleepMoveUpdate = Time.time + 10f; 
            }
            
            LoggerProvider.LogDebugDebounced("SleepingCustomAnimations");
            
            // rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, SleepLeftArmRotation, Time.deltaTime * 30f);
            // leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, SleepLeftArmRotation, Time.deltaTime * 30f);

            Vector3 currentEuler = neckPivot.localEulerAngles;

// Normalize angles to avoid 360 wrap issues
            if (currentEuler.y > 180f) currentEuler.y -= 360f;
            if (currentEuler.z > 180f) currentEuler.z -= 360f;

// Smoothly move Y toward tilt
            currentEuler.y = Mathf.MoveTowards(currentEuler.y, sleepMoveUpdateTilt, Time.deltaTime * 30f);

// Smoothly move Z toward sleep pose (90°)
            currentEuler.z = Mathf.MoveTowards(currentEuler.z, 90f, Time.deltaTime * 60f);

            neckPivot.localEulerAngles = currentEuler;
        }

        public IEnumerator SleepingCustomAnimationsRoutine()
        {
            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 50000)
            {
                if (timer.ElapsedMilliseconds > 2000)
                {
                    _animator.enabled = false;
                }
                else
                {
                    yield return null;
                    continue;
                }
                
                // zero out the other spine values.
                spineTop.localRotation = Quaternion.Lerp(spineTop.localRotation, Quaternion.identity, Time.deltaTime * 30f);
                spine03.localRotation = Quaternion.Lerp(spine03.localRotation, Quaternion.identity, Time.deltaTime * 30f);
                neckUpDown.localRotation = Quaternion.Lerp(neckUpDown.localRotation, Quaternion.identity, Time.deltaTime * 30f);
                
                if (lastSleepMoveUpdate < Time.time)
                {
                    var wasPositive = sleepMoveUpdateTilt > 0;
                    var baseChange = wasPositive ? -2f : 2f;
                    sleepMoveUpdateTilt = baseChange * 10f + Random.Range(-1f, 1f) * 5f;
                    lastSleepMoveUpdate = Time.time + 3f * Random.Range(-1f, 1f); 
                }
                
                // rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, SleepLeftArmRotation, Time.deltaTime * 30f);
                // leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, SleepLeftArmRotation, Time.deltaTime * 30f);

                Vector3 currentEuler = neckPivot.localEulerAngles;

// Normalize angles to avoid 360 wrap issues
                if (currentEuler.y > 180f) currentEuler.y -= 360f;
                if (currentEuler.z > 180f) currentEuler.z -= 360f;

// Smoothly move Y toward tilt
                currentEuler.y = Mathf.MoveTowards(currentEuler.y, sleepMoveUpdateTilt, Time.deltaTime * 30f);

// Smoothly move Z toward sleep pose (90°)
                currentEuler.z = Mathf.MoveTowards(currentEuler.z, 90f, Time.deltaTime * 60f);

                neckPivot.localEulerAngles = currentEuler;
                
                yield return null;
            }
            
            StopSleeping();
        }

        public void ApplyDamage(Collider other)
        {
            // BloodEffects.transform.position = ;
            // var closestPoint = other.ClosestPoint(transform.position);
            // BloodEffects.transform.position = closestPoint;
            BloodEffects.Play();
            canPlayEffectOnFrame = false;
            var randomHit = Random.Range(2f, 10f);
            health = Mathf.Max(health - randomHit, 0f);
            if (health <= 0.1f)
            {
                SetDead();
            }
        }

        private XenoMovementParams GetMovementParams(bool isCloseToTarget, bool isFleeing = false)
        {
            float speed = isCloseToTarget ? closeMoveSpeed : moveSpeed;
            float accel = isCloseToTarget ? closeAccelForce : AccelerationForceSpeed;
            float turn = isCloseToTarget ? closeTurnSpeed : turnSpeed;

            if (isFleeing)
            {
                speed *= 1.5f;
                accel *= 1.25f;
            }

            return new XenoMovementParams
            {
                Speed = speed,
                Acceleration = accel,
                TurnSpeed = turn
            };
        }

        public void UpdateCurrentAnimationState()
        {
            CurrentAnimation = _animator.GetCurrentAnimatorStateInfo(0);
        }

        public void UpdateTargetData()
        {
            if (PrimaryTarget == null)
            {
                cachedDeltaPrimaryTarget = -1;
                return;
            }
            
            if (PrimaryTargetRB)
            {
                var closestPoint = PrimaryTargetRB.ClosestPointOnBounds(HeadColliderTransform.transform.position);
                cachedDeltaPrimaryTarget= Vector3.Distance(HeadColliderTransform.position, closestPoint);
                return;
            }
            
            cachedDeltaPrimaryTarget = Vector3.Distance(HeadColliderTransform.position, PrimaryTarget.position);
        }

        public void MoveAwayFromEnemies()
        {
            // 1. Gather all potential enemies
            List<Vector3> hostilePositions = new List<Vector3>();
            var mustClean = false;
            foreach (var enemy in EnemyRegistry.ActiveEnemies)
            {
                if (enemy == null)
                {
                    mustClean = true;
                    continue;
                }
                if (enemy == this) continue; // skip self
                if (Vector3.Distance(transform.position, enemy.transform.position) > 40f) continue; // skip distant
                hostilePositions.Add(enemy.transform.position);
            }
            if (mustClean) EnemyRegistry.ActiveEnemies.RemoveWhere(x => x == null);
            if (hostilePositions.Count == 0) return; // No enemies, nothing to run from

            // 2. Sample N candidate directions (e.g., every 30 degrees)
            int directionSamples = 12;
            float bestScore = float.MinValue;
            Vector3 bestDir = Vector3.zero;
            for (int i = 0; i < directionSamples; i++)
            {
                float angle = (360f / directionSamples) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                // 3. For each direction, sum distances to all enemies (higher sum = further from all)
                Vector3 candidatePos = transform.position + dir * 5f; // Check 5m in that direction
                
                // ---- SAFETY CHECK: Only consider directions with ground below ----
                bool safe = IsGroundBelow(candidatePos, 1.5f);
                if (!safe)
                    continue; // Skip this direction
                
                float score = 0f;
                foreach (var pos in hostilePositions)
                {
                    score += Vector3.Distance(candidatePos, pos);
                }

                // 4. Optionally, penalize directions that leave allowed run area
                Vector3 localOffset = candidatePos - transform.position;
                if (Mathf.Abs(localOffset.x) > maxRunRange.x || Mathf.Abs(localOffset.z) > maxRunRange.z)
                    score -= 10000f; // big penalty

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            // 5. Actually move in the chosen direction (accelerate)
            var movement = GetMovementParams(false, true);
            moveLerpVel = Mathf.MoveTowards(moveLerpVel, movement.Speed, movement.Acceleration * Time.deltaTime);
            _rb.AddForce(bestDir.normalized * moveLerpVel, ForceMode.Acceleration);
            
            // 6. Rotate to face run direction
            Quaternion targetRot = Quaternion.LookRotation(bestDir, Vector3.up);
            float runAwayTurn = movement.TurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(runAwayTurn));

            // 7. Update blend tree
            float normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
            _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
        }

        private bool IsGroundBelow(Vector3 position, float maxDrop)
        {
            // Check for ground within maxDrop meters below the position
            Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDrop + 0.5f, LayerMask.GetMask("Default", "terrain")))
            {
                // Optional: You can check for slope angle here, or walkable surface tag
                return true;
            }
            return false;
        }

        public XenoDroneAI GetClosestTargetDifferentPack()
        {
            XenoDroneAI closest = null;
            float closestDist = float.MaxValue;

            foreach (var xeno in Instances)
            {
                if (xeno == this) continue;
                if (xeno.IsDead()) continue; // must be alive
                if (xeno.packId == packId) continue; // skip same pack

                float dist = Vector3.Distance(transform.position, xeno.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = xeno;
                }
            }
            return closest;
        }

        public bool IsFleeing() => CurrentState == XenoAIState.Flee;

        public XenoDroneAI FindNearestAlly()
        {
            XenoDroneAI closest = null;
            float closestDist = float.MaxValue;

            foreach (var xeno in Instances)
            {
                if (xeno == this) continue;
                if (xeno.health <= 0.1f) continue;
                if (xeno.IsFleeing()) continue;
                if (xeno.packId != packId) continue; // ONLY same pack

                float dist = Vector3.Distance(transform.position, xeno.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = xeno;
                }
            }
            return closest;
        }

        public void FleeTowardSafeAllyOrRunAway()
        {
            // Try to find a nearby friendly not targeting us
            var friendly = FindNearestAlly();

            // Gather enemy positions (from registry)
            List<Vector3> hostilePositions = new();
            foreach (var enemyGO in EnemyRegistry.ActiveEnemies)
            {
                if (enemyGO == gameObject) continue;
                if (enemyGO == null) continue;
                hostilePositions.Add(enemyGO.transform.position);
            }

            // Direction logic
            Vector3 bestDir = Vector3.zero;
            float bestScore = float.MinValue;

            // Sample directions around the player
            int directionSamples = 12;
            for (int i = 0; i < directionSamples; i++)
            {
                float angle = (360f / directionSamples) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                // If we have a safe ally, bias toward them
                float allyBias = 0f;
                if (friendly != null)
                {
                    Vector3 toAlly = (friendly.transform.position - transform.position).normalized;
                    allyBias = Vector3.Dot(dir, toAlly) * 8f; // The higher, the more it prefers ally direction
                }

                // Sum distances from hostiles (the further from all, the better)
                Vector3 candidatePos = transform.position + dir * 5f;
                float enemyPenalty = 0f;
                foreach (var hostile in hostilePositions)
                {
                    float dist = Vector3.Distance(candidatePos, hostile);
                    enemyPenalty -= 1f / Mathf.Max(dist, 0.1f); // The closer an enemy, the bigger the penalty
                }

                // Is ground below?
                bool safe = IsGroundBelow(candidatePos, 1.5f);
                if (!safe)
                    continue;

                float score = allyBias + enemyPenalty;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            // If no valid direction (cornered!), fallback to run away from enemies only
            if (bestDir == Vector3.zero)
            {
                MoveAwayFromEnemies();
                return;
            }

            // Move and rotate as before
            float speed = moveSpeed * 1.25f;
            float accel = AccelerationForceSpeed * 1.1f;
            moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, accel * Time.deltaTime);
            _rb.AddForce(bestDir.normalized * moveLerpVel, ForceMode.Acceleration);

            Quaternion targetRot = Quaternion.LookRotation(bestDir, Vector3.up);
            float turn = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(turn));

            float normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
            _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
        }

        public void Flee()
        {
            CurrentState = XenoAIState.Flee;
        }

        public void StopFleeing()
        {
            CurrentState = XenoAIState.Idle; // Or Hunt/Attack as needed
        }


        public void RegenerateHealthOverTime()
        {
            if (health < maxHealth)
            {
                health = Mathf.Min(health + 5f, maxHealth);
            }
        }

        public bool IsSleeping() => CurrentState == XenoAIState.Sleeping;

        public void SetDead()
        {
            CancelInvoke(nameof(RegenerateHealthOverTime));
            
            // stop other animations etc
            Stop_Attack();
            _animator.SetBool(XenoBooleans.Movement, false);
            
            // have to swap to kinematic due to colliders causing the xeno to rotate
            _rb.isKinematic = true;
            
            CurrentState = XenoAIState.Dead;
            _animator.SetTrigger(XenoTriggers.Die);
            
            StartCoroutine(WaitForAnimationToEnd("die", 0, () =>
            {
                _rb.isKinematic = true;
            }));
        }

        public bool IsDead()
        {
            return CurrentState == XenoAIState.Dead;
        }

        public void UpdatePrimaryTarget()
        {
            if (cachedPrimaryTargetXeno && cachedPrimaryTargetXeno.IsDead())
            {
                PrimaryTarget = null;
                cachedPrimaryTargetXeno = null;
            }
            var target = GetClosestTargetDifferentPack();
            if (target)
            {
                PrimaryTarget = target.transform;
                PrimaryTargetRB = target.GetComponentInChildren<Rigidbody>();
                cachedPrimaryTargetXeno = target;
            }
        }

        public void DisableAttackColliders()
        {
            foreach (var activeCollider in attackColliders)
            {
                if (!activeCollider) continue;
                if (activeCollider.gameObject.layer == LayerHelpers.HitboxLayer)
                {
                    activeCollider.enabled = false;
                }
            }
        }

        public void EnableAttackColliders()
        {
            foreach (var activeCollider in attackColliders)
            {
                if (!activeCollider) continue;
                if (activeCollider.gameObject.layer == LayerHelpers.HitboxLayer)
                {
                    activeCollider.enabled = true;
                }           
            }
        }

        public void SetSleeping()
        {
            if (nextSleepUpdate > Time.fixedTime) return;
            if (IsSleeping()) return;
            canSleepAnimate = false;
            CurrentState = XenoAIState.Sleeping;
            // _animator.Play("sleep", 0, 1);
            _animator.SetTrigger(XenoTriggers.Sleep);
            _animator.SetBool(XenoBooleans.Movement, false);

            // _animator.Play(XenoTriggers.Sleep,0, 1f);
            Stop_Attack();
            if (_animationCompletionHandler.IsRunning)
            {
                _animationCompletionHandler.Stop();
            }
            _rb.isKinematic = true;
            // _animator.enabled = false;
            // _rb.isKinematic = true;
            canSleepAnimate = false;
            
            _animationCompletionHandler.Start(SleepingCustomAnimationsRoutine());
            // _animationCompletionHandler.Start(WaitForAnimationToEnd("sleep", 2, () =>
            // {
            //     // _rb.isKinematic = true;
            //     ActivateCamouflage();
            //     _animator.enabled = false;
            //     _rb.isKinematic = true;
            //     canSleepAnimate = true;
            // }));
        }

        public void StopSleeping()
        {
            _rb.isKinematic = false;
            _animator.enabled = true;
            
            foreach (var activeCollider in attackColliders)
            {
                activeCollider.enabled = false;
            }
            
            if (_animationCompletionHandler.IsRunning)
            {
                _animationCompletionHandler.Stop();
            }
            _animationCompletionHandler.Start(WaitForAnimationToEnd("awake",2, () =>
            {
                _rb.isKinematic = true;
                DeactivateCamouflage();
            }));
        }

        public IEnumerator WaitForAnimationToEnd(string stateName,int layerIndex,  Action onComplete)
        {
            var animatorState = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            // Wait for the animation to start
            const float maxBailSeconds = 10f;
            var currentSeconds = 0f;
            while (!animatorState.IsName(stateName) || currentSeconds > maxBailSeconds)
            {
                currentSeconds += Time.deltaTime;
                yield return null;
                animatorState = _animator.GetCurrentAnimatorStateInfo(0);
            }

            if (currentSeconds > maxBailSeconds)
            {
                LoggerProvider.LogDev($"Error did not find {stateName}"); 
                yield break;
            }
            
            // Wait for the animation to finish
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f || currentSeconds > maxBailSeconds)
            {
                currentSeconds += Time.deltaTime;
                yield return null;
            }
            
            if (currentSeconds > maxBailSeconds)
            {
                LoggerProvider.LogDev($"Error did not find {stateName}"); 
                yield break;
            }
            
            onComplete?.Invoke();
        }

        public void ActivateCamouflage()
        {
            if (!hasCamouflage) return;
            if (!modelSkinRenderer || modelSkinRenderer.materials == null || modelSkinRenderer.materials.Length != 2) return;
            _lastCamouflageState = true;
            Material[] materials = { TransparentMaterial, TransparentMaterial };
            modelSkinRenderer.materials = materials;
        }

        public void DeactivateCamouflage()
        {
          if (!_lastCamouflageState) return;
          if (!modelSkinRenderer || modelSkinRenderer.materials == null || modelSkinRenderer.materials.Length != 2) return;
          Material[] materials = { BodyMaterial, HeadMaterial };
          modelSkinRenderer.materials = materials;
        }


        private IEnumerator ScheduleWakeup()
        {
            yield return new WaitForSeconds(timeUntilWake);
            CurrentState = XenoAIState.Idle;
            _animator.enabled = true;
            _animator.SetTrigger(XenoTriggers.Awake);
            _animator.SetTrigger(XenoTriggers.Move);
        }

        private IEnumerator ScheduleSleep()
        { 
            _animator.SetBool(XenoBooleans.Idle, true);
            yield return new WaitForSeconds(timeUntilSleep);
            _animator.SetBool(XenoBooleans.Idle, false);
            SetSleeping();
            _sleepCoroutineHandler.Start(ScheduleWakeup());
        }

        public void UpdateBehavior()
        { 
            if (IsDead() || IsSleeping()) return;

            UpdatePrimaryTarget();

            if (PrimaryTarget == null || !PrimaryTarget.gameObject.activeInHierarchy)
            {
                if (_sleepCoroutineHandler.IsRunning) return;
                _sleepCoroutineHandler.Start(ScheduleSleep());
                CurrentState = XenoAIState.Idle;
                Stop_Attack();
                return;
            }
            
            if (_sleepCoroutineHandler.IsRunning)
            {
                _sleepCoroutineHandler.Stop();
            }
        
            if (IsSleeping())
            {
                StopSleeping();
            }

            if (health <= 0.1f)
            {
                SetDead();
                return;
            }
            
            if (health < 30f)
            {
                Flee();
                FleeTowardSafeAllyOrRunAway();
                Stop_Attack();
                return;
            }

            if (IsFleeing())
            {
                StopFleeing();
            }
            
            if (PrimaryTarget)
            {
                var isInAttackRange = DeltaPrimaryTarget < closeRange;
                if (isInAttackRange)
                {
                    // MoveTowardsTarget();
                    Start_Attack();
                }
                
                // todo add a lunge attack at less than 5f...random chance should be checked every X seconds.
                
                if (!isInAttackRange && DeltaPrimaryTarget < 100f)
                {
                    Stop_Attack();
                    MoveTowardsTarget();
                }

                // if (DeltaPrimaryTarget >= 10f)
                // {
                //     Start_HuntTarget();
                // }

            }
        }

        public void Start_Idle()
        {
            _animator.SetBool(XenoBooleans.Idle, true);
        }

        /// <summary>
        /// Advanced logic to hunt the target...in a creepy xeno way.
        /// </summary>
        public void Start_HuntTarget()
        {
            _animator.SetBool(XenoBooleans.Walk, true);
        }

        public void MoveTowardsTarget()
        {
            if (!PrimaryTarget) return;

            var movement = GetMovementParams(DeltaPrimaryTarget < closeRange);
            RotateTowardsTarget(movement.TurnSpeed);

            moveLerpVel = Mathf.MoveTowards(moveLerpVel, movement.Speed, movement.Acceleration * Time.deltaTime);
            _rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);

            float normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
            _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
        }

        private void RotateTowardsTarget(float customTurnSpeed)
        {
            if (!PrimaryTarget) return;
            Vector3 dir = (PrimaryTarget.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            float turnLerp = customTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(turnLerp));
        }

        public void UpdateAttackMode()
        {
            if (nextUpdateTime > Time.fixedTime) return;
            nextUpdateTime = Time.fixedTime + UpdatePauseTime;
            cachedAttackMode = Random.Range(0f, 1f);
        }

        public void Start_Attack()
        {
            EnableAttackColliders();
            UpdateAttackMode();
            _animator.SetBool(XenoParams.MoveAttack, true);
            _animator.SetFloat(XenoParams.AttackMode, cachedAttackMode);
        }

        public void Stop_Attack()
        {
            DisableAttackColliders();
            _animator.SetBool(XenoParams.MoveAttack, false);
        }

        public void IgnoreAllColliders()
        {
            foreach (var allCollider1 in allColliders)
            foreach (var allCollider2 in allColliders)
            {
                Physics.IgnoreCollision(allCollider1, allCollider2, true);;
            }
        }

        public Quaternion QuaternionMerge(Quaternion rot, float dirZ)
        {
            return Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, dirZ);
        }

        public void CollectAllBodyJoints()
        {
            foreach (var (list, root) in allLists)
            {
                RecursiveCollect(list, root, true );
            }
        }

        public void AddCapsuleCollidersToAllJoints()
        {
            foreach (var (list, root) in allLists)
            {
                AddCapsuleColliderToListObjs(list);
            }
        }

        public void AddCapsuleColliderToListObjs(HashSet<Transform> list)
        {
            foreach (var bone in list)
            {
                var boneName = bone.name;
                if (boneName.Contains("Toe") || boneName.Contains("Finger") || boneName.Contains("Thumb")) continue;
                if (bone.GetComponent<Collider>() != null) continue;
                var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                allColliders.Add(capsule);
                // Adjust collider size/orientation as needed for your model
                capsule.radius = 0.5f;
                capsule.height = 0.2f;
                capsule.direction = 2; // 0=X, 1=Y, 2=Z
            }
        }

        void RecursiveCollect(HashSet<Transform> list, Transform joint, bool skip = false)
        {
            if (!skip)
            {
                list.Add(joint);
            }
            foreach (Transform child in joint)
            {
                RecursiveCollect(list, child);
            }
        }

        public struct XenoMovementParams
        {
            public float Speed;
            public float Acceleration;
            public float TurnSpeed;
        }

        public static class XenoParams
        {
            public static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
            public static readonly int MoveAttack = Animator.StringToHash("moveAttack");
            public static readonly int AttackMode = Animator.StringToHash("attackMode");
        }

        public static class XenoTriggers
        {
            public static readonly int Die = Animator.StringToHash("die"); // should only be run once
            public static readonly int Awake = Animator.StringToHash("awake"); // should only be run once
            public static readonly int Sleep = Animator.StringToHash("sleep"); // should only be run once
            public static readonly int Move = Animator.StringToHash("move"); // should only be run once
        }

        public static class XenoBooleans
        {
            public static readonly int AttackArms = Animator.StringToHash("attack_arms");
            public static readonly int AttackTail = Animator.StringToHash("attack_tail");
            public static readonly int Walk = Animator.StringToHash("walk");
            public static readonly int Run = Animator.StringToHash("run");
            public static readonly int Idle = Animator.StringToHash("idle");
            public static readonly int Movement = Animator.StringToHash("movement"); // should only be run once
            public static readonly int Nothing = Animator.StringToHash("nothing"); // for triggers that do nothing after completing.
        }

        #region Coroutines

        private CoroutineHandle _sleepCoroutineHandler;
        private CoroutineHandle _wakeCoroutineHandler;
        private CoroutineHandle _animationCompletionHandler;

        private void InitCoroutineHandles()
        {
            _sleepCoroutineHandler ??= new CoroutineHandle(this);
            _wakeCoroutineHandler ??= new CoroutineHandle(this);
            _animationCompletionHandler ??= new CoroutineHandle(this);
        }

        #endregion

        #region Xeno Transforms

        [Header("Xeno Transforms")]
        private Transform xenoAnimatorRoot;
        private Transform xenoRoot;
        private Transform spine01;
        private Transform spine02;
        private Transform spine03;
        private Transform spineTop;
        private Transform neckUpDown;
        private Transform neckPivot;
        private Transform leftHip;
        private Transform rightHip;
        private Transform leftArm;
        private Transform rightArm;

        #endregion

    }
}
