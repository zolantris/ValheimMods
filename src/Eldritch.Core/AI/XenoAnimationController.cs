using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{
  public struct PoseTransition
  {
    public Dictionary<string, JointPose> PoseData;

    public float Pause; // pause after running the transition.
    /// <summary>Duration in seconds for this transition (applies when this is the *next* pose).</summary>
    public float Speed;

    [CanBeNull] public Action OnStart;
    [CanBeNull] public Action OnEnd;

    /// <summary>
    /// Progress/easing curve for this transition (applies when this is the *next* pose).
    /// Evaluated with input t in [0..1]. If null, uses linear (t).
    /// Examples: ease-in (fast end), ease-out (fast start), ease-in-out, bell/bump (slow middle).
    /// </summary>
    public AnimationCurve SpeedCurve;
  }

  public class XenoAnimationController : MonoBehaviour, IXenoAnimationController
  {

    // Animation hashes
    public static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
    public static readonly int AutoAttack = Animator.StringToHash("autoAttack"); // boolean
    public static readonly int ManualAttackCompleteTrigger = Animator.StringToHash("manualAttackComplete"); // trigger
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
    public Transform animationOffset;
    public Transform xenoMeshSkin, xenoRoot, spine01, spine02, spine03, spineTop, neckUpDown, neckPivot;
    public Transform leftHip, rightHip, leftArm, rightArm, tailRoot, leftToeTransform, rightToeTransform;
    public float UpdatePauseTime = 2f;
    public float attack_nextUpdateTime;
    public float nextUpdateInterval = 2.25f;

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
    private CoroutineHandle _tailAttackTransitionRoutine;

    private AnimatorStateIdUtil.StateId[] _armIds;
    private AnimatorStateIdUtil.StateId[] _tailIds;

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
    public Vector3 neckPivotStartRotation = new(0f, 0f, 90f);
    public Vector3 neckUpDownStartRotation = new(0f, 0f, 40f);

    // offset for animation root (some animations do not align perfectly)
    [NonSerialized] private Vector3 baseAnimationPosition = new(0, -0.7f, -0.48f);
    [NonSerialized] private Vector3 armAttackOffsetPosition = new(0, -0.5f, 0.2f);

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

      // Do not allow attack colliders after animation is in transition
      if (!IsAnimatingArmAttack() && !IsAnimatingTailAttack())
      {
        DisableAttackColliders();
      }

      if (OwnerAI.PrimaryTarget && OwnerAI.IsInAttackReachRange() || OwnerAI.IsInHuntingRange())
      {
        PointHeadTowardTarget(OwnerAI.PrimaryTarget);
      }

      if (IsAnimatingArmAttack())
      {
        animationOffset.localPosition = Vector3.Lerp(animationOffset.localPosition, baseAnimationPosition + armAttackOffsetPosition, 0.5f);
      }
      else
      {
        animationOffset.localPosition = Vector3.Lerp(animationOffset.localPosition, baseAnimationPosition, Time.deltaTime * 5f);
      }

      if (IsAnimatingTailAttack() && !_tailAttackTransitionRoutine.IsRunning)
      {
        TailAttackManualAnimation_Start();
      }

      // clears/updates all poses for next frame.
      if (_latePoseDirty && _latePoseTargets.Count > 0)
      {
        foreach (var kv in _latePoseTargets)
        {
          var data = kv.Value;
          if (!data.transform) continue;
          data.transform.localPosition = data.Position;
          data.transform.localRotation = data.Rotation;
        }

        LoggerProvider.LogDebugDebounced("LateUpdate applied " + _latePoseTargets.Count + " poses.");
        _latePoseTargets.Clear();
        _latePoseDirty = false;
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

    private Coroutine _debugTailRoutine;
    private float[] _savedLayerWeights;

    private void SaveLayerWeights()
    {
      if (!animator) return;
      var layers = animator.layerCount;
      _savedLayerWeights = new float[layers];
      for (var i = 0; i < layers; i++) _savedLayerWeights[i] = animator.GetLayerWeight(i);
    }

    private void RestoreLayerWeights()
    {
      if (!animator || _savedLayerWeights == null) return;
      var layers = Mathf.Min(animator.layerCount, _savedLayerWeights.Length);
      for (var i = 0; i < layers; i++) animator.SetLayerWeight(i, _savedLayerWeights[i]);
    }

    private AnimationClip GetDominantClipOnLayer(int layer)
    {
      if (!animator) return null;

      var infos = animator.GetCurrentAnimatorClipInfo(layer);
      AnimationClip best = null;
      var bestW = -1f;
      for (var i = 0; i < infos.Length; i++)
      {
        if (infos[i].clip && infos[i].weight > bestW)
        {
          bestW = infos[i].weight;
          best = infos[i].clip;
        }
      }

      // During transition, also consider next state
      if (animator.IsInTransition(layer))
      {
        var next = animator.GetNextAnimatorClipInfo(layer);
        for (var i = 0; i < next.Length; i++)
        {
          if (next[i].clip && next[i].weight > bestW)
          {
            bestW = next[i].weight;
            best = next[i].clip;
          }
        }
      }
      return best;
    }

    private void PlayTailAttackOnce()
    {
      TailAttackManualAnimation_Start();
    }

    // Right-click the component header to run these
    [ContextMenu("Debug/Tail Attack ▶")]
    private void Debug_TailAttack_0_8s()
    {
      PlayTailAttackOnce();
    }

    private void OnEnable()
    {
      SetupXenoTransforms();
      InitCoroutineHandlers();
      InitAnimators();
    }

    private void OnDisable()
    {
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

    public void PlayJump(List<string> skipTransformNames = null)
    {
      if (_jumpAnimationRoutine == null)
      {
        InitCoroutineHandlers();
      }
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
    public void SetMoveSpeed(float speed, bool shouldBypass = false)
    {
      if (!animator) return;

      // shouldBypass = shouldBypass || _cachedMoveSpeed == 0f && normalized > 0f;
      // if (!shouldBypass && moveSpeed_nextUpdateTime > Time.fixedTime) return;
      // moveSpeed_nextUpdateTime = Time.fixedTime + nextUpdateInterval;

      var normalizeMovedSpeed = Mathf.Clamp(speed, -1, 4f);
      _cachedMoveSpeed = normalizeMovedSpeed;
      animator.SetFloat(MoveSpeed, normalizeMovedSpeed);
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


    public static List<PoseTransition> TailAttackTransitions = new()
    {
      // transition from current pose to target pose
      // new PoseTransition
      // {
      //   // PoseData = poseSnapshot
      //   PoseData = XenoAnimationPoses.Idle
      // },
      // transition to idle pose first
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.TailAttack_ChargeTail,
        Speed = 0.2f
      },
      // transition to tail attack fully extended pos
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.TailAttack_PierceSwing1,
        Speed = 0.05f
      },
      // new()
      // {
      //   PoseData = XenoAnimationPoses.TailAttack_PierceSwing2,
      //   Speed = .1f
      // },
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.TailAttack_HitPierce,
        Speed = 0.1f,
        SpeedCurve = curveEaseOut
      }
    };

    [ContextMenu("Lerp To Selected Variant Pose")]
    public void LerpToSelectedVariantPose()
    {
      _tailAttackTransitionRoutine ??= new CoroutineHandle(this);
      if (_tailAttackTransitionRoutine.IsRunning) return;

      SetupXenoTransforms();
      SnapshotCurrentPose();
      // or posSnapshot

      var skipTransformNames = new List<string> { "XenosBiped_Neck_TopSHJnt" };
      _tailAttackTransitionRoutine.Start(LerpBetweenPoses(TailAttackTransitions, skipTransformNames));
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
      _armIds = AnimatorStateIdUtil.Build(armAttackStates);
      _tailIds = AnimatorStateIdUtil.Build(tailAttackStates);
      sleepAnimation.Setup(this, neckPivot);
    }

    private void InitCoroutineHandlers()
    {
      _sleepAnimationRoutine ??= new CoroutineHandle(this);
      _headTurnRoutine ??= new CoroutineHandle(this);
      _tailAttackTransitionRoutine ??= new CoroutineHandle(this);
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
      animationOffset = xenoAnimatorRoot.Find("drone_parent_offset");
      xenoMeshSkin = animationOffset.Find("alien_xenos_drone_SK_Xenos_Drone");
      xenoRoot = animationOffset.Find("alien_xenos_drone_SK_Xenos_Drone_skeleton/XenosBiped_TrajectorySHJnt/XenosBiped_ROOTSHJnt");
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
      JointPoseDumpUtility.DumpPoseToFile(poseSnapshot, $"Xeno_{poseVariant.ToString()}");
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
      if (!OwnerAI.IsAttacking()) return;
      if (canRandomize)
      {
        TryRandomizeAttackMode();
      }
      else
      {
        if (attackMode != _cachedAttackMode)
        {
          SetAttackMode(attackMode);
        }
      }


      var armAttack = _cachedAttackMode == 0;
      var tailAttack = _cachedAttackMode == 1;
      // if (!isUnchanged)
      // {
      //   var armAttack = _cachedAttackMode == 0;
      //   var tailAttack = _cachedAttackMode == 1;
      //   // must be run last as this will check the animator state (not the data)
      //   EnableAttackColliders(armAttack, tailAttack);
      // }

      // calling PlayAttack should trigger the attack animation is nothing is running
      if (!IsAnimatingArmAttack() && !IsAnimatingTailAttack())
      {
        // tail attack can enable mid animation but arm attack is full animation. We would have to time the animation to enable the collider. so we delay it for now
        if (armAttack)
        {
          EnableAttackCollidersDelayed(true, false, 0.1f);
        }
        // todo add a combo attack option.
        animator.SetTrigger(AttackSingle);
      }
    }

    public bool IsRunningAttack()
    {
      return animator.GetBool(AutoAttack) || IsAnimatingArmAttack() || IsAnimatingTailAttack();
    }

    // This will force stop an attack.
    public void StopAttack()
    {
      DisableAttackColliders();
      if (!animator.GetBool(AutoAttack)) return;
      animator.SetBool(AutoAttack, false);
      animator.SetTrigger(ManualAttackCompleteTrigger);
    }

    // Potential skip keys "Toe", "Leg", "Finger" "Thumb" "Spine"
    public static readonly List<string> _skippedTailAttackKeys = GetSkipKeys(XenoAnimationPoses.TailAttack_ChargeTail, new List<string> { "XenosBiped_Neck_TopSHJnt", "XenosBiped_Head_JawSHJnt" });
    public static readonly List<string> _skippedTailAttackKeysTailOnly = GetSkipKeys(XenoAnimationPoses.TailAttack_ChargeTail, new List<string> { "XenosBiped_Neck_TopSHJnt", "XenosBiped_Head_JawSHJnt", "Knee", "Toe", "Finger", "Arm", "Leg", "Head", "Spine" });

    public void TailAttackManualAnimation_Start(List<string> skipKeys = null)
    {
      SnapshotCurrentPose();
      var startPosition = new PoseTransition
      {
        PoseData = poseSnapshot,
        Speed = 0.25f
      };
      var returnPosition = new PoseTransition
      {
        PoseData = poseSnapshot,
        Speed = 0.55f
      };
      var pierceHitAndCurve = new PoseTransition
      {
        PoseData = XenoAnimationPoses.TailAttack_HitPierce_ToGround,
        Speed = 0.05f,
        Pause = 0.15f,
        OnStart = () =>
        {
          foreach (var attackTailCollider in attackTailColliders)
          {
            attackTailCollider.enabled = true;
          }
        },
        OnEnd = () =>
        {
          foreach (var attackTailCollider in attackTailColliders)
          {
            attackTailCollider.enabled = false;
          }
        },
        SpeedCurve = curveEaseOut
      };

      var transitions = new List<PoseTransition> {};

      transitions.Add(startPosition);
      transitions.AddRange(TailAttackTransitions);
      transitions.Add(pierceHitAndCurve);
      transitions.Add(returnPosition);


      _tailAttackTransitionRoutine.Start(LerpBetweenPoses(transitions, skipKeys ?? _skippedTailAttackKeys, () =>
      {
        animator.SetTrigger(ManualAttackCompleteTrigger);
      }));
    }

    public void TailAttackManualAnimation_Stop()
    {
      // do not run if there is no routine running.
      if (!_tailAttackTransitionRoutine.IsRunning) return;
      _tailAttackTransitionRoutine.Stop();
      foreach (var latePoseTarget in _latePoseTargets)
      {
        if (_skippedTailAttackKeys.Contains(latePoseTarget.Key))
        {
          _latePoseTargets.Remove(latePoseTarget.Key);
        }
      }
    }

    public void SetAttackMode(int mode)
    {
      _cachedAttackMode = mode;

      // do nothing if same value.
      if (animator.GetInteger(AttackMode) == _cachedAttackMode) return;
      animator.SetInteger(AttackMode, _cachedAttackMode);
      if (animator.GetInteger(AttackMode) == 1 && mode == 0)
      {
        TailAttackManualAnimation_Stop();
      }
    }

    [SerializeField] public float ChanceToTailAttack = 0.3f;
    [SerializeField] public float ChanceToArmAttack = 0.7f;

    public void TryRandomizeAttackMode()
    {
      if (attack_nextUpdateTime > Time.fixedTime) return;
      attack_nextUpdateTime = Time.fixedTime + nextUpdateInterval;

      var attackSpeed = Random.Range(0.8f, 1.2f);
      var randomValue = Random.Range(0f, ChanceToArmAttack + ChanceToTailAttack);

      if (randomValue > ChanceToArmAttack)
      {
        SetAttackMode(1);
      }
      else
      {
        SetAttackMode(0);
      }

      SetAttackSpeed(attackSpeed, true);
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
    public void EnableAttackColliders(bool armAttack, bool tailAttack)
    {
      ToggleColliderList(attackArmColliders, armAttack);
      ToggleColliderList(attackTailColliders, tailAttack);
    }

    public void EnableArmCollider()
    {
      _isArmColliderDelayRunning = false;
      ToggleColliderList(attackArmColliders, true);
    }

    private bool _isArmColliderDelayRunning = false;
    /// <summary>
    /// Used to delay enabling the attack collider so the collider is enabled near when it's ready to hit.
    /// </summary>
    public void EnableAttackCollidersDelayed(bool armAttack, bool tailAttack, float delay)
    {
      if (_isArmColliderDelayRunning) return;
      ToggleColliderList(attackTailColliders, false);

      _isArmColliderDelayRunning = true;
      Invoke(nameof(EnableArmCollider), delay);
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

    public static List<string> GetSkipKeys(Dictionary<string, JointPose> poseDictionary, List<string> skipTransformNames = null)
    {
      Regex skipRegexp = null;
      if (skipTransformNames is { Count: > 0 })
      {
        // Looser variant: match if *any* of the provided names appears in the key.
        // Escape each name to avoid regex metacharacters, then join with '|'.
        var pattern = string.Join("|", skipTransformNames.Select(Regex.Escape));
        skipRegexp = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
      }

      return poseDictionary.Keys
        .Where(key => skipRegexp == null || skipRegexp.IsMatch(key))
        .ToList();
    }

    public static List<string> GetCommonKeys(Dictionary<string, JointPose> a, Dictionary<string, JointPose> b, List<string> skipTransformNames = null)
    {
      Regex skipRegexp = null;
      if (skipTransformNames != null && skipTransformNames.Count > 0)
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

      return keys;
    }

    public IEnumerator LerpBetweenPoses(List<PoseTransition> poses,
      List<string> skipTransformNames = null, [CanBeNull] Action onComplete = null)
    {
      var len = poses.Count;
      for (var index = 0; index < len - 1; index++)
      {
        var currentPos = poses[index];
        var nextPose = poses[index + 1];

        var commonKeys = GetCommonKeys(currentPos.PoseData, nextPose.PoseData, skipTransformNames);

        nextPose.OnStart?.Invoke();

        // Pass duration (nextPose.Speed) and curve (nextPose.SpeedCurve) to LerpToPose:
        yield return LerpToPose(
          allAnimationJoints,
          currentPos.PoseData,
          nextPose.PoseData,
          skipTransformNames,
          commonKeys,
          nextPose.Speed,
          nextPose.SpeedCurve
        );

        if (nextPose.Pause > 0f)
        {
          yield return LerpToPose(
            allAnimationJoints,
            nextPose.PoseData,
            nextPose.PoseData,
            skipTransformNames,
            commonKeys,
            nextPose.Pause,
            nextPose.SpeedCurve
          );
        }

        nextPose.OnEnd?.Invoke();
      }

      yield return null;

      onComplete?.Invoke();
    }

    public static List<PoseTransition> HeadTurnPosLerp = new()
    {
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.Crouch,
        Speed = 0.6f
      },
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.CrouchHeadRight,
        Speed = 0.75f
      },
      new PoseTransition
      {
        PoseData = XenoAnimationPoses.Crouch,
        Speed = 0.6f
      }
    };

    [Header("Pose Lerp Curves (Defaults)")]
    [SerializeField] public static AnimationCurve curveEaseInOut = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] public static AnimationCurve curveEaseIn = new(new Keyframe(0, 0, 0, 1.5f), new Keyframe(1, 1));
    [SerializeField] public static AnimationCurve curveEaseOut = new(new Keyframe(0, 0), new Keyframe(1, 1, 1.5f, 0));
    [SerializeField] public static AnimationCurve curveBump = new(
      new Keyframe(0f, 0f, 0f, 3f),
      new Keyframe(0.5f, 1f, 0f, 0f),
      new Keyframe(1f, 0f, -3f, 0f)
    );

    private IEnumerator SimulateJumpWithPoseLerp(
      Dictionary<string, Transform> allJoints,
      Dictionary<string, JointPose> idlePose,
      Dictionary<string, JointPose> crouchPose,
      float crouchDuration,
      float airMinWaitTime,
      float standDuration,
      List<string> skipTransformNames = null,
      Action onComplete = null)
    {
      animator.SetBool(JumpTrigger, true);
      // 1. Disable animator (todo might not have to do this with animator order fixed)
      // DisableAnimator();

      // 2. Idle → Crouch
      yield return LerpToPose(allJoints, idlePose, crouchPose, skipTransformNames, null, crouchDuration);


      _headTurnRoutine.Start(LerpBetweenPoses(HeadTurnPosLerp, null));

      // 3. Stay crouched during "air time" (simulate jump apex)
      var timePassed = 0f;
      while (!OwnerAI.IsGrounded() || timePassed < airMinWaitTime)
      {
        timePassed += Time.deltaTime;
        yield return null;
      }

      _headTurnRoutine.Stop();

      // SnapshotCurrentPose();
      // yield return LerpToPose(allJoints, poseSnapshot, idlePose, skipTransformNames, null, 0.1f);
      //
      // // 4. Crouch → Idle
      // yield return LerpToPose(allJoints, crouchPose, idlePose, skipTransformNames, null, standDuration);

      // 5. Re-enable animator
      // EnableAnimator();
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
      List<string> skipTransformNames,
      List<string> commonKeys,
      float duration,
      [CanBeNull] AnimationCurve progressCurve = null)
    {
      commonKeys ??= GetCommonKeys(startPose, endPose, skipTransformNames);
      if (duration <= 0f) duration = 0.0001f;

      // Resolve once to avoid dictionary lookups per-iteration
      var jointList = new List<(Transform tr, JointPose a, JointPose b)>(commonKeys.Count);
      foreach (var key in commonKeys)
      {
        if (!allJoints.TryGetValue(key, out var tr) || !tr) continue;
        var a = startPose[key];
        var b = endPose[key];
        jointList.Add((tr, a, b));
      }

      var time = 0f;
      while (time < duration)
      {
        var t = time / duration;
        var tt = progressCurve != null ? Mathf.Clamp01(progressCurve.Evaluate(t)) : t;

        // Fill LateUpdate buffer (no direct transform writes here)
        foreach (var (tr, a, b) in jointList)
        {
          var pos = Vector3.LerpUnclamped(a.Position, b.Position, tt);
          var rot = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, tt);
          _latePoseTargets[tr.name] = new JointPose(pos, rot, tr);
        }
        _latePoseDirty = true;

        time += Time.deltaTime;
        yield return null; // compute during frame; apply in LateUpdate this frame
      }

      // Final snap (enqueue end pose one last time, LateUpdate will apply)
      foreach (var (tr, _, b) in jointList)
      {
        _latePoseTargets[tr.name] = b;
      }
      _latePoseDirty = true;

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

    [SerializeField] private float pitchDownMaxDeg = 30f; // your +40 at feet
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

      var nextNeckUpDown = neckUpDownAngle;


      nextNeckUpDown.y = MoveTowardsSigned(neckUpDownAngle.y, yawDeg, yawSpeedDegPerSec * Time.deltaTime);

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

      nextNeckUpDown.z = MoveTowardsSigned(neckUpDownAngle.z, desiredZ, pitchSpeedDegPerSec * Time.deltaTime);

      neckUpDownAngle = nextNeckUpDown;
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

  #region Tail Attack ;

    [Tooltip("Animator state name that plays the tail attack (optional if you prefer Attack/AttackMode gate).")]
    [SerializeField] private int armAttackLayerIndex = 1;
    [SerializeField] private string[] armAttackStates = { "attack_arms" }; // add "Base Layer.attack_arms" if you want fullPathHash too

    [SerializeField] private int tailAttackLayerIndex = 1;
    [SerializeField] private string[] tailAttackStates = { "attack_tail", "attack_tail_solo" };

    private readonly Dictionary<string, JointPose> _latePoseTargets = new();
    private bool _latePoseDirty;

// Replace existing checks with:
    public bool IsAnimatingArmAttack()
    {
      return AnimatorStateIdUtil.IsPlayingAny(animator, 1, _armIds);
    }

    public bool IsAnimatingTailAttack()
    {
      return AnimatorStateIdUtil.IsPlayingAny(animator, 1, _tailIds);
    }

  #endregion

  }
}