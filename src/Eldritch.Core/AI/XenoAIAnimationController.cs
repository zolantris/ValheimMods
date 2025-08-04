using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;

namespace Eldritch.Core
{
    public class XenoAIAnimationController : MonoBehaviour
    {
        [Header("Animator/Bones")]
        [SerializeField] private Animator _animator;
        [SerializeField] private XenoDroneAI _ai;
        [SerializeField] private ParticleSystem _bloodEffects;
        [SerializeField] private SkinnedMeshRenderer _skinRenderer;
        [SerializeField] private Material _transparentMaterial;
        [SerializeField] private Material _bodyMaterial, _headMaterial;
        [SerializeField] private Transform _neckPivot;

        [Header("Attack Colliders")]
        [SerializeField] private Collider[] attackTailColliders;
        [SerializeField] private Collider[] attackArmColliders;
        
        [SerializeField] private string _handAttackObjName = "xeno_arm_attack_collider";
        [SerializeField] private string _tailAttackObjName = "xeno_tail_attack_collider";

        // Animation hashes
        public static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
        public static readonly int MoveAttack = Animator.StringToHash("attack"); // boolean
        public static readonly int AttackMode = Animator.StringToHash("attackMode"); // int
        public static readonly int Die = Animator.StringToHash("die"); // trigger
        public static readonly int AwakeTrigger = Animator.StringToHash("awake"); // trigger
        public static readonly int Sleep = Animator.StringToHash("sleep"); // trigger
        public static readonly int Move = Animator.StringToHash("move"); // trigger
        public static readonly int Idle = Animator.StringToHash("idle"); // trigger
        public static readonly int JumpTrigger = Animator.StringToHash("jump"); // trigger


        // State
        private bool _lastCamouflageState = false;
        private bool _canPlayEffectOnFrame = true;
        private Coroutine _sleepRoutine;
        private Coroutine _waitAnimRoutine;
        
        [Header("Animation Transforms")]
        public Transform xenoAnimatorRoot;
        public Transform xenoRoot, spine01, spine02, spine03, spineTop, neckUpDown, neckPivot;
        public Transform leftHip, rightHip, leftArm, rightArm, tailRoot, leftToeTransform, rightToeTransform;
        public HashSet<Transform> leftArmJoints = new();
        public HashSet<Transform> rightArmJoints = new();
        public HashSet<Transform> tailJoints = new();
        public HashSet<Collider> allColliders = new();
        public HashSet<Collider> attackColliders = new();

        [Header("Animation Updaters")]
        private int _cachedAttackMode = 0;
        public float UpdatePauseTime = 2f;
        public float attack_nextUpdateTime;
        public float nextUpdateInterval = 0.25f;
        
        public float moveSpeed_nextUpdateTime;
        // public float nextUpdateInterval = 0.25f;
        public float SleepMoveUpdateTilt;
        public float LastSleepMoveUpdate;
        
        public string armAttackObjName = "xeno_arm_attack_collider";
        public string tailAttackObjName = "xeno_tail_attack_collider";
        private CoroutineHandle _sleepAnimationRoute;
        private void Awake()
        {
            InitCoroutineHandlers();
            if (!_animator) _animator = GetComponentInChildren<Animator>();
            if (!_skinRenderer) _skinRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_bloodEffects == null) _bloodEffects = GetComponentInChildren<ParticleSystem>();
            

            BindUnOptimizedRoots();

            AddCapsuleCollidersToAllJoints();
            
            var preGeneratedColliders = GetComponentsInChildren<Collider>();
            
            AssignAttackColliders(preGeneratedColliders);

            CollectAllBodyJoints();
            RecursiveCollectAllJoints(xenoRoot);
        }

        private void OnEnable()
        {
            InitCoroutineHandlers();
        }

        private void InitCoroutineHandlers()
        {
            _sleepAnimationRoute ??= new CoroutineHandle(this);
        }

        #region Colliders

        public void AssignAttackColliders(IEnumerable<Collider> colliders)
        {
            foreach (var col in colliders)
            {
                if (!col)continue;
                var colName = col.name;
                if (colName == armAttackObjName)
                {
                    attackArmColliders.AddItem(col);
                }
                if (colName == tailAttackObjName)
                {
                    attackTailColliders.AddItem(col);
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
                col = t.gameObject.AddComponent<CapsuleCollider>();
            allColliders.Add(col);
        }

        #endregion   
       
        
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
            leftToeTransform = leftHip.Find("XenosBiped_l_Leg_Knee1SHJnt/XenosBiped_l_Leg_Knee2_CurveSHJnt/XenosBiped_l_Leg_AnkleSHJnt/XenosBiped_l_Toe02_Base_SHJnt"); // Update to match your rig
            rightToeTransform = rightHip.Find("XenosBiped_r_Leg_Knee1SHJnt/XenosBiped_r_Leg_Knee2_CurveSHJnt/XenosBiped_r_Leg_AnkleSHJnt/XenosBiped_r_Toe02_Base_SHJnt");
        }
        
        private readonly Dictionary<string, Transform> allAnimationJoints = new();

        public void DisableAnimator()
        {
            if (!_animator) return;
            _animator.enabled = false;
        }
        
        public void EnableAnimator()
        {
            if (!_animator) return;
            _animator.enabled = true;
        }

        private void RecursiveCollectAllJoints(Transform joint, bool skip = false)
        {
            if (!skip)
                allAnimationJoints[joint.name] = joint;
            foreach (Transform child in joint)
                RecursiveCollectAllJoints(child);
        }
        
        private Dictionary<string, (Vector3 position, Quaternion rotation)> poseSnapshot;

        public void SnapshotCurrentPose()
        {
            poseSnapshot = new Dictionary<string, (Vector3, Quaternion)>();
            foreach (var kvp in allAnimationJoints)
                poseSnapshot[kvp.Key] = (kvp.Value.localPosition, kvp.Value.localRotation);
        }
        public void RestorePose()
        {
            if (poseSnapshot == null) return;
            foreach (var kvp in poseSnapshot)
            {
                if (allAnimationJoints.TryGetValue(kvp.Key, out var t) && t != null)
                {
                    t.localPosition = kvp.Value.position;
                    t.localRotation = kvp.Value.rotation;
                }
            }
        }

        // --- ANIMATION API ---
        public void SetMoveSpeed(float normalized)
        {
            if (!_animator) return;
            if (moveSpeed_nextUpdateTime > Time.fixedTime) return;
            moveSpeed_nextUpdateTime = Time.fixedTime + nextUpdateInterval;
            
            _animator.SetFloat(MoveSpeed, normalized);
        }
        
        public void PlayDead()
        {
            if (!_animator) return;
            _animator.SetBool(Move, false);
            _animator.SetTrigger(Die);
        }

        public void PlayJump()
        {
            if (!_animator) return;
            _animator.SetTrigger(JumpTrigger);
        }
        
        public void PlayAwake() { if (_animator) _animator.SetTrigger(AwakeTrigger); }
        public void PlaySleep()
        {
            if (!_animator) return;
            _animator.SetTrigger(Sleep);
            _animator.SetBool(Move, false);
        }

        public void PlayAttack()
        {
            PlayAttack(_cachedAttackMode);
        }
        public void PlayAttack(int attackMode)
        {
            if (!_animator) return; 
            EnableAttackColliders(attackMode);
            UpdateAttackMode();
            _animator.SetBool(MoveAttack, true);
        }
        public void StopAttack()
        {
            if (!_animator) return;
            DisableAttackColliders();
            _animator.SetBool(MoveAttack, false);
        }
        public void UpdateAttackMode()
        {
            if (!_animator) return;
            if (attack_nextUpdateTime > Time.fixedTime) return;

            attack_nextUpdateTime = Time.fixedTime + nextUpdateInterval;
            _cachedAttackMode = Mathf.RoundToInt(Random.Range(0, 1));
            _animator.SetInteger(AttackMode, _cachedAttackMode);
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

        public static void ToggleColliderList( IEnumerable<Collider>colliders, bool isEnabled)
        {
            foreach (var col in colliders)
            {
                if (!col) continue;
                col.enabled = isEnabled;
            }
        }

        // --- COLLIDER HELPERS ---
        
        /// <summary>
        /// Enables and disables attack colliders based on attack type
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

        // --- CAMOUFLAGE ---
        public void ActivateCamouflage()
        {
            if (_skinRenderer == null) return;
            _lastCamouflageState = true;
            if (_transparentMaterial)
            {
                var mats = new Material[_skinRenderer.materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _transparentMaterial;
                _skinRenderer.materials = mats;
            }
        }
        public void DeactivateCamouflage()
        {
            if (!_lastCamouflageState || _skinRenderer == null) return;
            if (_bodyMaterial && _headMaterial && _skinRenderer.materials.Length == 2)
            {
                Material[] mats = { _bodyMaterial, _headMaterial };
                _skinRenderer.materials = mats;
            }
            _lastCamouflageState = false;
        }

        // --- Sleeping Animation "Twitch" ---
        public void PlaySleepingCustomAnimations()
        {
            if (LastSleepMoveUpdate < Time.time)
            {
                var wasPositive = SleepMoveUpdateTilt > 0;
                var baseChange = wasPositive ? -2f : 2f;
                SleepMoveUpdateTilt = UnityEngine.Random.Range(-10f, 10f) + baseChange;
                LastSleepMoveUpdate = Time.time + 10f;
            }
            var currentEuler = _neckPivot.localEulerAngles;
            if (currentEuler.y > 180f) currentEuler.y -= 360f;
            if (currentEuler.z > 180f) currentEuler.z -= 360f;
            currentEuler.y = Mathf.MoveTowards(currentEuler.y, SleepMoveUpdateTilt, Time.deltaTime * 30f);
            currentEuler.z = Mathf.MoveTowards(currentEuler.z, 90f, Time.deltaTime * 60f);
            _neckPivot.localEulerAngles = currentEuler;
        }
        
        public void ScheduleSleepingAnimation(bool canSleepAnimate)
        {
            if (!canSleepAnimate) return;
            if (_sleepAnimationRoute.IsRunning) return;
            _sleepAnimationRoute.Start(SleepingAnimationCoroutine());
        }

        public IEnumerator SleepingAnimationCoroutine()
        {
            while (_ai.IsSleeping())
            {
                PlaySleepingCustomAnimations();
                yield return new WaitForFixedUpdate();
            }
        }
        
        public Transform GetFurthestToe()
        {
            if (leftToeTransform == null && rightToeTransform == null) return null;
            if (leftToeTransform != null && rightToeTransform == null) return leftToeTransform;
            if (rightToeTransform != null && leftToeTransform == null) return rightToeTransform;

            Vector3 forward = transform.forward.normalized;
            float leftProj = Vector3.Dot(leftToeTransform.position - transform.position, forward);
            float rightProj = Vector3.Dot(rightToeTransform.position - transform.position, forward);

            return leftProj > rightProj ? leftToeTransform : rightToeTransform;
        }

        public void PointHeadTowardTarget(Transform owner, Transform target)
        {
            if (!target || neckPivot == null) return;
            var toTarget = target.position - neckPivot.position;
            toTarget.y = 0;
            var localDir = owner.InverseTransformDirection(toTarget.normalized);

            var yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -40f, 40f);

            var roll = Mathf.Lerp(40f, 90f, Mathf.Abs(yaw / 40f));
            roll = Mathf.Clamp(roll, 40f, 90f);

            var currentEuler = neckPivot.localEulerAngles;
            if (currentEuler.y > 180f) currentEuler.y -= 360f;
            if (currentEuler.z > 180f) currentEuler.z -= 360f;

            currentEuler.y = Mathf.MoveTowards(currentEuler.y, yaw, Time.deltaTime * 120f);
            currentEuler.z = Mathf.MoveTowards(currentEuler.z, roll, Time.deltaTime * 60f);

            neckPivot.localEulerAngles = currentEuler;
        }
    }
}
