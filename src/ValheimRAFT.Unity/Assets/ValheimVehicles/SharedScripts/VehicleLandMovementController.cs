#region

using System;
using JetBrains.Annotations;
using UnityEngine;
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

    private const float GroundTouchGraceSeconds = 0.2f;
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

    public Action OnWheelsInitialized = () => {};

    internal MovingTreadComponent treadsLeftMovingComponent;
    internal MovingTreadComponent treadsRightMovingComponent;
    internal Rigidbody vehicleRootBody;

    private bool _isBraking;
    private bool _isTreadsInitialized;
    private bool _isVehicleInitialized;
    private bool _isBrakePressedDown;
    private float _currentAccelerationScale = 0.7f;
    private float _lastGroundTouchElapsed = 10f;

    public bool IsVehicleReady => _isVehicleInitialized && _isTreadsInitialized && vehicleRootBody && treadsLeftTransform && treadsRightTransform;

    [UsedImplicitly]
    public bool IsBraking
    {
      get => _isBraking;
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

    public void UpdateIsOnGround()
    {
      if (_lastGroundTouchElapsed < GroundTouchGraceSeconds)
      {
        _lastGroundTouchElapsed += Time.fixedDeltaTime;
      }

      if (treadsLeftTransform && treadsRightTransform)
      {
        var leftGrounded = Physics.Raycast(treadsLeftTransform.position + Vector3.up * 0.2f, Vector3.down, 1.0f);
        var rightGrounded = Physics.Raycast(treadsRightTransform.position + Vector3.up * 0.2f, Vector3.down, 1.0f);

        if (leftGrounded || rightGrounded)
        {
          _lastGroundTouchElapsed = 0f;
        }
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

    private void ApplyAirControl()
    {
      var driveTransform = GetDriveTransform();
      var localAngularVelocity = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity);

      vehicleRootBody.AddRelativeTorque(-localAngularVelocity * airborneAngularDamping, ForceMode.Acceleration);
      vehicleRootBody.AddForce(-Vector3.up * (airborneDownforce * vehicleRootBody.mass), ForceMode.Force);
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

    private void UpdateTrackedMovement()
    {
      if (!IsVehicleReady) return;

      var driveTransform = GetDriveTransform();
      var driveForward = driveTransform.forward;
      var driveRight = driveTransform.right;
      var driveUp = driveTransform.up;

      var moveInput = Mathf.Clamp(inputMovement, -1f, 1f);
      var turnInput = Mathf.Clamp(inputTurnForce, -1f, 1f);

      if (_currentAccelerationScale <= 0f)
      {
        moveInput = 0f;
        turnInput = 0f;
      }

      var worldVelocity = vehicleRootBody.linearVelocity;
      var localVelocity = driveTransform.InverseTransformDirection(worldVelocity);
      var planarLocalVelocity = new Vector3(localVelocity.x, 0f, localVelocity.z);

      currentLocalPlanarVelocity = planarLocalVelocity;
      currentForwardSpeed = planarLocalVelocity.z;
      currentYawRateDegrees = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
      currentTrackWidth = GetCurrentTrackWidth();
      currentVehicleCenter = GetVehicleCenterWorld();

      IsTurningInPlace = Mathf.Abs(moveInput) < 0.05f && Mathf.Abs(turnInput) > 0.05f;

      var targetForwardSpeed = moveInput >= 0f
        ? moveInput * maxForwardSpeed
        : moveInput * maxReverseSpeed;

      var maxDriveAccel = moveInput >= 0f
        ? maxForwardAcceleration * _currentAccelerationScale
        : maxReverseAcceleration * _currentAccelerationScale;

      if (IsBraking)
      {
        targetForwardSpeed = 0f;
        maxDriveAccel = maxBrakeAcceleration;
      }

      var forwardSpeedError = targetForwardSpeed - planarLocalVelocity.z;
      var desiredForwardAccel = Mathf.Clamp(forwardSpeedError * driveResponse, -maxDriveAccel, maxDriveAccel);

      if (!IsBraking && Mathf.Abs(moveInput) < 0.05f)
      {
        desiredForwardAccel = Mathf.Clamp(-planarLocalVelocity.z * rollingResistance, -maxBrakeAcceleration, maxBrakeAcceleration);
      }

      vehicleRootBody.AddForce(driveForward * (desiredForwardAccel * vehicleRootBody.mass), ForceMode.Force);

      var targetYawRate = IsTurningInPlace
        ? turnInput * maxNeutralTurnRate * neutralTurnBoost
        : turnInput * maxMovingTurnRate;

      var currentYawRate = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
      var yawRateError = targetYawRate - currentYawRate;
      var desiredYawAccel = Mathf.Clamp(yawRateError * yawVelocityResponse, -maxYawAcceleration, maxYawAcceleration);

      vehicleRootBody.AddTorque(driveUp * desiredYawAccel, ForceMode.Acceleration);

      var lateralGrip = lateralGripAcceleration;
      if (Mathf.Abs(planarLocalVelocity.z) < 0.5f)
      {
        lateralGrip *= lowSpeedGripBoost;
      }

      var desiredLateralAccel = Mathf.Clamp(-planarLocalVelocity.x * driveResponse, -lateralGrip, lateralGrip);
      vehicleRootBody.AddForce(driveRight * (desiredLateralAccel * vehicleRootBody.mass), ForceMode.Force);

      if (IsBraking)
      {
        var brakingAccel = Mathf.Clamp(-planarLocalVelocity.z * driveResponse, -maxBrakeAcceleration, maxBrakeAcceleration);
        vehicleRootBody.AddForce(driveForward * (brakingAccel * vehicleRootBody.mass), ForceMode.Force);
      }

      if (IsTurningInPlace)
      {
        var verticalVelocity = Vector3.Project(worldVelocity, Vector3.up);
        var planarWorldVelocity = worldVelocity - verticalVelocity;
        var inPlaceCorrection = -planarWorldVelocity * inPlaceTranslationDamping * vehicleRootBody.mass;
        vehicleRootBody.AddForce(inPlaceCorrection, ForceMode.Force);
      }

      vehicleRootBody.AddForce(-driveUp * (groundedDownforce * vehicleRootBody.mass), ForceMode.Force);

      ApplyAngularStability(targetYawRate);

      var targetYawRateRad = targetYawRate * Mathf.Deg2Rad;
      var halfTrackWidth = currentTrackWidth * 0.5f;
      var baseTrackSpeed = targetForwardSpeed;

      currentLeftTargetSpeed = baseTrackSpeed - targetYawRateRad * halfTrackWidth;
      currentRightTargetSpeed = baseTrackSpeed + targetYawRateRad * halfTrackWidth;

      currentLeftForce = currentLeftTargetSpeed;
      currentRightForce = currentRightTargetSpeed;
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

      if (!IsOnGround)
      {
        ApplyAirControl();
        UpdateTreadVisuals();
        return;
      }

      UpdateTrackedMovement();
      UpdateTreadVisuals();
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