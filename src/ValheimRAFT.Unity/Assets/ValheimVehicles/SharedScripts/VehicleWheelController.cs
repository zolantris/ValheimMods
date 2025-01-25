using System;
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
    public Transform centerOfMass;

    [Tooltip(
      "Front wheels used for steering by rotating the wheels left/right.")]
    public List<WheelCollider> front;

    [Tooltip("Rear wheels for steering by rotating the wheels left/right.")]
    public List<WheelCollider> rear;

    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public List<WheelCollider> poweredWheels;

    [Tooltip(
      "Wheels on the left side of the tank that are used for differential steering.")]
    public List<WheelCollider> left;

    [Tooltip(
      "Wheels on the right side of the tank that are used for differential steering.")]
    public List<WheelCollider> right;

    public SteeringType m_steeringType = SteeringType.Differential;

    [Header("Wheel Settings")]
    public Transform boundsTransform; // Reference for bounds

    public Transform forwardDirection; // Dynamic rotation reference

    [FormerlySerializedAs("wheelSetPrefab")]
    public GameObject wheelPrefab; // Prefab for a single wheel set

    public int minimumWheelSets = 3; // Minimum number of wheel sets
    public float axelPadding = 0.5f; // Extra length on both sides of axel

    public float
      vehicleSizeThresholdFor5thSet = 18f; // Threshold for adding 5th set

    public float
      vehicleSizeThresholdFor6thSet = 30f; // Threshold for adding 6th set

    public float forwardInput;


    public bool isBreaking = false;
    public bool UseManualControls = false;

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<WheelCollider, MeshRenderer>
      WheelcollidersToWheelRenderMap =
        new();

    private readonly List<GameObject> wheelInstances = new();

    private bool hasInitialized;

    private Rigidbody rigid;
    internal float turnInput;
    internal List<WheelCollider> wheelColliders = new();

    private Transform wheelParent;

    private void Awake()
    {
      var ghostContainer = transform.Find("ghostContainer");
      if (ghostContainer) ghostContainer.gameObject.SetActive(false);

      wheelParent = transform.Find("wheels");
      rigid = GetComponent<Rigidbody>();
      var centerOfMassTransform = transform.Find("center_of_mass");
      if (centerOfMassTransform != null) centerOfMass = centerOfMassTransform;
    }

    private void OnDestroy()
    {
      if (wheelInstances.Count > 0)
      {
        foreach (var set in wheelInstances) Destroy(set);
        wheelInstances.Clear();
      }

      WheelcollidersToWheelRenderMap.Clear();
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

    /// <summary>
    ///   Must pass a bounds in
    /// </summary>
    /// <param name="bounds"></param>
    public void ReInitializeWheels(Bounds bounds)
    {
      hasInitialized = false;
      GenerateWheelSets(bounds);
      SetupWheels();
      // Override center of mass when a reference is passed in.
      if (centerOfMass != null)
      {
        if (centerOfMass.parent == transform)
          rigid.centerOfMass = centerOfMass.localPosition;
        else
          Debug.LogWarning(name +
                           ": PhysicsTank cannot override center of mass when " +
                           centerOfMass.name + " is not a child of " +
                           transform.name);
      }

      hasInitialized = true;
    }

    private void SetupWheels()
    {
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
      }

      poweredWheels = wheelColliders;
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
        positionName = "rear";
      }

      return positionName;
    }

    /// <summary>
    ///   Generates wheel sets
    /// </summary>
    /// TODO fix the rotated wheel issue where wheels get out of alignment for 90 degrees and -90 variants.
    /// <param name="bounds"></param>
    private void GenerateWheelSets(Bounds bounds)
    {
      if (!boundsTransform || !wheelPrefab || !forwardDirection)
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

      var spacing = (isXBounds ? bounds.size.x : bounds.size.z) /
                    (totalWheelSets - 1);

      // Generate wheel sets dynamically
      for (var i = 0; i < totalWheelSets; i++)
      {
        for (var directionIndex = 0; directionIndex < 2; directionIndex++)
        {
          var isLeft = directionIndex == 0;
          var position =
            CalculateWheelSetPosition(i, isLeft, totalWheelSets, bounds,
              spacing,
              isXBounds);
          var wheelInstance = Instantiate(wheelPrefab, position,
            forwardDirection.rotation,
            wheelParent);
          var dirName = isLeft ? "left" : "right";
          var positionName = GetWheelPositionName(i, directionIndex);
          wheelInstance.name = $"wheel_{positionName}_{dirName}_{i}";
          AdjustWheelInstance(wheelInstance, bounds, isXBounds, i, isLeft,
            totalWheelSets);
          wheelInstances.Add(wheelInstance);
        }
      }

      // if (isXBounds) wheelParent.position += new Vector3(spacing, 0, 0);
      // if (isXBounds && totalWheelSets > 5) wheelParent.position += new Vector3(-2.5f, 0, 0);
      // wheelParent.transform.position += deltaWheelParentToBounds;
    }

    private Bounds GetBounds(Transform boundsTransform)
    {
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

    public float WheelBottomOffset = 3f;

    private Vector3 CalculateWheelSetPosition(int index, bool isLeft,
      int totalWheelSets,
      Bounds bounds, float spacing, bool isXBounds)
    {
      var xPosition =
        isXBounds
          ? bounds.min.x + spacing * index
          : (isLeft ? bounds.min.x : bounds.max.x);

      // if (isXBounds) xPosition += bounds.extents.x / totalWheelSets;

      var zPosition =
        isXBounds
          ? (isLeft ? bounds.min.z : bounds.max.z)
          : bounds.min.z + spacing * index;

      return new Vector3(xPosition, bounds.min.y - WheelBottomOffset,
        zPosition);
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

    private void AdjustWheelInstance(GameObject wheelObj, Bounds bounds,
      bool isXBoundsAligned, int wheelSetIndex, bool isLeft, int totalWheelSets)
    {
      var wheelRenderer =
        wheelObj.transform.Find("mesh").GetComponent<MeshRenderer>();
      var wheelCollider =
        wheelObj.transform.Find("collider").GetComponent<WheelCollider>();

      var isMiddle = IsMiddleIndex(wheelSetIndex, totalWheelSets);
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

    /// <summary>
    ///   POWERED WHEELS
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

      foreach (var wheel in poweredWheels)
      {
        if (isBreaking)
        {
          wheel.brakeTorque = forwardInput * motorTorque + additionalBreakForce;
        }
        else
        {
          wheel.brakeTorque = 0f;
          // To create a top speed for the tank, the motor torque just
          // cuts out when the tank starts moving fast enough.
          if (rigid.velocity.magnitude <= topSpeed)
            wheel.motorTorque = forwardInput * motorTorque;
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
        var isTorqueAndTurnNearZero = Mathf.Approximately(forwardInput, 0) &&
                                      Mathf.Approximately(turnInput, 0);
        if (wheel.motorTorque > 0 && isTorqueAndTurnNearZero)
        {
          wheel.motorTorque = Mathf.Lerp(wheel.motorTorque, 0,
            Time.fixedDeltaTime * 50f);
          continue;
        }

        if (isTorqueAndTurnNearZero) continue;

        wheel.motorTorque += motorTorque * turnInput;
      }

      foreach (var wheel in right)
      {
        var isTorqueAndTurnNearZero = Mathf.Approximately(forwardInput, 0) &&
                                      Mathf.Approximately(turnInput, 0);
        if (wheel.motorTorque > 0 && isTorqueAndTurnNearZero)
        {
          wheel.motorTorque = Mathf.Lerp(wheel.motorTorque, 0,
            Time.fixedDeltaTime * 50f);
          continue;
        }

        if (isTorqueAndTurnNearZero) continue;
        wheel.motorTorque += motorTorque * -turnInput;
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
        wheel.steerAngle = turnInput * steeringAngle;
      foreach (var wheel in rear)
        wheel.steerAngle = -turnInput * steeringAngle;
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
      var magicRotation = transform.rotation *
                          Quaternion.AngleAxis(
                            magicTurnRate * turnInput * Time.deltaTime,
                            transform.up);
      rigid.MoveRotation(magicRotation);
    }

    public bool IsControlling = false;

    public void SetAcceleration(float val)
    {
      forwardInput = val;
    }


    public void SetTurnInput(float val)
    {
      turnInput = val;
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
      forwardInput = Input.GetAxis("Vertical");
      turnInput = Input.GetAxis("Horizontal");
    }

    // We run this only in Unity Editor
#if UNITY_EDITOR
    private void Start()
    {
      if (!Application.isPlaying) return;
      if (boundsTransform != null)
        ReInitializeWheels(GetBounds(boundsTransform));
    }

    public float turnInputOverride = 0;

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