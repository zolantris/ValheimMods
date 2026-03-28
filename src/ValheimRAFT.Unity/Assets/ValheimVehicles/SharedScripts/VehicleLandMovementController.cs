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

      [Header("Slope Handling")]
      public float slopeGravityCompensation = 1.1f;
      public float slopeHoldAcceleration = 14f;
      public float slopeLateralHoldAcceleration = 14f;
      public float uphillAccelerationBoost = 18f;
      public float antiReverseAcceleration = 18f;
      public float downhillOverspeedBrakeAcceleration = 12f;

      [Header("Other")]
      private Vector3 _currentGroundNormal = Vector3.up;

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
        if (!treadsRightMovingComponent || !treadsLeftMovingComponent)
        {
          IsOnGround = false;
          return;
        }

        IsOnGround = treadsLeftMovingComponent.IsOnGround() || treadsRightMovingComponent.IsOnGround();
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

      private void ApplyGroundPhysics()
      {
        if (!vehicleRootBody) return;

        var moveInput = Mathf.Clamp(inputMovement, -1f, 1f);
        var turnInput = Mathf.Clamp(inputTurnForce, -1f, 1f);

        var groundNormal = _currentGroundNormal;

        var driveTransform = GetDriveTransform();
        var forward = Vector3.ProjectOnPlane(driveTransform.forward, groundNormal).normalized;
        var right = Vector3.ProjectOnPlane(driveTransform.right, groundNormal).normalized;

        var velocity = vehicleRootBody.linearVelocity;

        var forwardSpeed = Vector3.Dot(velocity, forward);
        var sidewaysSpeed = Vector3.Dot(velocity, right);

        var targetSpeed = moveInput >= 0f
          ? moveInput * maxForwardSpeed
          : moveInput * maxReverseSpeed;

        var accel = moveInput >= 0f
          ? maxForwardAcceleration
          : maxReverseAcceleration;

        var gravityAlongForward = Vector3.Dot(Physics.gravity, forward);
        var gravityAlongRight = Vector3.Dot(Physics.gravity, right);

        var forwardError = targetSpeed - forwardSpeed;
        var desiredForwardAccel = forwardError * driveResponse;

        desiredForwardAccel -= gravityAlongForward * slopeGravityCompensation;
        desiredForwardAccel = Mathf.Clamp(desiredForwardAccel, -accel, accel);

        vehicleRootBody.AddForce(forward * desiredForwardAccel, ForceMode.Acceleration);

        var desiredSideAccel = -sidewaysSpeed * driveResponse - gravityAlongRight * slopeGravityCompensation;
        desiredSideAccel = Mathf.Clamp(desiredSideAccel, -lateralGripAcceleration, lateralGripAcceleration);

        vehicleRootBody.AddForce(right * desiredSideAccel, ForceMode.Acceleration);

        if (moveInput > 0f && forwardSpeed < 0f)
        {
          vehicleRootBody.AddForce(forward * antiReverseAcceleration, ForceMode.Acceleration);
        }
        else if (moveInput < 0f && forwardSpeed > 0f)
        {
          vehicleRootBody.AddForce(-forward * antiReverseAcceleration, ForceMode.Acceleration);
        }

        var targetYawRate = Mathf.Abs(moveInput) < 0.05f
          ? turnInput * maxNeutralTurnRate
          : turnInput * maxMovingTurnRate;

        var currentYawRate = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
        var yawError = targetYawRate - currentYawRate;
        var yawAccel = Mathf.Clamp(yawError * yawVelocityResponse, -maxYawAcceleration, maxYawAcceleration);

        vehicleRootBody.AddTorque(driveTransform.up * yawAccel, ForceMode.Acceleration);

        var verticalVelocity = Vector3.Project(velocity, Vector3.up);
        var planarVelocity = velocity - verticalVelocity;
        var planarSpeed = planarVelocity.magnitude;

        var maxPlanarSpeed = moveInput >= 0f ? maxForwardSpeed : maxReverseSpeed;
        if (Mathf.Abs(moveInput) > 0.01f && planarSpeed > maxPlanarSpeed)
        {
          planarVelocity = planarVelocity.normalized * maxPlanarSpeed;
          vehicleRootBody.linearVelocity = planarVelocity + verticalVelocity;
        }
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
        var forwardSpeed = Vector3.Dot(worldVelocity, driveForward);
        var lateralSpeed = Vector3.Dot(worldVelocity, driveRight);

        currentForwardSpeed = forwardSpeed;
        currentLocalPlanarVelocity = new Vector3(lateralSpeed, 0f, forwardSpeed);
        currentYawRateDegrees = driveTransform.InverseTransformDirection(vehicleRootBody.angularVelocity).y * Mathf.Rad2Deg;
        currentTrackWidth = GetCurrentTrackWidth();
        currentVehicleCenter = GetVehicleCenterWorld();

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

        vehicleRootBody.AddForce(driveForward * (desiredForwardAccel * vehicleRootBody.mass), ForceMode.Force);

        var lateralHold = Mathf.Abs(moveInput) < 0.05f
          ? Mathf.Max(lateralGripAcceleration, slopeLateralHoldAcceleration)
          : lateralGripAcceleration;

        var desiredLateralAccel = -lateralSpeed * driveResponse - gravityAlongRight * slopeGravityCompensation;
        desiredLateralAccel = Mathf.Clamp(desiredLateralAccel, -lateralHold, lateralHold);

        vehicleRootBody.AddForce(driveRight * (desiredLateralAccel * vehicleRootBody.mass), ForceMode.Force);

        var maxAllowedForwardSpeed = moveInput >= 0f ? maxForwardSpeed : maxReverseSpeed;

        if (moveInput > 0f && forwardSpeed > maxAllowedForwardSpeed)
        {
          var overspeed = forwardSpeed - maxAllowedForwardSpeed;
          var overspeedBrake = Mathf.Min(overspeed * driveResponse, downhillOverspeedBrakeAcceleration);
          vehicleRootBody.AddForce(-driveForward * (overspeedBrake * vehicleRootBody.mass), ForceMode.Force);
        }
        else if (moveInput < 0f && -forwardSpeed > maxAllowedForwardSpeed)
        {
          var overspeed = -forwardSpeed - maxAllowedForwardSpeed;
          var overspeedBrake = Mathf.Min(overspeed * driveResponse, downhillOverspeedBrakeAcceleration);
          vehicleRootBody.AddForce(driveForward * (overspeedBrake * vehicleRootBody.mass), ForceMode.Force);
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
          var planarVelocity = Vector3.ProjectOnPlane(worldVelocity, groundNormal);
          var inPlaceCorrection = -planarVelocity * inPlaceTranslationDamping * vehicleRootBody.mass;
          vehicleRootBody.AddForce(inPlaceCorrection, ForceMode.Force);
        }

        vehicleRootBody.AddForce(-groundNormal * (groundedDownforce * vehicleRootBody.mass), ForceMode.Force);

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

        if (IsOnGround)
        {
          ApplyGroundPhysics();
        }
        else
        {
          ApplyAirbornePhysics();
        }

        UpdateTrackedMovement();
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