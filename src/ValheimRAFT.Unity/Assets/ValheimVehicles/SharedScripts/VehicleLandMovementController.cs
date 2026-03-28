#region

using System;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
using Vector3 = UnityEngine.Vector3;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  ///   Full tracked vehicle movement rewrite.
  ///   Physics is driven by the root rigidbody, but the effective vehicle center is the midpoint
  ///   between the left and right tread anchors. Wheels and individual tread pieces are cosmetic.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class VehicleLandMovementController : MonoBehaviour
  {
    public enum AccelerationType
    {
      Stop,
      Low,
      Medium,
      High
    }

    public enum SteeringType
    {
      Differential
    }

    private const float GroundTouchGraceSeconds = 0.05f;
    private const float InputDeadZone = 0.025f;

    [Header("Runtime Input")]
    [Range(-1f, 1f)] public float inputMovement;
    [Range(-1f, 1f)] public float inputTurnForce;
    public bool UseInputControls = true;
    public bool IsOnGround;
    public bool IsTurningInPlace { get; private set; }

    [Header("Drive Mode")]
    public AccelerationType accelerationType = AccelerationType.Medium;
    public SteeringType steeringType = SteeringType.Differential;

    [Header("Vehicle References")]
    public Transform forwardDirection;
    [Tooltip("Optional. Leave null or set to this transform to allow Unity to auto-compute COM.")]
    public Transform centerOfMassTransform;
    public Transform wheelParent;
    public Transform treadsParent;
    public Transform rotationEnginesParent;
    public Transform treadsRightTransform;
    public Transform treadsLeftTransform;

    [Header("Vehicle Generation")]
    public float maxTreadLength = 20f;
    public float maxTreadWidth = 8f;
    public float wheelBottomOffset = -1f;
    public float treadWidthXScale = 1f;
    public float minTreadDistances = 0.25f;
    public Bounds currentVehicleFrameBounds = new(Vector3.zero, Vector3.one * 5f);

    [Header("Wheel Compatibility")]
    public float wheelRadius = 1.5f;
    public static float treadRadiusScalar = 1f;

    [Header("Speed")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 5f;
    public float maxNeutralTurnRate = 70f;
    public float maxMovingTurnRate = 45f;

    [Header("Acceleration")]
    public float maxForwardAcceleration = 6f;
    public float maxReverseAcceleration = 4f;
    public float maxBrakeAcceleration = 8f;
    public float driveResponse = 5.5f;
    public float rollingResistance = 1.2f;

    [Header("Grip / Stability")]
    public float lateralGripAcceleration = 20f;
    public float brakingGripAcceleration = 16f;
    public float inPlaceTranslationDamping = 12f;
    public float yawDamping = 3.5f;
    public float rollPitchStabilization = 8f;
    public float groundedDownforce = 1.0f;
    public float lowSpeedGripBoost = 2.25f;
    public float neutralTurnBoost = 1.5f;

    [Header("Yaw Drive")]
    public float yawVelocityResponse = 3.5f;
    public float maxYawAcceleration = 3.25f;

    [Header("Air Control")]
    public float airborneAngularDamping = 1.5f;
    public float airborneDownforce = 2.0f;

    [Header("Brake")]
    public bool StartBraked;

    [Header("Tread Collider Material Compatibility")]
    public float dynamicFriction = 0.01f;
    public float staticFriction = 0.05f;
    public PhysicsMaterial treadPhysicMaterial;

    [Header("Visuals")]
    public bool HasVisualTreadAnimation = true;
    public bool DrawDebug;

    [Header("Runtime Debug")]
    public float currentLeftForce;
    public float currentRightForce;
    public float currentLeftTargetSpeed;
    public float currentRightTargetSpeed;
    public float currentForwardSpeed;
    public float currentYawRateDegrees;
    public float currentTrackWidth;
    public Vector3 currentLocalPlanarVelocity;
    public Vector3 currentVehicleCenter;

    [Header("Slope Handling")]
    public float slopeGravityCompensation = 1.1f;
    public float slopeHoldAcceleration = 14f;
    public float slopeLateralHoldAcceleration = 14f;
    public float uphillAccelerationBoost = 18f;
    public float antiReverseAcceleration = 18f;
    public float downhillOverspeedBrakeAcceleration = 12f;

    [Header("Idle Hold")]
    public float idlePlanarSnapSpeed = 0.08f;
    public float idleAngularSnapSpeed = 0.04f;
    public float idleHoldYawDamping = 8f;
    public float idleSlopeStickForce = 2.5f;


    [Header("Other")]
    public bool IsBrakeOverride = false;
    public bool IsBreakOverride_Value = false;

    private Vector3 _currentGroundNormal = Vector3.up;


    internal MovingTreadComponent treadsLeftMovingComponent;
    internal MovingTreadComponent treadsRightMovingComponent;
    internal Rigidbody vehicleRootBody;

    public BasePiecesController? pieceController;

    [SerializeField] private bool _isBraking;
    private bool _isTreadsInitialized;
    private bool _isVehicleInitialized;
    private bool _isBrakePressedDown;
    private float _currentAccelerationScale = 0.7f;
    private float _lastGroundTouchElapsed = 10f;
    private const float IdleStopDurationSeconds = 3f;

    private bool _wasIdleLastFixedUpdate;
    private float _idleStopElapsed;
    private Vector3 _idleStartPlanarVelocity;
    private float _idleStartYawVelocityLocal;

    public Action OnWheelsInitialized = () => {};
    public bool IsVehicleReady => _isVehicleInitialized && _isTreadsInitialized && vehicleRootBody && treadsLeftTransform && treadsRightTransform;


    [UsedImplicitly]
    public bool IsBraking
    {
      get => IsBrakeOverride ? IsBreakOverride_Value : _isBraking;
      private set => _isBraking = value;
    }

    private void Awake()
    {
#if !VALHEIM
      var ghostContainer = transform.Find("ghostContainer");
      if (ghostContainer) ghostContainer.gameObject.SetActive(false);
#endif

      vehicleRootBody = GetComponent<Rigidbody>();

      if (!wheelParent) wheelParent = transform.Find("vehicle_movement/wheels");
      if (!treadsParent) treadsParent = transform.Find("vehicle_movement/treads");
      if (!rotationEnginesParent) rotationEnginesParent = transform.Find("vehicle_movement/rotation_engines");
      if (!forwardDirection) forwardDirection = transform;

      if (!treadsRightTransform) treadsRightTransform = transform.Find("vehicle_movement/treads/treads_right");
      if (!treadsLeftTransform) treadsLeftTransform = transform.Find("vehicle_movement/treads/treads_left");

      if (!treadPhysicMaterial)
      {
        treadPhysicMaterial = new PhysicsMaterial("VehicleLandMovementController_TreadMaterial")
        {
          dynamicFriction = dynamicFriction,
          staticFriction = staticFriction,
          bounciness = 0f,
          bounceCombine = PhysicsMaterialCombine.Minimum,
          frictionCombine = PhysicsMaterialCombine.Multiply
        };
      }

      IsBraking = StartBraked;


      ConfigureRigidBody();
      InitTreads();
    }

    private void OnEnable()
    {
      ConfigureRigidBody();
      InitTreads();
    }

    private void OnDisable()
    {
      CleanupTreads();
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (collision.contactCount <= 0) return;
      _lastGroundTouchElapsed = 0f;
    }

    private void OnCollisionStay(Collision collision)
    {
      if (collision.contactCount <= 0) return;
      _lastGroundTouchElapsed = 0f;
    }

    private void ConfigureRigidBody()
    {
      if (!vehicleRootBody) return;

      vehicleRootBody.interpolation = RigidbodyInterpolation.Interpolate;
      vehicleRootBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
      vehicleRootBody.maxAngularVelocity = 8f;
    }

    // private void ConfigureRigidBody()
    // {
    //   if (!vehicleRootBody) return;
    //
    //   vehicleRootBody.interpolation = RigidbodyInterpolation.Interpolate;
    //   vehicleRootBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    //   vehicleRootBody.maxAngularVelocity = 8f;
    //
    //   if (centerOfMassTransform && centerOfMassTransform != transform)
    //   {
    //     vehicleRootBody.centerOfMass = transform.InverseTransformPoint(centerOfMassTransform.position);
    //   }
    //   else
    //   {
    //     vehicleRootBody.ResetCenterOfMass();
    //   }
    // }

    private void SetupSingleTread(GameObject treadObj, ref MovingTreadComponent movingTreadComponent)
    {
      if (!movingTreadComponent) movingTreadComponent = treadObj.GetComponent<MovingTreadComponent>();
      if (!movingTreadComponent) movingTreadComponent = treadObj.AddComponent<MovingTreadComponent>();

      movingTreadComponent.treadParent = treadObj.transform;
      movingTreadComponent.vehicleLandMovementController = this;
    }

    private void InitTreads()
    {
      _isTreadsInitialized = false;

      if (!treadsRightTransform || !treadsLeftTransform) return;

      SetupSingleTread(treadsRightTransform.gameObject, ref treadsRightMovingComponent);
      SetupSingleTread(treadsLeftTransform.gameObject, ref treadsLeftMovingComponent);

      _isTreadsInitialized = true;
    }

    private void CleanupTreads()
    {
      if (treadsLeftMovingComponent) Destroy(treadsLeftMovingComponent);
      if (treadsRightMovingComponent) Destroy(treadsRightMovingComponent);
    }

    public Bounds GetVehicleFrameBounds(Bounds bounds)
    {
      if (bounds.size.x < 2f || bounds.size.y < 0.05f || bounds.size.z < 2f)
      {
        bounds = new Bounds(bounds.center, new Vector3(Mathf.Max(bounds.size.x, 2f), Mathf.Max(bounds.size.y, 0.05f), Mathf.Max(bounds.size.z, 2f)));
      }

      if (bounds.size.x > maxTreadWidth || bounds.size.z > maxTreadLength)
      {
        bounds = new Bounds(
          bounds.center,
          new Vector3(
            Mathf.Min(bounds.size.x, maxTreadWidth),
            bounds.size.y,
            Mathf.Min(bounds.size.z, maxTreadLength)
          )
        );
      }

      return bounds;
    }

    private void UpdateTreads(Bounds bounds)
    {
      if (!treadsLeftTransform || !treadsRightTransform) return;

      var leftLocal = new Vector3(bounds.min.x, bounds.min.y + wheelBottomOffset, bounds.center.z);
      var rightLocal = new Vector3(bounds.max.x, bounds.min.y + wheelBottomOffset, bounds.center.z);

      treadsLeftTransform.localRotation = Quaternion.identity;
      treadsRightTransform.localRotation = Quaternion.identity;

      treadsLeftTransform.position = transform.TransformPoint(leftLocal);
      treadsRightTransform.position = transform.TransformPoint(rightLocal);

      var treadScale = new Vector3(treadWidthXScale, 1f, 1f);
      treadsLeftTransform.localScale = treadScale;
      treadsRightTransform.localScale = treadScale;

      if (treadsLeftMovingComponent) treadsLeftMovingComponent.GenerateTreads(bounds);
      if (treadsRightMovingComponent) treadsRightMovingComponent.GenerateTreads(bounds);
    }

    public void Initialize(Bounds? bounds)
    {
      currentVehicleFrameBounds = GetVehicleFrameBounds(bounds.GetValueOrDefault(new Bounds(Vector3.zero, Vector3.one * 4f)));
      UpdateTreads(currentVehicleFrameBounds);
      UpdateAccelerationValues(accelerationType, true);

      _isVehicleInitialized = true;
      OnWheelsInitialized();
    }

    private static readonly RaycastHit[] GroundHitBuffer = new RaycastHit[16];

    private bool TryGetValidGroundHit(MovingTreadComponent treadComponent, out RaycastHit validHit)
    {
      validHit = default;

      if (!treadComponent)
        return false;

      var centerPosition = treadComponent.CenterObj.transform.position;
      var treadLowestPosition = new Vector3(centerPosition.x, centerPosition.y - treadComponent.localBounds.extents.y, centerPosition.z);

      var origin = treadLowestPosition - Vector3.up * 0.25f;
      var hitCount = Physics.RaycastNonAlloc(
        origin,
        Vector3.down,
        GroundHitBuffer,
        1.5f,
        LayerHelpers.PhysicalLayerMask,
        QueryTriggerInteraction.Ignore
      );

      if (hitCount <= 0)
        return false;

      SortHitsByDistanceAscending(GroundHitBuffer, hitCount);

      for (var i = 0; i < hitCount; i++)
      {
        var hit = GroundHitBuffer[i];
        if (!IsValidGroundHit(hit))
          continue;

        validHit = hit;
        return true;
      }

      return false;
    }

    private bool IsValidGroundHit(RaycastHit hit)
    {
      if (!hit.collider)
        return false;

      var hitTransform = hit.collider.transform;
      if (!hitTransform)
        return false;

      if (IsIgnoredGroundHitTransform(hitTransform))
        return false;

      return true;
    }

    private bool IsIgnoredGroundHitTransform(Transform hitTransform)
    {
      var hitRoot = hitTransform.root;
      if (!hitRoot)
        return false;

      if (IsSameRoot(hitRoot, transform.root))
        return true;

      if (pieceController && IsSameRoot(hitRoot, pieceController.transform.root))
        return true;

      return false;
    }

    private static bool IsSameRoot(Transform a, Transform b)
    {
      return a && b && a == b;
    }

    private static void SortHitsByDistanceAscending(RaycastHit[] hits, int hitCount)
    {
      for (var i = 0; i < hitCount - 1; i++)
      {
        for (var j = i + 1; j < hitCount; j++)
        {
          if (hits[j].distance >= hits[i].distance)
            continue;

          var temp = hits[i];
          hits[i] = hits[j];
          hits[j] = temp;
        }
      }
    }

    public void UpdateIsOnGround()
    {
      if (_lastGroundTouchElapsed < GroundTouchGraceSeconds)
        _lastGroundTouchElapsed += Time.fixedDeltaTime;

      var hasGroundHit = false;
      var normalSum = Vector3.zero;
      var normalCount = 0;

      if (TryGetValidGroundHit(treadsLeftMovingComponent, out var leftHit))
      {
        hasGroundHit = true;
        normalSum += leftHit.normal;
        normalCount++;
      }

      if (TryGetValidGroundHit(treadsRightMovingComponent, out var rightHit))
      {
        hasGroundHit = true;
        normalSum += rightHit.normal;
        normalCount++;
      }

      if (hasGroundHit)
      {
        _lastGroundTouchElapsed = 0f;
        _currentGroundNormal = normalCount > 0
          ? (normalSum / normalCount).normalized
          : Vector3.up;
      }

      IsOnGround = _lastGroundTouchElapsed < GroundTouchGraceSeconds;
    }

    private float GetAccelerationScale(AccelerationType type)
    {
      return type switch
      {
        AccelerationType.Stop => 0f,
        AccelerationType.Low => 0.4f,
        AccelerationType.Medium => 0.7f,
        AccelerationType.High => 1f,
        _ => 0.7f
      };
    }

    public void UpdateAccelerationValues(AccelerationType acceleration, bool isMovingForward = true)
    {
      accelerationType = acceleration;
      _currentAccelerationScale = GetAccelerationScale(acceleration);
    }

    public void SetInputMovement(float value)
    {
      if (Mathf.Abs(value) < InputDeadZone)
      {
        inputMovement = 0f;
        return;
      }

      inputMovement = Mathf.Clamp(value, -1f, 1f);
    }

    [UsedImplicitly]
    public void SetTurnInput(float value)
    {
      if (Mathf.Abs(value) < InputDeadZone)
      {
        inputTurnForce = 0f;
        return;
      }

      inputTurnForce = Mathf.Clamp(value, -1f, 1f);
    }

    [UsedImplicitly]
    public void ToggleBrake()
    {
      IsBraking = !IsBraking;
    }

    [UsedImplicitly]
    public void SetBrake(bool value)
    {
      IsBraking = value;
    }

    public float GetWheelRadiusScalar()
    {
      return treadRadiusScalar;
    }

    private Transform GetDriveTransform()
    {
      return forwardDirection ? forwardDirection : transform;
    }

    private Vector3 GetVehicleCenterWorld()
    {
      if (treadsLeftTransform && treadsRightTransform)
      {
        return (treadsLeftTransform.position + treadsRightTransform.position) * 0.5f;
      }

      return vehicleRootBody ? vehicleRootBody.worldCenterOfMass : transform.position;
    }

    private float GetCurrentTrackWidth()
    {
      if (!treadsLeftTransform || !treadsRightTransform) return Mathf.Max(currentVehicleFrameBounds.size.x, 1.5f);
      return Mathf.Max(Vector3.Distance(treadsLeftTransform.position, treadsRightTransform.position), minTreadDistances);
    }

    private void ApplyAirbornePhysics()
    {
      if (!vehicleRootBody) return;

      // ✅ Let gravity feel heavier (optional but recommended)
      vehicleRootBody.AddForce(Physics.gravity * 1.5f, ForceMode.Acceleration);

      // ✅ Optional: mild air resistance (prevents infinite acceleration)
      var velocity = vehicleRootBody.linearVelocity;
      vehicleRootBody.AddForce(-velocity * 0.02f, ForceMode.Acceleration);

      // ✅ Optional: stabilize spinning
      var angularVelocity = vehicleRootBody.angularVelocity;
      vehicleRootBody.AddTorque(-angularVelocity * 0.5f, ForceMode.Acceleration);
    }

    private void ApplyAngularStability(float targetYawRateDegrees)
    {
      var driveTransform = GetDriveTransform();
      var localAngularVelocity = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity);
      var currentYawRate = localAngularVelocity.y * Mathf.Rad2Deg;

      if (Mathf.Abs(inputTurnForce) < 0.05f)
      {
        var yawDampAccel = -currentYawRate * yawDamping * 0.02f;
        vehicleRootBody.AddTorque(driveTransform.up * yawDampAccel, ForceMode.Acceleration);
      }

      var rollPitchAngular = new Vector3(localAngularVelocity.x, 0f, localAngularVelocity.z);
      var correctiveWorldTorque = driveTransform.TransformDirection(-rollPitchAngular * rollPitchStabilization);
      vehicleRootBody.AddTorque(correctiveWorldTorque, ForceMode.Acceleration);
    }

    private bool IsIdleInput()
    {
      return Mathf.Abs(inputMovement) < 0.05f && Mathf.Abs(inputTurnForce) < 0.05f;
    }

    private void BeginIdleStop(Vector3 groundNormal)
    {
      _wasIdleLastFixedUpdate = true;
      _idleStopElapsed = 0f;

      var velocity = vehicleRootBody.linearVelocity;
      _idleStartPlanarVelocity = Vector3.ProjectOnPlane(velocity, groundNormal);

      var driveTransform = GetDriveTransform();
      var localAngularVelocity = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity);
      _idleStartYawVelocityLocal = localAngularVelocity.y;
    }

    private void EndIdleStop()
    {
      _wasIdleLastFixedUpdate = false;
      _idleStopElapsed = 0f;
      _idleStartPlanarVelocity = Vector3.zero;
      _idleStartYawVelocityLocal = 0f;
    }

    private void ApplyIdleStop(Vector3 groundNormal)
    {
      if (!_wasIdleLastFixedUpdate)
      {
        BeginIdleStop(groundNormal);
      }
      else
      {
        _idleStopElapsed = Mathf.Min(_idleStopElapsed + Time.fixedDeltaTime, IdleStopDurationSeconds);
      }

      var t = IdleStopDurationSeconds <= 0f
        ? 1f
        : Mathf.Clamp01(_idleStopElapsed / IdleStopDurationSeconds);

      var remainingPlanarVelocity = Vector3.Lerp(_idleStartPlanarVelocity, Vector3.zero, t);

      var currentVelocity = vehicleRootBody.linearVelocity;
      var normalVelocity = Vector3.Project(currentVelocity, groundNormal);
      vehicleRootBody.linearVelocity = normalVelocity + remainingPlanarVelocity;

      var driveTransform = GetDriveTransform();
      var localAngularVelocity = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity);
      localAngularVelocity.y = Mathf.Lerp(_idleStartYawVelocityLocal, 0f, t);
      vehicleRootBody.angularVelocity = driveTransform.TransformDirection(localAngularVelocity);

      currentForwardSpeed = Vector3.Dot(remainingPlanarVelocity, Vector3.ProjectOnPlane(driveTransform.forward, groundNormal).normalized);
      currentLocalPlanarVelocity = driveTransform.InverseTransformDirection(remainingPlanarVelocity);
      currentYawRateDegrees = localAngularVelocity.y * Mathf.Rad2Deg;

      currentLeftTargetSpeed = 0f;
      currentRightTargetSpeed = 0f;
      currentLeftForce = 0f;
      currentRightForce = 0f;
      IsTurningInPlace = false;
    }

    private void ApplyGroundPhysics()
    {
      if (!vehicleRootBody) return;

      var driveTransform = GetDriveTransform();
      var rawForward = driveTransform.forward;
      var rawRight = driveTransform.right;
      var driveUp = driveTransform.up;

      var groundNormal = IsOnGround ? _currentGroundNormal : Vector3.up;

      var driveForward = Vector3.ProjectOnPlane(rawForward, groundNormal).normalized;
      var driveRight = Vector3.ProjectOnPlane(rawRight, groundNormal).normalized;

      if (driveForward.sqrMagnitude < 0.0001f)
      {
        driveForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
      }

      if (driveRight.sqrMagnitude < 0.0001f)
      {
        driveRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
      }

      var moveInput = Mathf.Clamp(inputMovement, -1f, 1f);
      var turnInput = Mathf.Clamp(inputTurnForce, -1f, 1f);

      if (_currentAccelerationScale <= 0f)
      {
        moveInput = 0f;
        turnInput = 0f;
      }

      var worldVelocity = vehicleRootBody.linearVelocity;
      var planarVelocity = Vector3.ProjectOnPlane(worldVelocity, groundNormal);
      var forwardSpeed = Vector3.Dot(planarVelocity, driveForward);
      var lateralSpeed = Vector3.Dot(planarVelocity, driveRight);

      currentForwardSpeed = forwardSpeed;
      currentLocalPlanarVelocity = new Vector3(lateralSpeed, 0f, forwardSpeed);
      currentYawRateDegrees = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
      currentTrackWidth = GetCurrentTrackWidth();
      currentVehicleCenter = GetVehicleCenterWorld();

      var isIdle = IsIdleInput() && !IsBraking;

      if (isIdle)
      {
        ApplyIdleStop(groundNormal);
        vehicleRootBody.AddForce(-groundNormal * groundedDownforce, ForceMode.Acceleration);
        return;
      }

      if (_wasIdleLastFixedUpdate)
      {
        EndIdleStop();
      }

      IsTurningInPlace = Mathf.Abs(moveInput) < 0.05f && Mathf.Abs(turnInput) > 0.05f;

      var gravity = Physics.gravity;
      var gravityAlongForward = Vector3.Dot(gravity, driveForward);
      var gravityAlongRight = Vector3.Dot(gravity, driveRight);

      var targetForwardSpeed = moveInput >= 0f
        ? moveInput * maxForwardSpeed
        : moveInput * maxReverseSpeed;

      var maxDriveAccel = moveInput >= 0f
        ? maxForwardAcceleration * _currentAccelerationScale
        : maxReverseAcceleration * _currentAccelerationScale;

      if (moveInput > 0f && driveForward.y > 0.01f)
      {
        maxDriveAccel += uphillAccelerationBoost * driveForward.y;
      }
      else if (moveInput < 0f && driveForward.y < -0.01f)
      {
        maxDriveAccel += uphillAccelerationBoost * -driveForward.y;
      }

      float desiredForwardAccel;

      if (IsBraking)
      {
        desiredForwardAccel = -forwardSpeed * driveResponse - gravityAlongForward * slopeGravityCompensation;
        desiredForwardAccel = Mathf.Clamp(desiredForwardAccel, -maxBrakeAcceleration, maxBrakeAcceleration);
      }
      else if (Mathf.Abs(moveInput) < 0.05f)
      {
        var holdAccel = Mathf.Max(maxBrakeAcceleration, slopeHoldAcceleration);
        desiredForwardAccel = -forwardSpeed * driveResponse - gravityAlongForward * slopeGravityCompensation;
        desiredForwardAccel = Mathf.Clamp(desiredForwardAccel, -holdAccel, holdAccel);
      }
      else
      {
        var forwardSpeedError = targetForwardSpeed - forwardSpeed;
        desiredForwardAccel = forwardSpeedError * driveResponse - gravityAlongForward * slopeGravityCompensation;
        desiredForwardAccel = Mathf.Clamp(desiredForwardAccel, -maxDriveAccel, maxDriveAccel);

        if (moveInput > 0f && forwardSpeed < -0.05f)
        {
          desiredForwardAccel += antiReverseAcceleration;
        }
        else if (moveInput < 0f && forwardSpeed > 0.05f)
        {
          desiredForwardAccel -= antiReverseAcceleration;
        }
      }

      vehicleRootBody.AddForce(driveForward * desiredForwardAccel, ForceMode.Acceleration);

      var lateralHold = Mathf.Abs(moveInput) < 0.05f
        ? Mathf.Max(lateralGripAcceleration, slopeLateralHoldAcceleration)
        : lateralGripAcceleration;

      var desiredLateralAccel = -lateralSpeed * driveResponse - gravityAlongRight * slopeGravityCompensation;
      desiredLateralAccel = Mathf.Clamp(desiredLateralAccel, -lateralHold, lateralHold);

      vehicleRootBody.AddForce(driveRight * desiredLateralAccel, ForceMode.Acceleration);

      var maxAllowedForwardSpeed = moveInput >= 0f ? maxForwardSpeed : maxReverseSpeed;

      if (moveInput > 0f && forwardSpeed > maxAllowedForwardSpeed)
      {
        var overspeed = forwardSpeed - maxAllowedForwardSpeed;
        var overspeedBrake = Mathf.Min(overspeed * driveResponse, downhillOverspeedBrakeAcceleration);
        vehicleRootBody.AddForce(-driveForward * overspeedBrake, ForceMode.Acceleration);
      }
      else if (moveInput < 0f && -forwardSpeed > maxAllowedForwardSpeed)
      {
        var overspeed = -forwardSpeed - maxAllowedForwardSpeed;
        var overspeedBrake = Mathf.Min(overspeed * driveResponse, downhillOverspeedBrakeAcceleration);
        vehicleRootBody.AddForce(driveForward * overspeedBrake, ForceMode.Acceleration);
      }

      var targetYawRate = IsTurningInPlace
        ? turnInput * maxNeutralTurnRate * neutralTurnBoost
        : turnInput * maxMovingTurnRate;

      var currentYawRate = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
      var yawRateError = targetYawRate - currentYawRate;
      var desiredYawAccel = Mathf.Clamp(yawRateError * yawVelocityResponse, -maxYawAcceleration, maxYawAcceleration);

      vehicleRootBody.AddTorque(driveUp * desiredYawAccel, ForceMode.Acceleration);

      if (IsTurningInPlace)
      {
        var inPlaceCorrection = -planarVelocity * inPlaceTranslationDamping;
        vehicleRootBody.AddForce(inPlaceCorrection, ForceMode.Acceleration);
      }

      vehicleRootBody.AddForce(-groundNormal * groundedDownforce, ForceMode.Acceleration);

      ApplyAngularStability(targetYawRate);

      var targetYawRateRad = targetYawRate * Mathf.Deg2Rad;
      var halfTrackWidth = currentTrackWidth * 0.5f;
      var baseTrackSpeed = targetForwardSpeed;

      currentLeftTargetSpeed = baseTrackSpeed - targetYawRateRad * halfTrackWidth;
      currentRightTargetSpeed = baseTrackSpeed + targetYawRateRad * halfTrackWidth;

      currentLeftForce = currentLeftTargetSpeed;
      currentRightForce = currentRightTargetSpeed;
    }
    private void ApplyIdleSlopeHold(Vector3 planarVelocity, Vector3 groundNormal, Vector3 driveUp)
    {
      if (!vehicleRootBody) return;

      var planarSpeed = planarVelocity.magnitude;
      var yawSpeed = Mathf.Abs(vehicleRootBody.angularVelocity.y);

      if (planarSpeed <= idlePlanarSnapSpeed)
      {
        var normalVelocity = Vector3.Project(vehicleRootBody.linearVelocity, groundNormal);
        vehicleRootBody.linearVelocity = normalVelocity;
      }
      else
      {
        vehicleRootBody.AddForce(-planarVelocity * idleSlopeStickForce, ForceMode.Acceleration);
      }

      if (yawSpeed <= idleAngularSnapSpeed)
      {
        var angular = vehicleRootBody.angularVelocity;
        angular.y = 0f;
        vehicleRootBody.angularVelocity = angular;
      }
      else
      {
        vehicleRootBody.AddTorque(-driveUp * (vehicleRootBody.angularVelocity.y * idleHoldYawDamping), ForceMode.Acceleration);
      }
    }
    private void UpdateTreadVisuals()
    {
      if (!HasVisualTreadAnimation) return;

      if (treadsLeftMovingComponent)
      {
        treadsLeftMovingComponent.SetDirection(currentLeftTargetSpeed >= 0f);
        treadsLeftMovingComponent.SetSpeed(Mathf.Abs(currentLeftTargetSpeed));
      }

      if (treadsRightMovingComponent)
      {
        treadsRightMovingComponent.SetDirection(currentRightTargetSpeed >= 0f);
        treadsRightMovingComponent.SetSpeed(Mathf.Abs(currentRightTargetSpeed));
      }
    }

    public void VehicleMovementFixedUpdateOwnerClient()
    {
      if (!IsVehicleReady) return;
      if (vehicleRootBody.isKinematic) return;

      UpdateIsOnGround();

      if (IsOnGround)
      {
        ApplyGroundPhysics();
      }
      else
      {
        if (_wasIdleLastFixedUpdate)
        {
          EndIdleStop();
        }

        ApplyAirbornePhysics();
      }
    }

    public void VehicleMovementFixedUpdateAllClients()
    {
      UpdateTreadVisuals();
    }

    public void UpdateControls()
    {
      if (!UseInputControls) return;

#if !VALHEIM
      if (Input.GetKeyDown(KeyCode.Space) && !_isBrakePressedDown)
      {
        ToggleBrake();
        _isBrakePressedDown = true;
      }

      if (Input.GetKeyUp(KeyCode.Space))
      {
        _isBrakePressedDown = false;
      }

      var moveInput = Input.GetAxisRaw("Vertical");
      var turnInput = Input.GetAxisRaw("Horizontal");

      SetInputMovement(moveInput);
      SetTurnInput(turnInput);

      if (Mathf.Abs(moveInput) > InputDeadZone || Mathf.Abs(turnInput) > InputDeadZone)
      {
        IsBraking = false;
      }

      UpdateAccelerationValues(accelerationType, moveInput >= 0f);
#endif
    }

#if !VALHEIM
    private void Update()
    {
      if (!Application.isPlaying) return;
      UpdateControls();
    }

    private void FixedUpdate()
    {
      if (!Application.isPlaying) return;

      if (!IsVehicleReady)
      {
        if (treadsLeftTransform && treadsRightTransform)
        {
          _isVehicleInitialized = true;
        }
      }

      VehicleMovementFixedUpdateOwnerClient();
      VehicleMovementFixedUpdateAllClients();
    }

    private void OnDrawGizmosSelected()
    {
      if (!DrawDebug) return;

      Gizmos.color = Color.cyan;
      if (treadsLeftTransform) Gizmos.DrawSphere(treadsLeftTransform.position, 0.15f);
      if (treadsRightTransform) Gizmos.DrawSphere(treadsRightTransform.position, 0.15f);

      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(GetVehicleCenterWorld(), 0.18f);

      var driveTransform = GetDriveTransform();

      Gizmos.color = Color.green;
      Gizmos.DrawLine(GetVehicleCenterWorld(), GetVehicleCenterWorld() + driveTransform.forward * 2f);

      Gizmos.color = Color.red;
      Gizmos.DrawLine(GetVehicleCenterWorld(), GetVehicleCenterWorld() + driveTransform.right * 2f);
    }
#endif
  }
}