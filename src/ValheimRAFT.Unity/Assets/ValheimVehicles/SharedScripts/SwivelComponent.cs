// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;
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

    public static float RotationInterpolateSpeedMultiplier = 3f;
    public static float MovementInterpolationSpeedMultiplier = 0.2f;

    [Description("Swivel Energy Settings")]
    public static float SwivelEnergyDrain = 0.01f;

    public static bool IsPoweredSwivel = true;

    [Header("Swivel General Settings")]
    [SerializeField] private SwivelMode mode = SwivelMode.Rotate;


    [SerializeField] private float interpolationSpeed = 10f;
    [SerializeField] public Transform animatedTransform;
    [SerializeField] public MotionState currentMotionState = MotionState.AtStart;

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

    [Header("Movement Mode Settings")]
    [SerializeField] public Vector3 movementOffset = new(0f, 0f, 0f);
    [SerializeField] public bool useWorldPosition;

    [Description("Piece container containing all children to be rotated or moved.")]
    public Transform piecesContainer;

    [Description("Shown until an object is connected to the swivel.")]
    public Transform connectorContainer;

    public Transform directionDebuggerArrow;

    public bool CanUpdate = true;

    public PowerConsumerComponent swivelPowerConsumer;
    private Rigidbody animatedRigidbody;

    [Description("This speed is computed with the base interpolation value to get a final interpolation.")]
    private float computedInterpolationSpeed = 10f;

    private Vector3 hingeEndEuler;
    private float hingeLerpProgress;
    public Action? onMovementReachedTarget;
    public Action? onMovementReturned;
    public Action? onRotationReachedTarget;
    public Action? onRotationReturned;
    private Transform snappoint;

    private Vector3 startLocalPosition;
    private Quaternion startRotation;
    private Vector3 targetMovementPosition;
    private Quaternion targetRotation;

    public MotionState CurrentMotionState => currentMotionState;
    public HingeAxis HingeAxes => hingeAxes;
    public Vector3 MaxEuler => maxRotationEuler;

    public static List<SwivelComponent> Instances = new();
    private Vector3 frozenLocalPos;
    private Quaternion frozenLocalRot;
    private bool isFrozen;

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

      if (IsPoweredSwivel)
      {
        InitPowerConsumer();
      }

      if (!Instances.Contains(this))
      {
        Instances.Add(this);
      }
    }

    public virtual void Start()
    {
      SyncSnappoint();
      SetInterpolationSpeed(interpolationSpeed);
    }

    protected virtual void OnDestroy()
    {
      Instances.Remove(this);
    }

    public virtual void FixedUpdate()
    {
      if (!CanUpdate || !animatedRigidbody || !animatedTransform.parent || !piecesContainer) return;
#if UNITY_EDITOR
      // for updating demand state on the fly due to toggling with serializer
      if (Mode == SwivelMode.Rotate || Mode == SwivelMode.Move)
      {
        swivelPowerConsumer.SetDemandState(MotionState != MotionState.AtStart);
      }
#endif

      var didMove = false;

      // Modes that bail early.
      if (mode == SwivelMode.Rotate && currentMotionState == MotionState.AtTarget)
      {
        animatedTransform.localRotation = CalculateRotationTarget();
        return;
      }
      if (mode == SwivelMode.Move && currentMotionState == MotionState.AtTarget)
      {
        animatedTransform.localPosition = startLocalPosition + movementOffset;
        return;
      }
      if (mode == SwivelMode.Rotate && currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localRotation = Quaternion.identity;
        return;
      }
      if (mode == SwivelMode.Move && currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localPosition = startLocalPosition;
        return;
      }

      // only called if the swivel is not a base state like Returned or AtTarget
      if (IsPoweredSwivel && swivelPowerConsumer && !swivelPowerConsumer.IsActive)
      {
        if (!isFrozen)
        {
          frozenLocalPos = animatedTransform.localPosition;
          frozenLocalRot = animatedTransform.localRotation;
          isFrozen = true;
        }

        var parent = animatedTransform.parent;
        if (parent != null)
        {
          var worldPos = parent.TransformPoint(frozenLocalPos);
          var worldRot = parent.rotation * frozenLocalRot;

          animatedRigidbody.Move(worldPos, worldRot);
        }

        return;
      }
      else
      {
        isFrozen = false;
      }

      switch (mode)
      {
        case SwivelMode.Rotate:
          targetRotation = CalculateRotationTarget();
          var currentRot = animatedTransform.localRotation;
          var maxAnglePerStep = computedInterpolationSpeed * Time.fixedDeltaTime;
          var interpolatedRot = Quaternion.RotateTowards(currentRot, targetRotation, maxAnglePerStep);
          animatedRigidbody.Move(transform.position, transform.rotation * interpolatedRot);
          didMove = true;

          var angleToTarget = Quaternion.Angle(currentRot, targetRotation);
          if (currentMotionState == MotionState.ToTarget && angleToTarget < angleThreshold)
          {
            SetMotionState(MotionState.AtTarget);
            SetRotationReachedTarget();
          }
          else if (currentMotionState == MotionState.ToStart && angleToTarget < angleThreshold)
          {
            SetMotionState(MotionState.AtStart);
            SetRotationReturned();
          }
          break;

        case SwivelMode.Move:
          targetMovementPosition = currentMotionState == MotionState.ToStart
            ? startLocalPosition
            : startLocalPosition + movementOffset;

          var currentLocal = animatedTransform.localPosition;
          var moveSpeed = computedInterpolationSpeed * Time.fixedDeltaTime;
          var nextLocal = Vector3.MoveTowards(currentLocal, targetMovementPosition, moveSpeed);
          var worldTarget = transform.TransformPoint(nextLocal);
          animatedRigidbody.Move(worldTarget, transform.rotation);
          didMove = true;

          var distance = Vector3.Distance(currentLocal, targetMovementPosition);
          if (currentMotionState == MotionState.ToTarget && distance < positionThreshold)
          {
            SetMotionState(MotionState.AtTarget);
            SetMoveReachedTarget();
          }
          else if (currentMotionState == MotionState.ToStart && distance < positionThreshold)
          {
            SetMotionState(MotionState.AtStart);
            SetMoveReturned();
          }
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
        if ((animatedRigidbody.position - syncPos).sqrMagnitude > 0.0001f || Quaternion.Angle(animatedRigidbody.rotation, syncRot) > 0.01f)
        {
          animatedRigidbody.Move(syncPos, syncRot);
        }
      }

      SyncSnappoint();
    }

    public virtual void InitPowerConsumer()
    {
      if (!swivelPowerConsumer)
      {
        swivelPowerConsumer = gameObject.GetComponent<PowerConsumerComponent>();
        if (!swivelPowerConsumer)
        {
          swivelPowerConsumer = gameObject.AddComponent<PowerConsumerComponent>();
        }

        if (swivelPowerConsumer)
        {
          UpdatePowerConsumer();
          UpdateBasePowerConsumption();
        }
      }
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

      var target = currentMotionState == MotionState.ToStart ? 0f : 1f;
      hingeLerpProgress = Mathf.MoveTowards(hingeLerpProgress, target, computedInterpolationSpeed * Time.fixedDeltaTime);
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
      var next = Quaternion.Slerp(current, target, computedInterpolationSpeed * Time.fixedDeltaTime);

      // Clamp Y (yaw) rotation
      var euler = next.eulerAngles;
      var clampedY = Mathf.Clamp(
        NormalizeAngle(euler.y),
        -maxRotationEuler.y,
        maxRotationEuler.y
      );

      return Quaternion.Euler(0f, clampedY, 0f);
    }

    public void UpdateBasePowerConsumption()
    {
      if (!IsPoweredSwivel || !swivelPowerConsumer) return;
      swivelPowerConsumer.BasePowerConsumption = SwivelEnergyDrain * computedInterpolationSpeed;
    }

    public void DeactivatePowerConsumer()
    {
      if (IsPoweredSwivel && swivelPowerConsumer)
      {
        swivelPowerConsumer.SetDemandState(false);
      }
    }

    public void ActivatePowerConsumer()
    {
      if (IsPoweredSwivel && swivelPowerConsumer)
      {
        swivelPowerConsumer.SetDemandState(true);
      }
    }

    public void UpdatePowerConsumer()
    {
      if (!IsPoweredSwivel) return;
      if (!swivelPowerConsumer)
      {
        InitPowerConsumer();
      }

      if (mode == SwivelMode.Rotate || mode == SwivelMode.Move)
      {

        if (currentMotionState == MotionState.ToStart || currentMotionState == MotionState.ToTarget)
        {
          ActivatePowerConsumer();
        }
        else
        {
          DeactivatePowerConsumer();
        }
      }
    }

    public void SetRotationReturned()
    {
      UpdatePowerConsumer();
      onRotationReturned?.Invoke();
    }

    public void SetRotationReachedTarget()
    {
      UpdatePowerConsumer();
      onRotationReachedTarget?.Invoke();
    }

    public void SetMoveReturned()
    {
      UpdatePowerConsumer();
      onMovementReturned?.Invoke();
    }

    public void SetMoveReachedTarget()
    {
      UpdatePowerConsumer();
      onMovementReachedTarget?.Invoke();
    }

    public void SetMode(SwivelMode newMode)
    {
      mode = newMode;
      UpdatePowerConsumer();
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
    public void SetInterpolationSpeed(float speed)
    {
      interpolationSpeed = Mathf.Clamp(speed, 1f, 100f);
      computedInterpolationSpeed = interpolationSpeed * (Mode == SwivelMode.Move ? MovementInterpolationSpeedMultiplier : RotationInterpolateSpeedMultiplier);
      UpdateBasePowerConsumption();
      UpdatePowerConsumer();
    }
    public void SetMotionState(MotionState state)
    {
      currentMotionState = state;
      UpdatePowerConsumer();
    }

  #region ISwivelConfig

    public float InterpolationSpeed
    {
      get => interpolationSpeed;
      set => SetInterpolationSpeed(value);
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
      set => SetMode(value);
    }

  #endregion

  }
}