#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
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

    // speed overrides
    public static float Override_WheelBottomOffset = 0;
    public static float Override_WheelRadius = 0;
    public static float Override_WheelSuspensionDistance = 0;
    public static float Override_TopSpeed = 0;
    public static float wheelDamping = 2f;

    // wheel regeneration unstable so leave this one for now
    public static bool shouldCleanupPerInitialize = true;

    private static Vector3 wheelMeshLocalScale = new(3f, 0.3f, 3f);

    public static float defaultBreakForce = 500f;

    [Tooltip("Top speed of the tank in m/s.")]
    public float topSpeed = 50.0f;

    [Tooltip(
      "For tanks with front/rear wheels defined, this is how far those wheels turn.")]
    public float steeringAngle = 30.0f;

    [Tooltip("Power of any wheel listed under powered wheels.")]
    public float baseMotorTorque = 10.0f;

    [Tooltip(
      "Turn rate that is \"magically\" applied regardless of what the physics state of the tank is.")]
    public float magicTurnRate = 45.0f;

    public float wheelMass = 500f;

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

    public int minimumWheelSets = 2; // Minimum number of wheel sets
    public int maxWheelSets = 5; // Minimum number of wheel sets
    public float
      wheelSetIncrement = 10f;
    public float axelPadding = 0.5f; // Extra length on both sides of axel

    [FormerlySerializedAs("accelerationForce")]
    [Header("USER Inputs (FOR FORCE EFFECTS")]
    [Tooltip(
      "User input")]
    public AccelerationType accelerationType = AccelerationType.Low;

    [Tooltip("This torque value is scaled for wheelmass radius and vehicle mass")]
    public float baseTorque;

    public float inputTurnForce;
    public float inputMovement;


    public float MaxWheelRPM = 3000f;
    public float turnInputOverride;

    public bool isBraking = true;
    public bool UseManualControls;

    public bool hasInitialized;

    [Tooltip("Wheel properties")]
    public float wheelBottomOffset;
    public float wheelRadius = 1.5f;
    public float wheelSuspensionDistance = 1.5f;
    public float wheelSuspensionSpring = 200f;
    public GameObject treadsPrefab;

    [Tooltip("Transforms")]
    public Transform wheelParent;
    public Transform treadsParent;
    public Transform rotationEnginesParent;
    public Transform treadsRight;
    public Transform treadsLeft;
    [FormerlySerializedAs("additionalBreakForce")]
    public float additionalBrakeForce = defaultBreakForce;
    public float currentMotorTorqueRight;
    public float currentMotorTorqueLeft;
    public float currentBreakTorqueLeft;
    public float currentBreakTorqueRight;

    public bool IsControlling;

    [Header("Wheel Physics settings")]
    [Tooltip("Wheel configuration")]
    public float wheelPowerStiffness = 2f;
    public float wheelPowerExtremumSlip = 0.4f;
    public float wheelPowerExtremumValue = 1.5f;
    public float wheelPowerAsymptoteSlip = 2.0f;
    public float wheelPowerAsymptoteValue = 0.5f;

    public float wheelPowerSidewaysStiffness = 1.2f;
    public float wheelPowerSidewaysExtremumSlip = 0.5f;
    public float wheelPowerSidewaysExtremumValue = 0.7f;
    public float wheelPowerSidewaysAsymptoteSlip = 1.5f;
    public float wheelPowerSidewaysAsymptoteValue = 0.3f;

    public float wheelCenterStiffness = 0.1f;
    public float wheelCenterExtremumSlip = 0.2f;
    public float wheelCenterAsymptoteSlip = 0.2f;
    public float wheelCenterSidewaysStiffness = 0.1f;
    public float wheelCenterSidewaysExtremumSlip = 0.2f;
    public float wheelCenterSidewaysAsymptoteSlip = 0.2f;

    [Header("Wheel Acceleration internal Values")]
    public float lowTorque;
    public float mediumTorque;
    public float highTorque;

    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public readonly List<WheelCollider> poweredWheels = new();

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, Transform>
      WheelcollidersToWheelVisualMap =
        new();
    private bool _hasSyncedWheelsOnCurrentFrame;
    internal List<Collider> colliders = new();

    private float currentSpeed; // Speed tracking
    private float deltaRunPoweredWheels = 0f;
    private float hillFactor = 1f; // Hill compensation multiplier

    private Coroutine? initializeWheelsCoroutine;

    private bool isBrakePressedDown = true;
    private bool isForward = true;
    private bool isLeftForward = true;
    private bool isRightForward = true;
    private float lerpedTurnFactor;
    private float maxSpeed = 25f; // Default top speed
    internal MovingTreadComponent movingTreadLeft;

    internal MovingTreadComponent movingTreadRight;

    // private void AdjustVehicleToGround(Bounds bounds)
    // {
    //   if (!rigid) return;
    //
    //   var terrainMask = LayerMask.GetMask("terrain"); // Ensure "Terrain" is a valid layer name in your project.
    //   var highestGroundPoint = float.MinValue;
    //   var isAboveTerrain = false;
    //   var maxWheelRadius = 0.5f;
    //
    //   foreach (var wheel in wheelColliders)
    //   {
    //     var wheelPosition = wheel.transform.position;
    //
    //     // Perform raycasts above and below the wheel position
    //     if (Physics.Raycast(wheelPosition, Vector3.down, out var hitBelow, 80, terrainMask))
    //     {
    //       highestGroundPoint = Mathf.Max(highestGroundPoint, hitBelow.point.y);
    //     }
    //
    //     if (Physics.Raycast(wheelPosition, Vector3.up, 80, terrainMask))
    //     {
    //       isAboveTerrain = true;
    //     }
    //
    //     maxWheelRadius = Mathf.Max(maxWheelRadius, wheel.radius);
    //   }
    //
    //   // If terrain is detected both above and below, skip adjustment
    //   if (isAboveTerrain)
    //   {
    //     Debug.Log("Vehicle is trapped between terrain above and below. Skipping adjustment.");
    //     return;
    //   }
    //
    //   var offsetY = maxWheelRadius + highestGroundPoint - bounds.min.y + wheelBottomOffset; // Align the bottom of the vehicle to the highest ground point
    //   // Calculate the required vertical adjustment
    //   var vehiclePosition = rigid.transform.position;
    //
    //   var newPosition = new Vector3(vehiclePosition.x, vehiclePosition.y + offsetY, vehiclePosition.z);
    //
    //
    //   // todo move all objects on rigidbody upwards without causing kinematic problems.
    //
    //   // Move the rigidbody kinematically
    //   var originalKinematicState = rigid.isKinematic;
    //   rigid.isKinematic = true;
    //   rigid.transform.position = newPosition;
    //   rigid.isKinematic = originalKinematicState;
    //
    //   if (rigid.isKinematic == false)
    //   {
    //     rigid.velocity = Vector3.zero;
    //     rigid.angularVelocity = Vector3.zero;
    //   }
    // }

    internal float powerWheelDeltaInterval = 0.3f;
    internal List<GameObject> rotationEngineInstances = new();
    internal List<HingeJoint> rotatorEngineHingeInstances = new();

    private Rigidbody vehicleRootBody;

    [Tooltip("Kinematic Objects and Colliders")]
    internal List<WheelCollider> wheelColliders = new();
    public WheelFrictionCurve wheelForwardFriction = new()
    {
      extremumSlip = 0f,
      extremumValue = 1f,
      asymptoteSlip = 1f,
      asymptoteValue = 1f,
      stiffness = 1
    };
    internal List<GameObject> wheelInstances = new();
    public WheelFrictionCurve wheelSidewaysFriction = new()
    {
      extremumSlip = 0f,
      extremumValue = 1f,
      asymptoteSlip = 1f,
      asymptoteValue = 1f,
      stiffness = 1f
    };

    private void Awake()
    {
#if UNITY_EDITOR
      var ghostContainer = transform.Find("ghostContainer");
      if (ghostContainer) ghostContainer.gameObject.SetActive(false);
#endif
      wheelParent = transform.Find("wheels");
      treadsParent = transform.Find("treads");
      rotationEnginesParent = transform.Find("treads/rotation_engines");

      vehicleRootBody = GetComponent<Rigidbody>();
      var centerOfMass = transform.Find("center_of_mass");
      if (centerOfMass != null) centerOfMassTransform = centerOfMass;
      if (centerOfMassTransform == null) centerOfMassTransform = transform;

      if (!treadsRight)
      {
        treadsRight = treadsParent.Find("treads_right");
      }
      if (!treadsLeft)
      {
        treadsLeft = treadsParent.Find("treads_left");
      }
      InitTreadComponent();
    }

    private void OnEnable()
    {
      UpdateMaxRPM();
      InitTreadComponent();
    }

    private void OnDisable()
    {
      CleanupTreads();
      Cleanup();
    }

    private void InitTreadComponent()
    {
      if (!treadsRight || !treadsLeft) return;
      if (!movingTreadLeft)
      {
        movingTreadLeft = treadsLeft.GetComponent<MovingTreadComponent>();
      }
      if (!movingTreadLeft)
      {
        movingTreadLeft = treadsLeft.transform.gameObject.AddComponent<MovingTreadComponent>();
      }

      if (!movingTreadRight)
      {
        movingTreadRight = treadsRight.GetComponent<MovingTreadComponent>();
      }
      if (!movingTreadRight)
      {
        movingTreadRight = treadsRight.transform.gameObject.AddComponent<MovingTreadComponent>();
      }

      movingTreadLeft.treadParent = treadsLeft;
      movingTreadRight.treadParent = treadsRight;

      if (treadsPrefab)
      {
        movingTreadLeft.treadPrefab = treadsPrefab;
        movingTreadRight.treadPrefab = treadsPrefab;
      }
    }

    public void ConfigureJoint(ConfigurableJoint joint, Rigidbody vehicleBody, Vector3 localPosition)
    {
      joint.connectedBody = vehicleBody;
      joint.anchor = localPosition;

      // Lock rotation completely
      joint.angularXMotion = ConfigurableJointMotion.Locked;
      joint.angularYMotion = ConfigurableJointMotion.Locked;
      joint.angularZMotion = ConfigurableJointMotion.Limited;

      // Allow expansion by keeping movement flexible along the growing axis
      joint.xMotion = ConfigurableJointMotion.Locked;
      joint.yMotion = ConfigurableJointMotion.Limited; // Adjust as needed
      joint.zMotion = ConfigurableJointMotion.Locked;
    }

    /// <summary>
    /// Requires SetupWheels to be called
    /// </summary>
    /// <param name="bounds"></param>
    private void UpdateTreads(Bounds bounds)
    {
      if (!movingTreadLeft || !movingTreadRight)
      {
        InitTreadComponent();
      }

      // var wheelLocalPositionToRoot = transform.InverseTransformPoint(wheelInstances[0].transform.position);
      // var deltaWheelY = bounds.center.y - wheelLocalPositionToRoot.y - wheelRadius * 2;
      // var newCenterPosition = new Vector3(bounds.center.x, deltaWheelY, bounds.center.z);
      // var newCenterBounds = new Bounds(newCenterPosition, bounds.size);

      // var treadRightFixedJoint = treadsRight.GetComponent<FixedJoint>();
      // var treadLeftFixedJoint = treadsLeft.GetComponent<FixedJoint>();
      // treads generate from their origin point forwards so Z axis should always be lowest value
      if (treadsLeft && treadsRight)
      {
        var leftJoint = treadsLeft.GetComponent<ConfigurableJoint>();
        var rightJoint = treadsRight.GetComponent<ConfigurableJoint>();

        var treadsAnchorLeftLocalPosition = new Vector3(bounds.min.x - 0.5f, treadsLeft.localPosition.y, bounds.center.z);
        var treadsAnchorRightLocalPosition = new Vector3(bounds.max.x + 0.5f, treadsRight.localPosition.y, bounds.center.z);

        var localPosY = Vector3.up * (-bounds.extents.y + wheelBottomOffset + wheelRadius);

        treadsLeft.localPosition = treadsAnchorLeftLocalPosition - localPosY;
        treadsRight.localPosition = treadsAnchorRightLocalPosition - localPosY;

        ConfigureJoint(leftJoint, vehicleRootBody, -treadsLeft.localPosition);
        ConfigureJoint(rightJoint, vehicleRootBody, -treadsRight.localPosition);
        // var leftRb = treadsAnchorLeft.GetComponent<Rigidbody>();
        // var rightRb = treadsAnchorRight.GetComponent<Rigidbody>();
        // leftRb.isKinematic = true;
        // rightRb.isKinematic = true;

        // leftRb.isKinematic = false;
        // rightRb.isKinematic = false;
      }
      // else
      // {
      //   treadsLeft.localPosition = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
      //   treadsRight.localPosition = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
      // }
      if (movingTreadLeft && movingTreadRight)
      {
        movingTreadLeft.wheelColliders = left;
        movingTreadRight.wheelColliders = right;
        movingTreadLeft.GenerateTreads(bounds);
        movingTreadRight.GenerateTreads(bounds);
      }

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

    // void RecenterPivotToCenterOfMass(Rigidbody vehicleRB)
    // {
    //   // Create a new root GameObject
    //   GameObject root = new GameObject(vehicleRB.name + "_Root");
    //   root.transform.position = vehicleRB.worldCenterOfMass; // Set root at COM
    //
    //   // Parent vehicle under root and reset local position
    //   Transform vehicleTransform = vehicleRB.transform;
    //   Vector3 localOffset = vehicleTransform.position - root.transform.position;
    //   vehicleTransform.SetParent(root.transform);
    //   vehicleTransform.localPosition = localOffset; // Maintain world position
    //
    //   // Add a new Rigidbody to the root and copy all settings
    //   Rigidbody rootRB = root.AddComponent<Rigidbody>();
    //   CopyRigidbodyProperties(vehicleRB, rootRB);
    //
    //   // Destroy old Rigidbody from the vehicle to prevent duplicate physics
    //   Destroy(vehicleRB);
    //
    //   Debug.Log("Pivot recentered to center of mass! New root: " + root.name);
    // }
    //
    // /// <summary>
    // /// Copies all Rigidbody properties from the original to the new Rigidbody.
    // /// </summary>
    // public static void CopyRigidbodyProperties(Rigidbody fromRB, Rigidbody toRB)
    // {
    //   toRB.mass = fromRB.mass;
    //   toRB.drag = fromRB.drag;
    //   toRB.angularDrag = fromRB.angularDrag;
    //   toRB.interpolation = fromRB.interpolation;
    //   toRB.collisionDetectionMode = fromRB.collisionDetectionMode;
    //   toRB.constraints = fromRB.constraints;
    //   toRB.isKinematic = fromRB.isKinematic;
    //   toRB.useGravity = fromRB.useGravity;
    //   toRB.maxDepenetrationVelocity = fromRB.maxDepenetrationVelocity;
    //   toRB.maxAngularVelocity = fromRB.maxAngularVelocity;
    //   toRB.sleepThreshold = fromRB.sleepThreshold;
    //   toRB.solverIterations = fromRB.solverIterations;
    //   toRB.solverVelocityIterations = fromRB.solverVelocityIterations;
    // }

    // void RecenterRigidbodyToCenterOfMass(Rigidbody rb)
    // {
    //   // Calculate offset to the center of mass
    //   Vector3 com = rb.worldCenterOfMass;
    //   Vector3 offset = rb.transform.position - com;
    //
    //   // Move all children to compensate for the shift
    //   foreach (Transform child in rb.transform)
    //   {
    //     child.position += offset;
    //   }
    //
    //   // Finally, move the Rigidbody object itself to the new center
    //   rb.transform.position = com;
    //
    //   Debug.Log("Rigidbody pivot recentered to Center of Mass!");
    // }

    private void UpdateCenterOfMass(float yOffset)
    {
      vehicleRootBody.ResetCenterOfMass();
      // RecenterRigidbodyToCenterOfMass(vehicleRootBody);

      var centerOfMass = vehicleRootBody.centerOfMass;
      centerOfMass = new Vector3(centerOfMass.x, yOffset, centerOfMass.z);

      vehicleRootBody.centerOfMass = centerOfMass;
    }

    private void CleanupTreads()
    {
      if (movingTreadLeft) Destroy(movingTreadLeft);
      if (movingTreadRight) Destroy(movingTreadRight);
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

    /// <summary>
    ///   To be called from VehicleMovementController
    /// </summary>
    public void VehicleMovementFixedUpdate()
    {
      if (!hasInitialized || wheelInstances.Count == 0 && rotatorEngineHingeInstances.Count == 0) return;
      _hasSyncedWheelsOnCurrentFrame = false;
      currentBreakTorqueLeft = 0f;
      currentBreakTorqueRight = 0f;
      currentMotorTorqueLeft = 0f;
      currentMotorTorqueRight = 0f;

      AdjustForHills();

      if (isBraking)
        ApplyBrakes();
      else
        ApplyTorque(inputMovement, inputTurnForce);
      // if (m_steeringType != SteeringType.Differential || m_steeringType == SteeringType.Differential && Mathf.Abs(inputTurnForce) < 0.5f)
      // {
      //   RunPoweredWheels();
      // }
      // todo only update if a property is changed.
      // UpdateRotatorEngines();
      // RunSteering();

      var currentSpeed = vehicleRootBody.velocity.magnitude;

      if (movingTreadLeft && movingTreadRight)
      {
        movingTreadLeft.SetSpeed(currentSpeed);
        movingTreadRight.SetSpeed(currentSpeed);
        UpdateSteeringTreadVisuals();
      }
    }
    /// <summary>
    ///   Must pass a local bounds into this wheel controller. The local bounds must be relative to the VehicleWheelController transform.
    /// </summary>
    /// 
    /// TODO make this a coroutine so it does not impact performance.
    /// <param name="bounds"></param>
    public void InitializeWheels(Bounds? bounds)
    {
      // if (initializeWheelsCoroutine != null)
      // {
      //   StopCoroutine(initializeWheelsCoroutine);
      // }
      // initializeWheelsCoroutine = StartCoroutine(InitializeWheelsCoroutine(bounds));

      if (bounds == null) return;
      hasInitialized = false;

      if (bounds.Value.size == Vector3.zero)
      {
        bounds = new Bounds(Vector3.zero, new Vector3(4f, 2f, 4f));
      }

      if (shouldCleanupPerInitialize)
      {
        Cleanup();
      }
      // if (wheelColliders.Count > 0 || front.Count > 0 || rear.Count > 0 || left.Count > 0 || right.Count > 0 || poweredWheels.Count > 0)
      // {
      //   Debug.LogWarning("VehicleWheelController is not probably cleaned up. Must call CleanupWheels before. Exiting InitializeWheels");
      //   hasInitialized = true;
      //   yield break;
      // }
      // var lowestYBounds = new Vector3(0, -bounds.Value.extents.y, 0);
      // treadsParent.localPosition = lowestYBounds;

      // it's within the treads parent. So we just need to align it with the treads which are generated above the localposition.
      // rotationEnginesParent.localPosition = new Vector3(0, MovingTreadComponent.treadPointYOffset / 2, 0);

      // GenerateRotatorEngines(bounds.Value);
      GenerateWheelSets(bounds.Value);
      SetupWheels();
      ApplyFrictionValuesToWheels();
      SetAcceleration(accelerationType, Math.Sign(inputMovement));


      // must be called after SetupWheels
      UpdateTreads(bounds.Value);
      // AdjustVehicleToGround(bounds.Value);
      hasInitialized = true;
    }


    public IEnumerator InitializeWheelsCoroutine(Bounds? bounds)
    {
      if (bounds == null) yield break;
      hasInitialized = false;

      if (bounds.Value.size == Vector3.zero)
      {
        bounds = new Bounds(Vector3.zero, new Vector3(4f, 2f, 4f));
      }

      if (shouldCleanupPerInitialize)
      {
        Cleanup();
      }
      // if (wheelColliders.Count > 0 || front.Count > 0 || rear.Count > 0 || left.Count > 0 || right.Count > 0 || poweredWheels.Count > 0)
      // {
      //   Debug.LogWarning("VehicleWheelController is not probably cleaned up. Must call CleanupWheels before. Exiting InitializeWheels");
      //   hasInitialized = true;
      //   yield break;
      // }

      GenerateWheelSets(bounds.Value);
      // AdjustVehicleToGround(bounds.Value);
      SetupWheels();
      hasInitialized = true;
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
    }

    /// <summary>
    ///   Generates wheel sets from a local bounds. This bounds must be local.
    /// </summary>
    /// TODO fix the rotated wheel issue where wheels get out of alignment for 90 degrees and -90 variants.
    /// <param name="bounds"></param>
    private void GenerateRotatorEngines(Bounds bounds)
    {
      if (!rotationEnginePrefab || !forwardDirection)
      {
        Debug.LogError(
          "Bounds Transform, Forward Direction, and Wheel Set Prefab must be assigned.");
        return;
      }

      SetOverrides();

      // var isXBounds = IsXBoundsAlignment();
      var totalRotators = CalculateTotalWheelSets(bounds);

      // we fully can regenerate wheels.
      // todo just remove the ones we no longer use
      if (totalRotators != wheelInstances.Count || wheelColliders.Count != totalRotators)
      {
        Cleanup();
      }

      var rotatorIndex = 0;
      var spacing = bounds.size.z / Math.Max(totalRotators - 1, 1);

      // var centerOfMassBounds = new Bounds(rigid.centerOfMass, bounds.size);
      // Generate wheel sets dynamically
      for (var i = 0; i < totalRotators; i++)
      {
        GameObject rotationEngineInstance;
        if (rotationEngineInstances.Count < rotatorIndex)
        {
          rotationEngineInstance = rotationEngineInstances[rotatorIndex];
        }
        else
        {
          rotationEngineInstance = Instantiate(rotationEnginePrefab, rotationEnginesParent);
        }

        SetRotationEngineProperties(rotationEngineInstance, bounds, i, totalRotators, spacing);

        if (rotationEngineInstances.Count >= rotatorIndex)
        {
          rotationEngineInstances.Add(rotationEngineInstance);
        }
        else
        {
          rotationEngineInstances[rotatorIndex] = rotationEngineInstance;
        }
        rotatorIndex += 1;
      }
    }

    private void SetRotationEngineProperties(GameObject rotationEngineInstance, Bounds bounds, int index, int totalEngines, float spacing)
    {
      var localPosition =
        GetRotationEnginePosition(index, bounds,
          spacing);
      var positionName = GetPositionName(index, totalEngines);

      rotationEngineInstance.name = $"ValheimVehicles_rotationEngine_{positionName}_{index}";
      rotationEngineInstance.transform.localPosition = localPosition;

      // scale the rotator to fit across the X bounds.
      var rotatorMeshTransform = rotationEngineInstance.transform.Find("scalar");
      var targetScale = new Vector3(bounds.size.x,
        MovingTreadComponent.treadPointYOffset,
        MovingTreadComponent.treadPointYOffset);
      ScaleMeshToFitBounds(rotatorMeshTransform.transform, targetScale);

      // joint setup has to be done last in order to align the item first before it's force locked.
      var joint = rotationEngineInstance.GetComponent<HingeJoint>();
      if (joint)
      {
        if (rotatorEngineHingeInstances.Count < index)
        {
          rotatorEngineHingeInstances[index] = joint;
        }
        else
        {
          rotatorEngineHingeInstances.Add(joint);
        }

        joint.connectedBody = vehicleRootBody;
      }
    }

    /// <summary>
    ///   Generates wheel sets from a local bounds. This bounds must be local.
    /// </summary>
    /// TODO fix the rotated wheel issue where wheels get out of alignment for 90 degrees and -90 variants.
    /// <param name="bounds"></param>
    private void GenerateWheelSets(Bounds bounds)
    {
      if (!wheelPrefab || !forwardDirection)
      {
        Debug.LogError(
          "Bounds Transform, Forward Direction, and Wheel Set Prefab must be assigned.");
        return;
      }

      SetOverrides();

      // var isXBounds = IsXBoundsAlignment();
      var totalWheelSets = CalculateTotalWheelSets(bounds);

      // we fully can regenerate wheels.
      // todo just remove the ones we no longer use
      if (totalWheelSets != wheelInstances.Count || wheelColliders.Count != totalWheelSets)
      {
        Cleanup();
      }

      var wheelIndex = 0;
      var spacing = bounds.size.z / Math.Max(totalWheelSets - 1, 1);

      // var centerOfMassBounds = new Bounds(rigid.centerOfMass, bounds.size);
      // Generate wheel sets dynamically
      for (var i = 0; i < totalWheelSets; i++)
      {
        for (var directionIndex = 0; directionIndex < 2; directionIndex++)
        {
          var isLeft = directionIndex == 0;
          var localPosition =
            GetWheelLocalPosition(i, isLeft, totalWheelSets, bounds,
              spacing);
          GameObject wheelInstance;
          if (wheelInstances.Count < wheelIndex)
          {
            wheelInstance = wheelInstances[wheelIndex];
          }
          else
          {
            wheelInstance = Instantiate(wheelPrefab, wheelParent);
          }

          wheelInstance.transform.localPosition = localPosition;

          var dirName = isLeft ? "left" : "right";
          var positionName = GetPositionName(i, totalWheelSets);
          wheelInstance.name = $"ValheimVehicles_VehicleLand_wheel_{positionName}_{dirName}_{i}";
          SetWheelProperties(wheelInstance, bounds, i, isLeft,
            totalWheelSets);

          if (wheelInstances.Count >= wheelIndex)
          {
            wheelInstances.Add(wheelInstance);
          }
          else
          {
            wheelInstances[wheelIndex] = wheelInstance;
          }
          wheelIndex += 1;
        }
      }
    }

    private Bounds GetBounds(Transform boundsTransform)
    {
      if (boundsTransform == null) return new Bounds(Vector3.zero, Vector3.one);
      var renderer = boundsTransform.GetComponent<Renderer>();
      if (renderer) return renderer.bounds;

      var collider = boundsTransform.GetComponent<Collider>();
      if (collider) return collider.bounds;

      Debug.LogError(
        "Bounds Transform must have a Renderer or Collider component.");
      return new Bounds(Vector3.zero, Vector3.zero);
    }

    private int CalculateTotalWheelSets(Bounds bounds)
    {
      var vehicleSize = bounds.size.z; // Assuming size along the Z-axis determines length
      var nearestIncrement = Mathf.Clamp(Mathf.RoundToInt(vehicleSize / wheelSetIncrement), minimumWheelSets, maxWheelSets);
      return nearestIncrement;
    }

    // private Vector3 GetWheelLocalPosition(int index, bool isLeft, int totalWheelSets,
    //   Bounds bounds, float spacing, bool isXBounds)
    // {
    //   var xPos = isLeft ? bounds.min.x : bounds.max.x;
    //   var zPos = bounds.min.z + spacing * index;
    //   var newPosition = new Vector3(xPos, -bounds.extents.y, zPos);
    //
    //   var rotatedPosition = wheelParent.TransformPoint(newPosition);
    //
    //   // var newPosition = new Vector3(bounds.min.z + spacing * index, -bounds.extents.y,
    //   //   isLeft ? bounds.min.x : bounds.max.x);
    //   return wheelParent.InverseTransformPoint(rotatedPosition);
    // }

    private Vector3 GetRotationEnginePosition(int index,
      Bounds bounds, float spacing)
    {
      // Calculate the local position directly within the bounds
      var xPos = bounds.center.x;
      var zPos = bounds.min.z + spacing * index;
      var localPosition = new Vector3(xPos, 0, zPos);
      return localPosition;
    }

    /// <summary>
    /// TODO This has problems with rotation alignment, rotating the wheel.root transform will cause the size to somehow inflate. 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isLeft"></param>
    /// <param name="totalWheelSets"></param>
    /// <param name="bounds"></param>
    /// <param name="spacing"></param>
    /// <returns></returns>
    private Vector3 GetWheelLocalPosition(int index, bool isLeft, int totalWheelSets,
      Bounds bounds, float spacing)
    {
      // Calculate the local position directly within the bounds
      var xPos = isLeft ? bounds.min.x : bounds.max.x;
      // var xPos = isLeft ? bounds.min.x : bounds.max.x;

      var zPos = bounds.min.z + spacing * index;

      // var ratio = index / Math.Max(totalWheelSets, 1);
      // var zPos = bounds.size.z * ratio * bounds.min.z;
      var localPosition = new Vector3(xPos, -(wheelBottomOffset + wheelRadius), zPos);

      // Return the local position without any world space conversions
      return localPosition;
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

    private void SetWheelProperties(GameObject wheelObj, Bounds bounds, int wheelSetIndex, bool isLeft, int totalWheelSets)
    {
      var wheelVisual =
        wheelObj.transform.Find("wheel_mesh");
      var wheelCollider =
        wheelObj.transform.Find("wheel_collider").GetComponent<WheelCollider>();

      var isMiddle = IsMiddleIndex(wheelSetIndex, totalWheelSets);
      wheelCollider.mass = wheelMass;
      wheelCollider.wheelDampingRate = wheelDamping;
      wheelCollider.radius = wheelRadius;
      wheelCollider.suspensionDistance = wheelSuspensionDistance;
      // wheelCollider.brakeTorque = 5000f;

      var suspensionSpring = wheelCollider.suspensionSpring;
      suspensionSpring.spring = wheelSuspensionSpring;
      wheelCollider.suspensionSpring = suspensionSpring;
      var wheelScalar = wheelCollider.radius / wheelBaseRadiusScale;
      if (!Mathf.Approximately(wheelScalar, 1f))
      {
        wheelVisual.transform.localScale = new Vector3(wheelScalar * wheelMeshLocalScale.x, wheelMeshLocalScale.y, wheelScalar * wheelMeshLocalScale.z);
      }

      // Setting higher forward stiffness for front and rear wheels allows for speeds to be picked up.
      if (!isMiddle)
      {
        var forwardRightFriction = wheelForwardFriction;
        forwardRightFriction.stiffness = 1f;
        wheelCollider.forwardFriction = forwardRightFriction;

        var sideFriction = wheelSidewaysFriction;
        sideFriction.stiffness = 1f;
        wheelCollider.sidewaysFriction = sideFriction;
      }
      else
      {
        var forwardRightFriction = wheelForwardFriction;
        forwardRightFriction.stiffness = 1f;
        wheelCollider.forwardFriction = forwardRightFriction;
        var sideFriction = wheelSidewaysFriction;
        sideFriction.stiffness = 1f;
        wheelCollider.sidewaysFriction = sideFriction;
      }

      if (!WheelcollidersToWheelVisualMap.ContainsKey(wheelCollider))
        WheelcollidersToWheelVisualMap.Add(wheelCollider, wheelVisual);
    }

    public void RunSteering()
    {
      switch (m_steeringType)
      {
        case SteeringType.Differential:
          RunDifferentialSteeringWheels();
          break;
        case SteeringType.Magic:
          RunMagicRotation();
          break;
        case SteeringType.FourWheel:
          RunFourWheelSteeringWheels();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private float GetAccelerationMultiplier(WheelCollider wheel)
    {
      return 1f;
      var normalizedRPM = Mathf.Clamp01(Mathf.Abs(wheel.rpm) / MaxWheelRPM);
      var accelerationMultiplier = Mathf.Lerp(10, 1f, normalizedRPM);
      return accelerationMultiplier;
    }

    /// <summary>
    /// Updates the shared torque values for both debugging and computation efficiency
    /// </summary>
    /// <param name="wheel"></param>
    public void UpdateSynchronizedWheelProperties(WheelCollider wheel, bool isLeft, ref float motorTorque, ref float brakeTorque)
    {
      if (!wheel) return;
      // var inputForceDir = Mathf.Sign(inputForwardForce);
      if (!isLeft && inputTurnForce > 0.20f || isLeft && inputTurnForce < -0.20f || Mathf.Abs(inputTurnForce) > 10f)
      {
        // breakTorque = Mathf.Clamp(wheel.brakeTorque, MaxWheelRPM, MaxWheelRPM);
        // motorTorque = Mathf.Clamp(wheel.motorTorque, MaxWheelRPM, MaxWheelRPM);
        return;
      }

      if (isBraking)
      {
        motorTorque = 0f;
        brakeTorque = Mathf.Abs(wheel.rpm + baseTorque * baseMotorTorque + additionalBrakeForce);
        return;
      }

      if (baseTorque == 0)
      {
        motorTorque = 0;
        brakeTorque = baseTorque * baseMotorTorque / 2f + additionalBrakeForce * Time.fixedDeltaTime;
        return;
      }

      if (Mathf.Abs(wheel.rpm) > MaxWheelRPM || vehicleRootBody.velocity.x + vehicleRootBody.velocity.z >= topSpeed)
      {
        brakeTorque = Mathf.Abs(wheel.rpm);
        return;
      }

      if (!wheel.isGrounded)
      {
        motorTorque = 0f;
        brakeTorque = 500f;
        return;
      }


      if (!Mathf.Approximately(MaxWheelRPM, 0f))
      {
        // To create a top speed for the tank, the motor torque just
        // cuts out when the tank starts moving fast enough.
        if (wheel.brakeTorque > 0f)
        {
          brakeTorque = 0f;
        }
        var nextMotorTorque = wheel.motorTorque;

        var accelerationMultiplier = GetAccelerationMultiplier(wheel);
        // using power will turn the number positive. We use sign to get -1 if below 1 or 1 if above 0
        var additiveTorque = baseTorque * baseMotorTorque * accelerationMultiplier * Time.fixedDeltaTime;

        // alternative to acceleration mulitplier
        // if (wheel.rpm < MaxWheelRPM * 0.1f)
        // {
        //   nextMotorTorque += inputForceDir * MaxWheelRPM * 0.1f;
        // }

        // When flipping directions we immediately must zero out the motor torque.
        if (wheel.motorTorque > 0f && additiveTorque < 0f || wheel.motorTorque < 0f && additiveTorque > 0f)
        {
          nextMotorTorque = 0f;
        }

        motorTorque = Mathf.Clamp(nextMotorTorque + additiveTorque, -MaxWheelRPM, MaxWheelRPM);
      }
    }

    /// <summary>
    ///   POWERED WHEELs
    ///   Sets the motor torque of the wheel based on forward input. This moves
    ///   the tank forwards and backwards.
    /// </summary>
    private void RunPoweredWheels()
    {
      // if (deltaRunPoweredWheels > powerWheelDeltaInterval)
      // {
      //   deltaRunPoweredWheels = 0f;
      // }
      // else
      // {
      //   deltaRunPoweredWheels += Time.fixedDeltaTime;
      //   return;
      // }

      if (poweredWheels.Count == 0) return;

      var firstWheelLeft = wheelColliders[0];
      var firstWheelRight = wheelColliders[1];
      // UpdateSynchronizedWheelProperties(firstWheelLeft, true, ref currentMotorTorqueLeft, ref currentBreakTorqueLeft);
      // UpdateSynchronizedWheelProperties(firstWheelRight, false, ref currentMotorTorqueRight, ref currentBreakTorqueRight);

      foreach (var wheel in poweredWheels)
      {
        if (wheel == null) continue;
        var torque = 0f;
        var brakeTorque = 0f;
        if (wheel.transform.parent.name.Contains("left"))
        {
          UpdateSynchronizedWheelProperties(firstWheelLeft, true, ref torque, ref brakeTorque);
          wheel.brakeTorque = torque;
          wheel.motorTorque = brakeTorque;
        }
        else
        {
          UpdateSynchronizedWheelProperties(firstWheelLeft, false, ref torque, ref brakeTorque);
          wheel.brakeTorque = torque;
          wheel.motorTorque = brakeTorque;
        }
        if (WheelcollidersToWheelVisualMap.TryGetValue(wheel,
              out var wheelVisual))
        {
          SyncWheelVisualWithCollider(wheelVisual, wheel);
        }
      }
    }

    public void SyncWheelVisual(WheelCollider wheel)
    {
      if (!_hasSyncedWheelsOnCurrentFrame && WheelcollidersToWheelVisualMap.TryGetValue(wheel,
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
      wheelTransform.position = position;
      RotateOnXAxis(wheelCollider, wheelVisual.transform);
    }

    private void RotateOnXAxis(WheelCollider wheelCollider,
      Transform wheelTransform)
    {
      var deltaRotation = Mathf.Clamp(wheelCollider.rpm, -359f, 359f) * Time.deltaTime;
      wheelTransform.Rotate(Vector3.down,
        deltaRotation, Space.Self);
    }

    /// <summary>
    /// Calculates torque values dynamically based on tank mass, wheel count, radius, wheelmass.
    /// </summary>
    public static (float Low, float Medium, float High) CalculateTorque(float tankMass, int wheelCount, float wheelRadius, float wheelMass)
    {
      if (wheelCount <= 0 || wheelRadius <= 0)
      {
        Debug.LogError("Wheel count and wheel radius must be greater than zero.");
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


    /// <summary>
    /// @deprecated
    /// </summary>
    /// <param name="wheel"></param>
    /// <param name="directionMultiplier"></param>
    private void RunDifferentialSteeringForWheel(WheelCollider wheel, float directionMultiplier)
    {
      var isTorqueAndTurnNearZero = Mathf.Approximately(baseTorque, 0) &&
                                    Mathf.Approximately(directionMultiplier, 0);
      var additionalForceToBeAdded = 0f;
      if (isBraking && wheel.brakeTorque > Mathf.Pow(additionalBrakeForce, 2))
      {
        return;
      }
      if (isBraking)
      {
        wheel.motorTorque = 0f;
        wheel.brakeTorque = additionalBrakeForce;
        return;
      }

      if (wheel.motorTorque > 0 && isTorqueAndTurnNearZero)
      {
        wheel.motorTorque = 0f;
        wheel.brakeTorque = baseMotorTorque + wheel.motorTorque;
        return;
      }
      if (Mathf.Abs(wheel.brakeTorque) > 0f)
      {
        wheel.brakeTorque = 0f;
      }

      if (wheel.transform.parent.name.Contains("front") || wheel.transform.parent.name.Contains("back"))
      {
        var forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = wheelPowerExtremumSlip;
        forwardFriction.asymptoteSlip = wheelPowerAsymptoteSlip;
        forwardFriction.extremumValue = 0.1f;
        forwardFriction.asymptoteValue = 0.5f;

        forwardFriction.stiffness = wheelPowerStiffness;

        var sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = wheelPowerSidewaysExtremumSlip;
        sidewaysFriction.asymptoteSlip = wheelPowerSidewaysAsymptoteSlip;
        sidewaysFriction.stiffness = wheelPowerSidewaysStiffness;
        sidewaysFriction.extremumValue = 0.1f;
        sidewaysFriction.asymptoteValue = 0.5f;

        wheel.forwardFriction = forwardFriction;
        wheel.sidewaysFriction = forwardFriction;
        additionalForceToBeAdded = baseMotorTorque * directionMultiplier;
      }
      else
      {
        var forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = wheelCenterExtremumSlip;
        forwardFriction.asymptoteSlip = wheelCenterAsymptoteSlip;
        forwardFriction.stiffness = wheelCenterStiffness;
        forwardFriction.extremumValue = 0.1f;
        forwardFriction.asymptoteValue = 0.5f;

        var sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = wheelCenterSidewaysExtremumSlip;
        sidewaysFriction.asymptoteSlip = wheelCenterSidewaysAsymptoteSlip;
        sidewaysFriction.stiffness = wheelCenterSidewaysStiffness;
        sidewaysFriction.extremumValue = 0.1f;
        sidewaysFriction.asymptoteValue = 0.5f;

        wheel.forwardFriction = forwardFriction;
        wheel.sidewaysFriction = forwardFriction;
        additionalForceToBeAdded = baseMotorTorque * directionMultiplier * 0.25f;
        // wheel.motorTorque += baseMotorTorque * directionMultiplier;
      }

      if (vehicleRootBody.velocity.magnitude < maxWheelSets)
      {
        wheel.motorTorque += additionalForceToBeAdded * directionMultiplier * 0.25f;
      }

      if (!_hasSyncedWheelsOnCurrentFrame && WheelcollidersToWheelVisualMap.TryGetValue(wheel,
            out var wheelVisual))
      {
        SyncWheelVisualWithCollider(wheelVisual, wheel);
      }
    }

    /// <summary>
    ///   DIFFERENTIAL STEERING
    ///   When turning, the left/right wheel colliders will apply an extra
    ///   torque in opposing directions and rotate the tank.
    ///   Note: Wheel sideways friction can easily prevent the tank from
    ///   rotating when this is done. Lowering side friction for wheels that
    ///   don't need it (i.e., wheels away from the center) can mitigate this.
    /// </summary>
    /// <limitations>
    /// 
    ///  - must be negative motor torque
    ///  - Does not work well for many wheels or long vehicles (need magic to fix this). Usually need odd set of wheels for better performance
    /// </limitations>
    private void RunDifferentialSteeringWheels()
    {
      var differentialPower = Mathf.Lerp(-1, 2, Mathf.Abs(steeringAngle));
      if (differentialPower < 0)
      {
        return;
      }

      foreach (var wheel in left)
      {
        RunDifferentialSteeringForWheel(wheel, inputTurnForce * differentialPower);
      }

      foreach (var wheel in right)
      {
        RunDifferentialSteeringForWheel(wheel, -inputTurnForce * differentialPower);
      }
    }

    private void UpdateSteeringTreadVisuals()
    {
      if (inputTurnForce > -0.2f && inputTurnForce < 0.2f) return;
      var isLeftForward = inputTurnForce > 0.2f;
      var isRightForward = inputTurnForce < -0.2f;

      if (baseTorque < 0f)
      {
        isLeftForward = !isLeftForward;
        isRightForward = !isRightForward;
      }

      movingTreadLeft.isForward = isLeftForward;
      movingTreadRight.isForward = isRightForward;
    }

    /// <summary>
    ///   FOUR WHEEL STEERING
    ///   Wheels assigned as front and rear wheels rotate to turn the tank.
    ///   This works great in motion, but will not turn the tank when standing
    ///   still.
    ///   Note: If only one set of wheels is filled out, only that set will
    ///   rotate.
    /// </summary>
    private void RunFourWheelSteeringWheels()
    {
      // foreach (var wheel in front)
      //   wheel.steerAngle = inputTurnForce * steeringAngle;
      // foreach (var wheel in rear)
      //   wheel.steerAngle = -inputTurnForce * steeringAngle;
    }

    private void RotateAroundCenterOfMass(Rigidbody rb, float turnRate, float inputTurnForce)
    {
      rb.ResetCenterOfMass();
      var com = rb.worldCenterOfMass; // Get world-space center of mass
      var rotationDelta = Quaternion.AngleAxis(turnRate * inputTurnForce * Time.deltaTime, rb.transform.up);

      // Compute the new position to maintain correct pivoting
      var offset = rb.position - com;
      var newPosition = com + rotationDelta * offset;

      // Apply the rotation and maintain the correct position
      rb.Move(newPosition, rotationDelta * rb.rotation);
      // rb.position = newPosition; // This ensures no teleportation occurs

      UpdateCenterOfMass(centerOfMassOffset);
    }

    /// <summary>
    ///   MAGIC ROTATION
    ///   Simply rotates the Rigidbody itself using a predefined rotation rate
    ///   and turning input. This has no connection to physics in any way, but
    ///   is very controllable and predictable.
    ///   Note: Since there is no connection to the physics, the tank could
    ///   turn even if it wasn't on the ground. A simple way to counter this
    ///   would be to check how many wheels are on the ground and then reduce
    ///   the turning speed depending on how many are touching the ground.
    /// </summary>
    // private void RunMagicRotation()
    // {
    //   if (isBreaking) return;
    //   if (Mathf.Approximately(currentMotorTorque, 0f)) return;
    //   if (Mathf.Approximately(inputTurnForce, 0f)) return;
    //
    //   var turningSpeed = Mathf.Lerp(Mathf.Min(1 / vehicleRootBody.velocity.magnitude, 1), 1, Time.fixedDeltaTime * magicTurnRate);
    //   // var newRotation = Quaternion.AngleAxis(currentMotorTorque, Vector3.up);
    //   
    // }
    private void RunMagicRotation()
    {
      var clampedRotation = transform.rotation;
      // if (Mathf.Abs(clampedRotation.eulerAngles.x) > 20f || Mathf.Abs(clampedRotation.eulerAngles.z) > 20f)
      // {
      //   var zeroedXZQuaternion = new Quaternion(1, clampedRotation.y, 1, clampedRotation.w);
      //   clampedRotation = Quaternion.Slerp(clampedRotation, zeroedXZQuaternion, Time.fixedDeltaTime);
      // }
      if (Mathf.Approximately(inputTurnForce, 0f))
      {
        if (transform.rotation != clampedRotation)
        {
          vehicleRootBody.MoveRotation(clampedRotation);
        }

        return;
      }
      // var magicRotation = transform.rotation *
      //                     Quaternion.AngleAxis(
      //                       magicTurnRate * inputTurnForce * Time.deltaTime,
      //                       transform.up);

      var magicRotation = clampedRotation *
                          Quaternion.AngleAxis(
                            magicTurnRate * inputTurnForce * Time.deltaTime,
                            transform.up);
      vehicleRootBody.MoveRotation(magicRotation);
    }

    /// <summary>
    /// This is the absolute max value torque has before it is clamped both positive and negative.
    /// </summary>
    public void UpdateMaxRPM()
    {
      var maxTotalTorque = baseMotorTorque * baseTorque * 4;
      MaxWheelRPM = Mathf.Clamp(Mathf.Abs(maxTotalTorque), 1000f, 5000f);
    }

    public void UpdateRotatorEngine(HingeJoint rotatorEngine)
    {
      if (rotatorEngine == null) return;
      var motor = rotatorEngine.motor;
      if (isBraking)
      {
        motor.force = 0;
        motor.targetVelocity = 0;
        rotatorEngine.motor = motor;
        return;
      }
      rotatorEngine.axis = new Vector3(isForward ? -1 : 1, 0, 0);
      motor.targetVelocity = MaxWheelRPM;
      motor.force = Mathf.Abs(baseTorque * baseMotorTorque * 1000);
      rotatorEngine.motor = motor;
    }

    private void AdjustForHills()
    {
      // Get forward direction of the tank
      var tankForward = transform.forward;

      // Project forward vector onto XZ plane to get horizontal direction
      var flatForward = Vector3.ProjectOnPlane(tankForward, Vector3.up).normalized;

      // Calculate the pitch angle using dot product
      var slopeFactor = Vector3.Dot(tankForward, flatForward);

      // Adjust hill factor: Steeper inclines require more torque
      hillFactor = Mathf.Clamp(1f + (1f - slopeFactor), 0.5f, 2f);
    }

    private float GetTankSpeed()
    {
      return Vector3.Dot(GetComponent<Rigidbody>().velocity, transform.forward);
    }


    private void ApplyBrakes()
    {
      var currentSpeed = GetTankSpeed();
      var brakeForce = vehicleRootBody.mass * Mathf.Abs(currentSpeed) / 2f; // Stops in ~2s

      foreach (var wheel in wheelColliders)
      {
        // Apply braking force based on current torque & momentum
        wheel.brakeTorque = Mathf.Max(brakeForce, Mathf.Abs(wheel.motorTorque) * 1.5f);
        wheel.motorTorque = 0; // Cut off power when braking
      }
    }


    private void ApplyFrictionValuesToWheels()
    {
      var frictionMultiplier = Mathf.Clamp(10f / wheelColliders.Count, 0.7f, 1.5f);
      wheelColliders.ForEach(wheel =>
      {
        var forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = 0.4f * frictionMultiplier;
        forwardFriction.extremumValue = 1.5f * frictionMultiplier;
        forwardFriction.asymptoteSlip = 2.0f * frictionMultiplier;
        forwardFriction.asymptoteValue = 0.5f * frictionMultiplier;
        forwardFriction.stiffness = 2.0f;
        wheel.forwardFriction = forwardFriction;

        var sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = 0.5f * frictionMultiplier;
        sidewaysFriction.extremumValue = 0.7f * frictionMultiplier;
        sidewaysFriction.asymptoteSlip = 1.5f * frictionMultiplier;
        sidewaysFriction.asymptoteValue = 0.3f * frictionMultiplier;
        sidewaysFriction.stiffness = 1.2f;
        wheel.sidewaysFriction = sidewaysFriction;

        // var forwardFriction = wheel.forwardFriction;
        // forwardFriction.extremumSlip = wheelPowerExtremumSlip;
        // forwardFriction.asymptoteSlip = wheelPowerAsymptoteSlip;
        // forwardFriction.extremumValue = 0.1f;
        // forwardFriction.asymptoteValue = 0.5f;
        //
        // forwardFriction.stiffness = wheelPowerStiffness;
        //
        // var sidewaysFriction = wheel.sidewaysFriction;
        // sidewaysFriction.extremumSlip = wheelPowerSidewaysExtremumSlip;
        // sidewaysFriction.asymptoteSlip = wheelPowerSidewaysAsymptoteSlip;
        // sidewaysFriction.stiffness = wheelPowerSidewaysStiffness;
        // sidewaysFriction.extremumValue = 0.1f;
        // sidewaysFriction.asymptoteValue = 0.5f;
        //
        // wheel.forwardFriction = forwardFriction;
        // wheel.sidewaysFriction = forwardFriction;
      });
    }

    private void ApplyTorque(float move, float turn)
    {
      if (Mathf.Approximately(move, 0f) && Mathf.Approximately(turn, 0f))
      {
        StopWheels();
        return;
      }

      // Smooth turn factor
      lerpedTurnFactor = Mathf.Lerp(lerpedTurnFactor, Mathf.Abs(turn), Time.deltaTime * 5f);

      // Adjusted torque for hills
      var forwardTorque = baseTorque * move * hillFactor;

      // Rotation logic
      var leftSideTorque = forwardTorque;
      var rightSideTorque = forwardTorque;

      if (Mathf.Abs(turn) >= 0.5f)
      {
        leftSideTorque = turn > 0 ? -baseTorque : baseTorque;
        rightSideTorque = turn > 0 ? baseTorque : -baseTorque;
      }
      else if (turn != 0)
      {
        var turnEffect = Mathf.Lerp(1f, 0f, lerpedTurnFactor);
        leftSideTorque *= turn > 0 ? 1 : turnEffect;
        rightSideTorque *= turn > 0 ? turnEffect : 1f;
      }

      // Apply torques to wheels
      foreach (var leftWheel in left)
      {
        leftWheel.motorTorque = leftSideTorque;
        leftWheel.brakeTorque = 0f;
        SyncWheelVisual(leftWheel);
      }
      foreach (var rightWheel in right)
      {
        rightWheel.motorTorque = rightSideTorque;
        rightWheel.brakeTorque = 0f;
        SyncWheelVisual(rightWheel);
      }
    }

    public void StopWheels()
    {
      var currentSpeed = GetTankSpeed();
      var engineBrakeForce = vehicleRootBody.mass * Mathf.Abs(currentSpeed) / 5f; // Lighter braking effect

      foreach (var wheel in wheelColliders)
      {
        wheel.motorTorque = 0f; // No acceleration
        wheel.brakeTorque = engineBrakeForce; // Light braking
        SyncWheelVisual(wheel);
      }
    }

    public void UpdateRotatorEngines()
    {
      rotatorEngineHingeInstances.ForEach(UpdateRotatorEngine);
    }

    public void SetAcceleration(AccelerationType acceleration, int direction)
    {
      (lowTorque, mediumTorque, highTorque) = CalculateTorque(vehicleRootBody.mass, wheelInstances.Count, wheelRadius, wheelMass);
      accelerationType = acceleration;
      baseTorque = acceleration switch
      {
        AccelerationType.High => highTorque,
        AccelerationType.Medium => mediumTorque,
        AccelerationType.Low => mediumTorque,
        _ => lowTorque
      };

      isForward = direction > 0;
    }


    public void SetTurnInput(float val)
    {
      inputTurnForce = val;
    }

    public void ToggleBrake()
    {
      isBraking = !isBraking;
      UpdateRotatorEngines();
    }

    public void SetBrake(bool val)
    {
      isBraking = val;
    }

    /// <summary>
    /// This will need logic to check if the player is the owner and if the player is controlling the vehicle actively.
    ///
    /// - To be called within VehicleMovementController.
    /// </summary>
    public void UpdateControls()
    {
      if (!IsControlling) return;
      if (UseManualControls) return;

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
      inputTurnForce = Input.GetAxis("Horizontal");
      inputMovement = Input.GetAxis("Vertical");
      SetAcceleration(accelerationType, Math.Sign(inputMovement));
    }

    // We run this only in Unity Editor
#if UNITY_EDITOR
    private float centerOfMassOffset = -10f;

    private void Update()
    {
      if (!Application.isPlaying) return;
      UpdateControls();
    }

    private void FixedUpdate()
    {
      if (!Application.isPlaying) return;
      UpdateCenterOfMass(centerOfMassOffset);
      UpdateMaxRPM();
      VehicleMovementFixedUpdate();
    }
#endif
  }
}