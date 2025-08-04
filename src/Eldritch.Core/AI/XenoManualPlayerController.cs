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
        public float turnSpeed = 1f;

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
            // Movement input
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(h, 0f, v);

            // Calculate normalized move speed for animator
            float speed = Mathf.Clamp01(move.magnitude);

            // Update animator "moveSpeed" parameter
            animationController.SetMoveSpeed(speed);

            // Jump input
            if (Input.GetKeyDown(KeyCode.Space) && ai != null && ai.IsGrounded())
            {
                jumpRequested = true;
                // Optional: trigger jump animation if you have a trigger for it
                // animationController.PlayJump(); // Implement PlayJump if you want!
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
                // If you have a jump animation trigger, call it here:
                // animationController.PlayJump();
            }
        }
    }
}
