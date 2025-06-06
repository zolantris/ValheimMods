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

    public static float RotationInterpolateSpeedMultiplier = 3f;
    public static float MovementInterpolationSpeedMultiplier = 0.2f;

    [Description("Swivel Energy Settings")]
    public static float SwivelEnergyDrain = 0.01f;
    public static bool IsPoweredSwivel = true;

    public static List<SwivelComponent> Instances = new();
    public static float UpdateInterval = 0.01f;
    public static bool CanRunSwivelDuringFixedUpdate = true;
    public static bool CanRunSwivelDuringUpdate = true;

    [Header("Swivel General Settings")]
    [SerializeField] public SwivelMode mode = SwivelMode.Rotate;

    [Description("per swivel this will be a shared transform. It is not meant to be mutated only observed.")]
    [SerializeField] public Transform windDirectionTransform;

    [SerializeField] private float interpolationSpeed = 10f;
    [SerializeField] public Transform animatedTransform;
    [SerializeField] public MotionState currentMotionState = MotionState.AtStart;

    [Header("Enemy Tracking Settings")]
    [SerializeField] public float minTrackingRange = 5f;
    [SerializeField] public float maxTrackingRange = 50f;
    [SerializeField] internal GameObject nearestTarget;

    [Header("Rotation Mode Settings")]
    [SerializeField] public HingeAxis hingeAxes = HingeAxis.X;
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
    private Rigidbody? parentRigidbody;
    private bool hasParentRigidbody = false;
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

    public static bool CanAllClientsSync = true;
    public static bool ShouldSkipClientOnlyUpdate = true;
    public static bool ShouldSyncClientOnlyUpdate = false;

    // authoratative updates
    protected bool _isAuthoritativeMotionActive;
    protected double _motionStartTime;
    protected float _motionDuration;
    protected Vector3 _motionFromPosition;
    protected Vector3 _motionToPosition;
    protected Quaternion _motionFromRotation;
    protected Quaternion _motionToRotation;
    protected bool _hasArrivedAtDestination = false;

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

      SetLocalStartValues(Vector3.zero, Quaternion.identity);
      GuardSwivelValues();

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

    public void SetLocalStartValues(Vector3 pos, Quaternion rot)
    {
      startLocalPosition = pos;
      startRotation = rot;
    }

    /// <summary>
    /// This guards against desyncs in rotation and movement from initialization and updates.
    /// </summary>
    public void GuardSwivelValues()
    {
      if (MotionState is MotionState.AtTarget)
      {
        if (Mode is SwivelMode.Move)
        {
          if (animatedTransform.localRotation != startRotation)
          {
            animatedTransform.localRotation = startRotation;
          }
        }

        if (Mode is SwivelMode.Rotate)
        {
          if (animatedTransform.localPosition != startLocalPosition)
          {
            animatedTransform.localPosition = startLocalPosition;
          }
        }
        return;
      }

      if (MotionState is MotionState.AtStart)
      {
        if (animatedTransform.localPosition != startLocalPosition)
        {
          animatedTransform.localPosition = startLocalPosition;
        }
        if (animatedTransform.localRotation != startRotation)
        {
          animatedTransform.localRotation = startRotation;
        }
      }
    }

    public virtual void Start()
    {
      SetInterpolationSpeed(interpolationSpeed);
    }

    public virtual void OnTransformParentChanged()
    {
      if (transform.root != transform)
      {
        parentRigidbody = transform.GetComponentInParent<Rigidbody>();
        hasParentRigidbody = parentRigidbody != null;
      }
      else
      {
        parentRigidbody = null;
        hasParentRigidbody = false;
      }
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

    public static float ParentVelocityThreshold = 0.3f;

    /// <summary>
    /// - allow other checks to run if calling on the same frame.
    /// - decreases frame sync time if parent rigidbody exists and is moving fast.
    /// </summary>
    public bool CanUpdate()
    {
      if (!_IsReady) return false;
      if (Mathf.Approximately(_lastUpdateTime, Time.time)) return true;

      try
      {
        if (parentRigidbody && parentRigidbody!.velocity.magnitude > ParentVelocityThreshold)
        {
          CanRunSwivelDuringUpdate = true;
          return true;
        }
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"Error while accessing rigidbody {e}");
        hasParentRigidbody = false;
        parentRigidbody = null;
      }

      CanRunSwivelDuringUpdate = false;
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

    public static float ComputeMotionDuration(SwivelCustomConfig config, MotionState direction)
    {
      // Match your speed multipliers—if you want to make these data-driven, add to config/ZDO!
      const float MoveMultiplier = 0.2f;
      const float RotateMultiplier = 3f;

      // Safety clamp, never divide by zero
      var speed = Mathf.Max(
        config.InterpolationSpeed *
        (config.Mode == SwivelMode.Move ? MoveMultiplier : RotateMultiplier),
        0.001f
      );

      if (config.Mode == SwivelMode.Move)
      {
        // Always use start as base, plus MovementOffset
        Vector3 from, to;
        if (direction == MotionState.ToTarget)
        {
          from = Vector3.zero; // startLocalPosition is always zero in pure data configs
          to = config.MovementOffset;
        }
        else
        {
          from = config.MovementOffset;
          to = Vector3.zero;
        }

        var distance = Vector3.Distance(from, to);
        return Mathf.Max(distance / speed, 0.01f); // Never less than 10ms
      }
      if (config.Mode == SwivelMode.Rotate)
      {
        // Calculate hingeEndEuler as your SwivelComponent does
        var hingeEndEuler = Vector3.zero;
        if ((config.HingeAxes & HingeAxis.X) != 0)
          hingeEndEuler.x = config.MaxEuler.x;
        if ((config.HingeAxes & HingeAxis.Y) != 0)
          hingeEndEuler.y = config.MaxEuler.y;
        if ((config.HingeAxes & HingeAxis.Z) != 0)
          hingeEndEuler.z = config.MaxEuler.z;

        Quaternion from, to;
        if (direction == MotionState.ToTarget)
        {
          from = Quaternion.identity;
          to = Quaternion.Euler(hingeEndEuler);
        }
        else
        {
          from = Quaternion.Euler(hingeEndEuler);
          to = Quaternion.identity;
        }
        var angle = Quaternion.Angle(from, to); // in degrees
        return Mathf.Max(angle / speed, 0.01f);
      }
      // fallback
      return 0.01f;
    }

    public virtual double GetSyncedTime()
    {
      return Time.time;
    }

    public virtual float GetDeltaTime()
    {
      var now = GetSyncedTime();
      return Mathf.Clamp01((float)((now - _motionStartTime) / _motionDuration));
    }

    public virtual void SwivelRotateUpdate()
    {
      var t = GetDeltaTime();

      if (_isAuthoritativeMotionActive)
      {
        InterpolateAndMove(t, _motionFromLocalPos, _motionFromLocalPos, _motionFromLocalRot, _motionToLocalRot);
        return;
      }

      if (currentMotionState == MotionState.AtTarget)
      {
        animatedTransform.localRotation = CalculateRotationTarget(1);
      }
      if (currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localRotation = CalculateRotationTarget(0);
      }

      GuardSwivelValues();
    }

    public virtual void SwivelMoveUpdate()
    {
      var t = GetDeltaTime();

      if (_isAuthoritativeMotionActive)
      {
        InterpolateAndMove(t, _motionFromLocalPos, _motionToLocalPos, _motionFromLocalRot, _motionFromLocalRot);
        return;
      }

      if (currentMotionState == MotionState.AtTarget)
      {
        animatedTransform.localPosition = startLocalPosition + movementOffset;
        return;
      }
      if (currentMotionState == MotionState.AtStart)
      {
        animatedTransform.localPosition = startLocalPosition;
        return;
      }
    }

    public virtual void SwivelTargetWindUpdate()
    {
      targetRotation = CalculateTargetWindDirectionRotation();
      animatedRigidbody.Move(transform.position, transform.rotation * targetRotation);
      _didMoveDuringUpdate = true;
    }

    public virtual void SwivelTargetEnemyUpdate()
    {
      targetRotation = CalculateTargetNearestEnemyRotation();
      animatedRigidbody.Move(transform.position, transform.rotation * targetRotation);
      _didMoveDuringUpdate = true;
    }

    /// <summary>
    /// This is meant to protect swivels if there is no update on a moving vehicle. If a moving vehicle is moving it will desync the swivel.
    /// </summary>
    public virtual void PostSwivelUpdate()
    {
      if (!_didMoveDuringUpdate)
      {
        // force syncs the swivel position if no update occurs. This should not happen as we should be in a end position.
        LoggerProvider.LogDebugDebounced("Did not move during update yet somehow go past early bailouts");
        var syncPos = animatedTransform.position;
        var syncRot = transform.rotation;
        if ((animatedRigidbody.position - syncPos).sqrMagnitude > 0.0001f || Quaternion.Angle(animatedRigidbody.rotation, syncRot) > 0.01f)
        {
          animatedRigidbody.Move(syncPos, syncRot);
        }
      }
    }

    private bool _didMoveDuringUpdate = false;

    public virtual void SwivelUpdate()
    {
      if (!animatedRigidbody || !animatedTransform.parent || !piecesContainer) return;

      _didMoveDuringUpdate = false;
      switch (mode)
      {
        case SwivelMode.Rotate:
          SwivelRotateUpdate();
          break;

        case SwivelMode.Move:
          SwivelRotateUpdate();
          break;
        case SwivelMode.TargetWind:
          SwivelTargetWindUpdate();
          break;
#if DEBUG
        case SwivelMode.TargetEnemy:
          SwivelTargetEnemyUpdate();
          break;
#endif
        default:
          LoggerProvider.LogWarning($"SwivelMode {mode} not implemented");
          break;
      }
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
      // todo this logic might not be accurate. (for flat wind check)
      // Ensure direction is valid
      var windPosition = windDirectionTransform.position;
      var flatWind = new Vector3(windPosition.x, 0f, windPosition.z).normalized;
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
        return;
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
      computedInterpolationSpeed = Mathf.Clamp(interpolationSpeed * (Mode == SwivelMode.Move ? MovementInterpolationSpeedMultiplier : RotationInterpolateSpeedMultiplier), 1, 200f);
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