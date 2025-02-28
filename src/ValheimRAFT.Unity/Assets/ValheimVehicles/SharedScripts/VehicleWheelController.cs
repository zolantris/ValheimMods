#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Enumerable = System.Linq.Enumerable;
using Vector3 = UnityEngine.Vector3;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// Controls all LandVehicle Wheel and Force Settings.
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
    private const float baseAccelerationMultiplier = 30f;
    public const float defaultTurnAccelerationMultiplier = 10f;
    private const float _lastTerrainTouchTimeExpiration = 1f;
    public static float baseTurnAccelerationMultiplier = defaultTurnAccelerationMultiplier;
    
    public static float defaultBreakForce = 500f;

    private static readonly float highAcceleration = 6 * baseAccelerationMultiplier;
    private static readonly float mediumAcceleration = 3 * baseAccelerationMultiplier;
    private static readonly float lowAcceleration = 1 * baseAccelerationMultiplier;

    [Header("USER Inputs (FOR FORCE EFFECTS")]
    [Tooltip(
      "User input")]
    public AccelerationType accelerationType = AccelerationType.Low;

    public float inputTurnForce;
    public float inputMovement;

    public float MaxWheelRPM = 3000f;
    public float turnInputOverride;

    public bool UseInputControls;

    [Header("Vehicle Generation Properties")]
    public int maxTreadLength = 20;
    public int maxTreadWidth = 8;

    [Header("Overrides")]
    public float Override_WheelBottomOffset;
    public float Override_WheelSpring;
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
    public List<GameObject> leftRenderers = new();
    public List<GameObject> rightRenderers = new();


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
    public float wheelBottomOffset = -1f;
    public float wheelRadius = 1.5f;
    public float wheelSuspensionDistance = 1.5f;
    public float wheelSuspensionSpring = 35000f;
    public float wheelSuspensionSpringDamper = 10000f;
    public float wheelSuspensionTarget = 0.4f;
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
    public float wheelMass = 500f;

    [Tooltip("Transforms")]
    public Transform wheelParent;
    public Transform treadsParent;
    public Transform rotationEnginesParent;

    public Transform treadsRightTransform;
    public Transform treadsLeftTransform;

    // public Rigidbody treadsLeftRb;
    // public Rigidbody treadsRightRb;

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
    public bool useWheelTorquePhysics;

    public Bounds currentVehicleFrameBounds = new(Vector3.zero, Vector3.one * 5);


    public int totalWheels;

    public Vector3 wheelPrefabCenterPointOffset = Vector3.zero;

    public Bounds wheelPrefabMeshSize;

    public float SuspensionDistanceMultiplier = 1f;

    public bool UseUnbalancedForce;

    public bool ShouldHideWheelRender;

    public float dynamicFriction = 0.01f;
    public float staticFriction = 0.02f;
    public PhysicMaterial treadPhysicMaterial;

    public bool IsOnGround;
    public float dampingDuration = 0.5f; // Time to fully eliminate sideways velocity
    public float angularDampingDuration = 0.25f; // Time to eliminate angular Y velocity

    public float maxHillFactorMultiplier = 3f;
    public bool ShouldApplyDownwardsForceOnSlop = true;
    // Set to false until it's stable/syncs with the treads.
    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public readonly List<WheelCollider> poweredWheels = new();

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, WheelSyncProperties> wheelSyncPropertiesMap =
      new();

    private bool _braking = true;

    // Vehicle states
    private bool _isTreadsInitialized;
    private bool _isWheelsInitialized;
    private float _lastTerrainTouchDeltaTime = 10f;

    private bool _shouldSyncVisualOnCurrentFrame = true; // wheel visual sync main controller variable
    private float angularVelocityYSmoothDamp; // For angular Y damping

    internal List<Collider> colliders = new();

    private float combinedTurnLerp;
    public WheelFrictionCurve currentForwardFriction;

    public WheelFrictionCurve currentSidewaysFriction;

    private float currentSpeed; // Speed tracking
    private float deltaRunPoweredWheels = 0f;
    private float elapsedTime = 0f;
    private Vector3 frontPosition; // Store the calculated front position for Gizmos

    private float hillFactor = 1f; // Hill compensation multiplier

    private Coroutine? initializeWheelsCoroutine;

    private bool isBrakePressedDown = true;

    internal bool isInAir = true;
    private bool isLeftForward = true;
    private bool isRightForward = true;
    private bool isTurningInPlace;
    private Vector3 lateralVelocitySmoothDamp = Vector3.zero; // For SmoothDamp velocity tracking
    // private float lerpedTurnFactor;
    private Vector3 m_angularVelocitySmoothSpeed = Vector3.zero;

    private Vector3 m_velocitySmoothSpeed = Vector3.zero;

    private float maxRotationSpeed = 1f; // Default top speed
    private float maxSpeed = 25f; // Default top speed

    public Action OnWheelsInitialized = () => {};

    internal float powerWheelDeltaInterval = 0.3f;
    internal List<GameObject> rotationEngineInstances = new();
    internal List<HingeJoint> rotatorEngineHingeInstances = new();

    private float stuckTime; // Timer for detecting if the tank is stuck
    internal MovingTreadComponent treadsLeftMovingComponent;
    internal MovingTreadComponent treadsRightMovingComponent;

    [Tooltip("Kinematic Objects and Colliders")]
    internal Rigidbody vehicleRootBody;
    internal List<WheelCollider> wheelColliders = new();

    internal List<GameObject> wheelInstances = new();
    public bool IsVehicleReady => _isWheelsInitialized && _isTreadsInitialized && vehicleRootBody; // important catchall for preventing fixedupdate physics from being applied until the vehicle is ready.
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

    private void OnCollisionEnter(Collision collision)
    {
      // Skip if there are no contact points (avoids unnecessary processing)
      if (collision.contactCount == 0) return;

      if (collision.collider.gameObject.layer == LayerHelpers.TerrainLayer)
      {
        _lastTerrainTouchDeltaTime = 0f;
      }

#if VALHEIM_RAFT
      if (collision.collider.GetComponentInParent<Character>() != null)
      {
        Debug.Log("ValheimVehicles.WheelController Hit a character!");
      }
#endif

      // Get first contact point and its collider
      var contact = collision.GetContact(0);
      var ownCollider = contact.thisCollider;

      // Skip collisions that are not from "convexHull" parts
      if (ownCollider == null || !ownCollider.name.StartsWith("convexHull")) return;

      // Compute the front position using the tank's bounds
      var frontPosition = transform.position + transform.forward * (currentVehicleFrameBounds.extents.z + 0.5f);

      // Only apply climbing force if the collision happens at the front of the tank
      if (Vector3.Dot(transform.forward, (contact.point - transform.position).normalized) > 0.5f)
      {
        ApplyClimbForce(frontPosition);
      }
    }
    private void OnCollisionStay(Collision collision)
    {
      if (LayerHelpers.IsContainedWithinMask(collision.gameObject.layer, LayerHelpers.PhysicalLayers))
      {
        _lastTerrainTouchDeltaTime = 0f;
      }
    }

    /// <summary>
    /// FixedUpdate Ground checks
    /// </summary>
    public void UpdateIsOnGround()
    {
      // for ground check
      if (_lastTerrainTouchDeltaTime < _lastTerrainTouchTimeExpiration)
      {
        _lastTerrainTouchDeltaTime += Time.fixedDeltaTime;
      }

      IsOnGround = _lastTerrainTouchDeltaTime < _lastTerrainTouchTimeExpiration;
    }

    private void ApplyClimbForce(Vector3 frontPosition)
    {
      // Apply force upwards & forward to push the tank over the obstacle
      vehicleRootBody.AddForceAtPosition(transform.up * 8000f, frontPosition, ForceMode.Force);
    }
    // Draw debug visualization
    // private void OnDrawGizmos()
    // {
    //   if (!Application.isPlaying) return;
    //
    //   Gizmos.color = Color.red; // Set Gizmo color to red for visibility
    //   Gizmos.DrawSphere(frontPosition, 0.2f); // Draw a small sphere at frontPosition
    //   Gizmos.DrawLine(frontPosition, frontPosition + transform.forward * 2f); // Draw ray forward
    // }

    private void AlignWheelsWithTreads(Bounds bounds)
    {
      var leftIndex = 0;
      var rightIndex = 0;

      // Physics.SyncTransforms();

      // TODO this needs to be fixed to be relative position due to how the position can mismatch we need to calculate based on local bounds of the tread or at least rotated based on the parent
      var sizePerIndex = bounds.size.z / (left.Length - 1);
      // Sync position correctly because it's pretty complicated getting these wheels to align well with treads center. Requires treads to be generated

      var wheelOffsetLeft = transform.InverseTransformPoint(treadsLeftTransform.position);
      var wheelOffsetRight = transform.InverseTransformPoint(treadsRightTransform.position);

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

        var treadsAnchorLeftLocalPosition = new Vector3(bounds.min.x, bounds.min.y + wheelBottomOffset, bounds.center.z);
        var treadsAnchorRightLocalPosition = new Vector3(bounds.max.x, bounds.min.y + wheelBottomOffset, bounds.center.z);

        if (leftJoint && rightJoint)
        {
          leftJoint.connectedBody = null;
          rightJoint.connectedBody = null;
          leftJoint.connectedAnchor = Vector3.zero;
          rightJoint.connectedAnchor = Vector3.zero;
        }

        // var localPoint = vehicleRootBody.transform.InverseTransformPoint();

        treadsLeftTransform.localRotation = Quaternion.identity;
        treadsRightTransform.localRotation = Quaternion.identity;
        treadsLeftTransform.position = vehicleRootBody.transform.TransformPoint(treadsAnchorLeftLocalPosition);
        treadsRightTransform.position = vehicleRootBody.transform.TransformPoint(treadsAnchorRightLocalPosition);

        // wheelParent.transform.localPosition = new Vector3(0, bounds.min.y + wheelBottomOffset, 0);
        // treadsLeft.rotation = vehicleRootBody.transform.rotation;
        // treadsRight.rotation = vehicleRootBody.transform.rotation;

        // if (leftJoint && rightJoint)
        // {
        //   ConfigureJoint(leftJoint, vehicleRootBody, treadsLeftRb, treadsAnchorLeftLocalPosition);
        //   ConfigureJoint(rightJoint, vehicleRootBody, treadsRightRb, treadsAnchorRightLocalPosition);
        // }
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

      // var collidersRight = treadsRightMovingComponent.GetComponentsInChildren<Collider>();
      // var collidersLeft = treadsLeftMovingComponent.GetComponentsInChildren<Collider>();
      // colliders.AddRange(collidersLeft);
      // colliders.AddRange(collidersRight);

      // todo ensure colliders are then ignored upstream. We want the treads to be able to hit the player or at least a convex collider variant to do this.

      // if (treadsRightRb && treadsLeftRb)
      // {
      // PhysicsHelpers.UpdateRelativeCenterOfMass(treadsLeftRb, centerOfMassOffset);
      // PhysicsHelpers.UpdateRelativeCenterOfMass(treadsRightRb, centerOfMassOffset);
      // }
    }
    public void SmoothAngularVelocity()
    {
      vehicleRootBody.maxAngularVelocity = maxRotationSpeed;

      if (Mathf.Abs(vehicleRootBody.angularVelocity.y) > maxRotationSpeed * 0.75f || !IsUsingEngine)
      {
        vehicleRootBody.angularVelocity = Vector3.SmoothDamp(vehicleRootBody.angularVelocity, new Vector3(vehicleRootBody.angularVelocity.x, vehicleRootBody.angularVelocity.y * 0.75f, vehicleRootBody.angularVelocity.z), ref m_angularVelocitySmoothSpeed, 2f);
      }
      else
      {
        m_angularVelocitySmoothSpeed = Vector3.Lerp(m_angularVelocitySmoothSpeed, Vector3.zero, Time.fixedDeltaTime * 10f);
      }
    }

    private void DampenAngularYVelocity()
    {
      var angularVelocity = vehicleRootBody.angularVelocity;

      // Smoothly damp only the Y-axis angular velocity
      var newAngularY = Mathf.SmoothDamp(angularVelocity.y, 0, ref angularVelocityYSmoothDamp, angularDampingDuration);

      // Apply the new angular velocity while keeping X & Z untouched
      vehicleRootBody.angularVelocity = new Vector3(angularVelocity.x, newAngularY, angularVelocity.z);
    }
    private void DampenSidewaysVelocity()
    {
      var velocity = vehicleRootBody.velocity;
      var forward = transform.forward;

      // Ensure forward direction is only in XZ plane
      forward.y = 0;
      forward.Normalize();

      // Get forward velocity (in XZ plane)
      var forwardVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
      forwardVelocity = Vector3.Project(forwardVelocity, forward);

      // Extract sideways velocity (X and Z only)
      var sidewaysVelocity = new Vector3(velocity.x, 0, velocity.z) - forwardVelocity;

      // Smoothly damp sideways velocity to zero over `dampingDuration`
      var newSidewaysVelocity = Vector3.SmoothDamp(sidewaysVelocity, Vector3.zero, ref lateralVelocitySmoothDamp, dampingDuration);

      // Preserve Y velocity (gravity & jumps)
      vehicleRootBody.velocity = new Vector3(forwardVelocity.x + newSidewaysVelocity.x, velocity.y, forwardVelocity.z + newSidewaysVelocity.z);
    }
    private void ApplyDecreasingForce()
    {
      var smoothTime = IsBraking ? 2f : 6f;
      if (Mathf.Abs(currentSpeed) > 0)
      {
        vehicleRootBody.velocity = Vector3.SmoothDamp(vehicleRootBody.velocity, Vector3.zero, ref m_velocitySmoothSpeed, smoothTime);
      }
      else
      {
        m_velocitySmoothSpeed = Vector3.Lerp(m_velocitySmoothSpeed, Vector3.zero, Time.fixedDeltaTime * 10f);
      }
      SmoothAngularVelocity();
    }

    public float currentLeftForce = 0f;
    public float currentRightForce = 0f; 

    public static float minTreadDistances = 0.1f;
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
        currentLeftForce = 0;
        currentRightForce = 0;
        ApplyDecreasingForce();

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
      combinedTurnLerp = turnForceLerp * baseTorqueTurnLerp;

      var leftForce = 0f;
      var rightForce = 0f;

      if (m_steeringType == SteeringType.Magic)
      {
        leftForce = combinedTurnLerp;
      }

      if (m_steeringType == SteeringType.Differential)
      {
        var dt = Time.fixedDeltaTime;
        var baseTurnRate = Mathf.Lerp(baseAccelerationMultiplier, 0f, Mathf.Clamp01(baseAcceleration / baseTurnAccelerationMultiplier));
        // leftForce = (inputMovement + combinedTurnLerp) * baseTorque * hillFactor;
        // rightForce = (inputMovement - combinedTurnLerp) * baseTorque * hillFactor;
        leftForce = (inputMovement + combinedTurnLerp) * (baseAcceleration + baseTurnRate) * hillFactor * dt;
        rightForce = (inputMovement - combinedTurnLerp) * (baseAcceleration + baseTurnRate) * hillFactor * dt;
      }


      if (UseUnbalancedForce)
      {
        var unbalancedForceLeft = Mathf.Lerp(1, 1 + inputTurnForce, Mathf.Abs(inputTurnForce));
        var unbalancedForceRight = Mathf.Lerp(1, 1 - inputTurnForce, Mathf.Abs(inputTurnForce));
        leftForce *= unbalancedForceLeft;
        rightForce *= unbalancedForceRight;
      }

      if (stuckTime > 0)
      {
        leftForce *= 1 + stuckTime;
        rightForce *= 1 + stuckTime;
      }

      // var upwardsForce = transform.up * baseTorque * 0.01f;

      // todo make this apply at front and back wheels depending on direction so the vehicle can ascend over terrain easily and not require wheels as much..
      var upwardsForce = Vector3.zero;

      var deltaTreads = Vector3.Distance(rightTreadPos, leftTreadPos);

      if (deltaTreads < minTreadDistances)
      {
        currentLeftForce = 0;
        currentRightForce = 0;
        // vehicle is not ready. The treads are too close.
        return;
      }

      vehicleRootBody.AddForceAtPosition(forward * leftForce + upwardsForce, treadsLeftTransform.position, ForceMode.Acceleration);
      vehicleRootBody.AddForceAtPosition(forward * rightForce + upwardsForce, treadsRightTransform.position, ForceMode.Acceleration);

      currentLeftForce = leftForce;
      currentRightForce = rightForce;
      // this removes sideways velocities quickly to prevent issues with vehicle at higher speeds turning.
      DampenSidewaysVelocity();
      DampenAngularYVelocity();
    }

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
    private void SetupSingleTread(GameObject treadObj, ref MovingTreadComponent movingTreadComponent)
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

      // shared dynamic physicMaterial. This can be different per vehicle used.
      treadPhysicMaterial = new PhysicMaterial("TreadPhysicMaterial")
      {
        dynamicFriction = IsBraking ? 0.5f : dynamicFriction,
        staticFriction = IsBraking ? 0.5f : staticFriction,
        bounciness = 0f,
        bounceCombine = PhysicMaterialCombine.Minimum,
        frictionCombine = PhysicMaterialCombine.Minimum
      };

      SetupSingleTread(treadsRightTransform.gameObject, ref treadsRightMovingComponent);
      SetupSingleTread(treadsLeftTransform.gameObject, ref treadsLeftMovingComponent);

      _isTreadsInitialized = true;
    }

    public void ConfigureJoint(ConfigurableJoint joint, Rigidbody vehicleBody, Rigidbody currentRB, Vector3 localPosition)
    {
      if (!currentRB.isKinematic)
      {
        currentRB.isKinematic = true;
      }

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

      currentRB.isKinematic = false;
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
      var tempList = Enumerable.ToList(items);
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
      wheelSyncPropertiesMap.Clear();
      poweredWheels.Clear();
      wheelColliders.Clear();
      colliders.Clear();

      // non-allocating.
      right = Array.Empty<WheelCollider>();
      left = Array.Empty<WheelCollider>();


      rear.Clear();
      front.Clear();
      rightRenderers.Clear();
      leftRenderers.Clear();
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
    public void VehicleMovementFixedUpdateOwnerClient()
    {
      if (!IsVehicleReady) return;
      _shouldSyncVisualOnCurrentFrame = true;

      UpdateIsOnGround();

      isTurningInPlace = Mathf.Approximately(inputMovement, 0f) && Mathf.Abs(inputTurnForce) > 0f;
      currentSpeed = GetTankSpeed();

      if (!IsOnGround)
      {
        ApplyDownforce();
      }

      HandleObstacleClimb();

      if (useDirectTreadPhysics)
      {
        ApplyTreadForces();
      }
    }

    public void VehicleMovementFixedUpdateAllClients()
    {
      SyncWheelAndTreadVisuals();
    }

    /// <summary>
    /// Compute the true bottom of the wheel visually to match the wheelcollider that could be larger.
    /// </summary>
    /// <param name="wheelCollider"></param>
    /// <param name="wheelSync"></param>
    private void AlignVisualWheels(WheelCollider wheelCollider, WheelSyncProperties wheelSync)
    {
      if (wheelCollider == null) return;

      wheelCollider.GetWorldPose(out var colliderPosition, out _);

      // todo not sure why wheelX require 1/4 divisor as it is already centered and should not be changing the wheel alignment.
      var wheelX = colliderPosition.x + wheelSync.wheelMeshScale.x / 4;
      var wheelY = colliderPosition.y - wheelRadius + wheelSync.wheelMeshScale.y / 2;
      var bottomContactPoint = new Vector3(wheelX, wheelY, colliderPosition.z);

      // Compute final position: Align the **bottom of the visual wheel** with the contact point
      // visualWheel.position = bottomContactPoint + visualWheel.up * visualRadius;
      // wheelSync.wheelVisual.transform.position = colliderPosition - (2.5f - 1f) * Vector3.up;
      wheelSync.wheelVisual.transform.position = bottomContactPoint;

      // **Correct rotation: Prevent tilting, but allow rolling**
      // var forward = wheelCollider.transform.forward; // Preserve rolling direction
      // var up = -Vector3.up; // Keep the wheel upright (prevent unwanted tilts)
      // visualWheel.rotation = Quaternion.LookRotation(forward, up);
    }
    /// <summary>
    /// Responsible for matching wheels and treads visually with their speed. This is throttled to make the visual performant across frames.
    /// </summary>
    public void SyncWheelAndTreadVisuals()
    {
      if (!_shouldSyncVisualOnCurrentFrame) return;

      if (treadsLeftMovingComponent && treadsRightMovingComponent)
      {
        // var lerpTime = Mathf.Clamp01(Mathf.Abs(inputMovement) * baseAcceleration + combinedTurnLerp * baseTurnAccelerationMultiplier / highAcceleration);
        var clampedSpeed = Mathf.Clamp(Mathf.Abs(currentLeftForce), 0, 8f);
        // var lerpSpeed = Mathf.Lerp(0f, 8f, lerpTime);
        if ((inputMovement > 0 || inputTurnForce > 0) && Mathf.Approximately(clampedSpeed, 0))
        {
          clampedSpeed = 0.5f;
        }
        treadsLeftMovingComponent.SetSpeed(clampedSpeed);
        treadsRightMovingComponent.SetSpeed(clampedSpeed);
        UpdateSteeringTreadDirectionVisuals();
      }

      if (ShouldSyncWheelsToCollider)
      {
        wheelColliders.ForEach(x =>
        {
          if (wheelSyncPropertiesMap.TryGetValue(x,
                out var wheelSyncProperties))
          {
            SyncWheelVisual(x, wheelSyncProperties);
          }
        });
      }

      // syncs the treads to align with the vehicle

      // if (leftRenderers.Count > 0 && rightRenderers.Count > 0)
      // {
      //   var averageYLeft = Enumerable.ToList(Enumerable.Select(leftRenderers, x => x.transform.position)).Average();
      //   var averageYRight = Enumerable.ToList(Enumerable.Select(rightRenderers, x => x.transform.position)).Average();
      //
      //   var leftPos = treadsLeftTransform.position;
      //   leftPos.y = averageYLeft.y;
      //   treadsLeftTransform.position = leftPos;
      //
      //   var rightPos = treadsRightTransform.position;
      //   rightPos.y = averageYRight.y;
      //   treadsRightTransform.position = rightPos;
      // }

      _shouldSyncVisualOnCurrentFrame = false;
    }

    /// <summary>
    /// This method is meant to constrain the wheel bounds so they are within realistic levels.
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public Bounds GetVehicleFrameBounds(Bounds bounds)
    {
      if (bounds.size.x < 4f || bounds.size.y < 4f || bounds.size.z < 4f)
      {
        var size = new Vector3(Mathf.Max(bounds.size.x, 2f), Mathf.Max(bounds.size.y, 0.05f), Mathf.Max(bounds.size.z, 2f));
        var center = bounds.center;
        bounds = new Bounds(center, size);
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
    public void Initialize(Bounds? bounds)
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
      // GenerateWheelSets(currentVehicleFrameBounds);
      // SetupWheels();

      // physics
      UpdateAccelerationValues(accelerationType, isForward);
      ApplyFrictionValuesToAllWheels();

      // AlignWheelsWithTreads(vehicleFrameBounds);
      _isWheelsInitialized = true;
      OnWheelsInitialized();
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

        var isRight = wheelCollider.transform.parent.name.Contains("right");
        if (isRight && !Enumerable.Contains(right, wheelCollider))
        {
          tmpRight.Add(wheelCollider);
        }
        if (isRight)
        {
          var wheelMesh = wheelCollider.transform.parent.Find("wheel_mesh");
          if (wheelMesh && !rightRenderers.Contains(wheelMesh.gameObject))
          {
            rightRenderers.Add(wheelMesh.gameObject);
          }
        }

        var isLeft = wheelCollider.transform.parent.name.Contains("left");
        if (isLeft && !Enumerable.Contains(left, wheelCollider))
        {
          tmpLeft.Add(wheelCollider);
        }

        if (isLeft)
        {
          var wheelMesh = wheelCollider.transform.parent.Find("wheel_mesh");
          if (wheelMesh && !leftRenderers.Contains(wheelMesh.gameObject))
          {
            leftRenderers.Add(wheelMesh.gameObject);
          }
        }

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

      if (Override_WheelSpring != 0)
      {
        wheelSuspensionSpring = Override_WheelSpring;
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
      var wheelLeftBounds = new Bounds(new Vector3(bounds.min.x, bounds.min.y + wheelRadius, bounds.center.z), treadsLeftMovingComponent.localBounds.size);
      var wheelRightBounds = new Bounds(new Vector3(bounds.max.x + 1f, bounds.min.y + wheelRadius, bounds.center.z), treadsRightMovingComponent.localBounds.size);
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
            wheelInstance = Instantiate(wheelPrefab, treadsParent);
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
      // var meshBounds = GetLocalMeshBounds(wheelInstance);

      // Convert local center to world space
      // var worldCenter = wheelInstance.transform.TransformPoint(wheelInstance.transform.localPosition);

      // Calculate local position using local bounds
      var xPos = isLeft ? treadBounds.min.x : treadBounds.max.x;
      var zPos = treadBounds.max.z * 0.95f - spacing * index * 0.95f;
      var yPos = treadBounds.center.y + wheelBottomOffset;

      return new Vector3(xPos, yPos, zPos);
    }

    public float GetWheelRadiusScalar()
    {
      if (!_isWheelsInitialized || totalWheels == 0) return 1;
      // return 1;
      // return wheelRadius / wheelBaseRadiusScale;
      return 1;
    }

    private void SetWheelProperties(GameObject wheelObj)
    {
      if (!vehicleRootBody) return;
      var wheelColliderTransform =
        wheelObj.transform.Find("wheel_collider");
      if (!wheelColliderTransform) return;

      var wheelCollider = wheelColliderTransform.GetComponent<WheelCollider>();

      wheelCollider.mass = wheelMass;
      // Larger radius is required for traversing valheim terrain. Small wheels will disappear.
      wheelCollider.radius = wheelColliderRadius;

      // Dynamically adjust targetPosition based on weight & radius
      // var massFactor = Mathf.Clamp01(vehicleRootBody.mass / 2000f); // Normalize weight influence
      // var radiusFactor = Mathf.Clamp01(wheelColliderRadius / 1f); // Normalize wheel size influence


      // wheelCollider.suspensionDistance = wheelColliderRadius * 2f * SuspensionDistanceMultiplier;
      // wheelCollider.suspensionDistance = Mathf.Max(wheelColliderRadius / 2f, wheelSuspensionDistance);
      wheelCollider.suspensionDistance = wheelSuspensionDistance;
      wheelCollider.forceAppPointDistance = wheelColliderRadius * 2f;

      // wheelCollider.wheelDampingRate = Override_wheelDampingRate;
      var suspensionSpring = wheelCollider.suspensionSpring;

      suspensionSpring.damper = wheelSuspensionSpringDamper;
      suspensionSpring.spring = wheelSuspensionSpring;
      suspensionSpring.targetPosition = wheelSuspensionTarget;

      wheelCollider.suspensionSpring = suspensionSpring;

      var wheelScalar = GetWheelRadiusScalar();

      if (!Mathf.Approximately(wheelScalar, 1f))
      {
        // wheelVisual.transform.localScale = new Vector3(wheelScalar * wheelMeshLocalScale.x, wheelScalar * wheelMeshLocalScale.y, wheelScalar * wheelMeshLocalScale.z);
      }

      if (!wheelSyncPropertiesMap.ContainsKey(wheelCollider))
        wheelSyncPropertiesMap.Add(wheelCollider, new WheelSyncProperties(wheelObj));
    }

    public void SyncWheelVisual(WheelCollider wheel, WheelSyncProperties wheelSyncProperties)
    {
      if (!_shouldSyncVisualOnCurrentFrame) return;

      if (ShouldHideWheelRender)
      {
        wheelSyncProperties.wheelVisual.gameObject.SetActive(false);
        return;
      }
      if (!ShouldHideWheelRender && !wheelSyncProperties.wheelVisual.gameObject.activeInHierarchy)
      {
        wheelSyncProperties.wheelVisual.gameObject.SetActive(true);
      }
      if (!wheelSyncProperties.wheelVisual || !wheelSyncProperties.wheelBottom || !wheelSyncProperties.wheelCenter)
      {
        wheelSyncPropertiesMap.Remove(wheel);
        return;
      }

      AlignVisualWheels(wheel, wheelSyncProperties);
      RotateOnXAxis(wheel, wheelSyncProperties.wheelVisual.transform);

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
      wheelCollider.GetWorldPose(out var position, out var rotation);
      if (ShouldSyncWheelsToCollider)
      {
        if (wheelRadius > 2)
        {
          position.y -= wheelRadius - 1;
        }
        wheelTransform.position = position;
      }
      wheelVisual.rotation = rotation;
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
          isLeftForward = isForward;
          isRightForward = !isForward;
          break;
        case < -0.2f:
          isLeftForward = !isForward;
          isRightForward = isForward;
          break;
        default:
          isLeftForward = isForward;
          isRightForward = isForward;
          break;
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
        hillFactor = Mathf.Lerp(1f, maxHillFactorMultiplier, Mathf.Clamp01(slope / maxSlope));
      }
      else
      {
        hillFactor = Mathf.Lerp(1f, 1f / maxHillFactorMultiplier, Mathf.Clamp01(-slope / maxSlope));
      }

      if (Mathf.Abs(slope) > 0.3f)
      {
        vehicleRootBody.AddForceAtPosition(vehicleRootBody.transform.up * Gravity, vehicleRootBody.worldCenterOfMass, ForceMode.Acceleration);
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
      if (wheelColliders.Count <= 0) return;
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
      vehicleRootBody.AddForce(-Vector3.up * Gravity, ForceMode.Acceleration);
      // treadsRightRb.AddForce(-Vector3.up * downforceAmount, ForceMode.Acceleration);
      // treadsLeftRb.AddForce(-Vector3.up * downforceAmount, ForceMode.Acceleration);
    }
    private void ApplyFrictionToWheelCollider(WheelCollider wheel, float speed)
    {
      var frictionMultiplier = Mathf.Clamp(10f / wheelColliders.Count, 0.7f, 1.5f);

      var adjustedStiffness = Mathf.Lerp(2.5f, 3.5f, speed / topSpeed); // Adaptive stiffness based on speed

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

      wheel.forwardFriction = currentForwardFriction;
      wheel.sidewaysFriction = currentSidewaysFriction;
    }


    private void ApplyFrictionValuesToAllWheels()
    {
      if (!vehicleRootBody) return;
      wheelColliders.ForEach(x => ApplyFrictionToWheelCollider(x, currentSpeed));
    }

    private void AdjustSuspensionForTank()
    {
      var isApplyingForce = Mathf.Abs(inputMovement) > 0.1f || Mathf.Abs(inputTurnForce) > 0.1f;
      var isMoving = vehicleRootBody.velocity.magnitude > 0.5f;

      // If the tank is applying force but isn't moving, increase stuck timer
      if (isApplyingForce && !isMoving)
      {
        stuckTime += Time.fixedDeltaTime;
      }
      else
      {
        stuckTime = 0f; // Reset if moving
      }

      var stuckMultiplier = Mathf.Clamp01(stuckTime / 3f); // Smooth transition over 3 seconds

      foreach (var wheel in wheelColliders)
      {
        var suspensionSpring = wheel.suspensionSpring;

        // wheel.transform.localPosition = Vector3.Lerp(wheel.transform.localPosition, new Vector3(0, Mathf.Lerp(0, -0.5f, stuckMultiplier), 0), Time.fixedDeltaTime); // Move wheels down slightly
        // Adjust spring force dynamically based on mass
        suspensionSpring.spring = Mathf.Clamp(vehicleRootBody.mass * 10f, 35000f, 50000f);
        suspensionSpring.damper = Mathf.Lerp(1500f, 1500f, Mathf.Clamp01(vehicleRootBody.velocity.magnitude / maxSpeed));

        // Lower targetPosition when stuck to push wheels down for more grip
        // suspensionSpring.targetPosition = Mathf.Lerp(suspensionSpring.targetPosition, Mathf.Lerp(0.1f, 0.5f, stuckMultiplier), Time.fixedDeltaTime);

        wheel.suspensionSpring = suspensionSpring;

      }
    }

    private void HandleObstacleClimb()
    {
      // RaycastHit hit;
      // frontPosition = transform.position + transform.forward * 2f + Vector3.up * 1f; // Store position for Gizmos

      // var hasHit = Physics.Raycast(frontPosition, transform.forward, out hit, 2f);
      // Detect if the front of the tank is hitting an obstacle
      // if (hasHit)
      // {
      // var climbForce = transform.up * 10000f + transform.forward * 5000f;
      // vehicleRootBody.AddForceAtPosition(climbForce, frontPosition, ForceMode.Force);
      // }
    }

    // private void ApplyTorque(float move, float turn)
    // {
    //   if (Mathf.Approximately(move, 0f) && Mathf.Approximately(turn, 0f))
    //   {
    //     StopWheels();
    //     return;
    //   }
    //
    //   var lerpedTurnFactor = Mathf.Lerp(lerpedTurnFactor, Mathf.Abs(turn), Time.fixedDeltaTime * 5f);
    //   var speed = vehicleRootBody.velocity.magnitude;
    //   var angularSpeed = Mathf.Abs(vehicleRootBody.angularVelocity.y); // Get current rotation speed
    //
    //   var targetSpeed = move * maxSpeed;
    //   var torqueBoost = hillFactor * uphillTorqueMultiplier;
    //
    //   // if (move > 0 && Vector3.Dot(vehicleRootBody.velocity, transform.forward) < 0)
    //   // {
    //   //   torqueBoost += downhillResistance;
    //   // }
    //
    //   if (Mathf.Abs(speed - targetSpeed) < 1f)
    //   {
    //     torqueBoost *= 0.85f;
    //   }
    //
    //   var minTorque = baseTorque * 0.2f;
    //   var forwardTorque = Mathf.Max((baseTorque + torqueBoost) * move, minTorque);
    //
    //   var leftSideTorque = forwardTorque;
    //   var rightSideTorque = forwardTorque;
    //
    //   // **New: Apply Turn Boost if Rotating in Place**
    //   if (Mathf.Approximately(move, 0f) && Mathf.Abs(turn) > 0f)
    //   {
    //     // var maxTurnSpeed = 1.5f; // Max angular velocity before limiting turn boost
    //     var turnBoost = Mathf.Clamp(maxRotationSpeed - angularSpeed, 1f, maxRotationSpeed); // Increase torque at low speeds
    //
    //     leftSideTorque = turn > 0 ? -baseTorque * turnBoost : baseTorque * turnBoost;
    //     rightSideTorque = turn > 0 ? baseTorque * turnBoost : -baseTorque * turnBoost;
    //   }
    //   else if (Mathf.Abs(turn) >= 0.5f)
    //   {
    //     leftSideTorque = turn > 0 ? baseTorque : -baseTorque;
    //     rightSideTorque = turn > 0 ? -baseTorque : baseTorque;
    //   }
    //   else if (turn != 0)
    //   {
    //     var turnStrength = Mathf.Lerp(1f, 0.6f, lerpe);
    //
    //     if (turn > 0)
    //     {
    //       leftSideTorque *= 1f;
    //       rightSideTorque *= turnStrength;
    //     }
    //     else
    //     {
    //       leftSideTorque *= turnStrength;
    //       rightSideTorque *= 1f;
    //     }
    //   }
    //
    //   // Apply torques to wheels
    //   foreach (var leftWheel in left)
    //   {
    //     leftWheel.brakeTorque = 0f;
    //     leftWheel.motorTorque = leftSideTorque;
    //   }
    //   foreach (var rightWheel in right)
    //   {
    //     rightWheel.brakeTorque = 0f;
    //     rightWheel.motorTorque = rightSideTorque;
    //   }
    // }

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
      if (treadPhysicMaterial)
      {
        treadPhysicMaterial.dynamicFriction = currentVal ? 0.5f : dynamicFriction;
        treadPhysicMaterial.staticFriction = currentVal ? 0.5f : staticFriction;
      }
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
      UpdateMaxRPM();
      UpdateAccelerationValues(accelerationType, inputMovement >= 0);

      // critical call, meant for all components
      VehicleMovementFixedUpdateOwnerClient();
      VehicleMovementFixedUpdateAllClients();
    }
#endif
  }

  public struct WheelSyncProperties
  {
    public Transform wheelBottom;
    public Transform wheelCenter;
    public Transform wheelVisual;
    public Vector3 wheelMeshScale;

    public WheelSyncProperties(GameObject wheelObj)
    {
      wheelVisual =
        wheelObj.transform.Find("wheel_mesh");
      wheelBottom =
        wheelObj.transform.Find("bottom_point");
      wheelCenter =
        wheelObj.transform.Find("center_point");

      var meshFilter = wheelVisual.Find("wheel_rotator").GetComponent<MeshFilter>();
      var worldScale = Vector3.Scale(meshFilter.sharedMesh.bounds.size, meshFilter.transform.lossyScale);
      wheelMeshScale = worldScale;
    }
  }
}
