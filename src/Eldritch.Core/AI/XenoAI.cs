#region

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using Eldritch.Core.AI;
  using JetBrains.Annotations;
  using UnityEngine;
  using Zolantris.Shared;
  using Debug = UnityEngine.Debug;
  using Random = UnityEngine.Random;

#endregion

  namespace Eldritch.Core
  {
    public class XenoDroneAI : MonoBehaviour
    {

      public enum XenoAIState
      {
        Idle,
        Hunt,
        Attack,
        Flee,
        Dead,
        Sleeping
      }

      public static readonly HashSet<XenoDroneAI> Instances = new();

      public static Quaternion SleepNeckZRotation = Quaternion.Euler(0, 0, 80f);

      public static readonly float IdleThresholdTime = 0.0001f;
      [Header("Movement Tuning")]
      public float moveSpeed = 1f; // Normal approach speed
      public float closeMoveSpeed = 0.3f; // Slower speed near target
      public float AccelerationForceSpeed = 90f; // Normal acceleration
      public float closeAccelForce = 20f; // Reduced acceleration near target
      public float closeRange = 3f; // Distance for “slow down”

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

      [SerializeField] private float GravityMultiplier = 5f;


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

      public Vector3 maxRunRange = new(20f, 0f, 20f); // Editable in Inspector, e.g., limits to a 20x20 area
      private XenoAIState _currentState = XenoAIState.Idle;

      public XenoAIState CurrentState
      {
        get => _currentState;
        set
        {
          if (value == _currentState) return;
          OnStateUpdate(_currentState, value);
          _currentState = value;
        }
      }


      // For all behaviors. Any behavior returns true it will bail next evaluation
      private List<Func<bool>> _behaviorUpdaters = new();

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

      public Quaternion SleepLeftArmRotation = Quaternion.Euler(36.1819992f, 323.176208f, 251.765549f);

      public bool canSleepAnimate;

      private readonly HashSet<Collider> allColliders = new();
      private readonly HashSet<Collider> attackColliders = new();

      private readonly float walkThreshold = 1.0f; // speed to trigger run (above this = run)
      private Animator _animator;
      private bool _lastCamouflageState;
      private Rigidbody _rb;
      public bool CanJump = true;

      public HashSet<(HashSet<Transform>, Transform)> allLists = new();
      private Material BodyMaterial;

      public AnimatorStateInfo CurrentAnimation;
      private Material HeadMaterial;

      public HashSet<Transform> leftArmJoints = new();
      public HashSet<Transform> leftLeftJoints = new();

      private SkinnedMeshRenderer modelSkinRenderer;

      private float moveLerpVel;

      private float nextSleepUpdate;
      [CanBeNull] private Rigidbody PrimaryTargetRB;
      public HashSet<Transform> rightArmJoints = new();
      public HashSet<Transform> rightLegJoints = new();
      private float runStart = 0.75f; // start blending to run at this speed

      [Tooltip("List of all tail joints, from root to tip.")]
      public HashSet<Transform> tailJoints = new();

      private float velocity;
      // getters
      public float DeltaPrimaryTarget => cachedDeltaPrimaryTarget;

      private Transform cachedAnklePosition;

      public float lastCacheClear = 0;

      public float lastTouchedLand;
      
      public float lastLowestPointCheck;
      
      private Vector3 cachedLowestPoint = Vector3.zero;

      public Vector3 GetLowestPointOnRigidbody()
      {
        if (lastLowestPointCheck > Time.fixedTime)
        {
          return cachedLowestPoint;
        }
        lastLowestPointCheck = Time.fixedTime + 0.5f;
        
        float minY = float.MaxValue;
        foreach (var col in allColliders)
        {
          if (col == null) continue;
          var bounds = col.bounds;
          if (bounds.min.y < minY)
            minY = bounds.min.y;
        }
        var position = transform.position;
        cachedLowestPoint = new Vector3(position.x, minY, position.z);
        return cachedLowestPoint;
      }

      private void Awake()
      {
        _behaviorUpdaters = new List<Func<bool>>{Update_Death, Update_Flee, Update_Roam, Update_AttackTargetBehavior, Update_SleepBehavior};

        health = maxHealth;
        // find these easily with Gameobject -> Copy FullTransformPath from root.
        // You may need to adjust these paths to match your actual bone names/hierarchy


        BindUnOptimizedRoots();

        _animator = xenoAnimatorRoot.GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        // _rb.maxLinearVelocity = 15f;
        // _rb.maxAngularVelocity = 1f;

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

        allColliders.UnionWith(colliders);
        attackColliders.UnionWith(colliders);

        modelSkinRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        var materials = modelSkinRenderer.materials;
        if (materials.Length == 2)
        {
          BodyMaterial = materials[0];
          HeadMaterial = materials[1];
        }



        CollectAllBodyJoints();
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

      private void Update()
      {
        UpdateCurrentAnimationState();
      }

      public bool IsGrounded()
      {
        return lastTouchedLand < 0.5f;
      }

      public void FixedUpdate()
      {
        if (!IsGrounded())
        {
          Stop_Attack();
          return;
        }
        lastTouchedLand += Time.fixedDeltaTime;
        if (!_rb) return;
        var rot = _rb.rotation.eulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        _rb.rotation = Quaternion.Euler(rot);

        if (CurrentState == XenoAIState.Attack)
        {
          UpdateAttackMode();
        }

        if (IsDead()) return;
        canPlayEffectOnFrame = !BloodEffects.isPlaying;
        UpdateTargetData();
        UpdateBehavior();
      }

      private void LateUpdate()
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


          var tailCurveIncrease = Random.Range(tailMin, tailMax);
          var baseIncrease = Random.Range(tailMin, tailMax);
          foreach (var tailJoint in tailJoints)
          {
            tailCurveIncrease += baseIncrease * Random.Range(0.1f, 50f);
            tailJoint.localRotation = Quaternion.Lerp(tailJoint.localRotation, Quaternion.Euler(tailCurveIncrease, 0, tailCurveIncrease), Time.deltaTime);
          }
        }
      }

      private void OnEnable()
      {
        Instances.Add(this);
        EnemyRegistry.ActiveEnemies.Add(gameObject);
        InitCoroutineHandles();
      }

      private void OnDisable()
      {
        Instances.Remove(this);
        EnemyRegistry.ActiveEnemies.Remove(gameObject);
      }

      public void OnCollisionEnter(Collision other)
      {
        if (IsDead()) return;
        if (!canPlayEffectOnFrame) return;
        if (other.body == _rb || other.transform.root == transform.root)
        {
          foreach (var otherContact in other.contacts)
          {
            Physics.IgnoreCollision(otherContact.otherCollider, otherContact.thisCollider, true);
          }
          return;
        }
        var layer = other.gameObject.layer;
        if (LayerHelpers.IsContainedWithinLayerMask(layer, LayerHelpers.LandLayers))
        {
          lastTouchedLand = 0f;
        }
        
        if (layer != LayerHelpers.ItemLayer) return;

        // var contactsHasHitbox = other.contacts.Any(x => x.otherCollider.gameObject.layer == LayerHelpers.HitboxLayer || x.thisCollider.gameObject.layer == LayerHelpers.HitboxLayer);
        if (layer == LayerHelpers.ItemLayer)
        {
          LoggerProvider.LogDebugDebounced("Hit layer");
        }

        ApplyDamage(other.collider);
      }

      public void OnTriggerEnter(Collider other)
      {
        if (IsDead()) return;
        if (!canPlayEffectOnFrame) return;
        if (other.transform.root == transform.root)
        {
          LoggerProvider.LogDebugDebounced($"Hit self {other.transform}");
          return;
        }
        var layer = other.gameObject.layer;
        if (layer != LayerHelpers.ItemLayer) return;

        // var contactsHasHitbox = other.contacts.Any(x => x.otherCollider.gameObject.layer == LayerHelpers.HitboxLayer || x.thisCollider.gameObject.layer == LayerHelpers.HitboxLayer);
        if (layer == LayerHelpers.ItemLayer)
        {
          LoggerProvider.LogDebugDebounced("Hit by item");
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
        var toTarget = PrimaryTarget.position - neckPivot.position;
        toTarget.y = 0; // Flatten for Y-only tracking

        var localDir = transform.InverseTransformDirection(toTarget.normalized);

        // Calculate desired yaw (Y) and roll (Z)
        var yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        yaw = Mathf.Clamp(yaw, -40f, 40f);

        var roll = Mathf.Lerp(40f, 90f, Mathf.Abs(yaw / 40f));
        roll = Mathf.Clamp(roll, 40f, 90f);

        var currentEuler = neckPivot.localEulerAngles;

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

        var currentEuler = neckPivot.localEulerAngles;

// Normalize angles to avoid 360 wrap issues
        if (currentEuler.y > 180f) currentEuler.y -= 360f;
        if (currentEuler.z > 180f) currentEuler.z -= 360f;

// Smoothly move Y toward tilt
        currentEuler.y = Mathf.MoveTowards(currentEuler.y, sleepMoveUpdateTilt, Time.deltaTime * 30f);

// Smoothly move Z toward sleep pose (90°)
        currentEuler.z = Mathf.MoveTowards(currentEuler.z, 90f, Time.deltaTime * 60f);

        neckPivot.localEulerAngles = currentEuler;
      }
      private Vector3? roamTarget = null; // Null = not currently wandering
      private void MoveTowards(Vector3 targetPos, float speed)
      {
        var toTarget = targetPos - transform.position;
        float distance = toTarget.magnitude;

        // Always turn to face target
        RotateTowardsDirection(toTarget, turnSpeed);

        // Check for gap ahead, jump if needed
        if (distance > 2f && IsGapAhead(1.0f, 3f, 5f))
        {
          Vector3 jumpTarget;
          if (FindJumpableLanding(out jumpTarget))
          {
            JumpTo(jumpTarget);
            if (_animator) _animator.SetTrigger("Jump");
            roamTarget = null; // After a jump, pick a new wander target
            return;
          }
          else
          {
            BrakeHard();
            roamTarget = null;
            return;
          }
        }

        // Move forward
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, AccelerationForceSpeed * 0.5f * Time.deltaTime);
        _rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);

        var normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
        _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
      }

      public IEnumerator SleepingCustomAnimationsRoutine()
      {
        var timer = Stopwatch.StartNew();
        var timeUntilWakeInMS = timeUntilWake * 1000;
        while (timer.ElapsedMilliseconds < timeUntilWakeInMS)
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

          var currentEuler = neckPivot.localEulerAngles;

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
        var speed = isCloseToTarget ? closeMoveSpeed : moveSpeed;
        var accel = isCloseToTarget ? closeAccelForce : AccelerationForceSpeed;
        var turn = isCloseToTarget ? closeTurnSpeed : turnSpeed;

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
          cachedDeltaPrimaryTarget = Vector3.Distance(HeadColliderTransform.position, closestPoint);
          return;
        }

        cachedDeltaPrimaryTarget = Vector3.Distance(HeadColliderTransform.position, PrimaryTarget.position);
      }

      public void MoveAwayFromEnemies()
      {
        // 1. Gather all potential enemies
        var hostilePositions = new List<Vector3>();
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
        var directionSamples = 12;
        var bestScore = float.MinValue;
        var bestDir = Vector3.zero;
        for (var i = 0; i < directionSamples; i++)
        {
          var angle = 360f / directionSamples * i;
          var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

          // 3. For each direction, sum distances to all enemies (higher sum = further from all)
          var candidatePos = transform.position + dir * 5f; // Check 5m in that direction

          // ---- SAFETY CHECK: Only consider directions with ground below ----
          var safe = IsGroundBelow(candidatePos, 1.5f);
          if (!safe)
            continue; // Skip this direction

          var score = 0f;
          foreach (var pos in hostilePositions)
          {
            score += Vector3.Distance(candidatePos, pos);
          }

          // 4. Optionally, penalize directions that leave allowed run area
          var localOffset = candidatePos - transform.position;
          if (Mathf.Abs(localOffset.x) > maxRunRange.x || Mathf.Abs(localOffset.z) > maxRunRange.z)
            score -= 10000f; // big penalty

          if (score > bestScore)
          {
            bestScore = score;
            bestDir = dir;
          }
        }

        if (IsGapAhead(1.0f, 0.6f, 2.0f)) // Only jump if there is a real gap
        {
          Vector3 jumpTarget;
          if (FindJumpableLanding(out jumpTarget))
          {
            JumpTo(jumpTarget);
            if (_animator) _animator.SetTrigger("Jump");
            return;
          }
          else
          {
            BrakeHard();
            moveLerpVel = 0f;
            return;
          }
        }

        // 5. Actually move in the chosen direction (accelerate)
        var movement = GetMovementParams(false, true);
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, movement.Speed, movement.Acceleration * Time.deltaTime);
        _rb.AddForce(bestDir.normalized * moveLerpVel, ForceMode.Acceleration);

        // 6. Rotate to face run direction
        var targetRot = Quaternion.LookRotation(bestDir, Vector3.up);
        var runAwayTurn = movement.TurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(runAwayTurn));

        // 7. Update blend tree
        var normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
        _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
      }

      private bool IsGapAhead(float distance = 1.0f, float maxStepHeight = 2f, float maxDrop = 3.0f)
      {
        if (!IsGrounded()) return false;
        var checkOrigin = GetFurthestToe();
        if (Physics.Raycast(checkOrigin.position + Vector3.up, Vector3.down, out var hit, maxDrop + 1.2f, LayerMask.GetMask("Default", "terrain")))
        {
          var yDiff = checkOrigin.position.y - hit.point.y;
          // Is the drop small enough to step down? If so, not a gap!
          if (yDiff < maxStepHeight) return false;
        }
        // No ground, or ground too far down = gap
        return true;
      }

      private bool IsGroundBelow(Vector3 position, float maxDrop)
      {
        // Check for ground within maxDrop meters below the position
        var ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out var hit, maxDrop + 0.5f, LayerMask.GetMask("Default", "terrain")))
        {
          // Optional: You can check for slope angle here, or walkable surface tag
          return true;
        }
        return false;
      }
      
      /// <summary>
      /// Returns the toe (left or right) that's furthest in the character's forward direction.
      /// </summary>
      public Transform GetFurthestToe()
      {
        if (leftToeTransform == null && rightToeTransform == null) return null;
        if (leftToeTransform != null && rightToeTransform == null) return leftToeTransform;
        if (rightToeTransform != null && leftToeTransform == null) return rightToeTransform;

        // Project both toe positions onto the forward axis
        Vector3 forward = transform.forward.normalized;
        float leftProj = Vector3.Dot(leftToeTransform.position - transform.position, forward);
        float rightProj = Vector3.Dot(rightToeTransform.position - transform.position, forward);

        return leftProj > rightProj ? leftToeTransform : rightToeTransform;
      }

      public XenoDroneAI GetClosestTargetDifferentPack()
      {
        XenoDroneAI closest = null;
        var closestDist = float.MaxValue;

        foreach (var xeno in Instances)
        {
          if (xeno == this) continue;
          if (xeno.IsDead()) continue; // must be alive
          if (xeno.packId == packId) continue; // skip same pack

          var dist = Vector3.Distance(transform.position, xeno.transform.position);
          if (dist < closestDist)
          {
            closestDist = dist;
            closest = xeno;
          }
        }
        return closest;
      }

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

      public XenoDroneAI FindNearestAlly()
      {
        XenoDroneAI closest = null;
        var closestDist = float.MaxValue;

        foreach (var xeno in Instances)
        {
          if (xeno == this) continue;
          if (xeno.health <= 0.1f) continue;
          if (xeno.IsFleeing()) continue;
          if (xeno.packId != packId) continue; // ONLY same pack

          var dist = Vector3.Distance(transform.position, xeno.transform.position);
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
        var bestDir = Vector3.zero;
        var bestScore = float.MinValue;

        // Sample directions around the player
        var directionSamples = 12;
        for (var i = 0; i < directionSamples; i++)
        {
          var angle = 360f / directionSamples * i;
          var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

          // If we have a safe ally, bias toward them
          var allyBias = 0f;
          if (friendly != null)
          {
            var toAlly = (friendly.transform.position - transform.position).normalized;
            allyBias = Vector3.Dot(dir, toAlly) * 8f; // The higher, the more it prefers ally direction
          }

          // Sum distances from hostiles (the further from all, the better)
          var candidatePos = transform.position + dir * 5f;
          var enemyPenalty = 0f;
          foreach (var hostile in hostilePositions)
          {
            var dist = Vector3.Distance(candidatePos, hostile);
            enemyPenalty -= 1f / Mathf.Max(dist, 0.1f); // The closer an enemy, the bigger the penalty
          }

          // Is ground below?
          var safe = IsGroundBelow(candidatePos, 1.5f);
          if (!safe)
            continue;

          var score = allyBias + enemyPenalty;
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

        if (IsGapAhead(1.0f, 3f, 5f)) // Only jump if there is a real gap
        {
          Vector3 jumpTarget;
          if (FindJumpableLanding(out jumpTarget))
          {
            JumpTo(jumpTarget);
            if (_animator) _animator.SetTrigger("Jump");
            return;
          }
          else
          {
            BrakeHard();
            moveLerpVel = 0f;
            return;
          }
        }

        // Move and rotate as before
        var speed = moveSpeed * 1.25f;
        var accel = AccelerationForceSpeed * 1.1f;
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, speed, accel * Time.deltaTime);
        _rb.AddForce(bestDir.normalized * moveLerpVel, ForceMode.Acceleration);

        var targetRot = Quaternion.LookRotation(bestDir, Vector3.up);
        var turn = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(turn));

        var normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
        _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
      }

      private bool TryJumpAheadIfPossible()
      {
        Vector3 landingPoint;
        if (FindJumpableLanding(out landingPoint))
        {
          JumpTo(landingPoint);
          if (_animator) _animator.SetTrigger("Jump");
          return true;
        }
        return false;
      }

      public void Start_Flee()
      {
        CurrentState = XenoAIState.Flee;
      }

      public bool Update_Death()
      {
        if (health <= 0.1f)
        {
          SetDead();
          return true;
        }
        
        return false;
      }

      public bool Update_Flee()
      {
        var isFleeing = IsFleeing();
        var shouldFlee = health < 30f;
        if (shouldFlee)
        {
          if (!isFleeing)
          {
            Start_Flee();
          }
          FleeTowardSafeAllyOrRunAway();
          return true;
        }

        if (isFleeing)
        {
          StopFleeing();
        }

        return false;
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

    

      public void UpdatePrimaryTarget()
      {
        if (cachedPrimaryTargetXeno && cachedPrimaryTargetXeno.IsDead())
        {
          PrimaryTarget = null;
          cachedPrimaryTargetXeno = null;
          roamTarget = null;
        }
        var target = GetClosestTargetDifferentPack();
        if (target)
        {
          PrimaryTarget = target.transform;
          PrimaryTargetRB = target.GetComponentInChildren<Rigidbody>();
          cachedPrimaryTargetXeno = target;
          roamTarget = PrimaryTarget.position;
        }
      }

      public bool IsArmAttack()
      {
        var moveAttack = _animator.GetFloat(XenoParams.AttackMode);
        if (moveAttack == 0.5f) throw new Exception("Move attack should never be exactly 0.5f");
        if (moveAttack > 0.5f)
        {
          return true;
        }
        return false;
      }

      public void DisableAttackColliders()
      {
        foreach (var activeCollider in attackColliders)
        {
          if (!activeCollider) continue;
          if (activeCollider.gameObject.layer == LayerHelpers.ItemLayer)
          {
            activeCollider.enabled = false;
          }
        }
      }

      public void EnableAttackColliders()
      {
        var isArmAttack = IsArmAttack();
        const string handAttackObjName = "xeno_arm_attack_collider";
        const string tailAttackObjName = "xeno_tail_attack_collider";
        
        foreach (var activeCollider in attackColliders)
        {
          if (!activeCollider) continue;
          var go = activeCollider.gameObject;
          if (isArmAttack && go.name != handAttackObjName) continue;
          if (!isArmAttack && go.name != tailAttackObjName) continue;
          
          if (activeCollider.gameObject.layer == LayerHelpers.ItemLayer)
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

        // _animator.Play(XenoTriggers.Sleep,0, 1f);
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
        _animationCompletionHandler.Start(WaitForAnimationToEnd("awake", 2, () =>
        {
          _rb.isKinematic = true;
          DeactivateCamouflage();
        }));

        CurrentState = XenoAIState.Idle;
      }

      public IEnumerator WaitForAnimationToEnd(string stateName, int layerIndex, Action onComplete)
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
      
      [Header("Wander Settings")]
      public float wanderRadius = 8f;
      public float wanderCooldown = 5f;
      public float wanderMinDistance = 3f;
      public float wanderSpeed = 0.5f; // Slow

      private float nextWanderTime = 0;
      private Vector3 currentWanderTarget;
      private bool hasWanderTarget = false;

      private IEnumerator ScheduleSleep()
      {
        _animator.SetBool(XenoBooleans.Idle, true);
        yield return new WaitForSeconds(timeUntilSleep);
        _animator.SetBool(XenoBooleans.Idle, false);
        SetSleeping();
        _sleepCoroutineHandler.Start(ScheduleWakeup());
      }
      public void StartWander()
      {
        CurrentState = XenoAIState.Hunt;
        hasWanderTarget = false;
      }
      private void UpdateWander()
      {
        if (Time.time < nextWanderTime) return;

        // If no target or reached, pick a new one
        if (!hasWanderTarget || Vector3.Distance(transform.position, currentWanderTarget) < 1.2f)
        {
          if (TryPickRandomWanderTarget(out currentWanderTarget))
          {
            hasWanderTarget = true;
            nextWanderTime = Time.time + wanderCooldown;
          }
          else
          {
            hasWanderTarget = false;
            nextWanderTime = Time.time + 2f;
            return;
          }
        }

        var toTarget = currentWanderTarget - transform.position;
        float distance = toTarget.magnitude;

        // Turn to face wander target
        RotateTowardsDirection(toTarget, turnSpeed);

        // Check for gaps ahead, jump if needed!
        if (distance > 2f && IsGapAhead(1.0f, 3f, 5f))
        {
          Vector3 jumpTarget;
          if (FindJumpableLanding(out jumpTarget))
          {
            JumpTo(jumpTarget);
            if (_animator) _animator.SetTrigger("Jump");
            hasWanderTarget = false; // After a jump, pick a new target next
            return;
          }
          else
          {
            BrakeHard();
            hasWanderTarget = false;
            return;
          }
        }

        // Walk toward wander target
        var targetSpeed = wanderSpeed;
        moveLerpVel = Mathf.MoveTowards(moveLerpVel, targetSpeed, AccelerationForceSpeed * 0.5f * Time.deltaTime);
        _rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);

        var normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
        _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
      }
      
      private bool TryPickRandomWanderTarget(out Vector3 wanderTarget)
      {
        var origin = transform.position;
        for (int attempts = 0; attempts < 12; attempts++)
        {
          // Pick random angle/distance
          float angle = Random.Range(0, 360f);
          float dist = Random.Range(wanderMinDistance, wanderRadius);
          Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
          Vector3 candidate = origin + dir * dist;

          // Raycast down to ground
          Ray down = new Ray(candidate + Vector3.up * 3f, Vector3.down);
          if (Physics.Raycast(down, out var hit, 10f, LayerMask.GetMask("Default", "terrain")))
          {
            // Optional: only accept if slope not too steep
            if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
            {
              wanderTarget = hit.point;
              return true;
            }
          }
        }
        wanderTarget = Vector3.zero;
        return false; // No valid spot found
      }

      public void OnStateUpdate(XenoAIState previousState, XenoAIState nextState)
      {
        if (nextState != XenoAIState.Sleeping)
        {
          if (_sleepCoroutineHandler.IsRunning)
          {
            _sleepCoroutineHandler.Stop();
          }
          _animator.SetTrigger(XenoTriggers.Awake);
        }

        if (nextState == XenoAIState.Sleeping)
        {
          _animator.SetTrigger(XenoTriggers.Sleep);
          _animator.SetBool(XenoBooleans.Movement, false);
        }

        if (!PrimaryTarget || nextState != XenoAIState.Attack)
        {
          Stop_Attack();
        }
      }

      public void Start_Sleep()
      {
        if (_sleepCoroutineHandler.IsRunning) return;
        _sleepCoroutineHandler.Start(ScheduleSleep());
        CurrentState = XenoAIState.Idle;
      }

      public bool Update_Roam()
      {
        if (roamTarget == null || Vector3.Distance(transform.position, roamTarget.Value) < 1.2f)
        {
          Vector3 newWander;
          if (TryPickRandomWanderTarget(out newWander))
            roamTarget = newWander;
          else
            roamTarget = null;
        }
        if (roamTarget != null)
          MoveTowards(roamTarget.Value, wanderSpeed); // Wander speed

        return roamTarget != null;
      }

      public bool Update_AttackTargetBehavior()
      {
        if (!PrimaryTarget) return false;
        var isInAttackRange = DeltaPrimaryTarget < closeRange;
        if (isInAttackRange)
        {
          Start_Attack();
        }
        else
        {
          Stop_Attack();
        }
        return true;
      }

      public void Update_Position()
      {
        if (IsFleeing())
          MoveTowardsTarget();
      }

      public bool Update_SleepBehavior()
      {
        if (roamTarget != null || PrimaryTarget != null && PrimaryTarget.gameObject.activeInHierarchy) return false;
        Start_Sleep();
        return true;
      }

      public void UpdateBehavior()
      {
        if (IsDead() || IsSleeping()) return;

        UpdatePrimaryTarget();

        // This iterator invokes all methods until a behavior returns true to bail.
        var hasBailed = false;
        foreach (var behaviorUpdater in _behaviorUpdaters)
        {
          var result = behaviorUpdater.Invoke();
          if (!result) continue;
          LoggerProvider.LogDevDebounced($"Bailed on {behaviorUpdater.Method.Name}");
          hasBailed = true;
          break;
        }
        if (hasBailed) return;
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

      private float GetTargetAngle(Vector3 toTarget)
      {
        var forward = transform.forward;
        toTarget.y = 0;
        forward.y = 0;
        return Vector3.SignedAngle(forward, toTarget, Vector3.up); // degrees: -180 to +180
      }

      public void MoveTowardsTarget()
      {
        if (!PrimaryTarget) return;

        // Predict target's future position
        var predictedTarget = PredictTargetPosition();
        var toTarget = predictedTarget - transform.position;
        var distance = toTarget.magnitude;

        // Always rotate toward target
        RotateTowardsDirection(toTarget, turnSpeed);

        // Only move forward if:
        // - The target is not far behind (optional), AND
        // - There is safe ground ahead (not a ledge)

        if (IsGapAhead(1.0f, 3f, 5f)) // Only jump if there is a real gap
        {
          Vector3 jumpTarget;
          if (FindJumpableLanding(out jumpTarget))
          {
            JumpTo(jumpTarget);
            if (_animator) _animator.SetTrigger("Jump");
            return;
          }
          else
          {
            BrakeHard();
            // if (_animator) _animator.SetFloat(XenoParams.MoveSpeed, 0f);
            // moveLerpVel = 0f;
            return;
          }
        }

        // Optionally, do not move if target is very far behind you
        var targetAngle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
        var shouldMove = Mathf.Abs(targetAngle) < 120f; // or any threshold

        var slowDownStart = 6f;
        var speedFactor = Mathf.Clamp01(distance / slowDownStart);
        var targetSpeed = Mathf.Lerp(closeMoveSpeed, moveSpeed, speedFactor);
        var targetAccel = Mathf.Lerp(closeAccelForce, AccelerationForceSpeed, speedFactor);

        if (shouldMove)
        {
          moveLerpVel = Mathf.MoveTowards(moveLerpVel, targetSpeed, targetAccel * Time.deltaTime);
          _rb.AddForce(transform.forward * moveLerpVel, ForceMode.Acceleration);
        }
        else
        {
          moveLerpVel = Mathf.MoveTowards(moveLerpVel, 0f, targetAccel * 2f * Time.deltaTime);
          // Optionally: trigger "turn in place" animation if angle > 90°
        }

        var normalized = Mathf.InverseLerp(0f, 8f, _rb.velocity.magnitude);
        _animator.SetFloat(XenoParams.MoveSpeed, normalized < IdleThresholdTime ? -1f : normalized);
      }

    public float MaxJumpDistance = 4f; // Exposed in Inspector, tweak as desired
    public float MaxJumpHeightArc = 2.5f; // Exposed in Inspector, tweak as desired

    public float lastJumpTime = 0f;
    
    // --- Find a safe jump landing, never exceeding MaxJumpDistance ---
    private bool FindJumpableLanding(out Vector3 landingPoint, float maxDrop = 2.5f, float fanAngle = 60f, int numChecks = 7)
{
    landingPoint = Vector3.zero;
    if (!CanJump || !IsGrounded()) return false;

    var jumpOrigin = GetFurthestToe()?.position ?? transform.position;


    float bestDistance = 0f;
    Vector3 bestLanding = Vector3.zero;
    bool found = false;

    for (int i = 0; i < numChecks; i++)
    {
        float angle = -fanAngle / 2f + fanAngle / (numChecks - 1) * i;
        Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
        // Use the actual edge of the collider + a small fudge
        var checkStart = jumpOrigin + dir;

        for (float dist = 1.0f; dist <= MaxJumpDistance; dist += 0.5f)
        {
            Vector3 rayOrigin = checkStart + dir * dist;
            Debug.DrawRay(rayOrigin, Vector3.down * (maxDrop + 0.5f), Color.cyan, 2f);

            Ray downRay = new Ray(rayOrigin, Vector3.down);
            if (Physics.Raycast(downRay, out var hit, maxDrop + 0.5f, LayerMask.GetMask("Default", "terrain")))
            {
                float verticalDrop = jumpOrigin.y - hit.point.y;
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                float landingDist = Vector3.Distance(jumpOrigin, hit.point);

                if (verticalDrop < maxDrop && slope < 40f && landingDist <= MaxJumpDistance)
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
          
    private void JumpTo(Vector3 landingPoint)
    {
      if (lastJumpTime > Time.fixedTime) return;
      lastJumpTime = Time.fixedTime + 0.5f;
      Vector3 origin = GetLowestPointOnRigidbody();

      // Clamp landing point if somehow it's out of range
      Vector3 jumpVec = landingPoint - origin;
      float dist = jumpVec.magnitude;
      if (dist > MaxJumpDistance)
      {
        jumpVec = jumpVec.normalized * MaxJumpDistance;
        landingPoint = origin + jumpVec;
        dist = MaxJumpDistance;
      }

      // Draw debug line in editor
      Debug.DrawLine(origin + Vector3.up * 0.2f, landingPoint + Vector3.up * 0.2f, Color.red, 2f);

      // --- Disable animator for physics jump (if desired)
      if (_animator && _animator.enabled)
        _animator.enabled = false;

      // --- Ballistic calculation
      // Force Y jump to reach arc height
      float gravity = Mathf.Abs(Physics.gravity.y);
      float timeToApex = Mathf.Sqrt(2 * MaxJumpHeightArc / gravity);
      float vy = Mathf.Sqrt(2 * gravity * MaxJumpHeightArc);
      float timeTotal = timeToApex + Mathf.Sqrt(2 * Mathf.Max(0, (landingPoint.y - origin.y)) / gravity);

      // Horizontal velocity to cover distance in total time
      Vector3 horiz = jumpVec;
      horiz.y = 0;
      Vector3 vxz = horiz / Mathf.Max(0.01f, timeTotal);

      // Set rigidbody velocity
      _rb.velocity = vxz + Vector3.up * vy;

      // --- Re-enable animator after delay (or on landing)
      StartCoroutine(ReenableAnimatorAfter(0.7f)); // You can tune this delay!
    }
      
      

      private IEnumerator ReenableAnimatorAfter(float seconds)
      {
        var timePassed = 0f;
        while (timePassed < 0.5f && !IsGrounded() || timePassed > seconds)
        {
          timePassed += Time.deltaTime;
          yield return new WaitForFixedUpdate();
        }
        if (_animator)
          _animator.enabled = true;
      }
      
      private void BrakeHard()
      {
        if (!IsGrounded()) return;
        var v = _rb.velocity;
        v.x = 0f;
        v.z = 0f;
        _rb.velocity = v;
        _rb.angularVelocity = Vector3.zero;
      }

      private void RotateTowardsDirection(Vector3 dir, float customTurnSpeed)
      {
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        var targetRot = Quaternion.LookRotation(dir);
        var turnLerp = customTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;

        // Instantly set rotation if angle is very large (target is behind)
        var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        if (Mathf.Abs(angle) > 170f)
        {
          // Prevents "stalling" when facing directly away from the target
          transform.rotation = targetRot;
        }
        else
        {
          transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(turnLerp));
        }
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

      private Vector3 PredictTargetPosition(float predictionTime = 0.5f)
      {
        if (!PrimaryTarget) return transform.position;

        // Try to get the target's velocity (via Rigidbody)
        var targetRb = PrimaryTargetRB ?? PrimaryTarget.GetComponent<Rigidbody>();
        if (targetRb)
        {
          return PrimaryTarget.position + targetRb.velocity * predictionTime;
        }
        // Fallback: just use their current position (not ideal)
        return PrimaryTarget.position;
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
          Physics.IgnoreCollision(allCollider1, allCollider2, true);
          ;
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
          RecursiveCollect(list, root, true);
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
          bone.gameObject.layer = LayerHelpers.PieceLayer;
          allColliders.Add(capsule);
          // Adjust collider size/orientation as needed for your model
          capsule.radius = 0.5f;
          capsule.height = 0.2f;
          capsule.direction = 2; // 0=X, 1=Y, 2=Z
        }
      }

      private void RecursiveCollect(HashSet<Transform> list, Transform joint, bool skip = false)
      {
        if (!skip)
        {
          list.Add(joint);
        }
        if (joint.name.Contains("XenosBiped_r_Toe02_Base_SHJnt"))
        {
          rightToeTransform = joint;
        } 
        if (joint.name.Contains("XenosBiped_l_Toe02_Base_SHJnt"))
        {
          leftToeTransform = joint;
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
      public Transform leftToeTransform;   // Assign to left foot/toe bone
      public Transform rightToeTransform;  // Assign to right foot/toe bone
    #endregion

    }
  }