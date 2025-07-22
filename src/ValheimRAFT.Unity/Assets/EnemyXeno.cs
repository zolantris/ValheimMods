#region
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class XenoDroneAI : MonoBehaviour
    {
        public float moveSpeed = 3f;

        public Transform spine01;
        public Transform spine02;
        public Transform spine03;

        public Transform leftHip;
        public Transform rightHip;

        public Transform leftArm;
        public Transform rightArm;

        [Header("Assign the tail root joint (e.g. XenosBiped_TailRoot_SHJnt)")]
        public Transform tailRoot;

        [SerializeField] public float tailMax;
        [SerializeField] public float tailMin = 5f;
        [SerializeField] public bool isHiding = true;

        [SerializeField] public bool canMove;

        [SerializeField] float GravityMultiplier = 5f;

        private readonly HashSet<Collider> allColliders = new();
        private Animator _animator;
        private Rigidbody _rb;

        public HashSet<(HashSet<Transform>,Transform)> allLists = new HashSet<(HashSet<Transform>,Transform)>();
        public HashSet<Transform> leftArmJoints = new HashSet<Transform>();
        public HashSet<Transform> leftLeftJoints = new HashSet<Transform>();
        public HashSet<Transform> rightArmJoints = new HashSet<Transform>();
        public HashSet<Transform> rightLegJoints = new HashSet<Transform>();

        [Tooltip("List of all tail joints, from root to tip.")]
        public HashSet<Transform> tailJoints = new HashSet<Transform>();

        private float velocity;

        private Transform xenoRoot;

        void Awake()
        { 
            // find these easily with Gameobject -> Copy FullTransformPath from root.
            // You may need to adjust these paths to match your actual bone names/hierarchy
            xenoRoot = transform.Find("alien_xenos_drone_SK_Xenos_Drone_skeleton/XenosBiped_TrajectorySHJnt/XenosBiped_ROOTSHJnt");
            spine01 = xenoRoot.Find("XenosBiped_Spine_01SHJnt");
            spine02 = spine01.Find("XenosBiped_Spine_02SHJnt");
            spine03 = spine02.Find("XenosBiped_Spine_03SHJnt");
            
            leftHip = xenoRoot.Find("XenosBiped_l_Leg_HipSHJnt");
            rightHip = xenoRoot.Find("XenosBiped_r_Leg_HipSHJnt");
            
            leftArm = spine03.Find("XenosBiped_l_Arm_ClavicleSHJnt/XenosBiped_l_Arm_ShoulderSHJnt");
            rightArm = spine03.Find("XenosBiped_r_Arm_ClavicleSHJnt/XenosBiped_r_Arm_ShoulderSHJnt");
            
            tailRoot = xenoRoot.Find("XenosBiped_TailRoot_SHJnt");
            _animator = GetComponent<Animator>();
            _rb = GetComponent<Rigidbody>();


            allLists = new HashSet<(HashSet<Transform>, Transform)> { (rightArmJoints, rightArm), (leftArmJoints, leftArm), (leftLeftJoints, leftHip), (rightLegJoints, rightHip), (tailJoints, tailRoot) };

            CollectAllBodyJoints();
            AddCapsuleCollidersToAllJoints();
            IgnoreAllColliders();
        }

        void Update()
        {
            if (canMove)
            {
                HandleMovemovement();
            }
        }

        public void FixedUpdate()
        {
            _rb.AddForceAtPosition(-Physics.gravity * GravityMultiplier, xenoRoot.transform.position);
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

        public void IgnoreAllColliders()
        {
            foreach (var allCollider1 in allColliders)
            foreach (var allCollider2 in allColliders)
            {
                Physics.IgnoreCollision(allCollider1, allCollider2, true);;
            }
        }

        public void HandleMovemovement()
        {
            // Get the current state
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

            Debug.Log(state.shortNameHash);
            // Check if we're in the 'walk' state (the state name in Animator)
            if (state.IsName("walk"))
            {
                velocity = Mathf.Min(Time.fixedDeltaTime + velocity, 50);
                // Only move when in 'walk'
                // transform.Translate(Vector3.forward * moveSpeed * velocity * Time.deltaTime);
                var random = new Vector3(Random.Range(-1f, 1f), 0, 0);
                _rb.AddForce((transform.forward + random) * moveSpeed * velocity * Time.deltaTime, ForceMode.Acceleration);
            }
            else if (state.IsName("run"))
            {
                velocity = Mathf.Min(Time.fixedDeltaTime + velocity, 1005);
                // Only move when in 'walk'
                // transform.Translate(Vector3.forward * moveSpeed * velocity * Time.deltaTime);
                var random = new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);
                _rb.AddForce((transform.forward + random) * moveSpeed * velocity * Time.deltaTime, ForceMode.Acceleration);
            }
            else
            {
                velocity = 0f;
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

        public void SetWalking(bool walking)
        {
            _animator.SetBool("isWalking", walking);
        }

        public void SetRunning(bool running)
        {
            _animator.SetBool("isRunning", running);
        }

        public void Attack()
        {
            _animator.SetTrigger("Attack"); // You'll want to add a "Attack" Trigger parameter and set up transition
        }

        public void Die()
        {
            _animator.SetBool("isDead", true);
        }
    }
}
