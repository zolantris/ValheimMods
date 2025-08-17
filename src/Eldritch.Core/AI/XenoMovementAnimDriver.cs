// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;

namespace Eldritch.Core
{
  /// Drives XenoAnimationController.moveSpeed from actual displacement.
  public class XenoMovementAnimDriver : MonoBehaviour
  {
    // static, private, public (tuning)
    private MotionSampleState _motion;
    private XenoAnimationController _anim;
    private Transform _resolvedMotionRoot;

    [Header("Bindings")]
    public Transform motionRoot; // leave null to auto-bind parent Rigidbody
    public bool autoBindRigidbodyRoot = true;

    [Header("Tuning")]
    public float maxForwardSpeed = 6f;
    public float deadzoneSpeed = 0.05f;
    public float smoothingTau = 0.08f;
    public float teleportDistance = 2f;
    public float turnInPlaceDegPerSec = 20f;

    private void Awake()
    {
      _anim = GetComponentInChildren<XenoAnimationController>();

      if (motionRoot)
      {
        _resolvedMotionRoot = motionRoot;
      }
      else if (autoBindRigidbodyRoot)
      {
        var rb = GetComponentInParent<Rigidbody>();
        _resolvedMotionRoot = rb ? rb.transform : transform;
      }
      else
      {
        _resolvedMotionRoot = transform;
      }
    }

    private void OnEnable()
    {
      _motion = default;
    }

    private void Update()
    {
      if (!_anim || !_resolvedMotionRoot) return;

      var dt = Time.deltaTime;
      var speedNorm = MotionSpeedSampler.SampleNormalizedMoveSpeed(
        _resolvedMotionRoot,
        ref _motion,
        dt,
        out var turningInPlace,
        out var yawDps,
        maxForwardSpeed,
        deadzoneSpeed,
        smoothingTau,
        teleportDistance
      );

      // Optional: suppress gait while pivoting in place
      if (Mathf.Abs(speedNorm) < 0.02f && Mathf.Abs(yawDps) > turnInPlaceDegPerSec)
        speedNorm = 0f;

      _anim.SetMoveSpeed(speedNorm);
    }

    public void SetMotionRoot(Transform root)
    {
      motionRoot = root;
      _resolvedMotionRoot = root ? root : transform;
      _motion = default;
    }
  }
}