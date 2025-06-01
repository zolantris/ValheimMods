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
    private const float angleThreshold = 0.01f;

    public static Vector3 cachedWindDirection = Vector3.zero;

    public static float RotationInterpolateSpeedMultiplier = 3f;
    public static float MovementInterpolationSpeedMultiplier = 0.2f;

    [Description("Swivel Energy Settings")]
    public static float SwivelEnergyDrain = 0.01f;
    public static bool IsPoweredSwivel = true;

    public static List<SwivelComponent> Instances = new();
    public static float UpdateInterval = 0.01f;
    public static bool CanRunSwivelDuringFixedUpdate = false;
    public static bool CanRunSwivelDuringUpdate = true;

    [Header("Swivel General Settings")]
    [SerializeField] public SwivelMode mode = SwivelMode.Rotate;


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

    public PowerConsumerComponent swivelPowerConsumer;

    [Description("This speed is computed with the base interpolation value to get a final interpolation.")]
    public float computedInterpolationSpeed = 10f;

    public Vector3 startLocalPosition;
    public Quaternion startRotation;
    public Vector3 targetMovementPosition;
    public Quaternion targetRotation;

    public float currentMovementLerp;

    // Update debouncing logic
    public float _lastUpdatedMotionTime;
    public float _lastUpdateTime;
    public float lastDeltaTime;
    public bool _IsReady = true;
    public Vector3 _motionFromLocalPos;
    public Vector3 _motionToLocalPos;
    public Quaternion _motionFromLocalRot;
    public Quaternion _motionToLocalRot;
    internal float _motionLerp; // Always between 0 (AtStart) and 1 (AtTarget)
    internal float _nextUpdate;
    private Rigidbody animatedRigidbody;
    private Vector3 frozenLocalPos;
    private Quaternion frozenLocalRot;

    private Vector3 hingeEndEuler;
    private float hingeLerpProgress;
    private bool isFrozen;
    public Action? onMovementReachedTarget;
    public Action? onMovementReturned;
    public Action? onRotationReachedTarget;
    public Action? onRotationReturned;
    private Transform snappoint;

    public HingeAxis HingeAxes => hingeAxes;
    public Vector3 MaxEuler => maxRotationEuler;
    public virtual int SwivelPersistentId { get; set; }

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

      InitPowerConsumer();

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

    public virtual void Update()
    {
      if (CanRunSwivelDuringUpdate)
      {
        SwivelUpdate();
      }
    }

    public virtual void FixedUpdate()
    {
      if (CanRunSwivelDuringFixedUpdate)
      {
        SwivelUpdate();
      }
    }

    protected virtual void OnDestroy()
    {
      Instances.Remove(this);
    }

    public virtual void Request_NextMotionState() {}

    protected virtual Vector3 GetCurrentTargetPosition()
    {
      return currentMotionState == MotionState.ToTarget
        ? startLocalPosition + movementOffset
        : startLocalPosition;
    }

    protected virtual Quaternion GetCurrentTargetRotation()
    {
      return mode == SwivelMode.Rotate
        ? CalculateRotationTarget(1)
        : Quaternion.identity;
    }

    public void FrozenRBUpdate()
    {
      // only called if the swivel is not a base state like Returned or AtTarget
      // if (IsPoweredSwivel && swivelPowerConsumer && !swivelPowerConsumer.IsActive)
      // {
      //   if (!isFrozen)
      //   {
      //     frozenLocalPos = animatedTransform.localPosition;
      //     frozenLocalRot = animatedTransform.localRotation;
      //     isFrozen = true;
      //   }
      //
      //   var parent = animatedTransform.parent;
      //   if (parent != null)
      //   {
      //     var worldPos = parent.TransformPoint(frozenLocalPos);
      //     var worldRot = parent.rotation * frozenLocalRot;
      //
      //     animatedRigidbody.Move(worldPos, worldRot);
      //   }
      //
      //   return;
      // }
      // else
      // {
      //   isFrozen = false;
      // }
    }

    public bool CanUpdate()
    {
      if (!_IsReady) return false;
      if (Mathf.Approximately(_lastUpdateTime, Time.time)) return true;
      if (Time.time < _nextUpdate) return false;
      _lastUpdateTime = _nextUpdate;
      _nextUpdate = Time.time + UpdateInterval;
      _lastUpdateTime = Time.time; // for comparison, if we run a check that hits this during same update.
      lastDeltaTime = _nextUpdate - _lastUpdateTime;
      return true;
    }


    // Interpolates between two states (can be called by both bridge and base logic)
    protected void InterpolateAndMove(float t, Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot)
    {
      var pos = Vector3.Lerp(fromPos, toPos, t);
      var rot = Quaternion.Slerp(fromRot, toRot, t);

      // Always use Rigidbody moves
      if (animatedRigidbody)
      {
        animatedRigidbody.Move(transform.TransformPoint(pos), transform.rotation * rot);
      }
      else
      {
        // fallback, but warn
        LoggerProvider.LogWarning("[Swivel] animatedRigidbody missing, falling back to transform.");
        animatedTransform.localPosition = pos;
        animatedTransform.localRotation = rot;
      }
    }

    public virtual void SwivelUpdate()
    {
      if (!CanUpdate() || !animatedRigidbody || !animatedTransform.parent || !piecesContainer) return;
#if UNITY_2022
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
        animatedTransform.localRotation = CalculateRotationTarget(1);
        return;
      }
      if (mode == SwivelMode.Rotate && currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localRotation = CalculateRotationTarget(0);
        return;
      }

      if (mode == SwivelMode.Move && currentMotionState == MotionState.AtTarget)
      {
        animatedTransform.localPosition = startLocalPosition + movementOffset;
        return;
      }
      if (mode == SwivelMode.Move && currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localPosition = startLocalPosition;
        return;
      }


      // This was disabled as IsActive updates could desync a client when instead it should be handled on the toggle to prevent pressing the button after power is consumed.
      // FrozenRBUpdate()

      switch (mode)
      {
        case SwivelMode.Rotate:
          targetRotation = CalculateRotationTarget(MotionState == MotionState.ToTarget ? 1 : 0);
          var currentRot = animatedTransform.localRotation;
          var maxAnglePerStep = computedInterpolationSpeed * Time.fixedDeltaTime;
          var interpolatedRot = Quaternion.RotateTowards(currentRot, targetRotation, maxAnglePerStep);
          animatedRigidbody.Move(transform.position, transform.rotation * interpolatedRot);
          didMove = true;

          var angleToTarget = Quaternion.Angle(currentRot, targetRotation);

          if (CanUpdateMotionState())
          {
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

          if (CanUpdateMotionState())
          {
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
          }
          break;

#if DEBUG
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
#endif
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

    public Quaternion CalculateRotationTarget(float lerp)
    {
      var hingeEndEuler = Vector3.zero;
      if ((hingeAxes & HingeAxis.X) != 0)
        hingeEndEuler.x = (xHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.x;
      if ((hingeAxes & HingeAxis.Y) != 0)
        hingeEndEuler.y = (yHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.y;
      if ((hingeAxes & HingeAxis.Z) != 0)
        hingeEndEuler.z = (zHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.z;
      return Quaternion.Euler(Vector3.Lerp(Vector3.zero, hingeEndEuler, lerp));
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

    public static PowerIntensityLevel GetPowerIntensityLevelFromLerp(float lerp)
    {
      if (lerp <= 25f)
      {
        return PowerIntensityLevel.Low;
      }
      if (lerp > 25f && lerp < 50f)
      {
        return PowerIntensityLevel.Medium;
      }
      return PowerIntensityLevel.High;
    }

    public virtual void UpdateBasePowerConsumption()
    {
      if (!IsPoweredSwivel || !swivelPowerConsumer) return;
      swivelPowerConsumer.Data.SetPowerIntensity(GetPowerIntensityLevelFromLerp(_motionLerp));
    }

    public void DeactivatePowerConsumer()
    {
      if (IsPoweredSwivel && swivelPowerConsumer)
      {
        swivelPowerConsumer.Data.SetDemandState(false);
      }
    }

    public void ActivatePowerConsumer()
    {
      if (IsPoweredSwivel && swivelPowerConsumer)
      {
        swivelPowerConsumer.Data.SetDemandState(true);
      }
    }

    public virtual void UpdatePowerConsumer()
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

    /// <summary>
    /// Must always be above this value otherwise the swivel could repeatedly fire the previous motion state as it is near that point..
    /// </summary>
    /// <returns></returns>
    public bool CanUpdateMotionState()
    {
      return Mathf.Abs(_lastUpdateTime - _lastUpdatedMotionTime) > 0.5f;
    }

    /// <summary>
    /// Integration will override this to prevent it from being called directly if not the owner.
    /// </summary>
    /// <param name="state"></param>
    public virtual void SetMotionState(MotionState state)
    {
      _lastUpdatedMotionTime = Time.time;
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

    public virtual MotionState MotionState
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