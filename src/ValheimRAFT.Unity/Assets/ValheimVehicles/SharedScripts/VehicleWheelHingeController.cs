#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
  public class VehicleWheelHingeController : MonoBehaviour
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
    public List<HingeJoint> front;

    [Tooltip("Rear wheels for steering by rotating the wheels left/right.")]
    public List<HingeJoint> rear;

    [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
    public List<HingeJoint> poweredWheels;

    [Tooltip(
      "Wheels on the left side of the tank that are used for differential steering.")]
    public List<HingeJoint> left;

    [Tooltip(
      "Wheels on the right side of the tank that are used for differential steering.")]
    public List<HingeJoint> right;

    public SteeringType m_steeringType = SteeringType.Differential;

    [Header("Wheel Settings")]
    public Transform boundsTransform; // Reference for bounds

    public Transform forwardDirection; // Dynamic rotation reference

    public GameObject wheelSetPrefab; // Prefab for a single wheel set
    public int minimumWheelSets = 3; // Minimum number of wheel sets
    public float axelPadding = 0.5f; // Extra length on both sides of axel

    public float
      vehicleSizeThresholdFor5thSet = 18f; // Threshold for adding 5th set

    public float
      vehicleSizeThresholdFor6thSet = 30f; // Threshold for adding 6th set

    public float forwardInput;

    // Used to associate a wheel with a one of the model prefabs.
    private readonly Dictionary<HingeJoint, Transform>
      WheelHingeJointMap =
        new();

    private readonly List<GameObject> wheelSets = new();

    private float deltaRunPoweredWheels;

    private bool hasInitialized;

    private Rigidbody rigid;
    internal float turnInput;
    internal List<HingeJoint> wheelHingeJoints = new();

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
      if (wheelSets.Count > 0)
      {
        foreach (var set in wheelSets) Destroy(set);
        wheelSets.Clear();
      }

      WheelHingeJointMap.Clear();
      wheelHingeJoints.Clear();
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
      if (!hasInitialized || wheelHingeJoints.Count == 0) return;
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
      wheelHingeJoints = wheelParent.GetComponentsInChildren<HingeJoint>()
        .ToList();

      foreach (var wheelHingeJoint in wheelHingeJoints)
      {
        if (wheelHingeJoint.name.StartsWith("Front"))
          front.Add(wheelHingeJoint);

        if (wheelHingeJoint.name.StartsWith("Rear"))
          rear.Add(wheelHingeJoint);

        if (wheelHingeJoint.name.Contains("right"))
          right.Add(wheelHingeJoint);

        if (wheelHingeJoint.name.Contains("left"))
          left.Add(wheelHingeJoint);
      }

      poweredWheels = wheelHingeJoints;
    }

    public bool IsXBoundsAlignment()
    {
      // Determine the forward direction (X or Z axis) based on ForwardDirection rotation
      var forwardAngle =
        Mathf.Round(forwardDirection.eulerAngles.y / 90f) * 90f;
      var isXBounds = Mathf.Approximately(Mathf.Abs(forwardAngle) % 180, 90);
      return isXBounds;
    }

    /// <summary>
    ///   Generates wheel sets
    /// </summary>
    /// TODO fix the rotated wheel issue where wheels get out of alignment for 90 degrees and -90 variants.
    /// <param name="bounds"></param>
    private void GenerateWheelSets(Bounds bounds)
    {
      if (!boundsTransform || !wheelSetPrefab || !forwardDirection)
      {
        Debug.LogError(
          "Bounds Transform, Forward Direction, and Wheel Set Prefab must be assigned.");
        return;
      }

      Physics.SyncTransforms();
      var isXBounds = IsXBoundsAlignment();
      var totalWheelSets = CalculateTotalWheelSets(bounds, isXBounds);

      // Clear any existing wheel sets
      foreach (var set in wheelSets) Destroy(set);
      wheelSets.Clear();

      var spacing = (isXBounds ? bounds.size.x : bounds.size.z) /
                    (totalWheelSets - 1);

      // Generate wheel sets dynamically
      for (var i = 0; i < totalWheelSets; i++)
      {
        var position =
          CalculateWheelSetPosition(i, totalWheelSets, bounds, spacing,
            isXBounds);
        var wheelSet = Instantiate(wheelSetPrefab, position,
          forwardDirection.rotation,
          wheelParent);
        AdjustWheelSet(wheelSet, bounds, isXBounds, i, totalWheelSets);
        wheelSets.Add(wheelSet);
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

    private Vector3 CalculateWheelSetPosition(int index, int totalWheelSets,
      Bounds bounds, float spacing, bool isXBounds)
    {
      var xPosition =
        isXBounds ? bounds.min.x + spacing * index : bounds.center.x;

      // if (isXBounds) xPosition += bounds.extents.x / totalWheelSets;

      var zPosition =
        isXBounds ? bounds.center.z : bounds.min.z + spacing * index;

      return new Vector3(xPosition, bounds.min.y, zPosition);
    }

    private void AdjustWheelSet(GameObject wheelSet, Bounds bounds,
      bool isXBoundsAligned, int index, int totalWheelsets)
    {
      var wheelAxel = wheelSet.transform.Find("wheel_axel");
      var wheelRight = wheelSet.transform.Find("wheel_right");
      var wheelLeft = wheelSet.transform.Find("wheel_left");
      var wheelConnector = wheelSet.transform.Find("wheel_connector");

      var wheelRightHinge =
        wheelRight.GetComponent<HingeJoint>();
      var wheelLeftHinge = wheelLeft
        .GetComponent<HingeJoint>();


      var wheelRightRb = wheelRight.GetComponent<Rigidbody>();
      var wheelLeftRb = wheelLeft.GetComponent<Rigidbody>();

      if (index == 0 || index == totalWheelsets - 1)
      {
        wheelLeftRb.linearDamping = 0.1f;
        wheelRightRb.linearDamping = 0.1f;
      }

      if (!WheelHingeJointMap.ContainsKey(wheelRightHinge))
        WheelHingeJointMap.Add(wheelRightHinge, wheelRight);

      if (!WheelHingeJointMap.ContainsKey(wheelLeftHinge))
        WheelHingeJointMap.Add(wheelLeftHinge, wheelLeft);

      if (!wheelAxel || !wheelLeftHinge || !wheelRightHinge)
      {
        Debug.LogError(
          "Wheel Set Prefab must contain wheel_axel, wheel_left, and wheel_right transforms.");
        return;
      }

      // Adjust axel scale and alignment
      var axelLength =
        (!isXBoundsAligned ? bounds.extents.x : bounds.extents.z) * 2 +
        2 * axelPadding;
      var axelScale = wheelAxel.localScale;
      axelScale.y = axelLength / 2;
      wheelAxel.localScale = axelScale;


      // Adjust wheel positions based on axel length
      // var offset = !isYAxis
      //   ? new Vector3(axelLength / 2, 0, 0)
      //   : new Vector3(0, 0, axelLength / 2);
      var wheelAxelLocalPosition = wheelAxel.localPosition;

      if (isXBoundsAligned)
        wheelAxelLocalPosition.x -= axelPadding * 2;
      else
        wheelAxelLocalPosition.x -= axelPadding * 2;
      wheelAxelLocalPosition.z = 0;

      var wheelConnectorLocalPosition = wheelConnector.localPosition;
      wheelConnectorLocalPosition.x = 0;

      wheelAxel.localPosition = wheelAxelLocalPosition;
      wheelConnector.localPosition = wheelConnectorLocalPosition;

      wheelLeftHinge.transform.localPosition = new Vector3(-axelLength / 2,
        wheelAxelLocalPosition.y, wheelAxelLocalPosition.z);
      wheelRightHinge.transform.localPosition = new Vector3(axelLength / 2,
        wheelAxelLocalPosition.y, wheelAxelLocalPosition.z);
      // if (!isYAxis)
      // {
      //   wheelLeft.localPosition = new Vector3(-axelLength / 2,
      //     wheelAxel.localPosition.y, wheelAxel.localPosition.z);
      //   wheelRight.localPosition = new Vector3(axelLength / 2,
      //     wheelAxel.localPosition.y, wheelAxel.localPosition.z);
      // }
      // else
      // {
      //   wheelLeft.localPosition = new Vector3(wheelAxel.localPosition.x,
      //     wheelAxel.localPosition.y, -axelLength / 2);
      //   wheelRight.localPosition = new Vector3(wheelAxel.localPosition.x,
      //     wheelAxel.localPosition.y, axelLength / 2);
      // }

      // Adjust wheel scale to fit bounds without colliding
      // var wheelScaleFactor = Mathf.Min(bounds.size.x, bounds.size.z) / 2;
      // wheelLeft.localScale = new Vector3(wheelScaleFactor,
      //   wheelLeft.localScale.y, wheelScaleFactor);
      // wheelRight.localScale = new Vector3(wheelScaleFactor,
      //   wheelRight.localScale.y, wheelScaleFactor);
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
        // To create a top speed for the tank, the motor torque just
        // cuts out when the tank starts moving fast enough.
        var motor = wheel.motor;
        var isBackward = forwardInput < 0;
        wheel.axis = isBackward ? Vector3.down : Vector3.up;
        if (rigid.linearVelocity.magnitude <= topSpeed)
        {
          motor.force = forwardInput * motorTorque;
          motor.targetVelocity = topSpeed;
        }

        else
        {
          motor.force = 0.0f;
          motor.targetVelocity = 0.0f;
        }

        wheel.motor = motor;

        // if (wheelSets != null &&
        //     WheelHingeJointMap.TryGetValue(wheel,
        //       out var wheelTransform))
        // {
        //   if (wheelTransform == null)
        //     continue;
        //   // wheel.GetWorldPose(out var position, out var rotation);
        //   // wheelTransform.position = position;
        //   // wheelTransform.rotation = rotation;
        //   var wheelRotation = wheelTransform.rotation.eulerAngles;
        //   wheelRotation.x = wheelTransform.rotation.eulerAngles.x *
        //                     deltaRunPoweredWheels;
        //   // var rot = Quaternion.Euler(
        //   //   Mathf.Clamp(Mathf.Lerp(365f / deltaRunPoweredWheels, 365f, 4f),
        //   //     0f, 365f), wheelRotation.y, wheelRotation.z);
        //   RotateOnXAxis(wheelTransform);
        // }
      }
      // Update wheel mesh positions to match the physics wheels.
    }

    private Quaternion RotateOnXAxis(Transform wheelTransform)
    {
      // Calculate the new rotation angle
      // Calculate the rotation increment
      var deltaRotation = forwardInput * 3 * Time.fixedDeltaTime;

      // Create a quaternion for the X-axis rotation
      var xRotation = Quaternion.Euler(deltaRotation, 0f, 0f);

      // Apply the rotation to the current transform without affecting other axes
      wheelTransform.rotation = xRotation * wheelTransform.rotation;

      // Normalize the quaternion to prevent precision errors over time
      wheelTransform.rotation = Quaternion.Normalize(wheelTransform.rotation);

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
    private void RunDifferentialSteeringWheels()
    {
      foreach (var wheel in left)
      {
        var motor = wheel.motor;

        wheel.axis = Vector3.up;
        if (rigid.linearVelocity.magnitude <= topSpeed)
        {
          motor.force = forwardInput * motorTorque;
        }

        motor.force += motorTorque * turnInput;
        motor.targetVelocity = topSpeed;
      }

      foreach (var wheel in right)
      {
        var motor = wheel.motor;

        // we use backward axis for this value... not sure if it works well with hinge joint though.
        wheel.axis = Vector3.down;

        motor.force = motorTorque * turnInput;
        motor.targetVelocity = topSpeed;
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
      // foreach (var wheel in front)
      //   wheel.steerAngle = turnInput * steeringAngle;
      // foreach (var wheel in rear)
      //   wheel.steerAngle = -turnInput * steeringAngle;
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

    // We run this only in Unity Editor
#if !VALHEIM
    private void Start()
    {
      if (!Application.isPlaying) return;
      if (boundsTransform != null)
        ReInitializeWheels(GetBounds(boundsTransform));
    }

    private void Update()
    {
      if (!Application.isPlaying) return;
      // Capture input in the Update, not the FixedUpdate!
      // forwardInput = Input.GetAxis("Vertical");
      turnInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
      VehicleMovementFixedUpdate();
    }
#endif
  }
}