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
    public float topSpeed = 10.0f;

    [Tooltip(
      "For tanks with front/rear wheels defined, this is how far those wheels turn.")]
    public float steeringAngle = 30.0f;

    [Tooltip("Power of any wheel listed under powered wheels.")]
    public float motorTorque = 10.0f;

    [Tooltip(
      "Turn rate that is \"magically\" applied regardless of what the physics state of the tank is.")]
    public float magicTurnRate = 45.0f;

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
      vehicleSizeThresholdFor5thSet = 30f; // Threshold for adding 5th set

    public float
      vehicleSizeThresholdFor6thSet = 60f; // Threshold for adding 6th set

    [Tooltip(
      "User input forces")]
    public float inputForwardForce;
    public float inputTurnForce;
    public float turnInputOverride = 0;


    public bool isBreaking = false;
    public bool UseManualControls = false;

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, MeshRenderer>
      WheelcollidersToWheelRenderMap =
        new();

    private readonly List<GameObject> wheelInstances = new();

    public bool hasInitialized;

    private Rigidbody rigid;
    internal List<WheelCollider> wheelColliders = new();

    public Transform wheelParent;

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
    }

    private void UpdateCenterOfMass(float yOffset)
    {
      rigid.automaticCenterOfMass = false;

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

      CleanupWheels();

      if (wheelColliders.Count > 0 || front.Count > 0 || rear.Count > 0 || left.Count > 0 || right.Count > 0 || poweredWheels.Count > 0)
      {
        Debug.LogWarning("VehicleWheelController is not probably cleaned up. Must call CleanupWheels before. Exiting InitializeWheels");
        yield break;
      }

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
      if (wheelColliders.Count > 0)
      {
        wheelColliders.Clear();
      }
      
      wheelColliders = wheelParent.GetComponentsInChildren<WheelCollider>()
        .ToList();

      foreach (var wheelCollider in wheelColliders)
      {
        if (wheelCollider.name.StartsWith("Front"))
          front.Add(wheelCollider);

        if (wheelCollider.name.StartsWith("Rear"))
          rear.Add(wheelCollider);

        if (wheelCollider.transform.parent.name.Contains("right"))
          right.Add(wheelCollider);

        if (wheelCollider.transform.parent.name.Contains("left"))
          left.Add(wheelCollider);

        poweredWheels.Add(wheelCollider);
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

      Physics.SyncTransforms();
      var isXBounds = IsXBoundsAlignment();
      var totalWheelSets = CalculateTotalWheelSets(bounds, isXBounds);

      // Clear any existing wheel sets
      foreach (var set in wheelInstances) Destroy(set);
      wheelInstances.Clear();

      // var spacing = (isXBounds ? bounds.size.x : bounds.size.z) /
      //               (totalWheelSets - 1);
      var spacing = bounds.size.z / Math.Max(totalWheelSets - 1, 1);

      // Generate wheel sets dynamically
      for (var i = 0; i < totalWheelSets; i++)
      {
        for (var directionIndex = 0; directionIndex < 2; directionIndex++)
        {
          var isLeft = directionIndex == 0;
          var localPosition =
            GetWheelLocalPosition(i, isLeft, totalWheelSets, bounds,
              spacing,
              isXBounds);
          var wheelInstance = Instantiate(wheelPrefab, wheelParent);
          wheelInstance.transform.localPosition = localPosition;
          var dirName = isLeft ? "left" : "right";
          var positionName = GetWheelPositionName(i, totalWheelSets);
          wheelInstance.name = $"ValheimVehicles_VehicleLand_wheel_{positionName}_{dirName}_{i}";
          SetWheelPhysics(wheelInstance, bounds, isXBounds, i, isLeft,
            totalWheelSets);
          wheelInstances.Add(wheelInstance);
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

    private int CalculateTotalWheelSets(Bounds bounds, bool isXBounds)
    {
      var vehicleSize =
        isXBounds
          ? bounds.size.x
          : bounds.size.z; // Assuming size along the Z-axis determines length

      if (vehicleSize >= vehicleSizeThresholdFor6thSet) return 6;

      if (vehicleSize >= vehicleSizeThresholdFor5thSet) return 5;

      return minimumWheelSets;
    }

    public float WheelBottomOffset = 0;
    public float WheelRadius = 1.5f;

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
    private Vector3 GetWheelLocalPosition(int index, bool isLeft, int totalWheelSets,
      Bounds bounds, float spacing, bool isXBounds)
    {
      // Calculate the local position directly within the bounds
      var xPos = isLeft ? bounds.min.x : bounds.max.x;
      var zPos = bounds.min.z + spacing * index;
      // var ratio = index / Math.Max(totalWheelSets, 1);
      // var zPos = bounds.size.z * ratio * bounds.min.z;
      var localPosition = new Vector3(xPos, -bounds.extents.y + WheelBottomOffset, zPos);

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

    private void SetWheelPhysics(GameObject wheelObj, Bounds bounds,
      bool isXBoundsAligned, int wheelSetIndex, bool isLeft, int totalWheelSets)
    {
      var wheelRenderer =
        wheelObj.transform.Find("mesh").GetComponent<MeshRenderer>();
      var wheelCollider =
        wheelObj.transform.Find("collider").GetComponent<WheelCollider>();

      var isMiddle = IsMiddleIndex(wheelSetIndex, totalWheelSets);

      wheelCollider.radius = WheelRadius;

      // Setting higher forward stiffness for front and rear wheels allows for speeds to be picked up.
      if (!isMiddle)
      {
        var forwardRightFriction = wheelCollider.forwardFriction;
        forwardRightFriction.stiffness = 1f;
        wheelCollider.forwardFriction = forwardRightFriction;

        var sideFriction = wheelCollider.sidewaysFriction;
        sideFriction.stiffness = 0.1f;
        wheelCollider.sidewaysFriction = sideFriction;
      }
      else
      {
        var forwardRightFriction = wheelCollider.forwardFriction;
        forwardRightFriction.stiffness = 0.5f;
        wheelCollider.forwardFriction = forwardRightFriction;
        var sideFriction = wheelCollider.sidewaysFriction;
        sideFriction.stiffness = 10f;
        wheelCollider.sidewaysFriction = sideFriction;
      }

      if (WheelcollidersToWheelRenderMap.ContainsKey(wheelCollider))
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

    public static float defaultBreakForce = 250f;
    public float additionalBreakForce = defaultBreakForce;
    private float deltaRunPoweredWheels = 0f;

    public float wheelOffset = 0.25f;
    private void AdjustVehicleToGround(Bounds bounds)
    {
      if (!rigid) return;

      var terrainMask = LayerMask.GetMask("terrain"); // Ensure "Terrain" is a valid layer name in your project.
      var highestGroundPoint = float.MinValue;
      var isAboveTerrain = false;
      var maxWheelRadius = 0.5f;

      foreach (var wheel in wheelColliders)
      {
        var wheelPosition = wheel.transform.position;

        // Perform raycasts above and below the wheel position
        if (Physics.Raycast(wheelPosition, Vector3.down, out var hitBelow, 80, terrainMask))
        {
          highestGroundPoint = Mathf.Max(highestGroundPoint, hitBelow.point.y);
        }

        if (Physics.Raycast(wheelPosition, Vector3.up, 80, terrainMask))
        {
          isAboveTerrain = true;
        }

        maxWheelRadius = Mathf.Max(maxWheelRadius, wheel.radius);
      }

      // If terrain is detected both above and below, skip adjustment
      if (isAboveTerrain)
      {
        Debug.Log("Vehicle is trapped between terrain above and below. Skipping adjustment.");
        return;
      }

      var offsetY = maxWheelRadius + highestGroundPoint - bounds.min.y + wheelOffset; // Align the bottom of the vehicle to the highest ground point
      // Calculate the required vertical adjustment
      var vehiclePosition = rigid.transform.position;

      var newPosition = new Vector3(vehiclePosition.x, vehiclePosition.y + offsetY, vehiclePosition.z);


      // todo move all objects on rigidbody upwards without causing kinematic problems.

      // Move the rigidbody kinematically
      var originalKinematicState = rigid.isKinematic;
      rigid.isKinematic = true;
      rigid.transform.position = newPosition;
      rigid.isKinematic = originalKinematicState;

      if (rigid.isKinematic == false)
      {
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
      }
    }

    /// <summary>
    ///   POWERED WHEELSoffs
    ///   Sets the motor torque of the wheel based on forward input. This moves
    ///   the tank forwards and backwards.
    /// </summary>
    private void RunPoweredWheels()
    {
      if (deltaRunPoweredWheels > 4)
      {
        deltaRunPoweredWheels = 0f;
      }
      else
      {
        deltaRunPoweredWheels += Time.fixedDeltaTime;
      }

      foreach (var wheel in poweredWheels.ToList())
      {
        if (wheel == null) continue;
        if (isBreaking)
        {
          wheel.brakeTorque = inputForwardForce * motorTorque + additionalBreakForce;
        }
        else
        {
          wheel.brakeTorque = 0f;
          // To create a top speed for the tank, the motor torque just
          // cuts out when the tank starts moving fast enough.
          if (rigid.velocity.magnitude <= topSpeed)
            wheel.motorTorque = inputForwardForce * motorTorque;
          else
            wheel.motorTorque = 0.0f;
        }

        if (wheelInstances != null &&
            WheelcollidersToWheelRenderMap.TryGetValue(wheel,
              out var wheelRenderer))
        {
          if (wheelRenderer == null)
            continue;
          // var wheelTransform = wheelRenderer.transform;
          // wheel.GetWorldPose(out var position, out var rotation);
          // wheelTransform.position = position;
          // wheelTransform.rotation = rotation;
          // var wheelRotation = wheelRenderer.transform.rotation.eulerAngles;
          // var rotX = wheelRenderer.transform.rotation.eulerAngles.x *
          //            deltaRunPoweredWheels;
          // var rot = Quaternion.Euler(
          //   Mathf.Clamp(rotX,
          //     0f, 365f), wheelRotation.y, wheelRotation.z);
          // wheelRenderer.transform.rotation = rot;
          RotateOnXAxis(wheel, wheelRenderer.transform);
        }
      }
      // Update wheel mesh positions to match the physics wheels.
    }

    private Quaternion RotateOnXAxis(WheelCollider wheelCollider,
      Transform wheelTransform)
    {
      // var wheelRb = wheelTransform.GetComponent<Rigidbody>();

      float deltaRotation = wheelCollider.motorTorque * Time.deltaTime;

      // wheels need to move their X coordinate but for some reason it's the Y axis rotated...baffling.
      wheelTransform.Rotate(deltaRotation > 0 ? Vector3.down : Vector3.up,
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

        wheel.motorTorque += motorTorque * inputTurnForce;
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
        wheel.motorTorque += motorTorque * -inputTurnForce;
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
      if (Mathf.Approximately(inputTurnForce, 0f)) return;
      var magicRotation = transform.rotation *
                          Quaternion.AngleAxis(
                            magicTurnRate * inputTurnForce * Time.deltaTime,
                            transform.up);
      rigid.MoveRotation(magicRotation);
    }

    public bool IsControlling = false;

    public void SetAcceleration(float val)
    {
      inputForwardForce = val;
    }


    public void SetTurnInput(float val)
    {
      inputTurnForce = val;
    }

    public void SetIsBreaking(bool val)
    {
      isBreaking = val;
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

      isBreaking = Input.GetKey(KeyCode.Space);
      inputForwardForce = Input.GetAxis("Vertical");
      inputTurnForce = Input.GetAxis("Horizontal");
    }

    // We run this only in Unity Editor
#if UNITY_EDITOR
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
    }
#endif
  }
}