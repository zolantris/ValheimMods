using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{
  public class XenoAnimationController : MonoBehaviour, IXenoAnimationController
  {

    // Animation hashes
    public static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
    public static readonly int Attack = Animator.StringToHash("attack"); // boolean
    public static readonly int AttackSingle = Animator.StringToHash("attackSingle"); // trigger
    public static readonly int AttackMode = Animator.StringToHash("attackMode"); // int
    public static readonly int AttackSpeed = Animator.StringToHash("attackSpeed"); // float
    public static readonly int Die = Animator.StringToHash("die"); // trigger
    public static readonly int AwakeTrigger = Animator.StringToHash("awake"); // trigger
    public static readonly int Move = Animator.StringToHash("move"); // trigger
    public static readonly int Sleep = Animator.StringToHash("sleep"); // trigger
    public static readonly int Idle = Animator.StringToHash("idle"); // trigger
    public static readonly int JumpTrigger = Animator.StringToHash("jump"); // bool (this can be used to disable other animations)
    [Header("Animator Poses")]
    [SerializeField] public XenoAnimationPoses.Variants poseVariant = XenoAnimationPoses.Variants.Crouch; // Type any pose name defined in XenoAnimationPoses
    [Header("Animator/Bones")]
    [SerializeField] public Animator animator;
    [SerializeField] public XenoDroneAI OwnerAI;
    [SerializeField] private ParticleSystem _bloodEffects;

    [SerializeField] private string _handAttackObjName = "xeno_arm_attack_collider";
    [SerializeField] private string _tailAttackObjName = "xeno_tail_attack_collider";

    public SkinnedMeshRenderer xenoSkinnedMeshRenderer;

    [Header("Animation Transforms")]
    public Transform xenoAnimatorRoot;
    public Transform xenoMeshSkin, xenoRoot, spine01, spine02, spine03, spineTop, neckUpDown, neckPivot;
    public Transform leftHip, rightHip, leftArm, rightArm, tailRoot, leftToeTransform, rightToeTransform;
    public float UpdatePauseTime = 2f;
    public float attack_nextUpdateTime;
    public float nextUpdateInterval = 0.25f;

    public float moveSpeed_nextUpdateTime;
    // public float nextUpdateInterval = 0.25f;
    public float SleepMoveUpdateTilt;
    public float SleepMoveZTilt = 90f;
    public float LastSleepMoveUpdate;

    public string armAttackObjName = "xeno_arm_attack_collider";
    public string tailAttackObjName = "xeno_tail_attack_collider";

    [SerializeField] private Vector3 _leftFootIKPos, _rightFootIKPos;

    public LayerMask groundLayer; // Assign in inspector or at runtime
    public float footRaycastHeight = 0.5f; // Height above toe to start ray
    public float footRaycastDistance = 1.5f;
    public float footOffset = 0.01f; // Offset so foot doesn’t clip ground

    public Vector2 yAngleRange = new(-20f, 20f); // Min, Max yaw (side to side)
    public Vector2 zAngleRange = new(-10f, 10f); // Min, Max tilt (side tilt)
    public float scanPeriod = 8f; // Seconds for a full left-right-left cycle
    public float randomizeAmount = 0.3f; // How much random drift to add

    // private AnimatorIKRelay ikRelay;
    public readonly Dictionary<string, Transform> allAnimationJoints = new();

    public readonly SleepAnimation sleepAnimation = new();

    [Header("Animation Updaters")]
    private int _cachedAttackMode;

    private float _cachedMoveSpeed;
    private bool _canPlayEffectOnFrame = true;
    private CoroutineHandle _jumpAnimationRoutine;
    private CoroutineHandle _headTurnRoutine;


    // State
    private bool _lastCamouflageState;
    private Quaternion _leftFootIKRot, _rightFootIKRot;
    private float _leftFootIKWeight = 1f, _rightFootIKWeight = 1f; // Usually always 1, or blend if needed
    private CoroutineHandle _sleepAnimationRoutine;

    private float baseY; // Set this to your starting Y angle (e.g. for sleep pose)
    private float baseZ; // Set this to your starting Z angle
    public HashSet<Collider> footColliders = new();
    public HashSet<Transform> leftArmJoints = new();
    public Dictionary<string, JointPose> poseSnapshot;

    private float randomOffsetY;
    private float randomOffsetZ;
    public HashSet<Transform> rightArmJoints = new();
    public HashSet<Transform> tailJoints = new();

    // --- Sleeping Animation "Twitch" ---
    private Quaternion targetNeckRotation = Quaternion.identity;

    [SerializeField] private Vector3 neckPivotAngle;
    [SerializeField] private Vector3 neckUpDownAngle;

    public Vector2 neckRangeX = new(-40f, 40f);
    public Vector2 neckRotationZRange = new(-40f, 40f);
    public Vector3 neckPivotStartRotation = new(0f, 0f, 65f);
    public Vector3 neckUpDownStartRotation = new(0f, 0f, 40f);

    private void Awake()
    {
      if (!animator) animator = GetComponentInChildren<Animator>();
      if (_bloodEffects == null) _bloodEffects = GetComponentInChildren<ParticleSystem>();
      OwnerAI = GetComponentInParent<XenoDroneAI>();

      SetupXenoTransforms();
      InitCoroutineHandlers();
      InitAnimators();
      // AddCapsuleCollidersToAllJoints();
    }

    private bool _hasRunHeadUpdate = false;

    private void LateUpdate()
    {
      _hasRunHeadUpdate = false;
      if (neckPivot && neckUpDown)
      {
        neckPivot.localRotation = Quaternion.Euler(neckPivotAngle);
        neckUpDown.localRotation = Quaternion.Euler(neckUpDownAngle);
      }
      // UpdateFootIK();
      // sleepAnimation.LateUpdate_MoveHeadAround(neckPivot);
      // PlaySleepingCustomAnimations();
      // ...any other animation/pose code
    }

    // public void SetupIKRelayReceiver()
    // {
    //     if (ikRelay)
    //     {
    //         ikRelay.SetReceiver(this);
    //         return;
    //     }
    //     ikRelay = xenoAnimatorRoot.gameObject.GetComponent<AnimatorIKRelay>();
    //     if (ikRelay == null)
    //     {
    //         ikRelay = xenoAnimatorRoot.gameObject.AddComponent<AnimatorIKRelay>();
    //     }
    //     ikRelay.SetReceiver(this);
    //     
    // }

    private void OnEnable()
    {
      SetupXenoTransforms();
      InitCoroutineHandlers();
      InitAnimators();
    }

    public void PlaySleepingAnimation(bool canSleepAnimate)
    {
      if (!canSleepAnimate) return;
      if (_sleepAnimationRoutine.IsRunning) return;
      _sleepAnimationRoutine.Start(SleepingAnimationCoroutine());
    }

    public void PlayDodgeAnimation(Vector3 dodgeDir)
    {
      PlayJump();
    }
    public void StopDodgeAnimation()
    {
      _jumpAnimationRoutine.Stop();
    }

    public void PlayJump(string[] skipTransformNames = null)
    {
      if (_jumpAnimationRoutine.IsRunning) return;

      _jumpAnimationRoutine.Start(SimulateJumpWithPoseLerp(
        allAnimationJoints,
        XenoAnimationPoses.Idle, // Idle or standing pose
        XenoAnimationPoses.Crouch,
        0.18f, // crouch time (down)
        0.25f, // air time (wait)
        0.24f, skipTransformNames));
    }

    // --- ANIMATION API ---
    public void SetMoveSpeed(float normalized, bool shouldBypass = false)
    {
      if (!animator) return;
      // shouldBypass = shouldBypass || _cachedMoveSpeed == 0f && normalized > 0f;
      // if (!shouldBypass && moveSpeed_nextUpdateTime > Time.fixedTime) return;
      // moveSpeed_nextUpdateTime = Time.fixedTime + nextUpdateInterval;

      _cachedMoveSpeed = normalized;
      animator.SetFloat(MoveSpeed, normalized);
    }

    [ContextMenu("Run SetupXenoTransforms")]
    public void SetupXenoTransforms()
    {
      BindUnOptimizedRoots();

      xenoSkinnedMeshRenderer = xenoMeshSkin.GetComponent<SkinnedMeshRenderer>();

      var preGeneratedColliders = GetComponentsInChildren<Collider>();

      AssignAttackColliders(preGeneratedColliders);
      AssignFootColliders(preGeneratedColliders);

      CollectAllBodyJoints();
      RecursiveCollectAllJoints(xenoRoot);

      // initial values.
      // neckPivotAngle = neckPivot.localEulerAngles;
      // neckUpDownAngle = neckUpDown.localEulerAngles;
      neckPivotAngle = neckPivotStartRotation;
      neckUpDownAngle = neckUpDownStartRotation;
    }

    public void ApplyPose(XenoAnimationPoses.Variants variant)
    {
      var pose = XenoAnimationPoses.GetPose(variant);
      foreach (var kvp in pose)
      {
        if (allAnimationJoints.TryGetValue(kvp.Key, out var t) && t != null)
        {
          t.localPosition = kvp.Value.Position;
          t.localRotation = kvp.Value.Rotation;
        }
      }
    }

    [ContextMenu("Apply Selected Variant Pose")]
    public void ApplySelectedVariantPose()
    {
      SetupXenoTransforms();
      ApplyPose(poseVariant);
    }

    // public void OnAnimatorIKRelay(int layerIndex)
    // {
    //     PlaySleepingCustomAnimations();
    //     // PointHeadTowardTarget();
    //     // UpdateFootIK();
    //     LoggerProvider.LogDebugDebounced("called IK event");
    //     // _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _leftFootIKWeight);
    //     // _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootIKPos);
    //     // _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _leftFootIKWeight);
    //     // _animator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootIKRot);
    //     //
    //     // _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _rightFootIKWeight);
    //     // _animator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootIKPos);
    //     // _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _rightFootIKWeight);
    //     // _animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootIKRot);
    // }

    // private void UpdateFootIK()
    // {
    //     // Use your toe or ankle bones (assign in inspector or bind in code)
    //     var leftFootT = leftToeTransform;
    //     var rightFootT = rightToeTransform;
    //
    //     _leftFootIKPos = GetFootIKPosition(leftFootT.position);
    //     _rightFootIKPos = GetFootIKPosition(rightFootT.position);
    //
    //     _leftFootIKRot = GetFootIKRotation(leftFootT, _leftFootIKPos);
    //     _rightFootIKRot = GetFootIKRotation(rightFootT, _rightFootIKPos);
    // }
    //
    // private Vector3 GetFootIKPosition(Vector3 footWorldPos)
    // {
    //     Vector3 rayOrigin = footWorldPos + Vector3.up * footRaycastHeight;
    //     if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, footRaycastDistance + footRaycastHeight, groundLayer))
    //         return hit.point + Vector3.up * footOffset;
    //     return footWorldPos;
    // }
    //
    // private Quaternion GetFootIKRotation(Transform foot, Vector3 targetPos)
    // {
    //     if (Physics.Raycast(targetPos + Vector3.up * 0.1f, Vector3.down, out var hit, 0.3f, groundLayer))
    //         return Quaternion.FromToRotation(Vector3.up, hit.normal) * foot.rotation;
    //     return foot.rotation;
    // }

    private void InitAnimators()
    {
      sleepAnimation.Setup(this, neckPivot);
    }

    private void InitCoroutineHandlers()
    {
      _sleepAnimationRoutine ??= new CoroutineHandle(this);
      _headTurnRoutine ??= new CoroutineHandle(this);
      _jumpAnimationRoutine ??= new CoroutineHandle(this);
    }


    // ---- BONE/JOIN SETUP/UTILITY ----
    public void CollectAllBodyJoints()
    {
      leftArmJoints = new HashSet<Transform>();
      rightArmJoints = new HashSet<Transform>();
      tailJoints = new HashSet<Transform>();

      RecursiveCollectTransform(leftArmJoints, leftArm);
      RecursiveCollectTransform(rightArmJoints, rightArm);
      RecursiveCollectTransform(tailJoints, tailRoot);
    }

    private void RecursiveCollectTransform(HashSet<Transform> list, Transform joint, bool skip = false)
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
        RecursiveCollectTransform(list, child);
      }
    }

    public void BindUnOptimizedRoots()
    {
      xenoAnimatorRoot = transform.Find("Visual") ?? transform;
      xenoMeshSkin = xenoAnimatorRoot.Find("alien_xenos_drone_SK_Xenos_Drone");
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
      leftToeTransform = leftHip.Find("XenosBiped_l_Leg_Knee1SHJnt/XenosBiped_l_Leg_Knee2_CurveSHJnt/XenosBiped_l_Leg_AnkleSHJnt/XenosBiped_l_Toe02_Base_SHJnt"); // Update to match your rig
      rightToeTransform = rightHip.Find("XenosBiped_r_Leg_Knee1SHJnt/XenosBiped_r_Leg_Knee2_CurveSHJnt/XenosBiped_r_Leg_AnkleSHJnt/XenosBiped_r_Toe02_Base_SHJnt");
    }

    public void DisableAnimator()
    {
      if (!animator) return;
      animator.enabled = false;
    }

    public void EnableAnimator()
    {
      if (!animator) return;
      animator.enabled = true;
    }

    public float GetAnimationMoveSpeed()
    {
      return animator.GetFloat(MoveSpeed);
    }

    private void RecursiveCollectAllJoints(Transform joint, bool skip = false)
    {
      if (!skip)
        allAnimationJoints[joint.name] = joint;
      foreach (Transform child in joint)
        RecursiveCollectAllJoints(child);
    }

    [ContextMenu("Dump PoseSnapshot as C# (to dated file)")]
    public void DumpCurrentPoseAsCSharpToFile()
    {
      SetupXenoTransforms();
      SnapshotCurrentPose();
      JointPoseDumpUtility.DumpPoseToFile(poseSnapshot, "Xeno_IdlePose");
    }

    [ContextMenu("Dump Delta PoseSnapshot as C# (to dated file)")]
    public void DumpDeltaPoseAsCSharpToFile()
    {
      SetupXenoTransforms();
      SnapshotCurrentPose();
      JointPoseDumpUtility.DumpDeltaPoseToFile(XenoAnimationPoses.Idle, poseSnapshot, "Crouch_HeadTurnDelta");
    }


    [ContextMenu("Run SnapshotCurrentPose")]
    public void SnapshotCurrentPose()
    {
      poseSnapshot = new Dictionary<string, JointPose>();
      foreach (var kvp in allAnimationJoints)
        poseSnapshot[kvp.Key] = new JointPose(kvp.Value.localPosition, kvp.Value.localRotation);
    }

    [ContextMenu("Run RestorePose")]
    public void RestorePoseFrom(Dictionary<string, JointPose> poseDict)
    {
      foreach (var kvp in poseDict)
      {
        if (allAnimationJoints.TryGetValue(kvp.Key, out var t) && t != null)
        {
          t.localPosition = kvp.Value.Position;
          t.localRotation = kvp.Value.Rotation;
        }
      }
    }

    public void PlayDead()
    {
      if (!animator) return;
      animator.SetBool(Move, false);
      animator.SetTrigger(Die);
    }

    public void PlayAwake()
    {
      animator.SetTrigger(AwakeTrigger);
    }
    public void PlaySleep()
    {
      animator.SetTrigger(Sleep);
      animator.SetBool(Move, false);
    }

    public void PlayAttack(bool canRandomize = true)
    {
      PlayAttack(_cachedAttackMode, canRandomize);
    }

    public float GetAttackSpeed()
    {
      return animator.GetFloat(AttackSpeed);
    }

    public void SetAttackSpeed(float val, bool skipValidation)
    {
      if (skipValidation || !Mathf.Approximately(GetAttackSpeed(), val))
      {
        animator.SetFloat(AttackSpeed, Mathf.Clamp(val, 0.5f, 2f));
      }
    }

    /// <summary>
    ///   Set the attack type. Otherwise use it without an arg to get a randomize
    ///   version.
    /// </summary>
    public void PlayAttack(int attackMode, bool canRandomize = false, bool isSingle = false)
    {
      EnableAttackColliders(attackMode);
      if (canRandomize)
      {
        TryRandomizeAttackMode();
      }
      else
      {
        SetAttackMode(attackMode);
      }

      if (isSingle)
      {
        animator.SetTrigger(AttackSingle);
      }
      else
      {
        animator.SetBool(Attack, true);
      }
    }

    public bool IsRunningAttack()
    {
      return animator.GetBool(Attack);
    }

    public void StopAttack()
    {
      DisableAttackColliders();
      animator.SetBool(Attack, false);
    }

    public void SetAttackMode(int mode)
    {
      attack_nextUpdateTime = Time.fixedTime + nextUpdateInterval;
      _cachedAttackMode = mode;
      animator.SetInteger(AttackMode, _cachedAttackMode);
    }

    public void TryRandomizeAttackMode()
    {
      if (attack_nextUpdateTime > Time.fixedTime) return;
      var attackSpeed = Random.Range(0.8f, 1.2f);
      var nextMode = Mathf.RoundToInt(Random.value);
      SetAttackSpeed(attackSpeed, true);
      SetAttackMode(nextMode);
    }

    public void PlayBloodEffect()
    {
      if (_canPlayEffectOnFrame && _bloodEffects != null)
      {
        _bloodEffects.Play();
        _canPlayEffectOnFrame = false;
      }
    }
    public void ResetBloodCooldown()
    {
      _canPlayEffectOnFrame = !_bloodEffects.isPlaying;
    }

    public static void ToggleColliderList(IEnumerable<Collider> colliders, bool isEnabled)
    {
      foreach (var col in colliders)
      {
        if (!col) continue;
        col.enabled = isEnabled;
      }
    }

    // --- COLLIDER HELPERS ---

    /// <summary>
    ///   Enables and disables attack colliders based on attack type
    /// </summary>
    /// <param name="attackMode"></param>
    public void EnableAttackColliders(int attackMode)
    {
      var isArmAttack = attackMode == 0;
      var isTailAttack = attackMode == 1;

      if (isArmAttack)
      {
        ToggleColliderList(attackArmColliders, true);
        ToggleColliderList(attackTailColliders, false);
        return;
      }

      if (isTailAttack)
      {
        ToggleColliderList(attackTailColliders, true);
        ToggleColliderList(attackArmColliders, false);
      }
    }
    public void DisableAttackColliders()
    {
      ToggleColliderList(attackArmColliders, false);
      ToggleColliderList(attackTailColliders, false);
    }

    private void PlaySleepingCustomAnimations()
    {

      // neckPivot.localRotation = Quaternion.Slerp(neckPivot.localRotation, Quaternion.identity, Time.deltaTime);
      // var t = Mathf.PingPong(Time.time / scanPeriod, 1f);
      // var smoothT = Mathf.SmoothStep(0, 1, t);
      //
      // // Oscillate between min/max
      // var yaw = Mathf.Lerp(yAngleRange.x, yAngleRange.y, smoothT) + baseY + randomOffsetY;
      // var ztilt = Mathf.Lerp(zAngleRange.x, zAngleRange.y, Mathf.PerlinNoise(Time.time * 0.2f, 42f)) + baseZ + randomOffsetZ;
      //
      // // Set local rotation, keeping original X angle if needed (or set to a fixed value)
      // var euler = neckUpDown.localEulerAngles;
      // var x = 0f; // Or euler.x if you want to preserve it
      //
      // neckUpDown.localRotation = Quaternion.Euler(x, yaw, ztilt);
    }

    public static string[] GetCommonKeys(Dictionary<string, JointPose> a, Dictionary<string, JointPose> b, string[] skipTransformNames = null)
    {
      Regex skipRegexp = null;
      if (skipTransformNames != null && skipTransformNames.Length > 0)
      {
        // No anchors: pattern is e.g. "spine|neck|tail", so any name containing any of those will match
        var pattern = string.Join("|", skipTransformNames);
        skipRegexp = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
      }

      var keys = new List<string>();
      foreach (var key in a.Keys)
      {
        if (!b.ContainsKey(key)) continue;
        if (skipRegexp != null && skipRegexp.IsMatch(key)) continue; // <--- contains match
        keys.Add(key);
      }

      return keys.ToArray();
    }

    public IEnumerator LerpBetweenPoses(Dictionary<string, JointPose> a,
      Dictionary<string, JointPose> b, float? timeout = null, string[] skipTransformNames = null)
    {
      var commonKeys = GetCommonKeys(a, b, skipTransformNames);
      var currentTime = 0f;
      var endTime = Time.time + timeout != null ? (float?)Time.time : null;
      while (isActiveAndEnabled && (timeout == null || endTime > currentTime))
      {
        yield return LerpToPose(allAnimationJoints, a, b, null, commonKeys);
        yield return LerpToPose(allAnimationJoints, b, a, null, commonKeys);
        if (endTime != null)
        {
          currentTime = Time.time;
        }
      }
    }

    private IEnumerator SimulateJumpWithPoseLerp(
      Dictionary<string, Transform> allJoints,
      Dictionary<string, JointPose> idlePose,
      Dictionary<string, JointPose> crouchPose,
      float crouchDuration,
      float airMinWaitTime,
      float standDuration,
      string[] skipTransformNames = null,
      Action onComplete = null)
    {
      animator.SetBool(JumpTrigger, true);
      // 1. Disable animator (todo might not have to do this with animator order fixed)
      DisableAnimator();

      // 2. Idle → Crouch
      yield return LerpToPose(allJoints, idlePose, crouchPose, skipTransformNames, null, crouchDuration);

      var lerpPoseCoroutine = StartCoroutine(LerpBetweenPoses(crouchPose, XenoAnimationPoses.CrouchHeadRight, null, skipTransformNames));

      // 3. Stay crouched during "air time" (simulate jump apex)
      var timePassed = 0f;
      while (!OwnerAI.IsGrounded() || timePassed < airMinWaitTime)
      {
        timePassed += Time.deltaTime;
        yield return null;
      }

      StopCoroutine(lerpPoseCoroutine);

      SnapshotCurrentPose();
      yield return LerpToPose(allJoints, poseSnapshot, idlePose, skipTransformNames, null, 0.1f);

      // 4. Crouch → Idle
      yield return LerpToPose(allJoints, crouchPose, idlePose, skipTransformNames, null, standDuration);

      // 5. Re-enable animator
      EnableAnimator();
      onComplete?.Invoke();

      animator.SetBool(JumpTrigger, false);
    }

    public void StartSleepAnimation()
    {
      _sleepAnimationRoutine.Start(SleepingAnimationCoroutine());
    }

    public IEnumerator LerpToPose(
      Dictionary<string, Transform> allJoints,
      Dictionary<string, JointPose> startPose,
      Dictionary<string, JointPose> endPose,
      string[] skipTransformNames = null,
      string[] commonKeys = null,
      float duration = 0.25f)
    {
      // Compute keys if not provided
      commonKeys ??= GetCommonKeys(startPose, endPose, skipTransformNames);

      var time = 0f;
      while (time < duration)
      {
        yield return new WaitForEndOfFrame();
        var t = time / duration;
        foreach (var jointName in commonKeys)
        {
          if (!allJoints.TryGetValue(jointName, out var joint) || joint == null)
            continue;
          var poseA = startPose[jointName];
          var poseB = endPose[jointName];
          joint.localPosition = Vector3.Lerp(poseA.Position, poseB.Position, t);
          joint.localRotation = Quaternion.Slerp(poseA.Rotation, poseB.Rotation, t);
        }
        time += Time.deltaTime;
      }
      // Snap to end pose
      foreach (var jointName in commonKeys)
      {
        if (!allJoints.TryGetValue(jointName, out var joint) || joint == null)
          continue;
        var poseB = endPose[jointName];
        joint.localPosition = poseB.Position;
        joint.localRotation = poseB.Rotation;
      }
    }

    public IEnumerator SleepingAnimationCoroutine()
    {
      PlaySleep();
      yield return new WaitForSeconds(2f);
      sleepAnimation.StartTurningHead();
      yield return new WaitUntil(() => !OwnerAI.IsSleeping());
      sleepAnimation.StopTurningHead();
    }

    public Transform GetFurthestToe()
    {
      if (leftToeTransform == null && rightToeTransform == null) return null;
      if (leftToeTransform != null && rightToeTransform == null) return leftToeTransform;
      if (rightToeTransform != null && leftToeTransform == null) return rightToeTransform;

      var forward = transform.forward.normalized;
      var leftProj = Vector3.Dot(leftToeTransform.position - transform.position, forward);
      var rightProj = Vector3.Dot(rightToeTransform.position - transform.position, forward);

      return leftProj > rightProj ? leftToeTransform : rightToeTransform;
    }

    #region Head Rotation

    [SerializeField] private float yawMaxDeg = 40f;
    [SerializeField] private float yawSpeedDegPerSec = 540f;

    [SerializeField] private float pitchDownMaxDeg = 40f; // your +40 at feet
    [SerializeField] private float pitchSpeedDegPerSec = 360f;
    [SerializeField] private float lookDownHeightThreshold = 0.25f; // meters below neck before we start pitching down

    private static float UnwrapSigned(float deg)
    {
      return deg > 180f ? deg - 360f : deg;
    }
    private static float MoveTowardsSigned(float current, float target, float maxDelta)
    {
      current = UnwrapSigned(current);
      target = UnwrapSigned(target);
      return Mathf.MoveTowards(current, target, maxDelta);
    }
    // Call from LateUpdate while aiming
    // Call from LateUpdate while aiming
    private void UpdateHeadAnglesToward(Transform target)
    {
      if (!target || !neckUpDown) return;
      var forwardYaw = transform.forward;

      var up = transform.up;

      // Vector to target
      var to = target.position - neckUpDown.position;
      var toNorm = to.normalized;

      // ===== YAW (left/right) on horizontal plane =====
      var toPlane = Vector3.ProjectOnPlane(toNorm, up);
      if (toPlane.sqrMagnitude < 1e-8f) return;

      var fwdPlane = Vector3.ProjectOnPlane(forwardYaw, up).normalized;
      var yawDeg = Mathf.Clamp(Vector3.SignedAngle(fwdPlane, toPlane, up), -yawMaxDeg, yawMaxDeg);

      neckUpDownAngle.y = MoveTowardsSigned(neckUpDownAngle.y, yawDeg, yawSpeedDegPerSec * Time.deltaTime);

      // ===== PITCH (down only) using heading-based right axis =====
      // Right axis tied to *heading* not current neck rotation → stable pitch sign.
      var rightHeading = Vector3.Cross(up, toPlane).normalized;

      // Elevation relative to heading plane (+up, -down)
      var pitchDegHeading = Vector3.SignedAngle(toPlane, toNorm, rightHeading);

      // Only look down if target is sufficiently below by height threshold.
      var verticalDelta = Vector3.Dot(to, up); // +above, -below
      var desiredZ = -pitchDownMaxDeg; // -40 = forward
      if (verticalDelta < -lookDownHeightThreshold)
      {
        // Map downward elevation to [-40 .. +40]
        var downDeg = Mathf.Clamp(-pitchDegHeading, 0f, 90f); // 0=level/up, 90=straight down
        var t = downDeg / 90f;
        desiredZ = Mathf.Lerp(-pitchDownMaxDeg, +pitchDownMaxDeg, t);
      }

      neckUpDownAngle.z = MoveTowardsSigned(neckUpDownAngle.z, desiredZ, pitchSpeedDegPerSec * Time.deltaTime);
    }

    #endregion

    public void PointHeadTowardTarget(Transform target)
    {
      if (!_hasRunHeadUpdate)
        UpdateHeadAnglesToward(target);
    }

    #region Colliders

    public HashSet<Collider> allColliders = new();
    public HashSet<Collider> attackTailColliders = new();
    public HashSet<Collider> attackArmColliders = new();

    #endregion

    #region Colliders

    public void AssignFootColliders(IEnumerable<Collider> colliders)
    {
      foreach (var col in colliders)
      {
        if (col.name.StartsWith("foot_pad_collider"))
        {
          footColliders.Add(col);
        }
      }
    }

    public void AssignAttackColliders(IEnumerable<Collider> colliders)
    {
      foreach (var col in colliders)
      {
        if (!col) continue;
        var colName = col.name;
        if (colName == armAttackObjName)
        {
          attackArmColliders.Add(col);
        }
        if (colName == tailAttackObjName)
        {
          attackTailColliders.Add(col);
        }
      }
    }

    public void AddCapsuleCollidersToAllJoints()
    {
      foreach (var t in leftArmJoints)
        AddCapsuleColliderIfMissing(t);
      foreach (var t in rightArmJoints)
        AddCapsuleColliderIfMissing(t);
      foreach (var t in tailJoints)
        AddCapsuleColliderIfMissing(t);
    }

    private void AddCapsuleColliderIfMissing(Transform t)
    {
      if (!t) return;
      var col = t.GetComponent<Collider>();
      if (!col)
      {
        var capsuleCol = t.gameObject.AddComponent<SphereCollider>();
        capsuleCol.radius = 0.1f;
        col = capsuleCol;
      }
      allColliders.Add(col);
    }

    #endregion

  }
}