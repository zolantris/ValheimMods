// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using UnityEngine;

namespace Eldritch.Core
{
    [RequireComponent(typeof(XenoAIMovementController))]
    [RequireComponent(typeof(XenoAIAnimationController))]
    [RequireComponent(typeof(Rigidbody))]
    public class XenoManualPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float jumpForce = 7f;
        public float turnSpeed = 10f;

        private XenoAIMovementController movement;
        private XenoAIAnimationController animationController;
        private XenoDroneAI ai;
        private Rigidbody rb;
        private bool jumpRequested;

        private void Awake()
        {
            movement = GetComponent<XenoAIMovementController>();
            animationController = GetComponent<XenoAIAnimationController>();
            ai = GetComponent<XenoDroneAI>();
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            ai.SetManualControls(true);
        }

        private void Update()
        {
            if (!ai.IsManualControlling) return;

            // Button action mapping for Alpha1-Alpha0 (keys 1-0)
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnInputAction(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnInputAction(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnInputAction(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) OnInputAction(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) OnInputAction(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) OnInputAction(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) OnInputAction(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) OnInputAction(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) OnInputAction(9);
            if (Input.GetKeyDown(KeyCode.Alpha0)) OnInputAction(10);

            // Movement input
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(h, 0f, v);

            float speed = Mathf.Clamp01(move.magnitude);
            animationController.SetMoveSpeed(speed);

            // Jump input
            if (Input.GetKeyDown(KeyCode.Space) && ai != null && ai.IsGrounded())
            {
                jumpRequested = true;
            }
        }

        private void FixedUpdate()
        {
            if (!ai.IsManualControlling) return;
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 move = new Vector3(h, 0f, v);
            if (move.magnitude > 1f) move.Normalize();

            Vector3 moveWorld = transform.TransformDirection(move) * moveSpeed;
            Vector3 velocity = rb.velocity;
            velocity.x = moveWorld.x;
            velocity.z = moveWorld.z;
            rb.velocity = velocity;

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveWorld, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * turnSpeed);
            }

            if (jumpRequested)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                jumpRequested = false;
                animationController.PlayJump();
            }
        }

        /// <summary>
        /// Called when 1–10 keys are pressed.
        /// </summary>
        /// <param name="actionIndex">1–10 (1 = Alpha1, 10 = Alpha0)</param>
        private void OnInputAction(int actionIndex)
        {
            switch (actionIndex)
            {
                case 1:
                    TriggerBehavior1();
                    break;
                case 2:
                    TriggerBehavior2();
                    break;
                case 3:
                    TriggerBehavior3();
                    break;
                case 4:
                    TriggerBehavior4();
                    break;
                case 5:
                    TriggerBehavior5();
                    break;
                case 6:
                    TriggerBehavior6();
                    break;
                case 7:
                    TriggerBehavior7();
                    break;
                case 8:
                    TriggerBehavior8();
                    break;
                case 9:
                    TriggerBehavior9();
                    break;
                case 10:
                    TriggerBehavior10();
                    break;
            }
        }

        private void TriggerBehavior1()
        {
            StartCoroutine(animationController.SimulateJumpWithPoseLerp(
                animationController.allAnimationJoints,
                XenoAnimationPoses.Idle, // Idle or standing pose
                XenoAnimationPoses.Crouch,
                0.18f,   // crouch time (down)
                0.25f,   // air time (wait)
                0.24f)); // stand up time
            
            Debug.Log("Behavior 1 triggered!");
        }
        private void TriggerBehavior2()  { Debug.Log("Behavior 2 triggered!"); }
        private void TriggerBehavior3()  { Debug.Log("Behavior 3 triggered!"); }
        private void TriggerBehavior4()  { Debug.Log("Behavior 4 triggered!"); }
        private void TriggerBehavior5()  { Debug.Log("Behavior 5 triggered!"); }
        private void TriggerBehavior6()  { Debug.Log("Behavior 6 triggered!"); }
        private void TriggerBehavior7()  { Debug.Log("Behavior 7 triggered!"); }
        private void TriggerBehavior8()  { Debug.Log("Behavior 8 triggered!"); }
        private void TriggerBehavior9()  { Debug.Log("Behavior 9 triggered!"); }
        private void TriggerBehavior10() { Debug.Log("Behavior 10 triggered!"); }
    }
}
