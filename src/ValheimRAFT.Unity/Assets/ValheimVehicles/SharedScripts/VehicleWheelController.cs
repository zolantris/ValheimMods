using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

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
    public enum SteeringType
    {
      Differential,
      Magic,
      FourWheel
    }

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

    public float wheelMass = 20f;
    public WheelFrictionCurve wheelForwardFriction = new()
    {
      extremumSlip = 0f,
      extremumValue = 1f,
      asymptoteSlip = 1f,
      asymptoteValue = 1f,
      stiffness = 1
    };
    public WheelFrictionCurve wheelSidewaysFriction = new()
    {
      extremumSlip = 0f,
      extremumValue = 1f,
      asymptoteSlip = 1f,
      asymptoteValue = 1f,
      stiffness = 1f
    };

    [Tooltip(
      "Assign this to override the center of mass. This can be useful to make the tank more stable and prevent it from flipping over. \n\nNOTE: THIS TRANSFORM MUST BE A CHILD OF THE ROOT TANK OBJECT.")]
    public Transform? centerOfMassTransform;

    [Tooltip(
      "Front wheels used for steering by rotating the wheels left/right.")]
    public List<WheelCollider> front = new();

    [Tooltip("Rear wheels for steering by rotating the wheels left/right.")]
    public List<WheelCollider> rear = new();

    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public readonly List<WheelCollider> poweredWheels = new();

    [Tooltip(
      "Wheels on the left side of the tank that are used for differential steering.")]
    public List<WheelCollider> left = new();

    [Tooltip(
      "Wheels on the right side of the tank that are used for differential steering.")]
    public List<WheelCollider> right = new();

    public SteeringType m_steeringType = SteeringType.Differential;

    [Header("Wheel Settings")]
    public Transform forwardDirection; // Dynamic rotation reference

    public GameObject wheelPrefab; // Prefab for a single wheel set
    public GameObject rotationEnginePrefab; // Prefab for a single wheel set

    public int minimumWheelSets = 2; // Minimum number of wheel sets
    public float axelPadding = 0.5f; // Extra length on both sides of axel

    public float
      vehicleSizeThresholdFor5thSet = 18f; // Threshold for adding 5th set

    public float
      vehicleSizeThresholdFor6thSet = 32f; // Threshold for adding 6th set

    [Tooltip(
      "User input")]
    public float inputForwardForce = 0f;

    public float inputTurnForce;
    public float MaxWheelRPM = 3000f;
    public float turnInputOverride = 0;


    public bool isBreaking = true;
    public bool UseManualControls = false;

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, Transform>
      WheelcollidersToWheelVisualMap =
        new();

    public bool hasInitialized;

    private Rigidbody vehicleRootBody;

    [Tooltip("Wheel properties")]
    public float wheelBottomOffset = 0;
    public float wheelRadius = 1.5f;
    public float wheelSuspensionDistance = 1.5f;
    public float wheelSuspensionSpring = 200f;

    // speed overrides
    public static float Override_WheelBottomOffset = 0;
    public static float Override_WheelRadius = 0;
    public static float Override_WheelSuspensionDistance = 0;
    public static float Override_TopSpeed = 0;
    public static float wheelDamping = 2f;
    public const float wheelBaseRadiusScale = 1.5f;
    public GameObject treadsPrefab;

    // wheel regeneration unstable so leave this one for now
    public static bool shouldCleanupPerInitialize = true;

    internal MovingTreadComponent movingTreadRight;
    internal MovingTreadComponent movingTreadLeft;

    [Tooltip("Transforms")]
    public Transform wheelParent;
    public Transform treadsParent;
    public Transform rotationEnginesParent;
    public Transform treadsRight;
    public Transform treadsLeft;

    [Tooltip("Kinematic Objects and Colliders")]
    internal List<WheelCollider> wheelColliders = new();
    internal List<Collider> colliders = new();
    internal List<GameObject> wheelInstances = new();
    internal List<GameObject> rotationEngineInstances = new();
    internal List<HingeJoint> rotatorEngineHingeInstances = new();
    private bool isForward = true;
    private bool isRightForward = true;
    private bool isLeftForward = true;

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
        var treadsAnchorLeftLocalPosition = new Vector3(bounds.min.x, 0, bounds.min.z);
        var treadsAnchorRightLocalPosition = new Vector3(bounds.max.x, 0, bounds.min.z);
        treadsLeft.localPosition = treadsAnchorLeftLocalPosition;
        treadsRight.localPosition = treadsAnchorRightLocalPosition;
        ConfigureJoint(leftJoint, vehicleRootBody, Vector3.zero);
        ConfigureJoint(rightJoint, vehicleRootBody, Vector3.zero);
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

      if (movingTreadLeft) movingTreadLeft.GenerateTreads(bounds);
      if (movingTreadRight) movingTreadRight.GenerateTreads(bounds);
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
      right.Clear();
      left.Clear();
      rear.Clear();
      front.Clear();
    }

    /// <summary>
    ///   To be called from VehicleMovementController
    /// </summary>
    public void VehicleMovementFixedUpdate()
    {
      if (!hasInitialized || (wheelInstances.Count == 0 && rotatorEngineHingeInstances.Count == 0)) return;
      RunPoweredWheels();
      UpdateMaxRPM();
      // todo only update if a property is changed.
      // UpdateRotatorEngines();
      RunSteering();

      var currentSpeed = vehicleRootBody.velocity.magnitude;

      if (movingTreadLeft && movingTreadRight)
      {
        movingTreadLeft.SetSpeed(currentSpeed);
        movingTreadRight.SetSpeed(currentSpeed);
        UpdateSteeringTreadVisuals();
      }
    }

    private Coroutine? initializeWheelsCoroutine;
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
      var lowestYBounds = new Vector3(0, -bounds.Value.extents.y, 0);
      treadsParent.localPosition = lowestYBounds;

      // it's within the treads parent. So we just need to align it with the treads which are generated above the localposition.
      rotationEnginesParent.localPosition = new Vector3(0, MovingTreadComponent.treadPointYOffset / 2, 0);

      // GenerateRotatorEngines(bounds.Value);
      GenerateWheelSets(bounds.Value);
      SetupWheels();
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
          right.Add(wheelCollider);

        if (wheelCollider.transform.parent.name.Contains("left") && !left.Contains(wheelCollider))
          left.Add(wheelCollider);

        if (!poweredWheels.Contains(wheelCollider))
        {
          poweredWheels.Add(wheelCollider);
        }
      }
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
      UpdateTreads(bounds);
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
      UpdateTreads(bounds);
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

      if (vehicleSize >= vehicleSizeThresholdFor6thSet) return 6;

      if (vehicleSize >= vehicleSizeThresholdFor5thSet) return 5;

      return minimumWheelSets;
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
      var localPosition = new Vector3(xPos, -bounds.extents.y + wheelBottomOffset + wheelRadius, zPos);

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
      else
      {
        // For odd size, one middle index
        var middle = size / 2;
        return index == middle;
      }
    }

    private static Vector3 wheelMeshLocalScale = new(3f, 0.3f, 3f);

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
      wheelCollider.brakeTorque = 5000f;

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

    public static float defaultBreakForce = 500f;
    public float additionalBreakForce = defaultBreakForce;
    private float deltaRunPoweredWheels = 0f;

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

    private float GetAccelerationMultiplier(WheelCollider wheel)
    {
      return 1f;
      var normalizedRPM = Mathf.Clamp01(Mathf.Abs(wheel.rpm) / MaxWheelRPM);
      var accelerationMultiplier = Mathf.Lerp(10, 1f, normalizedRPM);
      return accelerationMultiplier;
    }

    public float currentMotorTorque = 0f;
    public float currentBreakTorque = 0f;

    /// <summary>
    /// Updates the shared torque values for both debugging and computation efficiency
    /// </summary>
    /// <param name="wheel"></param>
    public void UpdateSynchronizedWheelProperties(WheelCollider wheel)
    {
      if (!wheel) return;
      // var inputForceDir = Mathf.Sign(inputForwardForce);

      if (isBreaking)
      {
        currentMotorTorque = 0f;
        currentBreakTorque = Mathf.Abs(wheel.rpm + inputForwardForce * baseMotorTorque + additionalBreakForce);
        return;
      }

      if (inputForwardForce == 0)
      {
        currentMotorTorque = 0;
        currentBreakTorque = inputForwardForce * baseMotorTorque / 2f + additionalBreakForce * Time.fixedDeltaTime;
        return;
      }

      if (Mathf.Abs(wheel.rpm) > MaxWheelRPM || vehicleRootBody.velocity.x + vehicleRootBody.velocity.z >= topSpeed)
      {
        currentBreakTorque = Mathf.Abs(wheel.rpm);
        return;
      }

      if (Mathf.Abs(wheel.rpm) > MaxWheelRPM || !wheel.isGrounded)
      {
        currentMotorTorque = 0f;
        currentBreakTorque = 0f;
        return;
      }

      if (!Mathf.Approximately(MaxWheelRPM, 0f))
      {
        // To create a top speed for the tank, the motor torque just
        // cuts out when the tank starts moving fast enough.
        if (wheel.brakeTorque > 0f)
        {
          currentBreakTorque = 0f;
        }
        var nextMotorTorque = wheel.motorTorque;

        var accelerationMultiplier = GetAccelerationMultiplier(wheel);
        // using power will turn the number positive. We use sign to get -1 if below 1 or 1 if above 0
        var additiveTorque = inputForwardForce * baseMotorTorque * accelerationMultiplier * Time.fixedDeltaTime;

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

        currentMotorTorque = Mathf.Clamp(nextMotorTorque + additiveTorque, -MaxWheelRPM, MaxWheelRPM);
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

      var firstWheel = wheelColliders[0];
      UpdateSynchronizedWheelProperties(firstWheel);

      foreach (var wheel in poweredWheels)
      {
        if (wheel == null) continue;
        wheel.brakeTorque = currentBreakTorque;
        wheel.motorTorque = currentMotorTorque;
        if (WheelcollidersToWheelVisualMap.TryGetValue(wheel,
              out var wheelVisual))
        {
          SyncWheelVisualWithCollider(wheelVisual, wheel);
        }
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

    private Quaternion RotateOnXAxis(WheelCollider wheelCollider,
      Transform wheelTransform)
    {
      // var wheelRb = wheelTransform.GetComponent<Rigidbody>();

      var deltaRotation = Mathf.Clamp(wheelCollider.rpm, -359f, 359f) * Time.deltaTime;

      // wheels need to move their X coordinate but for some reason it's the Y axis rotated...baffling.
      // deltaRotation > 0 ? Vector3.down : Vector3.up
      wheelTransform.Rotate(Vector3.down,
        deltaRotation, Space.Self);
      // Calculate the new rotation angle
      // Calculate the rotation increment
      // var rotX = wheelTransform.localRotation.eulerAngles.x;
      // var normalizedRotX = rotX > 360f ? 0 : rotX;
      // var identityRot = Quaternion.identity;
      // var newRot = Quaternion.Euler(normalizedRotX,
      //   wheelCollider.transform.rotation.eulerAngles.y,
      //   wheelCollider.transform.rotation.eulerAngles.z);
      // wheelTransform.localRotation = newRot;
      // Create a quaternion for the X-axis rotation

      // Apply the rotation to the current transform without affecting other axes
      // wheelTransform.rotation = xRotation * wheelTransform.rotation;

      // Normalize the quaternion to prevent precision errors over time
      // wheelTransform.rotation = Quaternion.Normalize(wheelTransform.rotation);

      // Apply the rotation back to the transform
      return wheelTransform.rotation;
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
      foreach (var wheel in left)
      {
        var isTorqueAndTurnNearZero = Mathf.Approximately(inputForwardForce, 0) &&
                                      Mathf.Approximately(inputTurnForce, 0);
        if (wheel.motorTorque > 0 && isTorqueAndTurnNearZero)
        {
          wheel.motorTorque = Mathf.Lerp(wheel.motorTorque, 0,
            Time.fixedDeltaTime * 50f);
          continue;
        }

        if (isTorqueAndTurnNearZero) continue;

        wheel.motorTorque += baseMotorTorque * inputTurnForce;
      }

      foreach (var wheel in right)
      {
        var isTorqueAndTurnNearZero = Mathf.Approximately(inputForwardForce, 0) &&
                                      Mathf.Approximately(inputTurnForce, 0);
        if (wheel.motorTorque > 0 && isTorqueAndTurnNearZero)
        {
          wheel.motorTorque = Mathf.Lerp(wheel.motorTorque, 0,
            Time.fixedDeltaTime * 50f);
          continue;
        }

        if (isTorqueAndTurnNearZero) continue;
        wheel.motorTorque += baseMotorTorque * -inputTurnForce;
      }
    }

    private void UpdateSteeringTreadVisuals()
    {
      if (inputTurnForce > -0.2f && inputTurnForce < 0.2f) return;
      var isLeftForward = inputTurnForce > 0.2f;
      var isRightForward = inputTurnForce < -0.2f;

      if (inputForwardForce < 0f)
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

    void RotateAroundCenterOfMass(Rigidbody rb, float turnRate, float inputTurnForce)
    {
      rb.ResetCenterOfMass();
      var com = rb.worldCenterOfMass; // Get world-space center of mass
      Quaternion rotationDelta = Quaternion.AngleAxis(turnRate * inputTurnForce * Time.deltaTime, rb.transform.up);

      // Compute the new position to maintain correct pivoting
      Vector3 offset = rb.position - com;
      Vector3 newPosition = com + rotationDelta * offset;

      // Apply the rotation and maintain the correct position
      rb.MoveRotation(rotationDelta * rb.rotation);
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
    private void RunMagicRotation()
    {
      // var clampedRotation = transform.rotation;
      // if (Mathf.Abs(clampedRotation.eulerAngles.x) > 20f || Mathf.Abs(clampedRotation.eulerAngles.z) > 20f)
      // {
      //   var zeroedXZQuaternion = new Quaternion(1, clampedRotation.y, 1, clampedRotation.w);
      //   clampedRotation = Quaternion.Slerp(clampedRotation, zeroedXZQuaternion, Time.fixedDeltaTime);
      // }
      // if (Mathf.Approximately(inputTurnForce, 0f))
      // {
      //   if (transform.rotation != clampedRotation)
      //   {
      //     vehicleRootBody.MoveRotation(clampedRotation);
      //   }
      //
      //   return;
      // }
      // var magicRotation = transform.rotation *
      //                     Quaternion.AngleAxis(
      //                       magicTurnRate * inputTurnForce * Time.deltaTime,
      //                       transform.up);

      // RotateAroundCenterOfMass(vehicleRootBody, magicTurnRate, inputForwardForce);
      // var magicRotation = transform.rotation *
      //                     Quaternion.AngleAxis(
      //                       magicTurnRate * inputTurnForce * Time.deltaTime,
      //                       transform.up);
      // vehicleRootBody.MoveRotation(magicRotation);
    }

    public bool IsControlling = false;

    /// <summary>
    /// This is the absolute max value torque has before it is clamped both positive and negative.
    /// </summary>
    public void UpdateMaxRPM()
    {
      var maxTotalTorque = baseMotorTorque * Mathf.Pow(inputForwardForce, 2) * 2;
      MaxWheelRPM = Mathf.Clamp(Mathf.Abs(maxTotalTorque), -2500f, 2500f);

      if (MaxWheelRPM == 0f)
      {
        MaxWheelRPM = 0.01f;
      }
    }

    public void UpdateRotatorEngine(HingeJoint rotatorEngine)
    {
      if (rotatorEngine == null) return;
      var motor = rotatorEngine.motor;
      if (isBreaking)
      {
        motor.force = 0;
        motor.targetVelocity = 0;
        rotatorEngine.motor = motor;
        return;
      }
      rotatorEngine.axis = new Vector3(isForward ? -1 : 1, 0, 0);
      motor.targetVelocity = MaxWheelRPM;
      motor.force = Mathf.Abs(inputForwardForce * baseMotorTorque * 1000);
      rotatorEngine.motor = motor;
    }

    public void UpdateRotatorEngines()
    {
      rotatorEngineHingeInstances.ForEach(UpdateRotatorEngine);
    }

    public void SetAcceleration(float val)
    {
      inputForwardForce = val;
      UpdateMaxRPM();
      UpdateRotatorEngines();
      isForward = inputForwardForce >= 0;
    }


    public void SetTurnInput(float val)
    {
      inputTurnForce = val;
    }

    public void ToggleIsBreaking()
    {
      isBreaking = !isBreaking;
      UpdateRotatorEngines();
    }

    public void SetIsBreaking(bool val)
    {
      isBreaking = val;
    }

    private bool isBreakDown = true;

    /// <summary>
    /// This will need logic to check if the player is the owner and if the player is controlling the vehicle actively.
    ///
    /// - To be called within VehicleMovementController.
    /// </summary>
    public void UpdateControls()
    {
      if (!IsControlling) return;
      if (UseManualControls) return;

      var isBreakingPressed = Input.GetKeyDown(KeyCode.Space);
      if (!isBreakingPressed && isBreakDown)
      {
        isBreakDown = false;
      }
      if (isBreakingPressed && !isBreakDown)
      {
        ToggleIsBreaking();
        isBreakDown = true;
      }

      inputForwardForce = Input.GetAxis("Vertical");
      inputTurnForce = Input.GetAxis("Horizontal");
    }
  }
}