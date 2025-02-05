using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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

    public int minimumWheelSets = 3; // Minimum number of wheel sets
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
    private readonly Dictionary<WheelCollider, MeshRenderer>
      WheelcollidersToWheelRenderMap =
        new();

    private readonly List<GameObject> wheelInstances = new();

    public bool hasInitialized;

    private Rigidbody rigid;
    internal List<WheelCollider> wheelColliders = new();
    public List<Collider> colliders = new();

    public Transform wheelParent;

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

    // wheel regeneration
    public static bool shouldCleanupPerInitialize = true;

    private void Awake()
    {
#if UNITY_EDITOR
      var ghostContainer = transform.Find("ghostContainer");
      if (ghostContainer) ghostContainer.gameObject.SetActive(false);
#endif
      wheelParent = transform.Find("wheels");

      rigid = GetComponent<Rigidbody>();
      var centerOfMass = transform.Find("center_of_mass");
      if (centerOfMass != null) centerOfMassTransform = centerOfMass;
      if (centerOfMassTransform == null) centerOfMassTransform = transform;
      UpdateMaxRPM();
    }

    private void UpdateCenterOfMass(float yOffset)
    {
      rigid.ResetCenterOfMass();

      var centerOfMass = rigid.centerOfMass;
      centerOfMass = new Vector3(centerOfMass.x, yOffset, centerOfMass.z);

      rigid.centerOfMass = centerOfMass;
    }

    private void OnDisable()
    {
      CleanupWheels();
    }

    private void CleanupWheels()
    {
      if (wheelInstances.Count > 0)
      {
        foreach (var set in wheelInstances)
        {
          if (set != null)
          {

            Destroy(set);
          }
        }
        wheelInstances.Clear();
      }

      WheelcollidersToWheelRenderMap.Clear();
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
      if (!hasInitialized || wheelColliders.Count == 0) return;
      RunPoweredWheels();
      RunSteering();
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
      if (initializeWheelsCoroutine != null)
      {
        StopCoroutine(initializeWheelsCoroutine);
      }
      initializeWheelsCoroutine = StartCoroutine(InitializeWheelsCoroutine(bounds));
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
        CleanupWheels();
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
      if (wheelColliders.Count != wheelInstances.Count)
      {
        wheelColliders = wheelParent.GetComponentsInChildren<WheelCollider>()
          .ToList();
        colliders = wheelParent.GetComponentsInChildren<Collider>().ToList();
      }

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

    public string GetWheelPositionName(int index, int indexLength)
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
      if (totalWheelSets != wheelInstances.Count)
      {
        CleanupWheels();
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
          var positionName = GetWheelPositionName(i, totalWheelSets);
          wheelInstance.name = $"ValheimVehicles_VehicleLand_wheel_{positionName}_{dirName}_{i}";
          SetWheelProperties(wheelInstance, bounds, i, isLeft,
            totalWheelSets);

          if (wheelInstances.Count >= wheelIndex)
          {
            wheelInstances.Add(wheelInstance);
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
      var localPosition = new Vector3(xPos, bounds.min.y + wheelBottomOffset, zPos);

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
      var wheelRenderer =
        wheelObj.transform.Find("wheel_mesh").GetComponent<MeshRenderer>();
      var wheelCollider =
        wheelObj.transform.Find("wheel_collider").GetComponent<WheelCollider>();

      var isMiddle = IsMiddleIndex(wheelSetIndex, totalWheelSets);
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
        wheelRenderer.transform.localScale = new Vector3(wheelScalar * wheelMeshLocalScale.x, wheelMeshLocalScale.y, wheelScalar * wheelMeshLocalScale.z);
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

      if (!WheelcollidersToWheelRenderMap.ContainsKey(wheelCollider))
        WheelcollidersToWheelRenderMap.Add(wheelCollider, wheelRenderer);
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

      var inputForceDir = Mathf.Sign(inputForwardForce);

      foreach (var wheel in poweredWheels)
      {
        if (wheel == null) continue;
        if (isBreaking)
        {
          wheel.motorTorque = 0f;
          wheel.brakeTorque = wheel.rpm + inputForwardForce * baseMotorTorque + additionalBreakForce;
        }
        if (!isBreaking)
        {
          if (inputForwardForce == 0)
          {
            wheel.motorTorque = 0;
            wheel.brakeTorque = inputForwardForce * baseMotorTorque / 2f + additionalBreakForce * Time.fixedDeltaTime;
          }
          else if (Mathf.Abs(wheel.rpm) > MaxWheelRPM || rigid.velocity.x + rigid.velocity.z >= topSpeed)
          {
            return;
          }
          else if (Mathf.Abs(wheel.rpm) > MaxWheelRPM || !wheel.isGrounded)
          {
            wheel.motorTorque = 0f;
          }
          else if (!Mathf.Approximately(MaxWheelRPM, 0f))
          {
            // To create a top speed for the tank, the motor torque just
            // cuts out when the tank starts moving fast enough.
            if (wheel.brakeTorque > 0f)
            {
              wheel.brakeTorque = 0f;
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

            wheel.motorTorque = Mathf.Clamp(nextMotorTorque + additiveTorque, -MaxWheelRPM, MaxWheelRPM);
          }
        }

        if (wheelInstances != null &&
            WheelcollidersToWheelRenderMap.TryGetValue(wheel,
              out var wheelRenderer))
        {
          SyncWheelVisualWithCollider(wheelRenderer, wheel);
        }
      }
    }

    /// <summary>
    /// Update wheel mesh positions to match the physics wheels.
    /// </summary>
    /// <param name="wheelRenderer"></param>
    /// <param name="wheelCollider"></param>
    private void SyncWheelVisualWithCollider(MeshRenderer wheelRenderer, WheelCollider wheelCollider)
    {
      if (wheelRenderer == null) return;
      var wheelTransform = wheelRenderer.transform;
      wheelCollider.GetWorldPose(out var position, out _);
      wheelTransform.position = position;
      RotateOnXAxis(wheelCollider, wheelRenderer.transform);
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
      foreach (var wheel in front)
        wheel.steerAngle = inputTurnForce * steeringAngle;
      foreach (var wheel in rear)
        wheel.steerAngle = -inputTurnForce * steeringAngle;
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
          rigid.MoveRotation(clampedRotation);
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
      rigid.MoveRotation(magicRotation);
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

    public void SetAcceleration(float val)
    {
      inputForwardForce = val;
      UpdateMaxRPM();
    }


    public void SetTurnInput(float val)
    {
      inputTurnForce = val;
    }

    public void ToggleIsBreaking()
    {
      isBreaking = !isBreaking;
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

    // We run this only in Unity Editor
#if UNITY_EDITOR
    private float centerOfMassOffset = -10f;
    private void Start()
    {
      if (!Application.isPlaying) return;
      // inputForwardForce = 500f;
      // if (boundsTransform != null)
      // InitializeWheels();
    }

    private void Update()
    {
      if (!Application.isPlaying) return;
      UpdateControls();
    }

    private void FixedUpdate()
    {
      if (!Application.isPlaying) return;
      VehicleMovementFixedUpdate();
      UpdateCenterOfMass(centerOfMassOffset);
      UpdateMaxRPM();
    }
#endif
  }
}