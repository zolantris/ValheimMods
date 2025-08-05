// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core
{
  public class XenoManualPlayerController : MonoBehaviour
  {
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float turnSpeed = 10f;
    private XenoDroneAI aiController;
    private XenoAnimationController animationController;
    private bool jumpRequested;

    private XenoAIMovementController movement;
    private Rigidbody rb;

    public bool IsDodging => aiController.Movement.IsDodging;

    private void Awake()
    {
      movement = GetComponent<XenoAIMovementController>();
      animationController = GetComponentInChildren<XenoAnimationController>();
      aiController = GetComponentInParent<XenoDroneAI>();
      rb = GetComponentInParent<Rigidbody>();
    }

    private void Start()
    {
      aiController.SetManualControls(true);
    }

    private void Update()
    {
      if (!aiController.IsManualControlling) return;

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
      var h = Input.GetAxis("Horizontal");
      var v = Input.GetAxis("Vertical");
      var move = new Vector3(h, 0f, v);

      if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
      {
        // Only dodge if not currently dodging, etc.
        FireDodge(move);
        return;
      }

      var moveMag = move.magnitude;
      var speed = move.magnitude <= 0 ? 0 : Mathf.Clamp01(Mathf.Lerp(animationController.GetAnimationMoveSpeed(), move.magnitude, Time.deltaTime));

      LoggerProvider.LogDebugDebounced($"speed {speed}");
      animationController.SetMoveSpeed(speed);

      // Jump input
      if (Input.GetKeyDown(KeyCode.Space) && aiController != null && aiController.IsGrounded())
      {
        animationController.PlayJump();
        jumpRequested = true;
      }
    }

    private void FixedUpdate()
    {
      if (!aiController.IsManualControlling) return;
      var h = Input.GetAxis("Horizontal");
      var v = Input.GetAxis("Vertical");

      var move = new Vector3(h, 0f, v);
      if (move.magnitude > 1f) move.Normalize();

      var moveWorld = transform.TransformDirection(move) * moveSpeed;
      var velocity = rb.velocity;
      velocity.x = moveWorld.x;
      velocity.z = moveWorld.z;

      if (!IsDodging)
      {
        rb.velocity = velocity;
      }

      if (move.sqrMagnitude > 0.01f)
      {
        var targetRot = Quaternion.LookRotation(moveWorld, Vector3.up);
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

    private void FireDodge(Vector3 move)
    {
      animationController.SetAttackMode(1);
      animationController.PlayAttack(1);
      // Use input direction if pressed, otherwise lunge forward
      var dodgeDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;

      if (aiController?.Movement?.dodgeAbility != null)
        aiController.Movement.dodgeAbility.TryDodge(dodgeDir, () => {});

      Debug.Log($"Dodge triggered! Direction: {dodgeDir}");
    }

    /// <summary>
    ///   Called when 1–10 keys are pressed.
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
      animationController.PlayJump();
      Debug.Log("Behavior 1 jump triggered!");
    }
    private void TriggerBehavior2()
    {
      if (aiController.IsSleeping())
      {
        aiController.StopSleeping();
      }
      else
      {
        aiController.StartSleeping();
        animationController.PlaySleepingAnimation(true);
      }
      Debug.Log("Behavior 2 sleep triggered!");
    }
    private void TriggerBehavior3()
    {
      Debug.Log("Behavior 3 triggered!");
    }
    private void TriggerBehavior4() { Debug.Log("Behavior 4 triggered!"); }
    private void TriggerBehavior5() { Debug.Log("Behavior 5 triggered!"); }
    private void TriggerBehavior6() { Debug.Log("Behavior 6 triggered!"); }
    private void TriggerBehavior7() { Debug.Log("Behavior 7 triggered!"); }
    private void TriggerBehavior8() { Debug.Log("Behavior 8 triggered!"); }
    private void TriggerBehavior9() { Debug.Log("Behavior 9 triggered!"); }
    private void TriggerBehavior10() { Debug.Log("Behavior 10 triggered!"); }
  }
}