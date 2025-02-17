#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Vector3 = UnityEngine.Vector3;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  ///   Meant to replace ValheimVehicle.Component.MovementController for integration
  ///   within unity.
  ///   - This will only interface with built-in unity project values.
  ///   MovementController from ValheimVehicles will then override properties and/or
  ///   set defaults.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class VehicleWheelController : MonoBehaviour
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
      Differential,
      Magic,
      FourWheel
    }

    public const float wheelBaseRadiusScale = 1.5f;

    private const float Gravity = 9.81f; // Earth gravity constant
    private const float uphillTorqueMultiplier = 1.5f; // Adjust for hill climbing power
    private const float downhillResistance = 500f; // Torque to counteract rolling backward
    private const float stabilityCorrectionFactor = 200f;
    private const float downforceAmount = 50f;
    public const float centerOfMassOffset = -4f;
    private const float baseAccelerationMultiplier = 30f;
    private const float defaultTurnAccelerationMultiplier = 30f;
    public static float baseTurnAccelerationMultiplier = defaultTurnAccelerationMultiplier;

    private static Vector3 wheelMeshLocalScale = new(3f, 0.3f, 3f);

    public static float defaultBreakForce = 500f;

    public static readonly Bounds VehicleFrameBoundsDefault = new(Vector3.up * 2, new Vector3(4f, 4f, 4f));
    private static float highAcceleration = 3 * baseAccelerationMultiplier;
    private static float mediumAcceleration = 2 * baseAccelerationMultiplier;
    private static float lowAcceleration = 1 * baseAccelerationMultiplier;

    [Header("USER Inputs (FOR FORCE EFFECTS")]
    [Tooltip(
      "User input")]
    public AccelerationType accelerationType = AccelerationType.Low;

    public float inputTurnForce = 1f;
    public float inputMovement;

    public float MaxWheelRPM = 3000f;
    public float turnInputOverride;

    public bool UseInputControls;

    [Header("Vehicle Generation Properties")]
    public int maxTreadLength = 20;
    public int maxTreadWidth = 8;

    [Header("Overrides")]
    public float Override_WheelBottomOffset;
    public float Override_WheelRadius;
    public float Override_WheelSuspensionDistance;
    public float Override_TopSpeed;
    public float Override_wheelDampingRate = 2f;
    public float Override_WheelSpringDamper = 2f;
    public int Override_IsBraking;

    [Tooltip(
      "Assign this to override the center of mass. This can be useful to make the tank more stable and prevent it from flipping over. \n\nNOTE: THIS TRANSFORM MUST BE A CHILD OF THE ROOT TANK OBJECT.")]
    public Transform? centerOfMassTransform;

    [Tooltip(
      "Front wheels used for steering by rotating the wheels left/right.")]
    public List<WheelCollider> front = new();

    [Tooltip("Rear wheels for steering by rotating the wheels left/right.")]
    public List<WheelCollider> rear = new();

    [Tooltip(
      "Wheels on the left side of the tank that are used for differential steering.")]
    public WheelCollider[] left = {};

    [Tooltip(
      "Wheels on the right side of the tank that are used for differential steering.")]
    public WheelCollider[] right = {};

    public SteeringType m_steeringType = SteeringType.Differential;

    [Header("Wheel Settings")]
    public Transform forwardDirection; // Dynamic rotation reference

    public GameObject wheelPrefab; // Prefab for a single wheel set
    public GameObject rotationEnginePrefab; // Prefab for a single wheel set

    public int minimumWheelSets = 3; // Minimum number of wheel sets
    public int maxWheelSets = 3; // Max number of wheel sets (FYI 20 is limit of all wheels so 10 total sets)
    public float
      wheelSetIncrement = 5f;
    public float axelPadding = 0.5f; // Extra length on both sides of axel

    [Tooltip("Wheel properties")]
    public float wheelBottomOffset;
    public float wheelRadius = 1.5f;
    public float wheelSuspensionDistance = 1.5f;
    public float wheelSuspensionSpring = 400f;
    public float wheelSuspensionDamper = 1f;
    public GameObject treadsPrefab;
    [Tooltip("This torque value is scaled for wheelmass radius and vehicle mass")]
    public float baseTorque;

    [Tooltip("Top speed of the tank in m/s.")]
    public float topSpeed = 50.0f;
    [Tooltip(
      "For tanks with front/rear wheels defined, this is how far those wheels turn.")]
    public float steeringAngle = 30.0f;

    [Header("Force Values")]
    [Tooltip("Power of any wheel listed under powered wheels.")]
    public float baseMotorTorque = 10.0f;

    [FormerlySerializedAs("baseVelocityChange")] [Tooltip("Multiplier used to convert input into a force applied at the treads an alternative to wheel motor torque.")]
    public float baseAcceleration = 5.0f; // Adjust this value as needed
    [Tooltip("Multiplier used to convert input turns of -1 and 1 to a bigger number")]
    public float baseTurnForce = 1f; // Adjust this value as needed

    [Tooltip(
      "Turn rate that is \"magically\" applied regardless of what the physics state of the tank is.")]
    public float magicTurnRate = 45.0f;

    public float wheelMass = 500f;

    [Tooltip("Transforms")]
    public Transform wheelParent;
    public Transform treadsParent;
    public Transform rotationEnginesParent;

    public Transform treadsRightTransform;
    public Transform treadsLeftTransform;

    public Rigidbody treadsLeftRb;
    public Rigidbody treadsRightRb;

    [Header("Wheel Acceleration internal Values")]
    public float lowTorque;
    public float mediumTorque;
    public float highTorque;
    public bool isForward = true;

    public bool lastTurningState;
    [Header("SYNC LOGIC")]
    public bool ShouldSyncWheelsToCollider;
    // wheel regeneration unstable so leave this one for now 
    [Tooltip("Prevent deletion of wheels if they already exist when regenerating. This is optimal to prevent wheel from being embedded in terrain")]
    public bool ShouldCleanupPerInitialize;

    [Tooltip("Toggle between wheel-collider torque and direct tread physics.")]
    public bool useDirectTreadPhysics = true;

    public Bounds currentVehicleFrameBounds = new(Vector3.zero, Vector3.one * 5);


    public int totalWheels;

    public Vector3 wheelPrefabCenterPointOffset = Vector3.zero;

    public Bounds wheelPrefabMeshSize;

    public float SuspensionDistanceMultiplier = 1f;
    // Set to false until it's stable/syncs with the treads.
    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public readonly List<WheelCollider> poweredWheels = new();

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, Transform>
      WheelcollidersToWheelVisualMap =
        new();

    private bool _braking;

    // Vehicle states
    private bool _isTreadsInitialized;
    private bool _isWheelsInitialized;

    private bool _shouldSyncVisualOnCurrentFrame = true; // wheel visual sync main controller variable

    internal List<Collider> colliders = new();
    public WheelFrictionCurve currentForwardFriction;

    public WheelFrictionCurve currentSidewaysFriction;

    private float currentSpeed; // Speed tracking
    private float deltaRunPoweredWheels = 0f;

    private float hillFactor = 1f; // Hill compensation multiplier

    private Coroutine? initializeWheelsCoroutine;

    private bool isBrakePressedDown = true;

    internal bool isInAir = true;
    private bool isLeftForward = true;
    private bool isRightForward = true;
    private bool isTurningInPlace;
    private float lerpedTurnFactor;
    private Vector3 m_angularVelocitySmoothSpeed = Vector3.zero;

    private Vector3 m_velocitySmoothSpeed = Vector3.zero;

    private float maxRotationSpeed = 5f; // Default top speed
    private float maxSpeed = 25f; // Default top speed

    internal float powerWheelDeltaInterval = 0.3f;
    internal List<GameObject> rotationEngineInstances = new();
    internal List<HingeJoint> rotatorEngineHingeInstances = new();
    internal MovingTreadComponent treadsLeftMovingComponent;
    internal MovingTreadComponent treadsRightMovingComponent;

    [Tooltip("Kinematic Objects and Colliders")]
    internal Rigidbody vehicleRootBody;
    internal List<WheelCollider> wheelColliders = new();

    public WheelFrictionCurve WheelForwardFriction = new()
    {
      extremumSlip = 0.4f, // Allows a bit of slip before losing grip
      extremumValue = 1.2f, // More grip when slipping
      asymptoteSlip = 0.8f,
      asymptoteValue = 0.6f,
      stiffness = 2.0f // Stronger forward traction
    };
    internal List<GameObject> wheelInstances = new();
    public WheelFrictionCurve WheelSidewayFriction = new()
    {
      extremumSlip = 0.15f, // Less sideways slip for stability
      extremumValue = 1.5f, // Stronger grip when sliding
      asymptoteSlip = 0.3f,
      asymptoteValue = 1.0f,
      stiffness = 2.2f // Higher stiffness to prevent sliding
    };
    public bool IsVehicleReady => _isWheelsInitialized && _isTreadsInitialized && wheelInstances.Count > 0 && vehicleRootBody; // important catchall for preventing fixedupdate physics from being applied until the vehicle is ready.
    public float wheelColliderRadius => Mathf.Clamp(wheelRadius, 0f, 5f);

    [UsedImplicitly]
    public bool IsBraking
    {
      get => _braking;
      private set
      {
        if (_braking == value) return;
        _braking = value;
        OnBrakingUpdate(value);
      }
    }

    // both inputTurnForce and inputMovement have to be zero to not use the engine.
    public bool IsUsingEngine => !Mathf.Approximately(inputTurnForce, 0f) || !Mathf.Approximately(inputMovement, 0f);

    private void Awake()
    {
#if UNITY_EDITOR
      var ghostContainer = transform.Find("ghostContainer");
      if (ghostContainer) ghostContainer.gameObject.SetActive(false);
#endif
      wheelParent = transform.Find("vehicle_movement/wheels");
      treadsParent = transform.Find("vehicle_movement/treads");

      vehicleRootBody = GetComponent<Rigidbody>();
      var centerOfMass = transform.Find("center_of_mass");
      if (centerOfMass != null) centerOfMassTransform = centerOfMass;
      if (centerOfMassTransform == null) centerOfMassTransform = transform;

      if (!treadsRightTransform)
      {
        treadsRightTransform = transform.Find("vehicle_movement/treads/treads_right");
      }
      if (!treadsLeftTransform)
      {
        treadsLeftTransform = transform.Find("vehicle_movement/treads/treads_left");
      }

      InitTreads();
    }

    private void OnEnable()
    {
      UpdateMaxRPM();
      InitTreads();
    }

    private void OnDisable()
    {
      CleanupTreads();
      Cleanup();
    }

    private void AlignWheelsWithTreads(Bounds bounds)
    {
      var leftIndex = 0;
      var rightIndex = 0;

      // Physics.SyncTransforms();

      // TODO this needs to be fixed to be relative position due to how the position can mismatch we need to calculate based on local bounds of the tread or at least rotated based on the parent
      var sizePerIndex = bounds.size.z / (left.Length - 1);
      // Sync position correctly because it's pretty complicated getting these wheels to align well with treads center. Requires treads to be generated

      var wheelOffsetLeft = wheelParent.InverseTransformPoint(treadsLeftTransform.position);
      var wheelOffsetRight = treadsRightTransform.InverseTransformPoint(treadsRightTransform.position);

      foreach (var wheelInstance in wheelInstances)
      {
        var newPos = Vector3.zero;
        if (wheelInstance.name.Contains("left"))
        {
          // wheelInstance.transform.position = treadsLeft.position;
          newPos = new Vector3(0, 0, sizePerIndex * leftIndex - bounds.extents.z) + wheelOffsetLeft;
          leftIndex += 1;
          wheelInstance.transform.localPosition = newPos;
        }
        if (wheelInstance.name.Contains("right"))
        {
          // wheelInstance.transform.position = treadsRight.position;
          newPos = new Vector3(0, 0, sizePerIndex * rightIndex - bounds.extents.z) + wheelOffsetRight;
          rightIndex += 1;
          wheelInstance.transform.localPosition = newPos;
        }
      }
    }

    /// <summary>
    /// Requires SetupWheels to be called
    /// </summary>
    /// <param name="bounds"></param>
    private void UpdateTreads(Bounds bounds)
    {
      if (!treadsLeftMovingComponent || !treadsRightMovingComponent || !_isTreadsInitialized)
      {
        InitTreads();
      }
      // if (!vehicleRootBody) return;
      if (treadsLeftTransform && treadsRightTransform)
      {
        var leftJoint = treadsLeftTransform.GetComponent<ConfigurableJoint>();
        var rightJoint = treadsRightTransform.GetComponent<ConfigurableJoint>();

        var treadsAnchorLeftLocalPosition = new Vector3(bounds.min.x - 1f, bounds.min.y + wheelBottomOffset, bounds.center.z);
        var treadsAnchorRightLocalPosition = new Vector3(bounds.max.x + 1f, bounds.min.y + wheelBottomOffset, bounds.center.z);

        if (leftJoint && rightJoint)
        {
          leftJoint.connectedBody = null;
          rightJoint.connectedBody = null;
          leftJoint.connectedAnchor = Vector3.zero;
          rightJoint.connectedAnchor = Vector3.zero;
        }

        // var localPoint = vehicleRootBody.transform.InverseTransformPoint();

        treadsLeftTransform.position = vehicleRootBody.transform.TransformPoint(treadsAnchorLeftLocalPosition);
        treadsRightTransform.position = vehicleRootBody.transform.TransformPoint(treadsAnchorRightLocalPosition);
        // treadsLeft.rotation = vehicleRootBody.transform.rotation;
        // treadsRight.rotation = vehicleRootBody.transform.rotation;

        if (leftJoint && rightJoint)
        {
          ConfigureJoint(leftJoint, vehicleRootBody, treadsAnchorLeftLocalPosition);
          ConfigureJoint(rightJoint, vehicleRootBody, treadsAnchorRightLocalPosition);
        }
        // if (!treadsLeftRb.isKinematic) treadsLeftRb.isKinematic = true;
        // if (!treadsRightRb.isKinematic) treadsRightRb.isKinematic = true;
      }
      if (treadsLeftMovingComponent && treadsRightMovingComponent)
      {
        if (!treadsLeftMovingComponent.vehicleWheelController || !treadsRightMovingComponent.vehicleWheelController)
        {
          InitTreads();
        }

        treadsLeftMovingComponent.wheelColliders = left;
        treadsRightMovingComponent.wheelColliders = right;
        treadsLeftMovingComponent.GenerateTreads(bounds);
        treadsRightMovingComponent.GenerateTreads(bounds);
      }

      var collidersRight = treadsRightMovingComponent.GetComponentsInChildren<Collider>();
      var collidersLeft = treadsLeftMovingComponent.GetComponentsInChildren<Collider>();
      colliders.AddRange(collidersLeft);
      colliders.AddRange(collidersRight);

      // todo ensure colliders are then ignored upstream. We want the treads to be able to hit the player or at least a convex collider variant to do this.

      if (treadsRightRb && treadsLeftRb)
      {
        // PhysicsHelpers.UpdateRelativeCenterOfMass(treadsLeftRb, centerOfMassOffset);
        // PhysicsHelpers.UpdateRelativeCenterOfMass(treadsRightRb, centerOfMassOffset);
      }
    }

    public bool IsVehicleInAir()
    {
      if (treadsRightMovingComponent.IsOnGround() || treadsRightMovingComponent.IsOnGround()) return false;
      return !wheelColliders.Any(w => w.isGrounded);
    }
    // New method: apply forces directly at the treads to simulate continuous treads
    private void ApplyTreadForces()
    {
      if (!IsVehicleReady) return;
      if (!IsUsingEngine || IsBraking)
      {
        if (IsBraking)
        {
          ApplyBrakes();
        }

        if (Mathf.Abs(currentSpeed) > 0)
        {
          vehicleRootBody.velocity = Vector3.SmoothDamp(vehicleRootBody.velocity, Vector3.zero, ref m_velocitySmoothSpeed, 20f);
        }
        else
        {
          m_velocitySmoothSpeed = Vector3.Lerp(m_velocitySmoothSpeed, Vector3.zero, Time.deltaTime * 10f);
        }
        return;
      }

      isInAir = IsVehicleInAir();
      if (isInAir)
      {
        ApplyDownforce();
        return;
      }

      AdjustForHills();
      var forward = transform.forward;
      var leftTreadPos = treadsLeftTransform.position;
      var rightTreadPos = treadsRightTransform.position;

      if (vehicleRootBody.velocity.magnitude > maxSpeed)
      {
        return;
      }
      // Differential steering:
      // - A positive inputMovement moves the tank forward.
      // - A positive inputTurnForce will add force to the left tread and subtract from the right,
      //   causing an in-place right turn (and vice versa).

      // Get current angular speed (rotation speed around the Y-axis)
      var angularSpeed = Mathf.Abs(vehicleRootBody.angularVelocity.y);

      // Lerp turn force: Starts high at low speeds (20), drops to 0.01 at max angular speed.
      var turnForceLerp = Mathf.Lerp(inputTurnForce * baseTurnAccelerationMultiplier, 0f, Mathf.Clamp01(angularSpeed / 5f));
      var baseTorqueTurnLerp = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(baseAcceleration / highAcceleration));
      var combinedTurnLerp = turnForceLerp * baseTorqueTurnLerp;

      var leftForce = 0f;
      var rightForce = 0f;

      if (m_steeringType == SteeringType.Magic)
      {
        leftForce = combinedTurnLerp;
      }

      if (m_steeringType == SteeringType.Differential)
      {
        var dt = Time.fixedDeltaTime;
        // leftForce = (inputMovement + combinedTurnLerp) * baseTorque * hillFactor;
        // rightForce = (inputMovement - combinedTurnLerp) * baseTorque * hillFactor;
        leftForce = (inputMovement + combinedTurnLerp) * baseAcceleration * hillFactor * dt;
        rightForce = (inputMovement - combinedTurnLerp) * baseAcceleration * hillFactor * dt;
      }

      // var upwardsForce = transform.up * baseTorque * 0.01f;

      // todo make this apply at front and back wheels depending on direction so the vehicle can ascend over terrain easily and not require wheels as much..
      var upwardsForce = Vector3.zero;

      // Differential steering: Modify turn force multiplier based on movement
      // var leftForceMagnitude = (inputMovement + combinedTurnLerp) * baseTorque * hillFactor;
      // var rightForceMagnitude = (inputMovement - combinedTurnLerp) * baseTorque * hillFactor;

      var deltaTreads = Vector3.Distance(rightTreadPos, leftTreadPos);

      if (deltaTreads <= 2)
      {
        // vehicle is not ready. The treads are too close.
        return;
      }

      vehicleRootBody.AddForceAtPosition(forward * leftForce + upwardsForce, treadsLeftTransform.position, ForceMode.Acceleration);
      vehicleRootBody.AddForceAtPosition(forward * rightForce + upwardsForce, treadsRightTransform.position, ForceMode.Acceleration);

      if (Mathf.Abs(angularSpeed) > maxRotationSpeed)
      {
        vehicleRootBody.angularVelocity = Vector3.SmoothDamp(vehicleRootBody.angularVelocity, Vector3.zero, ref m_angularVelocitySmoothSpeed, 20f);
      }
      else
      {
        m_angularVelocitySmoothSpeed = Vector3.Lerp(m_angularVelocitySmoothSpeed, Vector3.zero, Time.deltaTime * 10f);
      }
      // only apply force 1 time so things are stable.
      // vehicleRootBody.AddForceAtPosition(forward * leftForce + transform.up * 50, treadsLeftRb.position, ForceMode.Force);
      // vehicleRootBody.AddForceAtPosition(forward * rightForce + transform.up * 50, treadsRightRb.position, ForceMode.Force);

      // treadsLeftRb.AddForceAtPosition(forward * leftForce, leftTreadPos, ForceMode.Force);
      // treadsRightRb.AddForceAtPosition(forward * rightForce, rightTreadPos, ForceMode.Force);
    }


    // var inputMovementToSteeringRatio = Mathf.Lerp(1,0f,Mathf.Clamp01(Mathf.Abs(inputTurnForce) - 0.5f));

    // Differential steering: Modify turn force multiplier based on movement
    // var leftForceMagnitude = inputMovement * inputMovementToSteeringRatio * baseTorque * hillFactor;
    // var rightForceMagnitude = inputMovement * inputMovementToSteeringRatio * baseTorque * hillFactor;

    // var leftForceMagnitude = leftForceMagnitude * hillFactor;
    //
    // treadsLeftRb.AddForceAtPosition(forward * leftForceMagnitude, leftTreadPos, ForceMode.Force);
    // treadsRightRb.AddForceAtPosition(forward * rightForceMagnitude, rightTreadPos, ForceMode.Force);

    private float GetSteeringForceLerp()
    {
      var angularSpeed = Mathf.Abs(vehicleRootBody.angularVelocity.y);

      // Lerp turn force: Starts high at low speeds (20), drops to 0.01 at max angular speed.
      var turnForceLerp = Mathf.Lerp(50f * inputTurnForce * baseTurnAccelerationMultiplier, 0f, Mathf.Clamp01(angularSpeed / 5f));
      var baseTorqueTurnLerp = Mathf.Lerp(1.3f, 0.5f, Mathf.Clamp01(baseTorque / highTorque));
      var combinedTurnLerp = turnForceLerp * baseTorqueTurnLerp;

      return combinedTurnLerp;
    }

    /// <summary>
    /// Sets up all treads. Makes it easier to not mess up on left/right duplication of properties per tread
    /// </summary>
    /// <param name="treadObj"></param>
    /// <param name="movingTreadComponent"></param>
    /// <param name="treadRb"></param>
    private void SetupSingleTread(GameObject treadObj, ref MovingTreadComponent movingTreadComponent, ref Rigidbody treadRb)
    {
      if (!movingTreadComponent)
      {
        movingTreadComponent = treadObj.GetComponent<MovingTreadComponent>();
      }
      if (!movingTreadComponent)
      {
        movingTreadComponent = treadObj.AddComponent<MovingTreadComponent>();
      }

      movingTreadComponent.treadParent = treadObj.transform;

      if (treadsPrefab)
      {
        movingTreadComponent.treadPrefab = treadsPrefab;
      }
      movingTreadComponent.vehicleWheelController = this;

      if (!treadRb)
      {
        treadRb = treadObj.GetComponent<Rigidbody>();
      }
    }
    /// <summary>
    /// Init for both treads
    /// </summary>
    private void InitTreads()
    {
      _isTreadsInitialized = false;

      if (!treadsRightTransform || !treadsLeftTransform)
      {
        return;
      }

      SetupSingleTread(treadsRightTransform.gameObject, ref treadsRightMovingComponent, ref treadsRightRb);
      SetupSingleTread(treadsLeftTransform.gameObject, ref treadsLeftMovingComponent, ref treadsLeftRb);

      _isTreadsInitialized = true;
    }

    public void ConfigureJoint(ConfigurableJoint joint, Rigidbody vehicleBody, Vector3 localPosition)
    {
      joint.autoConfigureConnectedAnchor = false;
      joint.connectedBody = null;
      joint.anchor = Vector3.zero;

      joint.connectedAnchor = localPosition;
      joint.connectedBody = vehicleBody;

      // Lock rotation completely
      joint.angularXMotion = ConfigurableJointMotion.Locked;
      joint.angularYMotion = ConfigurableJointMotion.Locked;
      joint.angularZMotion = ConfigurableJointMotion.Limited;

      // Allow expansion by keeping movement flexible along the growing axis
      joint.xMotion = ConfigurableJointMotion.Locked;
      joint.yMotion = ConfigurableJointMotion.Limited; // Adjust as needed
      joint.zMotion = ConfigurableJointMotion.Locked;
    }

    public void ScaleAxle(Transform axle, float targetLength)
    {
      var meshBounds = axle.GetComponent<MeshFilter>().sharedMesh.bounds.size;
      axle.localScale = new Vector3(
        targetLength / meshBounds.x, // Scale only the length axis
        axle.localScale.y, // Keep other axes the same
        axle.localScale.z
      );
    }

    public void ScaleMeshToFitBounds(Transform obj, Vector3 targetSize)
    {
      var meshFilter = obj.GetComponent<MeshFilter>();
      if (!meshFilter) return;

      // Get original bounds in local space
      var meshBounds = meshFilter.sharedMesh.bounds.size;

      // Calculate required scale factor per axis
      var scaleFactor = new Vector3(
        targetSize.x / meshBounds.x,
        targetSize.y / meshBounds.y,
        targetSize.z / meshBounds.z
      );

      // Apply scale
      obj.localScale = scaleFactor;
    }

    private void CleanupTreads()
    {
      if (treadsLeftMovingComponent) Destroy(treadsLeftMovingComponent);
      if (treadsRightMovingComponent) Destroy(treadsRightMovingComponent);
    }

    private static void DeleteAllItems(List<GameObject> items)
    {
      if (items.Count <= 0) return;
      var tempList = items.ToList();
      foreach (var set in tempList)
      {
        if (set != null)
        {
          Destroy(set);
        }
      }
      items.Clear();
    }

    private void Cleanup()
    {
      DeleteAllItems(wheelInstances);
      DeleteAllItems(rotationEngineInstances);

      totalWheels = 0;
      rotationEngineInstances.Clear();
      rotatorEngineHingeInstances.Clear();
      WheelcollidersToWheelVisualMap.Clear();
      poweredWheels.Clear();
      wheelColliders.Clear();
      colliders.Clear();
      right = Array.Empty<WheelCollider>();
      left = Array.Empty<WheelCollider>();
      rear.Clear();
      front.Clear();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Todo may remove this logic, and it should not ever be included outside the editor until then.
    /// </summary>
    public void DEPRECATED_WheelTorqueFixedUpdate()
    {
      if (isTurningInPlace != lastTurningState)
      {
        lastTurningState = isTurningInPlace;
        ApplyFrictionValuesToAllWheels();
      }

      AdjustForHills();
      ApplyDownforce();

      if (IsBraking)
        ApplyBrakes();
      else
        ApplyTorque(inputMovement, inputTurnForce);
    }
#endif

    /// <summary>
    ///   To be called from VehicleMovementController
    /// </summary>
    public void VehicleMovementFixedUpdate()
    {
      if (!IsVehicleReady) return;
      _shouldSyncVisualOnCurrentFrame = false;

      isTurningInPlace = Mathf.Approximately(inputMovement, 0f) && Mathf.Abs(inputTurnForce) > 0f;
      currentSpeed = GetTankSpeed();

      if (isTurningInPlace != lastTurningState)
      {
        lastTurningState = isTurningInPlace;
        ApplyFrictionValuesToAllWheels();
      }

      if (useDirectTreadPhysics)
      {
        ApplyTreadForces();
      }

      SyncWheelAndTreadVisuals();
    }

    /// <summary>
    /// Responsible for matching wheels and treads visually with their speed. This is throttled to make the visual performant across frames.
    /// </summary>
    public void SyncWheelAndTreadVisuals()
    {
      if (_shouldSyncVisualOnCurrentFrame) return;

      if (treadsLeftMovingComponent && treadsRightMovingComponent)
      {
        var clampedSpeed = Mathf.Clamp(currentSpeed, -8f, 8f);
        treadsLeftMovingComponent.SetSpeed(clampedSpeed);
        treadsRightMovingComponent.SetSpeed(clampedSpeed);
        UpdateSteeringTreadDirectionVisuals();
      }

      wheelColliders.ForEach(SyncWheelVisual);
      _shouldSyncVisualOnCurrentFrame = true;
    }

    /// <summary>
    /// This method is meant to constrain the wheel bounds so they are within realistic levels.
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public Bounds GetVehicleFrameBounds(Bounds bounds)
    {
      if (bounds.size.x < 4f || bounds.size.y < 2f || bounds.size.z < 4f)
      {
        bounds = VehicleFrameBoundsDefault;
      }

      var isBoundsXOversized = bounds.size.x > maxTreadWidth;
      var isBoundsZOversized = bounds.size.z > maxTreadLength;

      // constrains the bounds to a workable level
      if (isBoundsXOversized || isBoundsZOversized)
      {
        bounds = new Bounds(bounds.center, new Vector3(isBoundsXOversized ? maxTreadWidth : bounds.size.x, bounds.size.y, isBoundsZOversized ? maxTreadLength : bounds.size.z));
      }

      return bounds;
    }

    /// <summary>
    ///   Must pass a local bounds into this wheel controller. The local bounds must be relative to the VehicleWheelController transform.
    /// </summary>
    /// 
    /// TODO make this a coroutine so it does not impact performance.
    /// <param name="bounds"></param>
    public void InitializeWheels(Bounds? bounds)
    {
      _isWheelsInitialized = false;
      var vehicleFrameBounds = GetVehicleFrameBounds(bounds.GetValueOrDefault());
      currentVehicleFrameBounds = vehicleFrameBounds;

      if (ShouldCleanupPerInitialize)
      {
        Cleanup();
      }

      // treads - initialize first so wheels can align within the drawn treads.
      UpdateTreads(currentVehicleFrameBounds);

      // wheels
      GenerateWheelSets(currentVehicleFrameBounds);
      SetupWheels();

      // physics
      UpdateAccelerationValues(accelerationType, isForward);
      ApplyFrictionValuesToAllWheels();

      // AlignWheelsWithTreads(vehicleFrameBounds);
      _isWheelsInitialized = true;
    }

    /// <summary>
    /// This assumes the already called clear for all lists. But still has a check
    /// </summary>
    private void SetupWheels()
    {
      wheelColliders = new List<WheelCollider>();
      colliders = new List<Collider>();
      var tmpLeft = new List<WheelCollider>();
      var tmpRight = new List<WheelCollider>();

      wheelInstances.ForEach(x =>
      {
        var wheelCollider = x.GetComponentInChildren<WheelCollider>();
        if (!wheelCollider) return;
        wheelColliders.Add(wheelCollider);
        var colliderComp = wheelCollider.GetComponent<Collider>();
        if (colliderComp) colliders.Add(colliderComp);
      });


      foreach (var wheelCollider in wheelColliders)
      {
        if (wheelCollider.name.StartsWith("Front") && !front.Contains(wheelCollider))
          front.Add(wheelCollider);

        if (wheelCollider.name.StartsWith("Rear") && !rear.Contains(wheelCollider))
          rear.Add(wheelCollider);

        if (wheelCollider.transform.parent.name.Contains("right") && !right.Contains(wheelCollider))
          tmpRight.Add(wheelCollider);

        if (wheelCollider.transform.parent.name.Contains("left") && !left.Contains(wheelCollider))
          tmpLeft.Add(wheelCollider);

        if (!poweredWheels.Contains(wheelCollider))
        {
          poweredWheels.Add(wheelCollider);
        }
      }

      left = tmpLeft.ToArray();
      right = tmpRight.ToArray();

      tmpLeft.Clear();
      tmpRight.Clear();
    }

    public bool IsXBoundsAlignment()
    {
      // Determine the forward direction (X or Z axis) based on ForwardDirection rotation
      var forwardAngle =
        Mathf.Round(forwardDirection.eulerAngles.y / 90f) * 90f;
      var isXBounds = Mathf.Approximately(Mathf.Abs(forwardAngle) % 180, 90);
      return isXBounds;
    }

    public string GetPositionName(int index, int indexLength)
    {
      var positionName = "mid";
      if (index == 0)
      {
        positionName = "front";
      }

      if (index == indexLength - 1)
      {
        positionName = "back";
      }

      return positionName;
    }

    /// <summary>
    /// For overriding wheels with values. Mainly for debugging
    /// </summary>
    [Conditional("DEBUG")]
    private void SetOverrides()
    {

      if (Override_WheelSuspensionDistance != 0)
      {
        wheelSuspensionDistance = Override_WheelSuspensionDistance;
      }
      if (Override_TopSpeed != 0)
      {
        topSpeed = Override_TopSpeed;
      }
      if (Override_WheelBottomOffset != 0)
      {
        wheelBottomOffset = Override_WheelBottomOffset;
      }
      if (Override_WheelRadius != 0)
      {
        wheelRadius = Override_WheelRadius;
      }

      var breakValue = Math.Sign(Override_IsBraking);
      if (breakValue != 0)
      {
        IsBraking = breakValue == -1;
      }
    }

    /// <summary>
    // todo figure out why GetRelativeWheelToTreadBounds fails to align when rotating
    /// </summary>
    /// <param name="isLeftTread"></param>
    /// <returns></returns>
    public Bounds GetRelativeWheelToTreadBounds(bool isLeftTread)
    {
      // todo this section of code can be computed one time.
      var treadsComponent = isLeftTread ? treadsLeftMovingComponent : treadsRightMovingComponent;
      var treadParentPosition = treadsComponent.transform.position;
      var treadBounds = treadsComponent.localBounds;
      var localTreadCenter = wheelParent.InverseTransformPoint(treadBounds.center + treadParentPosition);

      return new Bounds(localTreadCenter, treadBounds.size);
    }

    /// <summary>
    ///   Generates wheel sets from a local bounds. This bounds must be local.
    /// </summary>
    /// TODO fix the rotated wheel issue where wheels get out of alignment for 90 degrees and -90 variants.
    /// <param name="bounds"></param>
    private void GenerateWheelSets(Bounds bounds)
    {
      if (!vehicleRootBody) return;
      if (!wheelPrefab || !forwardDirection)
      {
        Debug.LogError(
          "Bounds Transform, Forward Direction, and Wheel Set Prefab must be assigned.");
        return;
      }

      // var isXBounds = IsXBoundsAlignment();
      var totalWheelSets = CalculateTotalWheelSets(bounds);
      // this is important to set before calling setter logic that may need to know how many wheels are to be added.
      // alternative is using an allocating array. Might be cleaner.
      totalWheels = totalWheelSets * 2;

      // we fully can regenerate wheels.
      // todo just remove the ones we no longer use
      if (totalWheels != wheelInstances.Count || wheelColliders.Count != totalWheels)
      {
        Cleanup();
      }

      var wheelIndex = 0;

      // var wheelLeftBounds = GetRelativeWheelToTreadBounds(isLeftTread: true);
      // var wheelRightBounds = GetRelativeWheelToTreadBounds(isLeftTread: false);
      // todo figure out why GetRelativeWheelToTreadBounds fails to align when rotating
      var wheelLeftBounds = new Bounds(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z), treadsLeftMovingComponent.localBounds.size);
      var wheelRightBounds = new Bounds(new Vector3(bounds.max.x + 1f, bounds.min.y, bounds.center.z), treadsRightMovingComponent.localBounds.size);
      var spacing = Mathf.Max(wheelLeftBounds.size.z - 1f, 4f) / Math.Max(totalWheelSets - 1, 1);

      // Generate wheel sets dynamically
      for (var i = 0; i < totalWheelSets; i++)
      {
        for (var directionIndex = 0; directionIndex < 2; directionIndex++)
        {
          var isLeft = directionIndex == 0;
          GameObject wheelInstance;
          // **Fix: Correct check for reusing existing instances**
          if (wheelIndex < wheelInstances.Count && wheelInstances[wheelIndex] != null)
          {
            wheelInstance = wheelInstances[wheelIndex];
          }
          else
          {
            wheelInstance = Instantiate(wheelPrefab, wheelParent);
            // wheelInstance = Instantiate(wheelPrefab, isLeft ?treadsLeftTransform: treadsRightTransform);
            wheelInstances.Add(wheelInstance); // **Only add new wheels**
          }
          var dirName = isLeft ? "left" : "right";
          var positionName = GetPositionName(i, totalWheelSets);
          wheelInstance.name = $"ValheimVehicles_VehicleLand_wheel_{positionName}_{dirName}_{i}";

          var treadBounds = isLeft ? wheelLeftBounds : wheelRightBounds;
          var localPosition = GetWheelLocalPosition(wheelInstance, isLeft, i, treadBounds, spacing);

          wheelInstance.transform.localPosition = localPosition;

          SetWheelProperties(wheelInstance);

          // **Fix: Replace instead of appending again**
          if (wheelIndex < wheelInstances.Count)
          {
            wheelInstances[wheelIndex] = wheelInstance; // Replace instead of adding extra entries
          }

          wheelIndex += 1;
        }
      }
    }

    private int CalculateTotalWheelSets(Bounds bounds)
    {
      var vehicleSize = bounds.size.z; // Assuming size along the Z-axis determines length
      var setInt = Mathf.RoundToInt(vehicleSize / wheelSetIncrement);
      if (setInt % 2 == 0)
      {
        setInt += 1;
      }

      var nearestIncrement = Mathf.Clamp(setInt, minimumWheelSets, maxWheelSets);
      return nearestIncrement;
    }

    private Bounds GetLocalMeshBounds(GameObject wheelInstance)
    {
      var meshCollider = wheelInstance.transform.Find("wheel_mesh/wheel_rotator").GetComponent<MeshCollider>();
      var localBounds = meshCollider.sharedMesh.bounds; // Local-space bounds

      return localBounds;
    }

    private Vector3 GetWheelLocalPosition(GameObject wheelInstance, bool isLeft, int index, Bounds treadBounds, float spacing)
    {
      var meshBounds = GetLocalMeshBounds(wheelInstance);

      // Convert local center to world space
      var worldCenter = wheelInstance.transform.TransformPoint(meshBounds.center);

      // Calculate local position using local bounds
      var xPos = treadBounds.center.x + (worldCenter.x - wheelInstance.transform.position.x) + meshBounds.extents.x;
      var zPos = treadBounds.max.z * 0.95f - spacing * index * 0.95f;
      var yPos = treadBounds.center.y;

      return new Vector3(xPos, yPos, zPos);
    }

    public bool IsMiddleIndex(int index, int size)
    {
      // Check if size is even or odd
      if (size % 2 == 0)
      {
        // For even size, two middle indices
        var middle1 = size / 2 - 1;
        var middle2 = size / 2;

        return index == middle1 || index == middle2;
      }
      // For odd size, one middle index
      var middle = size / 2;
      return index == middle;
    }

    public float GetWheelRadiusScalar()
    {
      if (!_isWheelsInitialized || totalWheels == 0) return 1;
      // return 1;
      return wheelRadius / wheelBaseRadiusScale;
    }

    private void SetWheelProperties(GameObject wheelObj)
    {
      if (!vehicleRootBody) return;
      var wheelVisual =
        wheelObj.transform.Find("wheel_mesh");
      var wheelCollider =
        wheelObj.transform.Find("wheel_collider").GetComponent<WheelCollider>();

      wheelCollider.mass = wheelMass;
      // Larger radius is required for traversing valheim terrain. Small wheels will disappear.
      wheelCollider.radius = wheelColliderRadius;

      // Dynamically adjust targetPosition based on weight & radius
      var massFactor = Mathf.Clamp01(vehicleRootBody.mass / 2000f); // Normalize weight influence
      var radiusFactor = Mathf.Clamp01(wheelColliderRadius / 1f); // Normalize wheel size influence


      // wheelCollider.suspensionDistance = wheelColliderRadius * 2f * SuspensionDistanceMultiplier;
      wheelCollider.suspensionDistance = Mathf.Max(wheelColliderRadius * 1.5f, wheelSuspensionDistance);
      wheelCollider.forceAppPointDistance = wheelColliderRadius * 1.2f;

      // wheelCollider.wheelDampingRate = Override_wheelDampingRate;
      var suspensionSpring = wheelCollider.suspensionSpring;
      suspensionSpring.damper = wheelSuspensionDamper;
      suspensionSpring.spring = wheelSuspensionSpring;
      suspensionSpring.targetPosition = Mathf.Lerp(0.3f, 0.5f, massFactor * radiusFactor); // Adjust dynamically

      wheelCollider.suspensionSpring = suspensionSpring;

      var wheelScalar = GetWheelRadiusScalar();

      if (!Mathf.Approximately(wheelScalar, 1f))
      {
        // wheelVisual.transform.localScale = new Vector3(wheelScalar * wheelMeshLocalScale.x, wheelScalar * wheelMeshLocalScale.y, wheelScalar * wheelMeshLocalScale.z);
      }

      if (!WheelcollidersToWheelVisualMap.ContainsKey(wheelCollider))
        WheelcollidersToWheelVisualMap.Add(wheelCollider, wheelVisual);
    }

    public void SyncWheelVisual(WheelCollider wheel)
    {
      if (!_shouldSyncVisualOnCurrentFrame && WheelcollidersToWheelVisualMap.TryGetValue(wheel,
            out var wheelVisual))
      {
        SyncWheelVisualWithCollider(wheelVisual, wheel);
      }
    }
    /// <summary>
    /// Update wheel mesh positions to match the physics wheels.
    /// </summary>
    /// <param name="wheelRenderer"></param>
    /// <param name="wheelCollider"></param>
    private void SyncWheelVisualWithCollider(Transform wheelVisual, WheelCollider wheelCollider)
    {
      if (wheelVisual == null) return;
      var wheelTransform = wheelVisual.transform;
      wheelCollider.GetWorldPose(out var position, out _);
      if (ShouldSyncWheelsToCollider)
      {
        wheelTransform.position = position;
      }
      RotateOnXAxis(wheelCollider, wheelVisual.transform);
    }

    private void RotateOnXAxis(WheelCollider wheelCollider,
      Transform wheelTransform)
    {
      var deltaRotation = Mathf.Clamp(wheelCollider.rpm * Time.fixedDeltaTime, -359f, 359f);
      wheelTransform.Rotate(Vector3.right,
        deltaRotation, Space.Self);
    }

    /// <summary>
    /// Calculates torque values dynamically based on tank mass, wheel count, radius, wheelmass.
    /// </summary>
    public static (float Low, float Medium, float High) CalculateTorque(float tankMass, int wheelCount, float wheelRadius, float wheelMass)
    {
      if (wheelCount <= 0 || wheelRadius <= 0)
      {
        return (0f, 0f, 0f);
      }

      // Acceleration presets (m/s²)
      var lowAcceleration = 2f;
      var mediumAcceleration = 5f;
      var highAcceleration = 10f;

      // Compute linear force per wheel (F = ma / wheelCount)
      var forceLow = tankMass * lowAcceleration / wheelCount;
      var forceMedium = tankMass * mediumAcceleration / wheelCount;
      var forceHigh = tankMass * highAcceleration / wheelCount;

      // Rotational torque contribution per wheel (τ = (1/2) * M * r * a)
      var rotationalLow = 0.5f * wheelMass * wheelRadius * lowAcceleration;
      var rotationalMedium = 0.5f * wheelMass * wheelRadius * mediumAcceleration;
      var rotationalHigh = 0.5f * wheelMass * wheelRadius * highAcceleration;

      // Total torque = Linear Torque + Rotational Torque
      var torqueLow = forceLow * wheelRadius + rotationalLow;
      var torqueMedium = forceMedium * wheelRadius + rotationalMedium;
      var torqueHigh = forceHigh * wheelRadius + rotationalHigh;

      return (torqueLow, torqueMedium, torqueHigh);
    }

    private void UpdateSteeringTreadDirectionVisuals()
    {
      switch (inputTurnForce)
      {
        case > 0.2f:
          isLeftForward = true;
          isRightForward = false;
          break;
        case < -0.2f:
          isLeftForward = false;
          isRightForward = true;
          break;
        default:
          isLeftForward = isForward;
          isRightForward = isForward;
          break;
      }

      // inverse the directions
      if (!isForward)
      {
        isLeftForward = !isLeftForward;
        isRightForward = !isRightForward;
      }

      treadsLeftMovingComponent.isForward = isLeftForward;
      treadsRightMovingComponent.isForward = isRightForward;
    }

    /// <summary>
    /// This is the absolute max value torque has before it is clamped both positive and negative.
    /// </summary>
    public void UpdateMaxRPM()
    {
      var maxTotalTorque = baseMotorTorque * baseTorque * 4;
      MaxWheelRPM = Mathf.Clamp(Mathf.Abs(maxTotalTorque), 1000f, 5000f);
    }

    // Adjusts hillFactor based on the tank's forward Y component.
    // Flat (forward.y == 0) results in hillFactor = 1.
    // Uphill (forward.y positive) scales hillFactor up to 3.
    // Downhill (forward.y negative) scales hillFactor down to 0.5.
    private void AdjustForHills()
    {
      // The Y component of the forward vector represents the vertical inclination.
      var slope = transform.forward.y;
      // maxSlope defines the normalized forward.y value at which we consider the hill "steep" (about 30°).
      var maxSlope = 0.5f;
      if (slope > 0)
      {
        hillFactor = Mathf.Lerp(1f, 3f, Mathf.Clamp01(slope / maxSlope));
      }
      else
      {
        hillFactor = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(-slope / maxSlope));
      }
    }

    // private void AdjustForHills()
    // {
    //   // Get forward direction of the tank
    //   var tankForward = transform.forward;
    //
    //   // Project forward vector onto XZ plane to get horizontal direction
    //   var flatForward = Vector3.ProjectOnPlane(tankForward, Vector3.up).normalized;
    //
    //   // Calculate the pitch angle using dot product
    //   var slopeFactor = Vector3.Dot(tankForward, flatForward);
    //
    //   // Adjust hill factor: Steeper inclines require more torque
    //   hillFactor = Mathf.Clamp(1f + (1f - slopeFactor), 0.5f, 2f);
    // }

    private float GetTankSpeed()
    {
      return Vector3.Dot(vehicleRootBody.velocity, transform.forward);
    }


    private void ApplyBrakes()
    {
      currentSpeed = GetTankSpeed();
      var brakeForce = vehicleRootBody.mass * Mathf.Abs(currentSpeed) / 2f; // Stops in ~2s

      foreach (var wheel in wheelColliders)
      {
        // Apply braking force based on current torque & momentum
        wheel.brakeTorque = Mathf.Max(brakeForce, (useDirectTreadPhysics ? Mathf.Abs(defaultBreakForce * baseAcceleration) : Mathf.Abs(wheel.motorTorque)) * 1.5f);
        wheel.motorTorque = 0; // Cut off power when braking
      }
    }

    private void ApplyDownforce()
    {
      vehicleRootBody.AddForce(-Vector3.up * downforceAmount, ForceMode.Acceleration);
      treadsRightRb.AddForce(-Vector3.up * downforceAmount, ForceMode.Acceleration);
      treadsLeftRb.AddForce(-Vector3.up * downforceAmount, ForceMode.Acceleration);
    }
    private void ApplyFrictionToWheelCollider(WheelCollider wheel, float speed)
    {
      var frictionMultiplier = Mathf.Clamp(10f / wheelColliders.Count, 0.7f, 1.5f);

      var adjustedStiffness = Mathf.Lerp(0.01f, 3.5f, speed / topSpeed); // Adaptive stiffness based on speed

      currentForwardFriction = new WheelFrictionCurve
      {
        extremumSlip = 0.4f * frictionMultiplier,
        extremumValue = 1.5f * frictionMultiplier,
        asymptoteSlip = 2.5f * frictionMultiplier, // Increased to prevent excessive grip switching
        asymptoteValue = 0.5f * frictionMultiplier,
        stiffness = adjustedStiffness // Adjust dynamically instead of hardcoding 10f
      };

      currentSidewaysFriction = new WheelFrictionCurve
      {
        extremumSlip = (isTurningInPlace ? 0.5f : 0.4f) * frictionMultiplier,
        extremumValue = (isTurningInPlace ? 0.7f : 0.8f) * frictionMultiplier,
        asymptoteSlip = (isTurningInPlace ? 1.8f : 2.0f) * frictionMultiplier,
        asymptoteValue = (isTurningInPlace ? 0.5f : 0.6f) * frictionMultiplier,
        stiffness = isTurningInPlace ? 1.8f : Mathf.Lerp(2f, 2.5f, speed / topSpeed)
      };

      // currentSidewaysFriction = new WheelFrictionCurve
      // {
      //   extremumSlip = (isTurningInPlace ? 0.8f : 0.5f) * frictionMultiplier,
      //   extremumValue = (isTurningInPlace ? 0.6f : 0.7f) * frictionMultiplier,
      //   asymptoteSlip = (isTurningInPlace ? 1.5f : 2.0f) * frictionMultiplier, // Increased for smoother grip transitions
      //   asymptoteValue = (isTurningInPlace ? 0.4f : 0.6f) * frictionMultiplier,
      //   stiffness = isTurningInPlace ? 1.5f : adjustedStiffness // Dynamic adjustment
      // };

      wheel.forwardFriction = currentForwardFriction;
      wheel.sidewaysFriction = currentSidewaysFriction;
    }


    private void ApplyFrictionValuesToAllWheels()
    {
      if (!vehicleRootBody) return;
      wheelColliders.ForEach(x => ApplyFrictionToWheelCollider(x, currentSpeed));
    }

    private float GetTurnForce()
    {
      return accelerationType switch
      {
        AccelerationType.Low => 10f,
        AccelerationType.Medium => 10f,
        AccelerationType.High => 10f,
        AccelerationType.Stop => 0f,
        _ => 1f
      };
    }

    private void ApplyTorque(float move, float turn)
    {
      if (Mathf.Approximately(move, 0f) && Mathf.Approximately(turn, 0f))
      {
        StopWheels();
        return;
      }

      lerpedTurnFactor = Mathf.Lerp(lerpedTurnFactor, Mathf.Abs(turn), Time.fixedDeltaTime * 5f);
      var speed = vehicleRootBody.velocity.magnitude;
      var angularSpeed = Mathf.Abs(vehicleRootBody.angularVelocity.y); // Get current rotation speed

      var targetSpeed = move * maxSpeed;
      var torqueBoost = hillFactor * uphillTorqueMultiplier;

      if (move > 0 && Vector3.Dot(vehicleRootBody.velocity, transform.forward) < 0)
      {
        torqueBoost += downhillResistance;
      }

      if (Mathf.Abs(speed - targetSpeed) < 1f)
      {
        torqueBoost *= 0.85f;
      }

      var minTorque = baseTorque * 0.2f;
      var forwardTorque = Mathf.Max((baseTorque + torqueBoost) * move, minTorque);

      var leftSideTorque = forwardTorque;
      var rightSideTorque = forwardTorque;

      // **New: Apply Turn Boost if Rotating in Place**
      if (Mathf.Approximately(move, 0f) && Mathf.Abs(turn) > 0f)
      {
        // var maxTurnSpeed = 1.5f; // Max angular velocity before limiting turn boost
        var turnBoost = Mathf.Clamp(maxRotationSpeed - angularSpeed, 1f, maxRotationSpeed); // Increase torque at low speeds

        leftSideTorque = turn > 0 ? -baseTorque * turnBoost : baseTorque * turnBoost;
        rightSideTorque = turn > 0 ? baseTorque * turnBoost : -baseTorque * turnBoost;
      }
      else if (Mathf.Abs(turn) >= 0.5f)
      {
        leftSideTorque = turn > 0 ? baseTorque : -baseTorque;
        rightSideTorque = turn > 0 ? -baseTorque : baseTorque;
      }
      else if (turn != 0)
      {
        var turnStrength = Mathf.Lerp(1f, 0.6f, lerpedTurnFactor);

        if (turn > 0)
        {
          leftSideTorque *= 1f;
          rightSideTorque *= turnStrength;
        }
        else
        {
          leftSideTorque *= turnStrength;
          rightSideTorque *= 1f;
        }
      }

      // Apply torques to wheels
      foreach (var leftWheel in left)
      {
        leftWheel.brakeTorque = 0f;
        leftWheel.motorTorque = leftSideTorque;
      }
      foreach (var rightWheel in right)
      {
        rightWheel.brakeTorque = 0f;
        rightWheel.motorTorque = rightSideTorque;
      }
    }

    public void StopWheels()
    {
      currentSpeed = GetTankSpeed();
      var engineBrakeForce = vehicleRootBody.mass * Mathf.Abs(currentSpeed) / 5f; // Lighter braking effect

      foreach (var wheel in wheelColliders)
      {
        wheel.motorTorque = 0f; // No acceleration
        wheel.brakeTorque = engineBrakeForce; // Light braking
      }
    }

    public void SetInputMovement(float val)
    {
      if (accelerationType == AccelerationType.Stop || IsBraking || Mathf.Approximately(val, 0f))
      {
        inputMovement = 0;
        return;
      }
      inputMovement = val;
    }

    /// <summary>
    /// Todo add a enum for directional forces. IE Forward, Reverse, Stop.
    /// </summary>
    /// <param name="acceleration"></param>
    /// <param name="isMovingForward"></param>
    public void UpdateAccelerationValues(AccelerationType acceleration, bool isMovingForward = true)
    {
      if (!vehicleRootBody) return;
      (lowTorque, mediumTorque, highTorque) = CalculateTorque(vehicleRootBody.mass, wheelInstances.Count, wheelRadius, wheelMass);

      accelerationType = acceleration;
      baseTorque = acceleration switch
      {
        AccelerationType.High => highTorque,
        AccelerationType.Medium => mediumTorque,
        AccelerationType.Low => lowTorque,
        AccelerationType.Stop => 0,
        _ => 0
      };

      baseAcceleration = acceleration switch
      {
        AccelerationType.High => highAcceleration,
        AccelerationType.Medium => mediumAcceleration,
        AccelerationType.Low => lowAcceleration,
        AccelerationType.Stop => 0,
        _ => 0
      };


      baseTurnForce = GetTurnForce();
      isForward = isMovingForward;

      if (!IsUsingEngine)
      {
        wheelColliders.ForEach(x =>
        {
          x.brakeTorque = 50f;
        });
      }
      else
      {
        wheelColliders.ForEach(x =>
        {
          x.brakeTorque = 0f;
        });
      }
    }

    [UsedImplicitly]
    public void SetTurnInput(float val)
    {
      inputTurnForce = val;
    }

    private void OnBrakingUpdate(bool currentVal)
    {
      if (!currentVal)
      {
        wheelColliders.ForEach(x => x.brakeTorque = 0f);
      }
    }

    [UsedImplicitly]
    public void ToggleBrake()
    {
      IsBraking = !IsBraking;
    }

    [UsedImplicitly]
    public void SetBrake(bool val)
    {
      IsBraking = val;
    }

    /// <summary>
    /// This will need logic to check if the player is the owner and if the player is controlling the vehicle actively.
    ///
    /// - To be called within VehicleMovementController.
    /// </summary>
    public void UpdateControls()
    {
      if (!UseInputControls) return;

#if UNITY_EDITOR
      var isBrakingPressed = Input.GetKeyDown(KeyCode.Space);
      if (!isBrakingPressed && isBrakePressedDown)
      {
        isBrakePressedDown = false;
      }
      if (isBrakingPressed && !isBrakePressedDown)
      {
        ToggleBrake();
        isBrakePressedDown = true;
      }
      var inputTurn = Input.GetAxis("Horizontal");
      inputTurnForce += inputTurn * Time.deltaTime;
      if (Mathf.Approximately(inputTurn, 0))
      {
        inputTurnForce = 0;
      }

      var inputVertical = Input.GetAxis("Vertical");
      SetInputMovement(inputVertical);
      UpdateAccelerationValues(accelerationType, inputMovement >= 0);
#endif
    }

    // We run this only in Unity Editor
#if UNITY_EDITOR
    private void Update()
    {
      if (!Application.isPlaying) return;
      UpdateControls();
      SetOverrides();
    }

    private void FixedUpdate()
    {
      if (!Application.isPlaying) return;

      // should not be called outside of editor. These should be optimized outside of a fixed update.
      PhysicsHelpers.UpdateRelativeCenterOfMass(vehicleRootBody, centerOfMassOffset);
      UpdateMaxRPM();
      UpdateAccelerationValues(accelerationType, inputMovement >= 0);

      // critical call, meant for all components
      VehicleMovementFixedUpdate();
    }
#endif
  }
}