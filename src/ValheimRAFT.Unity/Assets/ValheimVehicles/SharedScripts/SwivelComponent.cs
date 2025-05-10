// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.SharedScripts
{
  [Flags]
  public enum HingeAxis
  {
    None = 0,
    X = 1,
    Y = 2,
    Z = 4
  }

  public class SwivelComponent : MonoBehaviour, ISwivelConfig
  {
    public enum HingeDirection
    {
      Forward,
      Backward
    }

    public const string SNAPPOINT_TAG = "snappoint";
    public const string AnimatedContainerName = "animated";

    private const float positionThreshold = 0.01f;
    private const float angleThreshold = 0.1f;

    public static Vector3 cachedWindDirection = Vector3.zero;

    public static float RotationInterpolateSpeedMultiplier = 0.5f;
    public static float MovementInterpolationSpeedMultiplier = 0.05f;

    [Header("Swivel General Settings")]
    [SerializeField] public SwivelMode mode = SwivelMode.Rotate;
    [SerializeField] public float interpolationSpeed = 2f;
    [SerializeField] public Transform animatedTransform;
    [SerializeField] public MotionState currentMotionState = MotionState.Idle;

    [Header("Enemy Tracking Settings")]
    [SerializeField] public float minTrackingRange = 5f;
    [SerializeField] public float maxTrackingRange = 50f;
    [SerializeField] internal GameObject nearestTarget;

    [Header("Rotation Mode Settings")]
    [SerializeField] public HingeAxis hingeAxes = HingeAxis.Y;
    [SerializeField] public HingeDirection xHingeDirection = HingeDirection.Forward;
    [SerializeField] public HingeDirection yHingeDirection = HingeDirection.Forward;
    [SerializeField] public HingeDirection zHingeDirection = HingeDirection.Forward;
    [SerializeField] public Vector3 maxRotationEuler = new(45f, 90f, 45f);
    [SerializeField] public UnityEvent onRotationReachedTarget;
    [SerializeField] public UnityEvent onRotationReturned;

    [Header("Movement Mode Settings")]
    [SerializeField] public Vector3 movementOffset = new(0f, 0f, 0f);
    [SerializeField] public bool useWorldPosition;
    [SerializeField] public UnityEvent onMovementReachedTarget;
    [SerializeField] public UnityEvent onMovementReturned;

    [Description("Piece container containing all children to be rotated or moved.")]
    public Transform piecesContainer;

    [Description("Shown until an object is connected to the swivel.")]
    public Transform connectorContainer;

    public Transform directionDebuggerArrow;

    public bool CanUpdate = true;
    private Rigidbody animatedRigidbody;

    private bool hasReachedTarget;
    private bool hasReturned;
    private bool hasRotatedReturn;
    private bool hasRotatedTarget;

    private Vector3 hingeEndEuler;
    private float hingeLerpProgress;
    private Transform snappoint;

    private Vector3 startLocalPosition;
    private Quaternion startRotation;
    private Vector3 targetMovementPosition;
    private Quaternion targetRotation;

    public MotionState CurrentMotionState => currentMotionState;
    public HingeAxis HingeAxes => hingeAxes;
    public Vector3 MaxEuler => maxRotationEuler;

    public float InterpolationSpeed => interpolationSpeed;

    public virtual void Awake()
    {
      snappoint = transform.Find(SNAPPOINT_TAG);

      animatedTransform = transform.Find(AnimatedContainerName);
      if (!animatedTransform)
        throw new MissingComponentException("Missing animated container");

      piecesContainer = animatedTransform.Find("piece_container");
      directionDebuggerArrow = piecesContainer?.Find("direction_debugger_arrow");
      connectorContainer = transform.Find("connector_container");

      startRotation = animatedTransform.localRotation;
      startLocalPosition = animatedTransform.localPosition;

      animatedRigidbody = animatedTransform.GetComponent<Rigidbody>();
      if (!animatedRigidbody)
        animatedRigidbody = animatedTransform.gameObject.AddComponent<Rigidbody>();

      animatedRigidbody.isKinematic = true;
      animatedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public virtual void Start()
    {
      SyncSnappoint();
    }

    public virtual void FixedUpdate()
    {
      if (!piecesContainer || !animatedRigidbody || !animatedTransform.parent) return;
      if (!CanUpdate) return;

      var didMove = false;

      switch (mode)
      {
        case SwivelMode.Rotate:
          targetRotation = CalculateRotationTarget();
          var current = animatedTransform.localRotation;
          var interpolated = Quaternion.Slerp(current, targetRotation, interpolationSpeed * RotationInterpolateSpeedMultiplier * Time.fixedDeltaTime);
          animatedRigidbody.Move(transform.position, transform.rotation * interpolated);
          didMove = true;

          var angleToTarget = Quaternion.Angle(current, targetRotation);

          if (currentMotionState == MotionState.GoingToTarget && !hasRotatedTarget && angleToTarget < angleThreshold)
          {
            hasRotatedTarget = true;
            onRotationReachedTarget?.Invoke();
          }
          else if (currentMotionState == MotionState.Returning && !hasRotatedReturn && angleToTarget < angleThreshold)
          {
            hasRotatedReturn = true;
            onRotationReturned?.Invoke();
          }

          if (currentMotionState == MotionState.Returning && hasRotatedTarget) hasRotatedReturn = false;
          if (currentMotionState == MotionState.GoingToTarget && hasRotatedReturn) hasRotatedTarget = false;
          break;

        case SwivelMode.Move:
          targetMovementPosition = currentMotionState == MotionState.Returning
            ? startLocalPosition
            : startLocalPosition + movementOffset;

          var currentLocal = animatedTransform.localPosition;
          var nextLocal = Vector3.Lerp(currentLocal, targetMovementPosition, interpolationSpeed * MovementInterpolationSpeedMultiplier * Time.fixedDeltaTime);
          var worldTarget = transform.TransformPoint(nextLocal);
          var moveWorldRot = transform.rotation;

          animatedRigidbody.Move(worldTarget, moveWorldRot);
          didMove = true;

          var distanceToTarget = Vector3.Distance(currentLocal, targetMovementPosition);
          if (currentMotionState == MotionState.GoingToTarget && !hasReachedTarget && distanceToTarget < positionThreshold)
          {
            hasReachedTarget = true;
            onMovementReachedTarget?.Invoke();
          }
          else if (currentMotionState == MotionState.Returning && !hasReturned && distanceToTarget < positionThreshold)
          {
            hasReturned = true;
            onMovementReturned?.Invoke();
          }

          if (currentMotionState == MotionState.Returning && hasReachedTarget) hasReturned = false;
          if (currentMotionState == MotionState.GoingToTarget && hasReturned) hasReachedTarget = false;
          break;

        case SwivelMode.TargetEnemy:
          targetRotation = CalculateTargetNearestEnemyRotation();
          animatedRigidbody.Move(transform.position, transform.rotation * targetRotation);
          didMove = true;
          break;

        case SwivelMode.TargetWind:
          targetRotation = CalculateTargetWindDirectionRotation();
          animatedRigidbody.Move(transform.position, transform.rotation * targetRotation);
          didMove = true;
          break;
      }

      if (!didMove)
      {
        var syncPos = animatedTransform.position;
        var syncRot = transform.rotation;
        if ((animatedRigidbody.position - syncPos).sqrMagnitude > 0.0001f ||
            Quaternion.Angle(animatedRigidbody.rotation, syncRot) > 0.01f)
        {
          animatedRigidbody.Move(syncPos, syncRot);
        }
      }

      SyncSnappoint();
    }

    public void SetTrackingRange(float min, float max)
    {
      MinTrackingRange = min;
      MaxTrackingRange = max;
    }

    private void SyncSnappoint()
    {
      if (snappoint && connectorContainer)
        snappoint.position = connectorContainer.position;
    }

    private Quaternion CalculateRotationTarget()
    {
      hingeEndEuler = Vector3.zero;

      if ((hingeAxes & HingeAxis.X) != 0)
        hingeEndEuler.x = (xHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.x;
      if ((hingeAxes & HingeAxis.Y) != 0)
        hingeEndEuler.y = (yHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.y;
      if ((hingeAxes & HingeAxis.Z) != 0)
        hingeEndEuler.z = (zHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.z;

      var target = currentMotionState == MotionState.Returning ? 0f : 1f;
      hingeLerpProgress = Mathf.MoveTowards(hingeLerpProgress, target, interpolationSpeed * Time.fixedDeltaTime);
      return Quaternion.Euler(Vector3.Lerp(Vector3.zero, hingeEndEuler, hingeLerpProgress));
    }

    private Quaternion CalculateTargetNearestEnemyRotation()
    {
      if (!nearestTarget || !piecesContainer) return animatedTransform.localRotation;
      var toTarget = nearestTarget.transform.position - piecesContainer.position;
      if (toTarget.magnitude is < 5f or > 50f) return animatedTransform.localRotation;
      var flat = new Vector3(toTarget.x, 0f, toTarget.z);
      if (flat.sqrMagnitude < 0.001f) return animatedTransform.localRotation;
      return Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private static float NormalizeAngle(float angle)
    {
      angle %= 360f;
      return angle > 180f ? angle - 360f : angle;
    }

    public virtual Quaternion CalculateTargetWindDirectionRotation()
    {
      // Ensure direction is valid
      var flatWind = new Vector3(cachedWindDirection.x, 0f, cachedWindDirection.z).normalized;
      if (flatWind.sqrMagnitude < 0.001f)
        return animatedTransform.localRotation;

      // Calculate target look rotation
      var target = Quaternion.LookRotation(flatWind, Vector3.up);
      var current = animatedTransform.localRotation;

      // Lerp toward wind direction using movementLerpSpeed
      var next = Quaternion.Slerp(current, target, interpolationSpeed * Time.fixedDeltaTime);

      // Clamp Y (yaw) rotation
      var euler = next.eulerAngles;
      var clampedY = Mathf.Clamp(
        NormalizeAngle(euler.y),
        -maxRotationEuler.y,
        maxRotationEuler.y
      );

      return Quaternion.Euler(0f, clampedY, 0f);
    }

    public void SetMode(SwivelMode newMode)
    {
      mode = newMode;
    }
    public void SetHingeAxes(HingeAxis axes)
    {
      hingeAxes = axes;
    }
    public void SetMaxEuler(Vector3 maxEuler)
    {
      maxRotationEuler = maxEuler;
    }
    public void SetMovementOffset(Vector3 offset)
    {
      movementOffset = offset;
    }
    public void SetMovementLerpSpeed(float speed)
    {
      interpolationSpeed = Mathf.Clamp(speed, 1f, 100f);
    }
    public void SetMotionState(MotionState state)
    {
      currentMotionState = state;
      hasReachedTarget = false;
      hasReturned = false;
      hasRotatedTarget = false;
      hasRotatedReturn = false;
    }

    #region ISwivelConfig

    float ISwivelConfig.MovementLerpSpeed
    {
      get => interpolationSpeed;
      set => interpolationSpeed = value;
    }

    public float MinTrackingRange
    {
      get => minTrackingRange;
      set => minTrackingRange = value;
    }

    public float MaxTrackingRange
    {
      get => maxTrackingRange;
      set => maxTrackingRange = value;
    }

    HingeAxis ISwivelConfig.HingeAxes
    {
      get => hingeAxes;
      set => SetHingeAxes(value);
    }

    Vector3 ISwivelConfig.MaxEuler
    {
      get => maxRotationEuler;
      set => SetMaxEuler(value);
    }

    public Vector3 MovementOffset
    {
      get => movementOffset;
      set => SetMovementOffset(value);
    }

    public MotionState MotionState
    {
      get => currentMotionState;
      set => SetMotionState(value);
    }

    public SwivelMode Mode
    {
      get => mode;
      set => mode = value;
    }

    #endregion

  }
}