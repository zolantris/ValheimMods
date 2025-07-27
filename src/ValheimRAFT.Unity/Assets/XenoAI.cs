#region
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class XenoDroneAI : MonoBehaviour
    {

        public enum XenoAIState { Idle, Hunt, Attack, Flee, Dead }

        public static readonly HashSet<XenoDroneAI> Instances = new();
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

        public Vector3 maxRunRange = new Vector3(20f, 0f, 20f); // Editable in Inspector, e.g., limits to a 20x20 area
        public XenoAIState CurrentState = XenoAIState.Idle;

        [CanBeNull] public XenoDroneAI cachedPrimaryTargetXeno;

        private readonly HashSet<Collider> allColliders = new();

        private readonly float walkThreshold = 1.0f; // speed to trigger run (above this = run)
        private Animator _animator;
        private Rigidbody _rb;

        public HashSet<(HashSet<Transform>,Transform)> allLists = new HashSet<(HashSet<Transform>,Transform)>();

        public AnimatorStateInfo CurrentAnimation;
        public HashSet<Transform> leftArmJoints = new HashSet<Transform>();
        public HashSet<Transform> leftLeftJoints = new HashSet<Transform>();

        private float moveLerpVel;
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
            xenoAnimatorRoot = transform.Find("Xenomorph Default");
            xenoRoot = xenoAnimatorRoot.Find("alien_xenos_drone_SK_Xenos_Drone_skeleton/XenosBiped_TrajectorySHJnt/XenosBiped_ROOTSHJnt");
            spine01 = xenoRoot.Find("XenosBiped_Spine_01SHJnt");
            spine02 = spine01.Find("XenosBiped_Spine_02SHJnt");
            spine03 = spine02.Find("XenosBiped_Spine_03SHJnt");
            
            leftHip = xenoRoot.Find("XenosBiped_l_Leg_HipSHJnt");
            rightHip = xenoRoot.Find("XenosBiped_r_Leg_HipSHJnt");
            
            leftArm = spine03.Find("XenosBiped_l_Arm_ClavicleSHJnt/XenosBiped_l_Arm_ShoulderSHJnt");
            rightArm = spine03.Find("XenosBiped_r_Arm_ClavicleSHJnt/XenosBiped_r_Arm_ShoulderSHJnt");
            
            tailRoot = xenoRoot.Find("XenosBiped_TailRoot_SHJnt");
            _animator = xenoAnimatorRoot.GetComponent<Animator>();
            _rb = GetComponent<Rigidbody>();
            _rb.maxLinearVelocity = 5f;
            _rb.maxAngularVelocity = 5f;

            BloodEffects = GetComponentInChildren<ParticleSystem>();

            allLists = new HashSet<(HashSet<Transform>, Transform)> { (rightArmJoints, rightArm), (leftArmJoints, leftArm), (leftLeftJoints, leftHip), (rightLegJoints, rightHip), (tailJoints, tailRoot) };

            var colliders = GetComponentsInChildren<Collider>();
            allColliders.AddRange(colliders);

            CollectAllBodyJoints();
            AddCapsuleCollidersToAllJoints();
            IgnoreAllColliders();
            
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
            
        }

        void Update()
        {
            UpdateCurrentAnimationState();
        }

        public void FixedUpdate()
        {
            canPlayEffectOnFrame = !BloodEffects.isPlaying;
            UpdateTargetData();
            UpdateBehavior();
        }


        void LateUpdate()
        {
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
        }

        void OnDisable()
        {
            Instances.Remove(this);
            EnemyRegistry.ActiveEnemies.Remove(gameObject);
        }

        public void OnCollisionEnter(Collision other)
        {
            if (!canPlayEffectOnFrame) return;
            if (other.collider.gameObject.layer == LayerHelpers.HitboxLayer && other.contacts.Length > 0)
            {
                BloodEffects.transform.position = other.GetContact(0).point;
                BloodEffects.Play();
                canPlayEffectOnFrame = false;
                var randomHit = Random.Range(2f, 10f);
                health = Mathf.Max(health - randomHit, 0f);
            }
        }

        public void UpdateCurrentAnimationState()
        {
            CurrentAnimation = _animator.GetCurrentAnimatorStateInfo(0);
        }

        public void UpdateTargetData()
        {
            cachedDeltaPrimaryTarget = PrimaryTarget == null ? -1 : Vector3.Distance(transform.position, PrimaryTarget.position);
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
            float speed = moveSpeed * 1.5f; // Make running away a bit faster if desired
            float accel = AccelerationForceSpeed * 1.25f;
            moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, accel * Time.deltaTime);

            _rb.AddForce(bestDir.normalized * moveLerpVel, ForceMode.Acceleration);
            _rb.maxLinearVelocity = 5f;

            // 6. Rotate to face run direction
            Quaternion targetRot = Quaternion.LookRotation(bestDir, Vector3.up);
            float runAwayTurn = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(runAwayTurn));

            // 7. Update blend tree
            float normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
            _animator.SetFloat(XenoParams.MoveSpeed, normalized);
        }

        private bool IsGroundBelow(Vector3 position, float maxDrop)
        {
            // Check for ground within maxDrop meters below the position
            Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDrop + 0.5f, LayerMask.GetMask("Default", "Ground")))
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
                if (xeno.health <= 0.1f) continue; // must be alive
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
            _animator.SetFloat(XenoParams.MoveSpeed, normalized);
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

        public void SetDead()
        {
            CancelInvoke(nameof(RegenerateHealthOverTime));
            
            CurrentState = XenoAIState.Dead;
            Stop_Attack();
            
            _animator.SetBool(XenoBooleans.Die, true);
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
                cachedPrimaryTargetXeno = target;
            }
        }

        public void UpdateBehavior()
        {
            UpdatePrimaryTarget();
            
            if (CurrentState == XenoAIState.Dead) return;
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
            
            StopFleeing();
            
            if (PrimaryTarget)
            {
                var isInAttackRange = DeltaPrimaryTarget < closeRange;
                if (isInAttackRange)
                {
                    Start_Attack();
                    // MoveTowardsTarget();
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
                
                return;
            }
            
            if (!TryFindTarget())
            {
                Start_Idle();
            };
        }

        public bool TryFindTarget()
        {
            return false;
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
            
            // Adaptive movement and rotation
            float speed = (DeltaPrimaryTarget > closeRange) ? moveSpeed : closeMoveSpeed;
            float accel = (DeltaPrimaryTarget > closeRange) ? AccelerationForceSpeed : closeAccelForce;
            float turn = (DeltaPrimaryTarget > closeRange) ? turnSpeed : closeTurnSpeed;

            // Set rotation speed for this frame
            RotateTowardsTarget(turn);

            // Physically accelerate toward target
            var forward = transform.forward;
            moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, accel * Time.deltaTime);
            _rb.AddForce(forward * moveLerpVel, ForceMode.Acceleration);

            // Animation blending
            float normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
            _animator.SetFloat(XenoParams.MoveSpeed, normalized);
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
            UpdateAttackMode();
            _animator.SetBool(XenoParams.MoveAttack, true);
            _animator.SetFloat(XenoParams.AttackMode, cachedAttackMode);
        }

        public void Stop_Attack()
        {
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


        /// <summary>
        /// Recursively collects all child joints under the assigned tail root.
        /// </summary>
        public void CollectTailJoints()
        {
            tailJoints.Clear();
            if (tailRoot == null)
            {
                Debug.LogWarning("XenoTailJointsCollector: No tailRoot assigned.");
                return;
            }
            RecursiveCollect(tailJoints, tailRoot, true);
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
                capsule.radius = 0.05f;
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

        public static class XenoParams
        {
            public static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
            public static readonly int MoveAttack = Animator.StringToHash("moveAttack");
            public static readonly int AttackMode = Animator.StringToHash("attackMode");
        }

        public static class XenoBooleans
        {
            public static readonly int AttackArms = Animator.StringToHash("attack_arms");
            public static readonly int AttackTail = Animator.StringToHash("attack_tail");
            public static readonly int Walk = Animator.StringToHash("walk");
            public static readonly int Run = Animator.StringToHash("run");
            public static readonly int Idle = Animator.StringToHash("idle");
            public static readonly int Die = Animator.StringToHash("die"); // should only be run once
            public static readonly int Movement = Animator.StringToHash("die"); // should only be run once
        }

        #region Xeno Transforms

        [Header("Xeno Transforms")]
        private Transform xenoAnimatorRoot;
        private Transform xenoRoot;
        private Transform spine01;
        private Transform spine02;
        private Transform spine03;
        private Transform leftHip;
        private Transform rightHip;
        private Transform leftArm;
        private Transform rightArm;

        #endregion

    }
}
