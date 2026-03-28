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
  ///   Full rewrite of tracked land vehicle movement.
  ///   - No wheel collider drive physics
  ///   - Left/right treads are the drive contact points
  ///   - Wheels and visual tread pieces are cosmetic only
  ///   - Designed to work in plain Unity and in Valheim integration
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

    [Header("Speed")]
    [Tooltip("Maximum forward speed in meters per second.")]
    public float maxForwardSpeed = 8f;

    [Tooltip("Maximum reverse speed in meters per second.")]
    public float maxReverseSpeed = 4.5f;

    [Tooltip("Maximum yaw rate during neutral turn in degrees per second.")]
    public float maxNeutralTurnRate = 70f;

    [Tooltip("Maximum yaw rate while moving in degrees per second.")]
    public float maxMovingTurnRate = 45f;

    [Header("Acceleration")]
    [Tooltip("Forward acceleration at High profile in m/s².")]
    public float maxForwardAcceleration = 3.8f;

    [Tooltip("Reverse acceleration at High profile in m/s².")]
    public float maxReverseAcceleration = 2.6f;

    [Tooltip("Brake deceleration in m/s².")]
    public float maxBrakeAcceleration = 6.5f;

    [Tooltip("How aggressively track speed chases the target speed.")]
    public float driveResponse = 5.5f;

    [Tooltip("Additional drag when no throttle is applied.")]
    public float rollingResistance = 1.2f;

    [Header("Grip / Stability")]
    [Tooltip("Planar sideways correction in m/s². Higher value = less lateral slip.")]
    public float lateralGripAcceleration = 18f;

    [Tooltip("Planar forward/reverse correction used when braking.")]
    public float brakingGripAcceleration = 16f;

    [Tooltip("How strongly in-place turning removes world drift.")]
    public float inPlaceTranslationDamping = 14f;

    [Tooltip("How strongly yaw is damped when there is little or no steering input.")]
    public float yawDamping = 3.5f;

    [Tooltip("Roll/pitch stabilization while grounded.")]
    public float rollPitchStabilization = 8f;

    [Tooltip("A small constant downforce to keep contact stable on uneven terrain.")]
    public float groundedDownforce = 1.0f;

    [Header("Air Control")]
    public float airborneAngularDamping = 1.5f;
    public float airborneDownforce = 2.0f;

    [Header("Tread Visuals")]
    public bool ShouldSyncWheelsToCollider;
    public bool ShouldHideWheelRender;
    public bool HasVisualTreadAnimation = true;

    [Header("Brake")]
    public bool StartBraked = true;

    [Header("Debug")]
    public bool DrawDebug;
    public float currentLeftForce;
    public float currentRightForce;
    public float currentLeftTargetSpeed;
    public float currentRightTargetSpeed;
    public float currentForwardSpeed;
    public float currentYawRateDegrees;
    public float currentTrackWidth;
    public Vector3 currentLocalPlanarVelocity;

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

      var centerOfMass = transform.Find("center_of_mass");
      if (!centerOfMassTransform && centerOfMass) centerOfMassTransform = centerOfMass;
      if (!centerOfMassTransform) centerOfMassTransform = transform;

      if (!treadsRightTransform) treadsRightTransform = transform.Find("vehicle_movement/treads/treads_right");
      if (!treadsLeftTransform) treadsLeftTransform = transform.Find("vehicle_movement/treads/treads_left");

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

      if (centerOfMassTransform)
      {
        vehicleRootBody.centerOfMass = transform.InverseTransformPoint(centerOfMassTransform.position);
      }
    }

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

      // Compute the midpoint between the two tread center objects in WORLD space,
      // then store that position relative to the centerOfMassTransform's PARENT so
      // the localPosition is not affected by the centerOfMassTransform's own rotation.
      if (centerOfMassTransform != null && treadsLeftMovingComponent != null && treadsRightMovingComponent != null &&
          treadsLeftMovingComponent.CenterObj != null && treadsRightMovingComponent.CenterObj != null)
      {
        var leftWorld = treadsLeftMovingComponent.CenterObj.transform.position;
        var rightWorld = treadsRightMovingComponent.CenterObj.transform.position;
        var midWorld = (leftWorld + rightWorld) * 0.5f;

        var parent = centerOfMassTransform.parent;
        if (parent != null)
        {
          // Store midpoint in local coordinates of the parent so centerOfMassTransform.rotation doesn't affect it
          centerOfMassTransform.localPosition = parent.InverseTransformPoint(midWorld);
        }
        else
        {
          // No parent: set world position directly
          centerOfMassTransform.position = midWorld;
        }
      }
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
        var leftGrounded = Physics.Raycast(treadsLeftTransform.position + transform.up * 0.2f, -transform.up, 1.0f);
        var rightGrounded = Physics.Raycast(treadsRightTransform.position + transform.up * 0.2f, -transform.up, 1.0f);

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

    private float GetCurrentTrackWidth()
    {
      if (!treadsLeftTransform || !treadsRightTransform) return Mathf.Max(currentVehicleFrameBounds.size.x, 1.5f);
      return Mathf.Max(Vector3.Distance(treadsLeftTransform.position, treadsRightTransform.position), minTreadDistances);
    }

    private float GetPointForwardSpeed(Transform pointTransform)
    {
      var pointVelocity = vehicleRootBody.GetPointVelocity(pointTransform.position);
      return Vector3.Dot(pointVelocity, transform.forward);
    }

    private float GetPointSideSpeed(Transform pointTransform)
    {
      var pointVelocity = vehicleRootBody.GetPointVelocity(pointTransform.position);
      return Vector3.Dot(pointVelocity, transform.right);
    }

    private void ApplyTrackForceAtTread(Transform treadTransform, float targetSpeed, float maxAccel, bool isLeftTread)
    {
      var pointForwardSpeed = GetPointForwardSpeed(treadTransform);
      var pointSideSpeed = GetPointSideSpeed(treadTransform);

      var speedError = targetSpeed - pointForwardSpeed;
      var desiredForwardAccel = Mathf.Clamp(speedError * driveResponse, -maxAccel, maxAccel);

      if (Mathf.Approximately(targetSpeed, 0f) && !IsBraking)
      {
        desiredForwardAccel += -pointForwardSpeed * rollingResistance;
      }

      if (IsBraking)
      {
        desiredForwardAccel = Mathf.Clamp(-pointForwardSpeed * driveResponse, -maxBrakeAcceleration, maxBrakeAcceleration);
      }

      var desiredSideAccel = Mathf.Clamp(-pointSideSpeed * driveResponse, -lateralGripAcceleration, lateralGripAcceleration);

      if (IsBraking)
      {
        desiredSideAccel = Mathf.Clamp(-pointSideSpeed * driveResponse, -brakingGripAcceleration, brakingGripAcceleration);
      }

      var forwardForce = transform.forward * (desiredForwardAccel * vehicleRootBody.mass * 0.5f);
      var sideForce = transform.right * (desiredSideAccel * vehicleRootBody.mass * 0.5f);
      var downForce = -transform.up * (groundedDownforce * vehicleRootBody.mass * 0.5f);

      vehicleRootBody.AddForceAtPosition(forwardForce + sideForce + downForce, treadTransform.position, ForceMode.Force);

      if (isLeftTread)
      {
        currentLeftForce = Vector3.Dot(forwardForce, transform.forward);
      }
      else
      {
        currentRightForce = Vector3.Dot(forwardForce, transform.forward);
      }
    }

    private void ApplyInPlaceTurnDriftCorrection()
    {
      var localVelocity = transform.InverseTransformDirection(vehicleRootBody.linearVelocity);
      var planarVelocity = new Vector3(localVelocity.x, 0f, localVelocity.z);
      var correction = -transform.TransformDirection(planarVelocity) * (vehicleRootBody.mass * inPlaceTranslationDamping);

      vehicleRootBody.AddForce(correction, ForceMode.Force);
    }

    private void ApplyAngularStability(float desiredYawRateDegrees)
    {
      var localAngularVelocity = transform.InverseTransformDirection(vehicleRootBody.angularVelocity);
      var currentYawRate = localAngularVelocity.y * Mathf.Rad2Deg;
      var yawError = desiredYawRateDegrees - currentYawRate;

      if (Mathf.Abs(inputTurnForce) < 0.05f)
      {
        var yawCorrection = -currentYawRate * yawDamping;
        vehicleRootBody.AddRelativeTorque(Vector3.up * (yawCorrection * Mathf.Deg2Rad), ForceMode.Acceleration);
      }
      else
      {
        var yawAssist = yawError * 0.08f;
        vehicleRootBody.AddRelativeTorque(Vector3.up * (yawAssist * Mathf.Deg2Rad), ForceMode.Acceleration);
      }

      var rollPitch = new Vector3(localAngularVelocity.x, 0f, localAngularVelocity.z);
      vehicleRootBody.AddRelativeTorque(-rollPitch * rollPitchStabilization, ForceMode.Acceleration);
    }

    private void ApplyAirControl()
    {
      var localAngularVelocity = transform.InverseTransformDirection(vehicleRootBody.angularVelocity);
      vehicleRootBody.AddRelativeTorque(-localAngularVelocity * airborneAngularDamping, ForceMode.Acceleration);
      vehicleRootBody.AddForce(-transform.up * (airborneDownforce * vehicleRootBody.mass), ForceMode.Force);
    }

    private void UpdateTrackedMovement()
    {
      if (!IsVehicleReady) return;

      var movementInput = Mathf.Clamp(inputMovement, -1f, 1f);
      var turnInput = Mathf.Clamp(inputTurnForce, -1f, 1f);

      if (_currentAccelerationScale <= 0f)
      {
        movementInput = 0f;
        turnInput = 0f;
      }

      IsTurningInPlace = Mathf.Abs(movementInput) < 0.05f && Mathf.Abs(turnInput) > 0.05f;

      currentTrackWidth = GetCurrentTrackWidth();

      var effectiveForwardSpeed = movementInput >= 0f ? maxForwardSpeed : maxReverseSpeed;
      var desiredLinearSpeed = movementInput * effectiveForwardSpeed;

      var desiredYawRate = IsTurningInPlace
        ? turnInput * maxNeutralTurnRate
        : turnInput * maxMovingTurnRate;

      var desiredYawRateRad = desiredYawRate * Mathf.Deg2Rad;
      var halfTrackWidth = currentTrackWidth * 0.5f;

      currentLeftTargetSpeed = desiredLinearSpeed - desiredYawRateRad * halfTrackWidth;
      currentRightTargetSpeed = desiredLinearSpeed + desiredYawRateRad * halfTrackWidth;

      var forwardAccel = maxForwardAcceleration * _currentAccelerationScale;
      var reverseAccel = maxReverseAcceleration * _currentAccelerationScale;

      var leftAccel = currentLeftTargetSpeed >= 0f ? forwardAccel : reverseAccel;
      var rightAccel = currentRightTargetSpeed >= 0f ? forwardAccel : reverseAccel;

      ApplyTrackForceAtTread(treadsLeftTransform, currentLeftTargetSpeed, leftAccel, true);
      ApplyTrackForceAtTread(treadsRightTransform, currentRightTargetSpeed, rightAccel, false);

      if (IsTurningInPlace)
      {
        ApplyInPlaceTurnDriftCorrection();
      }

      ApplyAngularStability(desiredYawRate);

      currentForwardSpeed = Vector3.Dot(vehicleRootBody.linearVelocity, transform.forward);
      currentYawRateDegrees = transform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
      currentLocalPlanarVelocity = transform.InverseTransformDirection(vehicleRootBody.linearVelocity);
      currentLocalPlanarVelocity.y = 0f;
    }

    private void UpdateTreadVisuals()
    {
      if (!HasVisualTreadAnimation) return;

      if (treadsLeftMovingComponent)
      {
        treadsLeftMovingComponent.isForward = currentLeftTargetSpeed >= 0f;
        treadsLeftMovingComponent.SetSpeed(Mathf.Abs(currentLeftTargetSpeed));
      }

      if (treadsRightMovingComponent)
      {
        treadsRightMovingComponent.isForward = currentRightTargetSpeed >= 0f;
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
      var brakePressed = Input.GetKeyDown(KeyCode.Space);
      if (brakePressed && !_isBrakePressedDown)
      {
        ToggleBrake();
        _isBrakePressedDown = true;
      }

      if (Input.GetKeyUp(KeyCode.Space))
      {
        _isBrakePressedDown = false;
      }

      SetTurnInput(Input.GetAxisRaw("Horizontal"));
      SetInputMovement(Input.GetAxisRaw("Vertical"));

      if (Mathf.Abs(inputMovement) > InputDeadZone || Mathf.Abs(inputTurnForce) > InputDeadZone)
      {
        IsBraking = false;
      }

      UpdateAccelerationValues(accelerationType, inputMovement >= 0f);
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

      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

      Gizmos.color = Color.red;
      Gizmos.DrawLine(transform.position, transform.position + transform.right * 2f);
    }
#endif
  }
}