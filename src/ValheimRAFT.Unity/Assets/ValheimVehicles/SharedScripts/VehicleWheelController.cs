#region

using System;
using System.Collections.Generic;
using System.Linq;
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

    public int minimumWheelSets = 3; // Minimum number of wheel sets
    public int maxWheelSets = 5; // Max number of wheel sets (FYI 20 is limit of all wheels so 10 total sets)
    public float
      wheelSetIncrement = 5f;
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


    public float inputTurnMultiplier = 1f;
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
    public bool isForward = true;

    public bool lastTurningState;
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
    private bool isLeftForward = true;
    private bool isRightForward = true;
    private bool isTurningInPlace;
    private float lerpedTurnFactor;
    private float maxSpeed = 25f; // Default top speed
    internal MovingTreadComponent movingTreadLeft;
    internal MovingTreadComponent movingTreadRight;

    internal float powerWheelDeltaInterval = 0.3f;
    internal List<GameObject> rotationEngineInstances = new();
    internal List<HingeJoint> rotatorEngineHingeInstances = new();

    private float smoothRotationTime;

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
        treadsRight = transform.Find("treads/treads_right");
      }
      if (!treadsLeft)
      {
        treadsLeft = transform.Find("treads/treads_left");
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

      if (treadsLeft && treadsRight)
      {
        var leftJoint = treadsLeft.GetComponent<ConfigurableJoint>();
        var rightJoint = treadsRight.GetComponent<ConfigurableJoint>();

        var treadsAnchorLeftLocalPosition = new Vector3(bounds.min.x - 0.5f, bounds.min.y + wheelBottomOffset + wheelRadius * 2, bounds.center.z);
        var treadsAnchorRightLocalPosition = new Vector3(bounds.max.x + 0.5f, bounds.min.y + wheelBottomOffset + wheelRadius * 2, bounds.center.z);

        // var localPosY = Vector3.up * (-bounds.extents.y + wheelRadius);

        treadsLeft.localPosition = treadsAnchorLeftLocalPosition;
        treadsRight.localPosition = treadsAnchorRightLocalPosition;

        ConfigureJoint(leftJoint, vehicleRootBody, Vector3.zero);
        ConfigureJoint(rightJoint, vehicleRootBody, Vector3.zero);
      }
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
      if (!hasInitialized || wheelInstances.Count == 0) return;
      _hasSyncedWheelsOnCurrentFrame = false;
      currentBreakTorqueLeft = 0f;
      currentBreakTorqueRight = 0f;
      currentMotorTorqueLeft = 0f;
      currentMotorTorqueRight = 0f;
      isTurningInPlace = Mathf.Approximately(inputMovement, 0f) && Mathf.Abs(inputTurnForce) > 0f;

      if (isTurningInPlace != lastTurningState)
      {
        lastTurningState = isTurningInPlace;
        ApplyFrictionValuesToAllWheels();
      }

      if (smoothRotationTime > 5f)
      {
        smoothRotationTime = 0f;
      }
      smoothRotationTime += Time.fixedDeltaTime;
      currentSpeed = vehicleRootBody.velocity.magnitude;

      AdjustForHills();
      // ApplyDownforce();

      if (isBraking)
        ApplyBreaks();
      else
        ApplyTorque(inputMovement, inputTurnForce);

      if (movingTreadLeft && movingTreadRight)
      {
        movingTreadLeft.SetSpeed(currentSpeed);
        movingTreadRight.SetSpeed(currentSpeed);
        UpdateSteeringTreadVisuals();
      }

      wheelColliders.ForEach(SyncWheelVisual);
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

      if (bounds.Value.size.x < 4f || bounds.Value.size.y < 2f || bounds.Value.size.z < 4f)
      {
        bounds = new Bounds(Vector3.zero, new Vector3(1, 1f, 1f));
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
      UpdateAccelerationValues(accelerationType, isForward);


      // must be called after SetupWheels
      UpdateTreads(bounds.Value);
      // AdjustVehicleToGround(bounds.Value);
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
      // ScaleMeshToFitBounds(rotatorMeshTransform.transform, targetScale);

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
      var setInt = Mathf.RoundToInt(vehicleSize / wheelSetIncrement);
      if (setInt % 2 == 0)
      {
        setInt += 1;
      }

      var nearestIncrement = Mathf.Clamp(setInt, minimumWheelSets, maxWheelSets);
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

      // var ratio = index / Math.Max(totalWheelSets, 1) KF UdYCOU
      // var zPos = bounds.size.z * ratio * bounds.min.z;
      var localPosition = new Vector3(xPos, bounds.min.y - (wheelBottomOffset + wheelRadius), zPos);

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

      wheelCollider.mass = wheelMass;
      wheelCollider.wheelDampingRate = wheelDamping;
      wheelCollider.radius = wheelRadius;
      wheelCollider.suspensionDistance = wheelSuspensionDistance;

      var suspensionSpring = wheelCollider.suspensionSpring;
      suspensionSpring.spring = wheelSuspensionSpring;
      wheelCollider.suspensionSpring = suspensionSpring;
      var wheelScalar = wheelCollider.radius / wheelBaseRadiusScale;

      if (!Mathf.Approximately(wheelScalar, 1f))
      {
        wheelVisual.transform.localScale = new Vector3(wheelScalar * wheelMeshLocalScale.x, wheelMeshLocalScale.y * wheelBaseRadiusScale, wheelScalar * wheelMeshLocalScale.z);
      }

      if (wheelCollider)
      {
        ApplyFrictionToWheelCollider(wheelCollider);
      }

      if (!WheelcollidersToWheelVisualMap.ContainsKey(wheelCollider))
        WheelcollidersToWheelVisualMap.Add(wheelCollider, wheelVisual);
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
      var deltaRotation = Mathf.Clamp(wheelCollider.rpm * Time.fixedDeltaTime, -359f, 359f);
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

    // private void RotateAroundCenterOfMass(Rigidbody rb, float turnRate, float inputTurnForce)
    // {
    //   rb.ResetCenterOfMass();
    //   var com = rb.worldCenterOfMass; // Get world-space center of mass
    //   var rotationDelta = Quaternion.AngleAxis(turnRate * inputTurnForce * Time.deltaTime, rb.transform.up);
    //
    //   // Compute the new position to maintain correct pivoting
    //   var offset = rb.position - com;
    //   var newPosition = com + rotationDelta * offset;
    //
    //   // Apply the rotation and maintain the correct position
    //   rb.Move(newPosition, rotationDelta * rb.rotation);
    //   // rb.position = newPosition; // This ensures no teleportation occurs
    //
    //   UpdateCenterOfMass(cent);
    // }

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


    private void ApplyBreaks()
    {
      currentSpeed = GetTankSpeed();
      var brakeForce = vehicleRootBody.mass * Mathf.Abs(currentSpeed) / 2f; // Stops in ~2s

      foreach (var wheel in wheelColliders)
      {
        // Apply braking force based on current torque & momentum
        wheel.brakeTorque = Mathf.Max(brakeForce, Mathf.Abs(wheel.motorTorque) * 1.5f);
        wheel.motorTorque = 0; // Cut off power when braking
      }
    }

    private void ApplyDownforce()
    {
      vehicleRootBody.AddForce(-transform.up * downforceAmount, ForceMode.Acceleration);
    }

    private void ApplyFrictionToWheelCollider(WheelCollider wheel)
    {
      var frictionMultiplier = Mathf.Clamp(10f / wheelColliders.Count, 0.7f, 1.5f);

      var newForwardFriction = new WheelFrictionCurve
      {
        extremumSlip = 0.4f * frictionMultiplier,
        extremumValue = 1.5f * frictionMultiplier,
        asymptoteSlip = 2.0f * frictionMultiplier,
        asymptoteValue = 0.5f * frictionMultiplier,
        stiffness = 2.0f
      };

      var newSidewaysFriction = new WheelFrictionCurve
      {
        extremumSlip = isTurningInPlace ? 0.8f * frictionMultiplier : 0.5f * frictionMultiplier,
        extremumValue = isTurningInPlace ? 0.6f * frictionMultiplier : 0.7f * frictionMultiplier,
        asymptoteSlip = isTurningInPlace ? 1.5f * frictionMultiplier : 1.5f * frictionMultiplier,
        asymptoteValue = isTurningInPlace ? 0.4f * frictionMultiplier : 0.3f * frictionMultiplier,
        stiffness = isTurningInPlace ? 1.0f : 1.2f // Less grip for turning
      };

      wheel.forwardFriction = newForwardFriction;
      wheel.sidewaysFriction = newSidewaysFriction;
    }

    private void ApplyFrictionValuesToAllWheels()
    {
      wheelColliders.ForEach(ApplyFrictionToWheelCollider);
    }

    private float GetTurnMultiplier()
    {
      return accelerationType switch
      {
        AccelerationType.Low => 1f,
        AccelerationType.Medium => 1f,
        AccelerationType.High => 0.65f,
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

      // Smooth turn factor
      lerpedTurnFactor = Mathf.Lerp(lerpedTurnFactor, Mathf.Abs(turn), Time.fixedDeltaTime * 5f);
      var speed = vehicleRootBody.velocity.magnitude;

      // Expected speed at full acceleration
      var targetSpeed = move * maxSpeed;
      var torqueBoost = hillFactor * uphillTorqueMultiplier;


      // If rolling backward and should be going forward, counteract gravity
      if (move > 0 && Vector3.Dot(vehicleRootBody.velocity, transform.forward) < 0)
      {
        torqueBoost += downhillResistance;
      }

      // Smoothly scale down extra torque when near target speed
      if (Mathf.Abs(speed - targetSpeed) < 1f)
      {
        torqueBoost *= 0.5f;
      }

      // Adjusted torque for hills
      var forwardTorque = (baseTorque + torqueBoost) * move;

      // Rotation logic
      var leftSideTorque = forwardTorque;
      var rightSideTorque = forwardTorque;

      if (Mathf.Abs(turn) >= 0.5f)
      {
        // **Full differential turn when turn is 0.5 or higher**
        leftSideTorque = turn > 0 ? baseTorque : -baseTorque;
        rightSideTorque = turn > 0 ? -baseTorque : baseTorque;
      }
      else if (turn != 0)
      {
        // **Ensure one side is stronger while the other weakens instead of scaling both down**
        var turnStrength = Mathf.Lerp(1f, 0.3f, lerpedTurnFactor); // More aggressive difference

        if (turn > 0) // Turning Right
        {
          leftSideTorque *= 1f; // Full power on left
          rightSideTorque *= turnStrength; // Reduced power on right
        }
        else // Turning Left
        {
          leftSideTorque *= turnStrength; // Reduced power on left
          rightSideTorque *= 1f; // Full power on right
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

    public void UpdateRotatorEngines()
    {
      rotatorEngineHingeInstances.ForEach(UpdateRotatorEngine);
    }

    public void SetInputMovement(float val)
    {
      if (accelerationType == AccelerationType.Stop || isBraking || Mathf.Approximately(val, 0f))
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
        AccelerationType.Low => mediumTorque,
        AccelerationType.Stop => 0f,
        _ => lowTorque
      };
      inputTurnMultiplier = GetTurnMultiplier();
      isForward = isMovingForward;
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
      var inputVertical = Input.GetAxis("Vertical");
      SetInputMovement(inputVertical);
      UpdateAccelerationValues(accelerationType, inputMovement >= 0);
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

      // should not be called outside of editor. These should be optimized outside of a fixed update.
      UpdateCenterOfMass(centerOfMassOffset);
      UpdateMaxRPM();
      UpdateAccelerationValues(accelerationType, inputMovement >= 0);

      // critical call, meant for all components
      VehicleMovementFixedUpdate();
    }
#endif
  }
}