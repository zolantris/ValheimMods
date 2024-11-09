using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Config;
using ValheimRAFT.Patches;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : ValheimBaseGameShip, IVehicleMovement,
  IValheimShip,
  IMonoUpdater
{
  private bool _hasRegister = false;

  // unfortunately, the current approach does not allow increasing this beyond 1f otherwise it causes massive jitters when changing altitude.
  private float _maxVerticalOffset =>
    IsNotFlying
      ? PropulsionConfig.UnderwaterClimbingOffset.Value
      : PropulsionConfig.FlightClimbingOffset.Value;

  public bool isAnchored;

  // prevents updating repeatedly firing while the key is down
  private bool _isHoldingAnchor = false;

  public enum DirectionChange
  {
    Forward,
    Backward,
    Stop,
  }

  public SteeringWheelComponent lastUsedWheelComponent;

  public IVehicleShip? ShipInstance => vehicleShip;

  public VehiclePiecesController? PiecesController =>
    vehicleShip?.PiecesController;

  private VehicleShip vehicleShip;
  public Vector3 detachOffset = new(0f, 0.5f, 0f);

  public VehicleMovementFlags MovementFlags { get; set; }

  internal bool m_forwardPressed;

  internal bool m_backwardPressed;

  internal float m_sendRudderTime;


  internal float m_rudder;
  public float m_rudderSpeed = 0.5f;
  internal float m_rudderValue;
  public ZSyncTransform zsyncTransform;
  public Rigidbody rigidbody => m_body;
  private Ship.Speed vehicleSpeed;

  // flying mechanics
  private bool _isAscending;
  private bool _isDescending;
  private bool _isHoldingDescend = false;
  private bool _isHoldingAscend = false;

  private const float InitialTargetHeight = 0f;
  public float TargetHeight { get; private set; } = 0f;
  public Transform AttachPoint { get; set; }
  public bool HasOceanSwayDisabled { get; set; }

  public const string m_attachAnimation = "Standing Torch Idle right";

  public GameObject RudderObject { get; set; }

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;

  public VehicleOnboardController OnboardController;

  public bool isCreative => ShipInstance?.Instance?.isCreative ?? false;

  public const float m_balanceForce = 0.03f;

  public const float m_liftForce = 20f;

  // combo of Z and X enum
  public const RigidbodyConstraints FreezeBothXZ = (RigidbodyConstraints)80;

  public bool isBeached = false;
  private Ship.Speed VehicleSpeed => GetSpeedSetting();

  public Transform ShipDirection { get; set; } = null!;

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

  public static List<VehicleMovementController> Instances { get; } = [];

  public static List<IMonoUpdater> MonoUpdaterInstances { get; } = [];

  public Transform m_controlGuiPos { get; set; }

  public BoxCollider BlockingCollider { get; set; }
  public BoxCollider OnboardCollider { get; set; }

  public BoxCollider FloatCollider
  {
    get => m_floatcollider;
    set => m_floatcollider = value;
  }

  public Transform ControlGuiPosition
  {
    get => m_controlGuiPos;
    set => m_controlGuiPos = value;
  }

  public Rigidbody GetRigidbody()
  {
    if (m_body) return m_body;
    if (!m_body)
    {
      m_body = GetComponent<Rigidbody>();
    }

    return m_body;
  }

  private bool IsNotFlying =>
    !IsFlying();

  /// <summary>
  ///  Removes player from boat if not null, disconnects can make the player null
  /// </summary>
  private void RemovePlayersBeforeDestroyingBoat()
  {
    foreach (var mPlayer in m_players)
    {
      if (!mPlayer) continue;
      mPlayer?.transform?.SetParent(null);
    }
  }

  /// <summary>
  /// Sets the rudderForce and returns it's value
  /// </summary>
  /// noting that rudderforce must be negative when speed is Backwards
  /// <returns></returns>
  private float GetRudderForcePerSpeed()
  {
    if (!ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
    {
      _rudderForce = 1f;
      return _rudderForce;
    }

    switch (VehicleSpeed)
    {
      case Ship.Speed.Stop:
        _rudderForce = 0f;
        break;
      case Ship.Speed.Back:
        _rudderForce =
          Mathf.Clamp(ValheimRaftPlugin.Instance.VehicleRudderSpeedBack.Value,
            0,
            ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      case Ship.Speed.Slow:
        _rudderForce = Mathf.Clamp(
          ValheimRaftPlugin.Instance.VehicleRudderSpeedSlow.Value, 0,
          ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      case Ship.Speed.Half:
        _rudderForce = Mathf.Clamp(
          ValheimRaftPlugin.Instance.VehicleRudderSpeedHalf.Value, 0,
          ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      case Ship.Speed.Full:
        _rudderForce = Mathf.Clamp(
          ValheimRaftPlugin.Instance.VehicleRudderSpeedFull.Value, 0,
          ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      default:
        Logger.LogError(
          $"Speed value could not handle this variant, {VehicleSpeed}");
        _rudderForce = 1f;
        break;
    }

    return _rudderForce;
  }


  /**
   * adds guard for when ship controls do not exist on the ship.
   * - previous ship would assume m_shipControlls was connected because it was part of the base prefab
   */
  public bool HaveControllingPlayer()
  {
    if (m_players.Count != 0)
    {
      return HaveValidUser();
    }

    return false;
  }

  public bool IsReady()
  {
    var netView = GetComponent<ZNetView>();
    if (netView == null) return false;
    return netView?.isActiveAndEnabled ?? false;
  }

  /// <summary>
  /// caps the vehicle speeds to these values
  /// </summary>
  public void UpdateVehicleSpeedThrottle()
  {
    m_body.maxAngularVelocity =
      ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value;
    m_body.maxLinearVelocity =
      ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value;
  }

  public void InitColliders()
  {
    var vehicleCollidersParentObj =
      VehicleShip.GetVehicleMovementCollidersObj(transform);

    var floatColliderObj =
      vehicleCollidersParentObj.transform.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      vehicleCollidersParentObj.transform.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      vehicleCollidersParentObj.transform.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    if (onboardColliderObj?.gameObject)
    {
      OnboardController = onboardColliderObj.gameObject
        .AddComponent<VehicleOnboardController>();
      if (!OnboardController.GetMovementController())
      {
        Logger.LogError(
          "OnboardController controller initialized but null controller, manually initializng from VehicleMovementController parent");
        OnboardController.SetMovementController(this);
      }
    }

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    ShipDirection =
      floatColliderObj.Find(PrefabNames.VehicleShipMovementOrientation);
    BlockingCollider = blockingColliderObj.GetComponent<BoxCollider>();
    FloatCollider = floatColliderObj.GetComponent<BoxCollider>();
    OnboardCollider = onboardColliderObj.GetComponent<BoxCollider>();
  }

  public void SetupImpactEffect()
  {
    _impactEffect = GetComponent<ImpactEffect>();

    // fallback assignment
    if (!_impactEffect)
    {
      _impactEffect = gameObject.AddComponent<ImpactEffect>();
      _impactEffect.m_triggerMask = LayerMask.GetMask("Default", "character",
        "piece", "terrain",
        "static_solid", "Default_small", "character_net", "vehicle",
        LayerMask.LayerToName(29));
      _impactEffect.m_toolTier = 1000;
    }

    _impactEffect.m_nview = m_nview;
    _impactEffect.m_body = m_body;
    _impactEffect.m_hitType = HitData.HitType.Boat;
    _impactEffect.m_interval = 0.5f;
    _impactEffect.m_minVelocity = 0.1f;
  }

  /**
   * A performant way to guard against ship problems
   */
  private IEnumerator ShipFixRoutine()
  {
    while (isActiveAndEnabled)
    {
      FixShipRotation();
      FixShipPosition();
      yield return new WaitForSeconds(5f);
    }
  }

  public enum PhysicsTarget
  {
    VehicleShip,
    VehiclePieces,
  }

  public static PhysicsTarget PhysicsSyncTarget = PhysicsTarget.VehicleShip;

  public static bool HasPieceSyncTarget =>
    PhysicsSyncTarget == PhysicsTarget.VehiclePieces;

  public void SetupPhysicsSync()
  {
    if (!zsyncTransform)
    {
      zsyncTransform =
        GetComponent<ZSyncTransform>();
    }

    if (zsyncTransform)
    {
      zsyncTransform.m_body = GetRigidbody();
    }
  }

  /// <summary>
  /// disabled/unused for now. The code below attempted to sync the rigidbody with the piece controller but it likely is the cause of some nasty desync problems for players 
  /// </summary>
  public void UNSTABLE_SetupPhysicsSync()
  {
    // switch (PhysicsSyncTarget)
    // {
    //   case PhysicsTarget.VehicleShip:
    //     zsyncTransform.m_body = GetRigidbody();
    //     return;
    //   case PhysicsTarget.VehiclePieces:
    //   {
    //     if (ShipInstance != null)
    //     {
    //       zsyncTransform.m_body =
    //         ShipInstance?.VehiclePiecesController?.m_body ??
    //         GetRigidbody();
    //     }
    //
    //     break;
    //   }
    // }
  }

  /// <summary>
  /// Meant to change physics targets when config is updated
  /// </summary>
  /// <param name="val"></param>
  public static void SetPhysicsSyncTarget(VehiclePhysicsMode val)
  {
    PhysicsSyncTarget = val == VehiclePhysicsMode.DesyncedJointRigidbodyBody
      ? PhysicsTarget.VehiclePieces
      : PhysicsTarget.VehicleShip;
    foreach (var vehicleMovementController in Instances)
    {
      vehicleMovementController.SetupPhysicsSync();
    }
  }

  public void SetupZsyncTransform()
  {
    zsyncTransform = GetComponent<ZSyncTransform>();
    if (zsyncTransform == null) return;
    zsyncTransform.m_syncPosition = true;
    zsyncTransform.m_syncBodyVelocity = true;
    zsyncTransform.m_syncRotation = true;
  }

  public void AwakeSetupShipComponents()
  {
    vehicleShip = GetComponent<VehicleShip>();
    GetRigidbody();
    SetupZsyncTransform();
    SetupPhysicsSync();
    SetupImpactEffect();
    InitColliders();
    UpdateVehicleSpeedThrottle();

    if (!(bool)m_mastObject)
    {
      m_mastObject = new GameObject()
      {
        name = PrefabNames.VehicleSailMast,
        transform = { parent = transform }
      };
    }

    if (!(bool)m_sailObject)
    {
      m_sailObject = new GameObject()
      {
        name = PrefabNames.VehicleSail,
        transform = { parent = transform }
      };
    }
  }

  /// <summary>
  /// This will autofix vehicles stuck underground on spawn
  /// </summary>
  /// bounds are based on the vehicle position which is the same as the MovementController transform.position
  public void FixShipPosition()
  {
#if !DEBUG
    // early exit for release variants so this code does not cause issues
    return;
#endif

    if (PiecesController == null) return;

    if (!VehicleDebugConfig.PositionAutoFix.Value) return;

    // Heavier but more accurate player check
    var playersOnboard =
      PiecesController.GetComponentsInChildren<Player>() ?? [];
    if (playersOnboard.Length < 1 || m_players.Count < 1)
    {
      if (!playersOnboard.ToList().Equals(m_players))
      {
        m_players = playersOnboard.ToList();
      }

      SendDelayedAnchor();
    }

    var vehicleBounds = PiecesController.GetVehicleBounds();
    var currentLowestHeight = transform.position.y - vehicleBounds.extents.y;
    var groundHeight = ZoneSystem.instance.GetGroundHeight(transform.position);
    var floatColliderLowestPoint = FloatCollider.transform.position.y -
                                   FloatCollider.size.y / 2;

    // approximateGroundHeight used so add a buffer, so this only applies if the vehicle is stuck within the ground
    var approximateGroundHeight = groundHeight - Math.Max(2,
      VehicleDebugConfig.PositionAutoFixThreshold.Value);
    var isFloatingBelowGround =
      floatColliderLowestPoint < approximateGroundHeight;
    var isVehicleBelowGround = currentLowestHeight < approximateGroundHeight;
    var waterLevel =
      Floating.GetWaterLevel(transform.position, ref m_previousCenter);
    var isWaterNearGroundHeight =
      waterLevel - 3f < groundHeight && waterLevel + 3f > groundHeight;

    // Vehicle is not below the ground near float collider nor is the lowest part of the vehicle embedded in the ground
    // and not above the ground significantly where isVehicleBelowGround becomes inaccurate for landvehicles
    if (!isFloatingBelowGround &&
        (isWaterNearGroundHeight || !isVehicleBelowGround)) return;
    if (!isFloatingBelowGround &&
        approximateGroundHeight - 10f > waterLevel) return;

    if (waterLevel < groundHeight)
    {
      // prevents movement when above water when a ship is stuck on land.
      isBeached = !(PropulsionConfig.EnableLandVehicles.Value ||
                    ValheimRaftPlugin.Instance.AllowFlight.Value);

      // makes your vehicle literally float on the land and also prevents the vehicle from being embedded in the ground
      transform.position = new Vector3(transform.position.x,
        transform.position.y + vehicleBounds.extents.y, transform.position.z);
      return;
    }

    if (waterLevel > m_disableLevel && waterLevel > groundHeight)
    {
      transform.position = new Vector3(transform.position.x,
        waterLevel + vehicleBounds.extents.y, transform.position.z);
    }
  }

  public void FixShipRotation()
  {
    var eulerAngles = transform.rotation.eulerAngles;
    var eulerX = eulerAngles.x;
    var eulerY = eulerAngles.y;
    var eulerZ = eulerAngles.z;

    var transformedX = eulerX;
    var transformedZ = eulerZ;
    var shouldUpdate = false;

    if (eulerX is > 60 and < 300)
    {
      transformedX = 0;
      shouldUpdate = true;
    }

    if (eulerZ is > 60 and < 300)
    {
      transformedZ = 0;
      shouldUpdate = true;
    }

    if (shouldUpdate)
    {
      transform.rotation = Quaternion.Euler(transformedX, eulerY, transformedZ);
    }
  }

  private void UpdateRemovePieceCollisionExclusions()
  {
    var excludedLayers = LayerMask.GetMask("piece_nonsolid");

    if (!m_body)
    {
      GetRigidbody();
    }

    if (m_body)
    {
      var physicalLayers = LayerMask.GetMask("Default", "character", "piece",
        "terrain",
        "static_solid", "Default_small", "character_net", "vehicle",
        LayerMask.LayerToName(29));
      m_body.includeLayers = physicalLayers;
      m_body.excludeLayers = excludedLayers;
    }
  }

  private new void Awake()
  {
    AwakeSetupShipComponents();

    m_nview = GetComponent<ZNetView>();

    var excludedLayers = LayerMask.GetMask("piece", "piece_nonsolid");
    m_body.excludeLayers = excludedLayers;

    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    if (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      OnFlightChangePolling();
    }

    base.Awake();
  }

  public void Start()
  {
    // this delay is added to prevent added items from causing collisions in the brief moment they are not ignoring collisions.
    Invoke(nameof(UpdateRemovePieceCollisionExclusions), 5f);

    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    if (!m_body)
    {
      m_body = GetComponent<Rigidbody>();
    }

    var newFlags =
      (VehicleMovementFlags)m_nview.GetZDO()
        .GetInt(VehicleZdoVars.VehicleFlags, (int)VehicleMovementFlags.None);
    MovementFlags = newFlags;
    isAnchored = MovementFlags.HasFlag(VehicleMovementFlags.IsAnchored);

    StartCoroutine(ShipFixRoutine());

    InitializeRPC();
    SyncShip();
  }

  public void CustomFixedUpdate(float deltaTime)
  {
    if (!(bool)m_body || !(bool)m_floatcollider)
    {
      return;
    }

    if (!vehicleShip
          .PiecesController.isInitialActivationComplete)
    {
      m_body.isKinematic = true;
      return;
    }

    if (ValheimRaftPlugin.Instance.AllowFlight.Value ||
        WaterConfig.ManualBallast.Value)
    {
      SyncTargetHeight();
    }

    UpdateShipWheelTurningSpeed();

    /*
     * creative mode should not allow movement, and applying force on an object will cause errors, when the object is kinematic
     */
    if (isCreative)
    {
      return;
    }

    if (m_body.isKinematic)
    {
      m_body.isKinematic = false;
    }

    VehiclePhysicsFixedUpdateAllClients();
    VehicleMovementUpdatesOwnerOnly();
  }

  /// <summary>
  /// Unused, but required for IMonoUpdaters which Valheim uses to sync client and server lifecycle updates
  /// </summary>
  /// <param name="deltaTime"></param>
  /// <param name="time"></param>
  public void CustomUpdate(float deltaTime, float time)
  {
  }

  public void CustomLateUpdate(float deltaTime)
  {
    SyncShip();
  }

  public void UpdateShipDirection(Quaternion steeringWheelRotation)
  {
    var rotation = Quaternion.Euler(0, steeringWheelRotation.eulerAngles.y, 0);
    if (!(bool)ShipDirection)
    {
      ShipDirection = transform;
      return;
    }

    if (ShipDirection.localRotation.Equals(rotation)) return;
    ShipDirection.localRotation = rotation;
  }

  private static Vector3 CalculateAnchorStopVelocity(Vector3 currentVelocity)
  {
    var zeroVelocity = Vector3.zero;
    return Vector3.SmoothDamp(currentVelocity * 0.5f, Vector3.zero,
      ref zeroVelocity, 5f);
  }

  public void AddForceAtPosition(Vector3 force, Vector3 position,
    ForceMode forceMode)
  {
    m_body.AddForceAtPosition(force, position, forceMode);
  }

  /**
   * BasedOnInternalRotation
   */
  private float GetFloatSizeFromDirection(Vector3 direction)
  {
    if (direction == Vector3.right)
    {
      return m_floatcollider.size.x / 2;
    }

    return m_floatcollider.size.z / 2;
  }

  public static float
    BallastSmoothtime = 0.1f;

  private float _previousBallastTargetOffset = 0f; // lerp value
  private float _currentBallastTargetOffset = 0f; // lerp value
  private float _previousBallastOffset = 0f;
  private float _currentBallastOffset = 0f;

  public float groundHeightPaddingOffset = 2f;

  private bool HandleLowestBallast(ShipFloatation shipFloatation,
    float highestResult)
  {
    var floatHeightPosition = FloatCollider.transform.position.y +
                              groundHeightPaddingOffset;
    return floatHeightPosition < highestResult;
  }

  private float _lastHighestGroundPoint = 0f;

  private float GetHighestGroundPoint(ShipFloatation shipFloatation)
  {
    // Create an array of ground levels
    float[] groundLevels =
    [
      shipFloatation.GroundLevelCenter,
      shipFloatation.GroundLevelBack,
      shipFloatation.GroundLevelForward,
      shipFloatation.GroundLevelLeft,
      shipFloatation.GroundLevelRight
    ];
    _lastHighestGroundPoint = groundLevels.Max();
    // Use LINQ to find the maximum value
    return _lastHighestGroundPoint;
  }

  public static void UpdateYPosition(Transform targetTransform,
    float newYPosition)
  {
    var localPosition = targetTransform.localPosition;
    targetTransform.localPosition =
      new Vector3(localPosition.x, newYPosition, localPosition.z);
  }

  /// <summary>
  /// Cannot use this without requiring a water force update to logic reliant on this automatic center of mass.
  /// </summary>
  public void UpdateCenterOfMass()
  {
    m_body.automaticCenterOfMass = false;
    if (OnboardCollider.bounds.min.y > BlockingCollider.bounds.min.y)
    {
      m_body.centerOfMass = new Vector3(OnboardCollider.center.x,
        OnboardCollider.bounds.center.y, OnboardCollider.center.z);
    }
    else
    {
      m_body.centerOfMass = new Vector3(BlockingCollider.center.x,
        BlockingCollider.bounds.center.y, BlockingCollider.center.z);
    }
  }

  private void UpdateColliderPositions()
  {
    if (PiecesController == null) return;

    var expectedLowestBlockingColliderPoint =
      BlockingCollider.transform.position.y - BlockingCollider.bounds.extents.y;

    if (IsFlying())
    {
      var flyingFloatPositionY =
        PiecesController.FloatColliderDefaultPosition.y +
        -ZoneSystem.instance.m_waterLevel;
      var flyingBlockingPositionY =
        PiecesController.BlockingColliderDefaultPosition.y +
        -ZoneSystem.instance.m_waterLevel;
      UpdateYPosition(FloatCollider.transform, flyingFloatPositionY);
      UpdateYPosition(BlockingCollider.transform, flyingBlockingPositionY);
      return;
    }

    // ForceUpdates TargetPosition in case the blocking collider is below the ground. This would cause issues, as the vehicle would then not be able to ascend properly.
    if (_lastHighestGroundPoint > expectedLowestBlockingColliderPoint)
    {
      // will be a positive number.
      var heightDifference = (_lastHighestGroundPoint + 1f) -
                             BlockingCollider.transform.position.y;
      // we force update it.
      UpdateTargetHeight(TargetHeight - (heightDifference + 1f), false);
    }

    // ForceUpdateIfBelowGround, this can happen if driving the vehicle forwards into the ground.
    // Prevents the ship from exceeding the lowest height above the water
    if (_lastHighestGroundPoint > OnboardCollider.bounds.min.y)
    {
      // will be a positive number.
      var heightDifference = _lastHighestGroundPoint -
                             OnboardCollider.bounds.min.y;
      // we force update it.
      UpdateTargetHeight(TargetHeight - heightDifference, true);
    }

    var floatPositionY =
      PiecesController.FloatColliderDefaultPosition.y + TargetHeight;
    var blockingPositionY = PiecesController.BlockingColliderDefaultPosition.y +
                            TargetHeight;

    // Flying logic does not update float collider
    if (floatPositionY <= -ZoneSystem.instance.m_waterLevel ||
        blockingPositionY <= -ZoneSystem.instance.m_waterLevel)
    {
      return;
    }

    UpdateYPosition(FloatCollider.transform, floatPositionY);
    UpdateYPosition(BlockingCollider.transform, blockingPositionY);
    // UpdateCenterOfMass();
  }

  public float GetFlyingTargetHeight() => TargetHeight * -1;

  private float prevFrontUpwardForce = 0f;
  private float prevBackUpwardsForce = 0f;
  private float prevLeftUpwardsForce = 0f;
  private float prevRightUpwardsForce = 0f;
  private float prevCenterUpwardsForce = 0f;

// Optional: Define a smooth time for smoothing the damping
  public float
    smoothTime = 0.3f; // Adjust this value to control the smoothing speed

  public void Flying_UpdateShipBalancingForce()
  {
    var front = ShipDirection.position +
                ShipDirection.forward * OnboardCollider.size.z / 2f;
    var back = ShipDirection.position -
               ShipDirection.forward * OnboardCollider.size.z / 2f;
    var left = ShipDirection.position -
               ShipDirection.right * OnboardCollider.size.x / 2f;
    var right = ShipDirection.position +
                ShipDirection.right * OnboardCollider.size.x / 2f;
    var centerpos2 = ShipDirection.position;

    var frontForce = m_body.GetPointVelocity(front);
    var backForce = m_body.GetPointVelocity(back);
    var leftForce = m_body.GetPointVelocity(left);
    var rightForce = m_body.GetPointVelocity(right);

    var flyingTargetHeight = GetFlyingTargetHeight();

    // Calculate the target upwards forces for each position
    var frontUpwardsForce = GetUpwardsForce(flyingTargetHeight,
      front.y + frontForce.y, m_balanceForce);
    var backUpwardsForce = GetUpwardsForce(flyingTargetHeight,
      back.y + backForce.y, m_balanceForce);
    var leftUpwardsForce = GetUpwardsForce(flyingTargetHeight,
      left.y + leftForce.y, m_balanceForce);
    var rightUpwardsForce = GetUpwardsForce(flyingTargetHeight,
      right.y + rightForce.y, m_balanceForce);
    var centerUpwardsForce = GetUpwardsForce(flyingTargetHeight,
      centerpos2.y + m_body.velocity.y, m_liftForce);

    // Smoothly transition the forces towards the target values using SmoothDamp
    frontUpwardsForce = Mathf.SmoothDamp(prevFrontUpwardForce,
      frontUpwardsForce, ref prevFrontUpwardForce, smoothTime);
    backUpwardsForce = Mathf.SmoothDamp(prevBackUpwardsForce, backUpwardsForce,
      ref prevBackUpwardsForce, smoothTime);
    leftUpwardsForce = Mathf.SmoothDamp(prevLeftUpwardsForce, leftUpwardsForce,
      ref prevLeftUpwardsForce, smoothTime);
    rightUpwardsForce = Mathf.SmoothDamp(prevRightUpwardsForce,
      rightUpwardsForce, ref prevRightUpwardsForce, smoothTime);
    centerUpwardsForce = Mathf.SmoothDamp(prevCenterUpwardsForce,
      centerUpwardsForce, ref prevCenterUpwardsForce, smoothTime);

    // Apply the smoothed forces at the corresponding positions
    AddForceAtPosition(Vector3.up * frontUpwardsForce, front,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * backUpwardsForce, back,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * leftUpwardsForce, left,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * rightUpwardsForce, right,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * centerUpwardsForce, centerpos2,
      ForceMode.VelocityChange);
  }

  public void UpdateAndFreezeRotation()
  {
    var isAproxZeroX = Mathf.Approximately(m_body.rotation.eulerAngles.x, 0);
    var isApproxZeroZ = Mathf.Approximately(m_body.rotation.eulerAngles.z, 0);

    if (!isAproxZeroX || !isApproxZeroZ)
    {
      m_body.constraints = RigidbodyConstraints.None;
      var newRotation = Quaternion.Euler(0, m_body.rotation.eulerAngles.y, 0);
      m_body.MoveRotation(newRotation);
    }

    if (m_body.constraints != FreezeBothXZ)
    {
      m_body.constraints = FreezeBothXZ;
    }
  }

  public void UpdateFlying()
  {
    UpdateVehicleStats(true, false);
    UpdateAndFreezeRotation();
    // early exit if anchored.
    if (UpdateAnchorVelocity(m_body.velocity))
    {
      return;
    }

    m_body.WakeUp();
    Flying_UpdateShipBalancingForce();

    if (!ValheimRaftPlugin.Instance.FlightHasRudderOnly.Value)
    {
      ApplySailForce(this, true);
    }
  }

  public void UpdateShipLandSpeed()
  {
    UpdateVehicleStats(false, false);
    // early exit if anchored.
    if (UpdateAnchorVelocity(m_body.velocity))
    {
      return;
    }

    m_body.WakeUp();

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value &&
        PropulsionConfig.EnableLandVehicles.Value)
    {
      ApplySailForce(this);
    }
  }

  /// <summary>
  /// Calculates damage from impact using vehicle weight
  /// </summary>
  /// <returns></returns>
  private float GetDamageFromImpact()
  {
    if (!(bool)m_body) return 50f;

    const float damagePerPointOfMass = 0.01f;
    const float baseDamage = 25f;
    const float maxDamage = 500f;

    var rigidBodyMass = m_body?.mass ?? 1000f;
    var massDamage = rigidBodyMass * damagePerPointOfMass;
    var damage = Math.Min(maxDamage, baseDamage + massDamage);

    return damage;
  }

  // todo move these to a physics variant struct
  // flight
  public static float flightAngularDamping =>
    PhysicsConfig.flightAngularDamping.Value;

  public static float flightSidewaysDamping =>
    PhysicsConfig.flightSidewaysDamping.Value;

  public static float flightDamping => PhysicsConfig.flightDamping.Value;
  public static float flightSteerForce => PhysicsConfig.flightSteerForce.Value;

  public static float flightSailForceFactor =>
    PhysicsConfig.flightSailForceFactor.Value;

  public static float flightDrag => PhysicsConfig.flightDrag.Value;

  public static float flightAngularDrag =>
    PhysicsConfig.flightAngularDrag.Value;

  // water
  public static float waterAngularDamping =>
    PhysicsConfig.waterAngularDamping.Value;

  public static float waterSidewaysDamping =>
    PhysicsConfig.waterSidewaysDamping.Value;

  public static float waterSteerForce => PhysicsConfig.waterSteerForce.Value;
  public static float waterDamping => PhysicsConfig.waterDamping.Value;

  public static float waterSailForceFactor =>
    PhysicsConfig.waterSailForceFactor.Value;

  public static float waterDrag => PhysicsConfig.waterDrag.Value;
  public static float waterAngularDrag => PhysicsConfig.waterAngularDrag.Value;

  private void UpdateFlightStats()
  {
    m_angularDamping = PhysicsConfig.flightSteerForce.Value;
    m_damping = PhysicsConfig.flightDamping.Value;
    m_dampingSideway = PhysicsConfig.flightSidewaysDamping.Value;
    m_sailForceFactor = PhysicsConfig.flightSailForceFactor.Value;
    m_stearForce = PhysicsConfig.flightSteerForce.Value;

    var drag = PhysicsConfig.flightDrag.Value;
    var angularDrag = PhysicsConfig.flightAngularDrag.Value;

    ShipInstance?.VehiclePiecesController?.SyncRigidbodyStats(drag, angularDrag,
      true);
  }

  public void UpdateSubmergedStats()
  {
    m_angularDamping =
      PhysicsConfig.submersibleAngularDamping.Value;
    m_damping = PhysicsConfig.submersibleDamping.Value;
    m_dampingSideway = PhysicsConfig.submersibleSidewaysDamping.Value;
    m_sailForceFactor = PhysicsConfig.submersibleSailForceFactor.Value;
    m_stearForce = PhysicsConfig.submersibleSteerForce.Value;

    var drag = PhysicsConfig.submersibleDrag.Value;
    var angularDrag = PhysicsConfig.submersibleAngularDrag.Value;

    ShipInstance?.VehiclePiecesController?.SyncRigidbodyStats(drag, angularDrag,
      false);
  }

  public void UpdateWaterStats()
  {
    m_angularDamping = PhysicsConfig.flightAngularDamping.Value;
    m_damping = PhysicsConfig.waterDamping.Value;
    m_dampingSideway = PhysicsConfig.waterSidewaysDamping.Value;
    m_sailForceFactor = PhysicsConfig.waterSailForceFactor.Value;
    m_stearForce = PhysicsConfig.waterSteerForce.Value;

    var drag = PhysicsConfig.waterDrag.Value;
    var angularDrag = PhysicsConfig.waterAngularDrag.Value;

    ShipInstance?.VehiclePiecesController?.SyncRigidbodyStats(drag, angularDrag,
      false);
  }

  private float vehicleStatSyncTimer = 1.0f;
  private bool previousSyncFlight = false;
  private bool previousSyncSubmerged = false;

  /// <summary>
  /// Updates all physics stats. Calls alot but should not do much
  /// </summary>
  /// Todo debounce this when values do not change
  /// <param name="flight"></param>
  /// <param name="submerged"></param>
  private void UpdateVehicleStats(bool flight, bool submerged)
  {
    vehicleStatSyncTimer += Time.deltaTime;
    if (vehicleStatSyncTimer < 30f && previousSyncFlight == flight &&
        previousSyncSubmerged == submerged)
    {
      return;
    }

    vehicleStatSyncTimer = 0f;
    previousSyncFlight = flight;
    previousSyncSubmerged = submerged;

    if (flight)
    {
      UpdateFlightStats();
    }
    else if (submerged)
    {
      UpdateSubmergedStats();
    }
    else
    {
      UpdateWaterStats();
    }

    m_force = 3f;
    m_forceDistance = 5f;
    m_stearVelForceFactor = 1.3f;
    m_waterImpactDamage = 0f;
    m_backwardForce = 1f;

    if ((bool)_impactEffect)
    {
      _impactEffect.m_damages.m_blunt = GetDamageFromImpact();
    }
    else
    {
      Logger.LogDebug(
        "No Ship ImpactEffect detected, this needs to be added to the custom ship");
    }
  }

  public void UpdateWaterForce(ShipFloatation shipFloatation)
  {
    var shipLeft = shipFloatation.ShipLeft;
    var shipForward = shipFloatation.ShipForward;
    var shipBack = shipFloatation.ShipBack;
    var shipRight = shipFloatation.ShipRight;
    var waterLevelLeft = shipFloatation.WaterLevelLeft;
    var waterLevelRight = shipFloatation.WaterLevelRight;
    var waterLevelForward = shipFloatation.WaterLevelForward;
    var waterLevelBack = shipFloatation.WaterLevelBack;
    var currentDepth = shipFloatation.CurrentDepth;
    var worldCenterOfMass = m_body.worldCenterOfMass;

    if (shipFloatation.IsAboveBuoyantLevel)
    {
      return;
    }

    m_body.WakeUp();

    if (m_waterImpactDamage > 0f)
    {
      UpdateWaterForce(currentDepth, Time.fixedDeltaTime);
    }

    var leftForce = new Vector3(shipLeft.x, waterLevelLeft, shipLeft.z);
    var rightForce = new Vector3(shipRight.x, waterLevelRight, shipRight.z);
    var forwardForce =
      new Vector3(shipForward.x, waterLevelForward, shipForward.z);
    var backwardForce = new Vector3(shipBack.x, waterLevelBack, shipBack.z);

    var fixedDeltaTime = Time.fixedDeltaTime;
    var deltaForceMultiplier = fixedDeltaTime * 50f;

    var currentDepthForceMultiplier =
      Mathf.Clamp01(Mathf.Abs(currentDepth) / m_forceDistance);
    var upwardForceVector = Vector3.up * m_force * currentDepthForceMultiplier;

    AddForceAtPosition(upwardForceVector * deltaForceMultiplier,
      worldCenterOfMass,
      ForceMode.VelocityChange);

    // todo rename variables for this section to something meaningful
    // todo abstract this to a method
    var deltaForward = Vector3.Dot(m_body.velocity, ShipDirection.forward);
    var deltaRight = Vector3.Dot(m_body.velocity, ShipDirection.right);
    var velocity = m_body.velocity;
    var deltaUp = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping *
                  currentDepthForceMultiplier;

    var deltaForwardClamp = deltaForward * deltaForward *
                            Mathf.Sign(deltaForward) *
                            m_dampingForward *
                            currentDepthForceMultiplier;
    var deltaRightClamp = deltaRight * deltaRight * Mathf.Sign(deltaRight) *
                          m_dampingSideway *
                          currentDepthForceMultiplier;

    velocity.y -= Mathf.Clamp(deltaUp, -1f, 1f);
    velocity -= ShipDirection.forward * Mathf.Clamp(deltaForwardClamp, -1f, 1f);
    velocity -= ShipDirection.right * Mathf.Clamp(deltaRightClamp, -1f, 1f);

    if (velocity.magnitude > m_body.velocity.magnitude)
      velocity = velocity.normalized * m_body.velocity.magnitude;

    m_body.velocity = velocity;
    m_body.angularVelocity -=
      m_body.angularVelocity * m_angularDamping * currentDepthForceMultiplier;

    // clamps the force to a specific number
    // todo rename variables for this section to something meaningful
    // todo abstract to a method
    var num7 = 0.15f;
    var num8 = 0.5f;
    var f = Mathf.Clamp((forwardForce.y - shipForward.y) * num7, 0f - num8,
      num8);
    var f2 = Mathf.Clamp((backwardForce.y - shipBack.y) * num7, 0f - num8,
      num8);
    var f3 = Mathf.Clamp((leftForce.y - shipLeft.y) * num7, 0f - num8, num8);
    var f4 = Mathf.Clamp((rightForce.y - shipRight.y) * num7, 0f - num8, num8);
    f = Mathf.Sign(f) * Mathf.Abs(Mathf.Pow(f, 2f));
    f2 = Mathf.Sign(f2) * Mathf.Abs(Mathf.Pow(f2, 2f));
    f3 = Mathf.Sign(f3) * Mathf.Abs(Mathf.Pow(f3, 2f));
    f4 = Mathf.Sign(f4) * Mathf.Abs(Mathf.Pow(f4, 2f));

    AddForceAtPosition(Vector3.up * f * deltaForceMultiplier, shipForward,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * f2 * deltaForceMultiplier, shipBack,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * f3 * deltaForceMultiplier, shipLeft,
      ForceMode.VelocityChange);
    AddForceAtPosition(Vector3.up * f4 * deltaForceMultiplier, shipRight,
      ForceMode.VelocityChange);
  }

  public void UpdateShipFloatation(ShipFloatation shipFloatation)
  {
    UpdateVehicleStats(false, TargetHeight > 5f);
    UpdateWaterForce(shipFloatation);
    // BROKEN_UpdateShipBalancingForce();
    ApplyEdgeForce(Time.fixedDeltaTime);
    if (HasOceanSwayDisabled)
    {
      UpdateAndFreezeRotation();
    }
    else
    {
      if (m_body.constraints == FreezeBothXZ)
      {
        m_body.constraints = RigidbodyConstraints.None;
      }
    }

    if (UpdateAnchorVelocity(m_body.velocity)) return;

    ApplySailForce(this);
  }

  /// <summary>
  /// Used to stop the ship and prevent further velocity calcs if anchored.
  /// </summary>
  /// <param name="velocity"></param>
  /// <returns></returns>
  private bool UpdateAnchorVelocity(Vector3 velocity)
  {
    if (m_players.Count != 0 &&
        !isAnchored) return false;

    var anchoredVelocity = CalculateAnchorStopVelocity(velocity);
    var anchoredAngularVelocity =
      CalculateAnchorStopVelocity(m_body.angularVelocity);

    m_body.velocity = anchoredVelocity;
    m_body.angularVelocity = anchoredAngularVelocity;
    return true;
  }

  public ShipFloatation GetShipFloatationObj()
  {
    var worldCenterOfMass = m_body.worldCenterOfMass;
    var shipForward = ShipDirection!.position +
                      ShipDirection.forward *
                      GetFloatSizeFromDirection(Vector3.forward);
    var shipBack = ShipDirection.position -
                   ShipDirection.forward *
                   GetFloatSizeFromDirection(Vector3.forward);
    var shipLeft = ShipDirection.position -
                   ShipDirection.right *
                   GetFloatSizeFromDirection(Vector3.right);
    var shipRight = ShipDirection.position +
                    ShipDirection.right *
                    GetFloatSizeFromDirection(Vector3.right);
    var waterLevelCenter =
      Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter);
    var waterLevelLeft = Floating.GetWaterLevel(shipLeft, ref m_previousLeft);
    var waterLevelRight =
      Floating.GetWaterLevel(shipRight, ref m_previousRight);
    var waterLevelForward =
      Floating.GetWaterLevel(shipForward, ref m_previousForward);
    var waterLevelBack = Floating.GetWaterLevel(shipBack, ref m_previousBack);
    var averageWaterHeight =
      (waterLevelCenter + waterLevelLeft + waterLevelRight + waterLevelForward +
       waterLevelBack) /
      5f;


    var groundLevelCenter =
      ZoneSystem.instance.GetGroundHeight(worldCenterOfMass);
    var groundLevelLeft = ZoneSystem.instance.GetGroundHeight(shipLeft);
    var groundLevelRight =
      ZoneSystem.instance.GetGroundHeight(shipRight);
    var groundLevelForward =
      ZoneSystem.instance.GetGroundHeight(shipForward);
    var groundLevelBack = ZoneSystem.instance.GetGroundHeight(shipBack);


    var currentDepth =
      worldCenterOfMass.y - averageWaterHeight - m_waterLevelOffset;
    var isInvalid = false;
    if (averageWaterHeight <= -10000 || averageWaterHeight < m_disableLevel)
    {
      currentDepth = 30;
      isInvalid = true;
    }

    // todo may need to offset this too.
    var isAboveBuoyantLevel = currentDepth > m_disableLevel || isInvalid;

    return new ShipFloatation()
    {
      AverageWaterHeight = averageWaterHeight,
      CurrentDepth = currentDepth,
      IsAboveBuoyantLevel = isAboveBuoyantLevel,
      ShipLeft = shipLeft,
      ShipForward = shipForward,
      ShipBack = shipBack,
      ShipRight = shipRight,
      WaterLevelLeft = waterLevelLeft,
      WaterLevelRight = waterLevelRight,
      WaterLevelForward = waterLevelForward,
      WaterLevelBack = waterLevelBack,
      GroundLevelLeft = groundLevelLeft,
      GroundLevelRight = groundLevelRight,
      GroundLevelForward = groundLevelForward,
      GroundLevelBack = groundLevelBack,
      GroundLevelCenter = groundLevelCenter
    };
  }

  private bool _prevGravity = true;

// Updates gravity and target height (which is used to compute gravity)
  public void UpdateGravity()
  {
    var isGravityEnabled = !IsFlying();
    if (_prevGravity == isGravityEnabled && m_body.useGravity == _prevGravity &&
        zsyncTransform.m_useGravity == _prevGravity) return;
    _prevGravity = isGravityEnabled;
    m_body.useGravity = isGravityEnabled;
    zsyncTransform.m_useGravity = isGravityEnabled;
  }

  public void UpdateShipCreativeModeRotation()
  {
    if (!isCreative) return;
    var rotationY = m_body.rotation.eulerAngles.y;

    if (PatchController.HasGizmoMod)
    {
      if (PatchConfig.ComfyGizmoPatchCreativeHasNoRotation.Value)
      {
        rotationY = 0;
      }
      else
      {
        rotationY =
          ComfyGizmo_Patch.GetNearestSnapRotation(m_body.rotation.eulerAngles
            .y);
      }
    }

    var rotationWithoutTilt = Quaternion.Euler(0, rotationY, 0);
    m_body.rotation = rotationWithoutTilt;
  }

  /// <summary>
  /// Only Updates for the controlling player. Only players are synced
  /// </summary>
  public void VehiclePhysicsFixedUpdateAllClients()
  {
    if (!(bool)ShipInstance?.VehiclePiecesController || !(bool)m_nview ||
        m_nview.m_zdo == null ||
        !(bool)ShipDirection) return;

    UpdateGravity();

    var hasControllingPlayer = HaveControllingPlayer();

    // Sets values based on m_speed
    UpdateShipWheelTurningSpeed();
    UpdateShipSpeed(hasControllingPlayer, m_players.Count);

    //base ship direction controls
    UpdateControls(Time.fixedDeltaTime);
    UpdateSail(Time.fixedDeltaTime);

    // rudder direction
    UpdateRudder(Time.fixedDeltaTime, hasControllingPlayer);

    // raft pieces transforms
    SyncVehicleRotationDependentItems();
  }

  /// <summary>
  /// The owner of the vehicle netview will only be able to fire these updates
  /// </summary>
  ///
  /// Physics syncs on 1 client are better otherwise the ships will desync across clients and both will stutter
  public void VehicleMovementUpdatesOwnerOnly()
  {
    if (!m_nview) return;
    var owner = m_nview.IsOwner();
    if ((!VehicleDebugConfig.SyncShipPhysicsOnAllClients.Value && !owner) ||
        isBeached) return;


    _currentShipFloatation = GetShipFloatationObj();

    UpdateColliderPositions();


    if (!ShipFloatationObj.IsAboveBuoyantLevel || IsNotFlying)
    {
      if (m_body.constraints != RigidbodyConstraints.None)
      {
        m_body.constraints = RigidbodyConstraints.None;
      }
    }

    if (!ShipFloatationObj.IsAboveBuoyantLevel && IsNotFlying)
    {
      UpdateShipFloatation(ShipFloatationObj);
    }
    else if (IsFlying())
    {
      UpdateFlying();
    }
    else if (PropulsionConfig.EnableLandVehicles.Value)
    {
      UpdateShipLandSpeed();
    }

    // both flying and floatation use this
    ApplyRudderForce();
  }

  public void UpdateRudder(float dt, bool haveControllingPlayer)
  {
    if (!m_rudderObject)
    {
      return;
    }

    Quaternion b = Quaternion.Euler(0f,
      m_rudderRotationMax * (0f - m_rudderValue), 0f);
    if (haveControllingPlayer)
    {
      if (VehicleSpeed == Ship.Speed.Slow)
      {
        m_rudderPaddleTimer += dt;
        b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * 6f) * 20f,
          0f);
      }
      else if (VehicleSpeed == Ship.Speed.Back)
      {
        m_rudderPaddleTimer += dt;
        b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * -3f) * 40f,
          0f);
      }
    }

    m_rudderObject.transform.localRotation =
      Quaternion.Slerp(m_rudderObject.transform.localRotation, b, 0.5f);
  }

  public bool IsSailUp()
  {
    if (VehicleSpeed != Ship.Speed.Half)
    {
      return VehicleSpeed == Ship.Speed.Full;
    }

    return true;
  }

  public void UpdateSailSize(float dt)
  {
    float num = 0f;
    var speed = VehicleSpeed;

    switch (speed)
    {
      case Ship.Speed.Back:
        num = 0.1f;
        break;
      case Ship.Speed.Half:
        num = 0.5f;
        break;
      case Ship.Speed.Full:
        num = 1f;
        break;
      case Ship.Speed.Slow:
        num = 0.1f;
        break;
      case Ship.Speed.Stop:
        num = 0.1f;
        break;
    }

    Vector3 localScale = m_sailObject.transform.localScale;
    bool flag = Mathf.Abs(localScale.y - num) < 0.01f;
    if (!flag)
    {
      localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
      m_sailObject.transform.localScale = localScale;
    }

    // if ((bool)m_sailCloth)
    // {
    //   if (speed == Ship.Speed.Stop || speed == Ship.Speed.Slow || speed == Ship.Speed.Back)
    //   {
    //     if (flag && m_sailCloth.enabled)
    //     {
    //       m_sailCloth.enabled = false;
    //     }
    //   }
    //   else if (flag)
    //   {
    //     if (!m_sailWasInPosition)
    //     {
    //       Utils.RecreateComponent(ref m_sailCloth);
    //     }
    //   }
    //   else
    //   {
    //     m_sailCloth.enabled = true;
    //   }
    // }

    m_sailWasInPosition = flag;
  }

  public void UpdateSail(float deltaTime)
  {
    UpdateSailSize(deltaTime);
    var windDir = EnvMan.instance.GetWindDir();
    windDir = Vector3.Cross(Vector3.Cross(windDir, ShipDirection.up),
      ShipDirection.up);
    var t = 0.5f + Vector3.Dot(ShipDirection.forward, windDir) * 0.5f;

    switch (VehicleSpeed)
    {
      case Ship.Speed.Full:
      case Ship.Speed.Half:
      {
        var to = Quaternion.LookRotation(
          -Vector3.Lerp(windDir,
            Vector3.Normalize(windDir - ShipDirection.forward), t),
          ShipDirection.up);
        m_mastObject.transform.rotation =
          Quaternion.RotateTowards(m_mastObject.transform.rotation, to,
            30f * deltaTime);
        break;
      }
      case Ship.Speed.Back:
      {
        var from =
          Quaternion.LookRotation(-ShipDirection.forward, ShipDirection.up);
        var to2 = Quaternion.LookRotation(-windDir, ShipDirection.up);
        to2 = Quaternion.RotateTowards(from, to2, 80f);
        m_mastObject.transform.rotation =
          Quaternion.RotateTowards(m_mastObject.transform.rotation, to2,
            30f * deltaTime);
        break;
      }
    }
  }

  public new float GetShipYawAngle()
  {
    var mainCamera = Utils.GetMainCamera();
    if (mainCamera == null)
    {
      return 0f;
    }

    return 0f -
           Utils.YawFromDirection(
             mainCamera.transform.InverseTransformDirection(ShipDirection
               .forward));
  }

  public float GetWindAngle()
  {
    // moder power support
    var isWindPowerActive = IsWindControllActive();

    var windDir = isWindPowerActive
      ? ShipDirection.forward
      : EnvMan.instance.GetWindDir();
    return 0f -
           Utils.YawFromDirection(
             ShipDirection.InverseTransformDirection(windDir));
  }

  float IValheimShip.GetWindAngleFactor()
  {
    return GetWindAngleFactor();
  }


  /**
   * In theory, we can just make the sailComponent and mastComponent parents of the masts/sails of the ship. This will make any mutations to those parents in sync with the sail changes
   */
  private void SyncVehicleRotationDependentItems()
  {
    if (!isActiveAndEnabled) return;

    if (ShipInstance?.VehiclePiecesController == null) return;
    foreach (var mast in ShipInstance.VehiclePiecesController.m_mastPieces
               .ToList())
    {
      if (!(bool)mast)
      {
        ShipInstance.VehiclePiecesController.m_mastPieces.Remove(mast);
        continue;
      }

      if (mast.m_allowSailRotation &&
          PropulsionConfig.AllowBaseGameSailRotation.Value)
      {
        var newRotation = m_mastObject.transform.localRotation;
        mast.transform.localRotation = newRotation;
      }

      if (mast.m_allowSailShrinking)
      {
        if (mast.m_sailObject.transform.localScale !=
            m_sailObject.transform.localScale)
          mast.m_sailCloth.enabled = false;
        mast.m_sailObject.transform.localScale =
          m_sailObject.transform.localScale;
        mast.m_sailCloth.enabled = true;
      }
      else
      {
        mast.m_sailObject.transform.localScale = Vector3.one;
        mast.m_sailCloth.enabled = !mast.m_disableCloth;
      }
    }

    foreach (var rudder in ShipInstance.VehiclePiecesController.m_rudderPieces
               .ToList())
    {
      if (!(bool)rudder)
      {
        ShipInstance.VehiclePiecesController.m_rudderPieces.Remove(rudder);
        continue;
      }

      if (!rudder.PivotPoint)
      {
        Logger.LogError("No pivot point detected for rudder");
        continue;
      }

      var newRotation = Quaternion.Slerp(
        rudder.PivotPoint.localRotation,
        Quaternion.Euler(0f, m_rudderRotationMax * (0f - GetRudderValue()) * 2,
          0f), 0.5f);
      rudder.PivotPoint.localRotation = newRotation;
    }

    foreach (var wheel in ShipInstance.VehiclePiecesController
               ._steeringWheelPieces
               .ToList())
    {
      if (!(bool)wheel)
      {
        ShipInstance.VehiclePiecesController._steeringWheelPieces.Remove(wheel);
      }
      else if (wheel.wheelTransform != null)
      {
        wheel.wheelTransform.localRotation = Quaternion.Slerp(
          wheel.wheelTransform.localRotation,
          Quaternion.Euler(
            m_rudderRotationMax * (0f - m_rudderValue) *
            wheel.m_wheelRotationFactor, 0f, 0f), 0.5f);
      }
    }
  }

  private float GetInterpolatedWindAngleFactor()
  {
    var windIntensity = IsWindControllActive()
      ? 1f
      : EnvMan.instance.GetWindIntensity();
    var interpolatedWindIntensity = Mathf.Lerp(0.25f, 1f, windIntensity);

    var windAngleFactor = GetWindAngleFactor();
    windAngleFactor *= interpolatedWindIntensity;
    return windAngleFactor;
  }

  private float GetSailForceEnergy(float sailSize, float windAngleFactor)
  {
    return windAngleFactor * m_sailForceFactor * sailSize;
  }

  /// <summary>
  /// Considered flying when below the negative waterLevel target height. The FloatCollider/Blocking will not update below these values.
  /// </summary>
  /// <returns></returns>
  public bool IsFlying()
  {
    if (!ValheimRaftPlugin.Instance.AllowFlight.Value) return false;
    return TargetHeight < -ZoneSystem.instance.m_waterLevel;
  }

  private Vector3 GetSailForce(float sailSize, float dt, bool isFlying)
  {
    var windDir = IsWindControllActive()
      ? Vector3.zero
      : EnvMan.instance.GetWindDir();
    var windAngleFactorInterpolated = GetInterpolatedWindAngleFactor();

    Vector3 target;
    if (isFlying)
    {
      target = Vector3.Normalize(ShipDirection.forward) *
               (windAngleFactorInterpolated * m_sailForceFactor * sailSize);
    }
    else
    {
      target = Vector3.Normalize(windDir + ShipDirection.forward) *
               GetSailForceEnergy(sailSize, windAngleFactorInterpolated);
    }

    m_sailForce = Vector3.SmoothDamp(m_sailForce, target,
      ref m_windChangeVelocity, 1f, 99f);

    return Vector3.ClampMagnitude(m_sailForce, 20f);
  }

  private new float GetWindAngleFactor()
  {
    if (IsWindControllActive()) return 1f;
    var num =
      Vector3.Dot(EnvMan.instance.GetWindDir(), -ShipDirection!.forward);
    var num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
    var num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
    return num2 * num3;
  }

  private Vector3 GetAdditiveSteerForce(float directionMultiplier)
  {
    if (_rudderForce == 0 || VehicleSpeed == Ship.Speed.Stop || isAnchored)
      return Vector3.zero;

    var shipAdditiveSteerForce = ShipDirection.right *
                                 (m_stearForce * (0f - m_rudderValue) *
                                  directionMultiplier);

    if (ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
    {
      shipAdditiveSteerForce *= Mathf.Clamp(_rudderForce, 1f, 10f);
    }

    // Adds additional speeds to turning
    if (PiecesController?.m_rudderPieces.Count > 0)
    {
      shipAdditiveSteerForce *= PropulsionConfig.TurnPowerWithRudder.Value;
    }
    else
    {
      shipAdditiveSteerForce *= PropulsionConfig.TurnPowerNoRudder.Value;
    }

    return shipAdditiveSteerForce;
  }

  /// <summary>
  /// Sets the speed of the ship with rudder speed added to it.
  /// </summary>
  /// Does not apply for stopped or anchored states
  /// 
  private void ApplyRudderForce()
  {
    if (VehicleSpeed == Ship.Speed.Stop ||
        isAnchored) return;

    var direction = Vector3.Dot(m_body.velocity, ShipDirection.forward);
    var rudderForce = GetRudderForcePerSpeed();
    // steer offset will need to be size x or size z depending on location of rotation.
    // todo GetFloatSizeFromDirection may not be needed anymore.
    var steerOffset = ShipDirection.position -
                      ShipDirection.forward *
                      GetFloatSizeFromDirection(Vector3.forward);

    var steeringVelocityDirectionFactor = direction * m_stearVelForceFactor;
    var steerOffsetForce = ShipDirection.right *
                           (steeringVelocityDirectionFactor *
                            (0f - m_rudderValue) *
                            Time.fixedDeltaTime);

    AddForceAtPosition(
      steerOffsetForce,
      steerOffset, ForceMode.VelocityChange);

    var steerForce = ShipDirection.forward *
                     (m_backwardForce * rudderForce *
                      (1f - Mathf.Abs(m_rudderValue)));

    var directionMultiplier =
      ((VehicleSpeed != Ship.Speed.Back) ? 1 : (-1));
    steerForce *= directionMultiplier;

    // todo see if this is necessary. This logic is from the Base game Ship

    if (ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
    {
      steerForce += GetAdditiveSteerForce(directionMultiplier);
    }
    else if (VehicleSpeed is Ship.Speed.Back or Ship.Speed.Slow)
    {
      steerForce += GetAdditiveSteerForce(directionMultiplier);
    }


    if (IsFlying())
    {
      transform.rotation =
        Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    AddForceAtPosition(steerForce * Time.fixedDeltaTime, steerOffset,
      ForceMode.VelocityChange);
  }

  private static void ApplySailForce(VehicleMovementController instance,
    bool isFlying = false)
  {
    if (!instance?.m_body || !instance?.ShipDirection ||
        instance.isAnchored) return;

    var sailArea = 0f;

    if (instance?.ShipInstance?.VehiclePiecesController != null)
    {
      sailArea =
        instance.ShipInstance.VehiclePiecesController.GetSailingForce();
    }

    // intellij seems to think 1370 does not have enough guards if this check is at the top of the function.
    if (instance == null) return;

    switch (instance.VehicleSpeed)
    {
      case Ship.Speed.Full:
        break;
      case Ship.Speed.Half:
        sailArea *= 0.5f;
        break;
      case Ship.Speed.Slow:
        sailArea = 0;
        break;
      case Ship.Speed.Stop:
      case Ship.Speed.Back:
      default:
        sailArea = 0f;
        break;
    }

    // backup guard, inTheory not possible to get here
    if (instance.isAnchored)
    {
      sailArea = 0f;
    }

    var sailForce =
      instance.GetSailForce(sailArea, Time.fixedDeltaTime, isFlying);

    var position = instance.m_body.worldCenterOfMass;


    instance.AddForceAtPosition(
      sailForce,
      position,
      ForceMode.VelocityChange);
  }

  public void Forward()
  {
    SendSpeedChange(DirectionChange.Forward);
  }

  public void Backward() =>
    SendSpeedChange(DirectionChange.Backward);

  public void Stop() =>
    SendSpeedChange(DirectionChange.Stop);


  public void SendSyncBounds()
  {
    m_nview?.InvokeRPC(0, nameof(RPC_SyncBounds));
  }

  /// <summary>
  /// Forces a resync of bounds for all players on the ship, this may need to be only from host but then would require syncing all collider data that updates in the OnBoundsUpdate
  /// </summary>
  public void RPC_SyncBounds(long sender)
  {
    if (!PiecesController) return;
    SyncVehicleBounds();
  }

  public void SyncVehicleBounds()
  {
    PiecesController?.DebouncedRebuildBounds();
  }

  public void UpdateControlls(float dt) => UpdateControls(dt);


  public void AddPlayerIfMissing(Player player)
  {
    if (m_players.Contains(player))
    {
      return;
    }

    m_players.Add(player);
  }

  public Ship.Speed GetSpeedSetting() => vehicleSpeed;

  public float GetRudderValue() => m_rudderValue;
  public float GetRudder() => m_rudder;

  public void OnFlightChangePolling()
  {
    CancelInvoke(nameof(SyncTargetHeight));
    if (!IsBallastAndFlightDisabled)
    {
      InvokeRepeating(nameof(SyncTargetHeight), 2f, 2f);
    }
    else
    {
      TargetHeight = 0f;
      SetTargetHeight(TargetHeight);
    }
  }

  /// <summary>
  /// Updates the rudder turning speed based on the shipShip.Speed. Higher speeds will make turning the rudder harder
  /// </summary>
  /// m_rudder = rotation speed of rudder icon
  /// m_rudderValue = position of rudder
  /// m_rudderSpeed = the force speed applied when moving the ship
  public void UpdateShipWheelTurningSpeed()
  {
    switch (GetSpeedSetting())
    {
      case Ship.Speed.Stop:
      case Ship.Speed.Back:
      case Ship.Speed.Slow:
        m_rudderSpeed = 2f;
        break;
      case Ship.Speed.Half:
        m_rudderSpeed = 1.5f;
        break;
      case Ship.Speed.Full:
        m_rudderSpeed = 1f;
        break;
      default:
        Logger.LogError(
          $"Speed value could not handle this variant, {vehicleSpeed}");
        m_rudderSpeed = 1f;
        break;
    }
  }

  private bool HasPendingAnchor = false;

  /// <summary>
  /// Meant for realism and testing but will allow the ship to continue for a bit even when the player is logged out on server.
  /// </summary>
  public void DelayedAnchor()
  {
    HasPendingAnchor = false;
    SendSetAnchor(true);
  }

  /// <summary>
  /// Will always send true for anchor state. Not meant to remove anchor on delay
  /// </summary>
  public void SendDelayedAnchor()
  {
    if (VehicleDebugConfig.HasAutoAnchorDelay.Value)
    {
      var autoDelayInMS = VehicleDebugConfig.AutoAnchorDelayTime.Value * 1000f;
      Invoke(nameof(DelayedAnchor),
        autoDelayInMS);
      HasPendingAnchor = true;
      return;
    }

    SendSetAnchor(true);
  }


  public void UpdateShipSpeed(bool hasControllingPlayer, int playerCount)
  {
    if (isAnchored && vehicleSpeed != Ship.Speed.Stop)
    {
      vehicleSpeed = Ship.Speed.Stop;
      // force resets rudder to 0 degree position
      m_rudderValue = 0f;
    }

    var isNotAnchoredWithNobodyOnboard = playerCount == 0 && !isAnchored;

    if (isNotAnchoredWithNobodyOnboard)
    {
      // exits
      if (VehicleDebugConfig.HasAutoAnchorDelay.Value) return;
      SendSetAnchor(true);
      SendSpeedChange(DirectionChange.Stop);
      return;
    }

    var isUncontrolledRowing = !hasControllingPlayer &&
                               vehicleSpeed is Ship.Speed.Slow
                                 or Ship.Speed.Back &&
                               !PropulsionConfig.SlowAndReverseWithoutControls
                                 .Value;
    if (isUncontrolledRowing)
    {
      SendSpeedChange(DirectionChange.Stop);
    }
  }

  private void Update()
  {
    OnControllingWithHotKeyPress();
    AutoVerticalFlightUpdate();
  }

  /// <summary>
  /// Handles updating direction controls, update Controls is called within the FixedUpdate of VehicleShip
  /// </summary>
  /// <param name="dt"></param>
  public void UpdateControls(float dt)
  {
    if (m_nview.IsOwner())
    {
      m_nview.GetZDO().Set(ZDOVars.s_forward, (int)vehicleSpeed);
      m_nview.GetZDO().Set(ZDOVars.s_rudder, m_rudderValue);
      return;
    }

    if (Time.time - m_sendRudderTime > 1f)
    {
      if (HasPendingAnchor) return;
      vehicleSpeed = (Ship.Speed)m_nview.GetZDO().GetInt(ZDOVars.s_forward);
      m_rudderValue = m_nview.GetZDO().GetFloat(ZDOVars.s_rudder);
    }
  }

  private void SetTargetHeight(float val)
  {
    switch (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      case false:
        m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, 0f);
        break;
      case true:
        m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, val);
        break;
    }
  }

  private void SyncTargetHeight()
  {
    if (!ShipInstance?.NetView) return;
    if (ShipInstance.NetView == null) return;

    var zdoTargetHeight = ShipInstance.NetView.m_zdo.GetFloat(
      VehicleZdoVars.VehicleTargetHeight,
      TargetHeight);
    TargetHeight = zdoTargetHeight;
  }

  private void InitializeRPC()
  {
    if (m_nview && !_hasRegister)
    {
      RegisterRPCListeners();
      _hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    // ship piece bounds syncing
    m_nview.Unregister(nameof(RPC_SyncBounds));

    // ship speed
    m_nview.Unregister(nameof(RPC_SpeedChange));

    // anchor logic
    m_nview.Unregister(nameof(RPC_SetAnchor));

    // rudder direction
    m_nview.Unregister(nameof(RPC_Rudder));

    // boat sway
    m_nview.Unregister(nameof(RPC_SetOceanSway));

    // steering
    m_nview.Unregister(nameof(RPC_RequestControl));
    m_nview.Unregister(nameof(RPC_RequestResponse));
    m_nview.Unregister(nameof(RPC_ReleaseControl));

    _hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    // ship piece bounds syncing
    m_nview.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

    // ship speed
    m_nview.Register<int>(nameof(RPC_SpeedChange), RPC_SpeedChange);

    // anchor logic
    m_nview.Register<bool>(nameof(RPC_SetAnchor), RPC_SetAnchor);

    // rudder direction
    m_nview.Register<float>(nameof(RPC_Rudder), RPC_Rudder);

    // boat sway
    m_nview.Register<bool>(nameof(RPC_SetOceanSway), RPC_SetOceanSway);

    // steering
    m_nview.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    m_nview.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    m_nview.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);

    _hasRegister = true;
  }


  /**
   * Will not be supported in v3.x.x
   */
  public void DEPRECATED_InitializeRudderWithShip(
    SteeringWheelComponent steeringWheel, Ship ship)
  {
    m_nview = ship.m_nview;
    ship.m_controlGuiPos = steeringWheel.transform;
    var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint;
    }

    InitializeRPC();
  }

  public void InitializeWheelWithShip(
    SteeringWheelComponent steeringWheel)
  {
    vehicleShip.m_controlGuiPos = transform;

    var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }
  }

  private void OnEnable()
  {
    Instances.Add(this);
    MonoUpdaterInstances.Add(this);
    StartCoroutine(nameof(ShipFixRoutine));
  }

  private void OnDisable()
  {
    Instances.Remove(this);
    MonoUpdaterInstances.Remove(this);
    StopCoroutine(nameof(ShipFixRoutine));
  }

  private void OnDestroy()
  {
    if (_hasRegister)
    {
      UnRegisterRPCListeners();
    }

    RemovePlayersBeforeDestroyingBoat();

    CancelInvoke(nameof(SyncTargetHeight));
  }

  public void SendRequestControl(long playerId, Transform attachTransform)
  {
    m_nview?.InvokeRPC(nameof(RPC_RequestControl),
      [playerId, attachTransform]);
  }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var isOwner = m_nview?.IsOwner() ?? false;
    var isInBoat = IsPlayerInBoat(playerID);

#if DEBUG
    if (!isInBoat)
    {
      Logger.LogDebug(
        "RPC_RequestControl requested the owner to give control but they are not within the boat. Skipping");
    }

    if (!isOwner)
    {
      Logger.LogDebug(
        "Error, the requestControl RPC made it to the wrong user even though they were the netView ZDO owner");
    }
#endif

    if (!isOwner || !isInBoat)
    {
      return;
    }

    var isValidUser = false;
    if (GetUser() == playerID || !HaveValidUser())
    {
      m_nview?.GetZDO().Set(ZDOVars.s_user, playerID);
      isValidUser = true;
    }

    m_nview?.InvokeRPC(sender, nameof(RPC_RequestResponse), isValidUser);
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (m_nview == null) return;

    if (m_nview.IsOwner() && GetUser() == playerID)
    {
      m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
    }
  }

  private float _previousTargetHeight = 0f;
  private ShipFloatation? _currentShipFloatation;

  public ShipFloatation ShipFloatationObj
  {
    get
    {
      if (_currentShipFloatation != null) return _currentShipFloatation.Value;
      _currentShipFloatation = GetShipFloatationObj();
      return _currentShipFloatation.Value;
    }
  }

  public static float maxFlyingHeight = 5000f;


  public void Descend()
  {
    if (MovementFlags.HasFlag(VehicleMovementFlags.IsAnchored))
    {
      SendSetAnchor(state: false);
    }

    // lerped
    _previousTargetHeight = TargetHeight;

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value &&
        !WaterConfig.ManualBallast.Value)
    {
      UpdateTargetHeight(0f);
    }
    else
    {
      if (!FloatCollider)
      {
        return;
      }

      UpdateTargetHeight(TargetHeight + _maxVerticalOffset);

      if (FloatCollider.transform.position.y - _maxVerticalOffset <=
          ZoneSystem.instance.m_waterLevel && !WaterConfig.ManualBallast.Value)
      {
        Logger.LogMessage("Vehicle below the collision zone");
        // UpdateTargetHeight(0f);
      }
    }
  }

  public void Ascend()
  {
    if (isAnchored)
    {
      SendSetAnchor(false);
    }

    _previousTargetHeight = TargetHeight;
    var currentTargetHeight = TargetHeight;

    if (IsBallastAndFlightDisabled)
    {
      UpdateTargetHeight(0f);
    }
    else
    {
      if (!FloatCollider)
      {
        return;
      }

      UpdateTargetHeight(currentTargetHeight - _maxVerticalOffset);
    }
  }

  /// <summary>
  /// A negative offset pushing collider lower, forcing ship higher
  /// todo This will need to be leverage for calculating ship high in flight too
  /// </summary>
  /// <returns></returns>
  public float GetSurfaceOffset()
  {
    if (IsBallastAndFlightDisabled) return 0f;
    // this may need to be positive as a 2000f is pretty large render distance when calculating land collision. Likely can just inverse some flight mechanic logic.
    if (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      // fly up to 2000f in the sky.
      return -2000f;
    }

    if (WaterConfig.ManualBallast.Value)
    {
      return -OnboardCollider.bounds.extents.y;
    }

    return 0f;
  }

  /// <summary>
  /// Max depth the vehicle can go underwater or on ground.
  /// A (in water) positive offset that pushes the collider upwards, dropping the vehicle lower
  /// </summary>
  public float GetMaxDepthOffset()
  {
    var highestGroundPoint = GetHighestGroundPoint(ShipFloatationObj);
    return ZoneSystem.instance.m_waterLevel - highestGroundPoint;
  }

  private float lastForceUpdateTimer = 0f;

  /// <summary>
  /// Supports force updates in case we need to update the target based on an emergency.
  /// </summary>
  /// <param name="rawValue">Negative makes the ship go upwards, positive makes the ship go downwards</param>
  /// <param name="forceUpdate"></param>
  public void UpdateTargetHeight(float rawValue, bool forceUpdate = false)
  {
    _previousTargetHeight = TargetHeight;
    var maxSurfaceLevelOffset = GetSurfaceOffset();
    var maxDepthOffset = GetMaxDepthOffset();

    var clampedValue = Mathf.Clamp(
      rawValue,
      maxSurfaceLevelOffset, maxDepthOffset);

    var canForceUpdate = lastForceUpdateTimer == 0f && forceUpdate;
    var timeMult = canForceUpdate
      ? 0.5f
      : Time.fixedDeltaTime * WaterConfig.AutoBallastSpeed.Value;

    TargetHeight = Mathf.Lerp(_previousTargetHeight, clampedValue, timeMult);

    if (Mathf.Approximately(_previousTargetHeight, TargetHeight)) return;

    m_nview?.GetZDO().Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
    ShipInstance?.Instance?.UpdateShipEffects();

    if (forceUpdate && lastForceUpdateTimer is >= 0f and < 0.2f)
    {
      lastForceUpdateTimer += Time.fixedDeltaTime;
    }
    else if (lastForceUpdateTimer > 0f)
    {
      lastForceUpdateTimer = 0f;
    }
  }

  public void AutoAscendUpdate()
  {
    if (!_isAscending || _isHoldingAscend || _isHoldingDescend) return;
    var clampedHeight = Mathf.Clamp(
      FloatCollider.transform.position.y + _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    TargetHeight = Mathf.Lerp(TargetHeight, clampedHeight,
      Time.fixedDeltaTime * WaterConfig.AutoBallastSpeed.Value);
    m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
  }

  public void AutoDescendUpdate()
  {
    if (!_isDescending || _isHoldingDescend || _isHoldingAscend) return;
    var clampedHeight = Mathf.Clamp(
      FloatCollider.transform.position.y - _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    TargetHeight = Mathf.Lerp(TargetHeight, clampedHeight,
      Time.fixedDeltaTime * WaterConfig.AutoBallastSpeed.Value);
    m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
  }

  public void AutoVerticalFlightUpdate()
  {
    if (!ValheimRaftPlugin.Instance.AllowFlight.Value ||
        !ValheimRaftPlugin.Instance.FlightVerticalToggle.Value ||
        isAnchored) return;

    if (Mathf.Approximately(TargetHeight, 200f) ||
        Mathf.Approximately(TargetHeight, ZoneSystem.instance.m_waterLevel))
    {
      _isAscending = false;
      _isDescending = false;
      return;
    }

    AutoAscendUpdate();
    AutoDescendUpdate();
  }


  private void ToggleAutoDescend()
  {
    if (IsNotFlying)
    {
      _isAscending = false;
      _isDescending = false;
      return;
    }

    if (!ValheimRaftPlugin.Instance.FlightVerticalToggle.Value) return;

    if (!_isHoldingDescend && _isDescending)
    {
      _isAscending = false;
      _isDescending = false;
      return;
    }

    _isAscending = false;
    _isDescending = true;
  }

  public static bool ShouldHandleControls()
  {
    var character = (Character)Player.m_localPlayer;
    if (!character) return false;

    var isAttachedToShip = character.IsAttachedToShip();
    var isAttached = character.IsAttached();

    return isAttached && isAttachedToShip;
  }

  public static void DEPRECATED_OnFlightControls(
    MoveableBaseShipComponent mbShip)
  {
    if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
    {
      mbShip.Ascend();
    }
    else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
    {
      mbShip.Descent();
    }
  }

  public static bool IsBallastAndFlightDisabled =>
    !ValheimRaftPlugin.Instance.AllowFlight.Value &&
    !WaterConfig.ManualBallast.Value;

  public void OnFlightControls()
  {
    if (IsBallastAndFlightDisabled || isAnchored) return;
    if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
    {
      if (_isAscending || _isDescending)
      {
        _isAscending = false;
        _isDescending = false;
      }
      else
      {
        Ascend();
        ToggleAutoAscend();
      }
    }
    else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
    {
      if (_isDescending || _isAscending)
      {
        _isDescending = false;
        _isAscending = false;
      }
      else
      {
        Descend();
        ToggleAutoDescend();
      }
    }
    else
    {
      _isHoldingAscend = false;
      _isHoldingDescend = false;
    }
  }

  public static bool GetAnchorKeyUp()
  {
    if (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
        "False" &&
        ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
        "Not set")
    {
      var mainKeyString = ValheimRaftPlugin.Instance.AnchorKeyboardShortcut
        .Value.MainKey
        .ToString();
      var buttonDownDynamic =
        ZInput.GetButtonUp(mainKeyString);

      if (buttonDownDynamic)
      {
        Logger.LogDebug($"Dynamic Anchor Button down: {mainKeyString}");
      }

      return buttonDownDynamic ||
             ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsUp();
    }

    var isPressingRun =
      ZInput.GetButtonUp("Run") || ZInput.GetButtonUp("JoyRun");
    var isPressingJoyRun = ZInput.GetButtonUp("JoyRun");

    return isPressingRun || isPressingJoyRun;
  }

  public static bool GetAnchorKeyDown()
  {
    if (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
        "False" &&
        ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
        "Not set")
    {
      var mainKeyString = ValheimRaftPlugin.Instance.AnchorKeyboardShortcut
        .Value.MainKey
        .ToString();
      var buttonDownDynamic =
        ZInput.GetButtonDown(mainKeyString);

      if (buttonDownDynamic)
      {
        Logger.LogDebug($"Dynamic Anchor Button down: {mainKeyString}");
      }

      return buttonDownDynamic ||
             ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown();
    }

    var isPressingRun =
      ZInput.GetButtonDown("Run") || ZInput.GetButtonDown("JoyRun");
    var isPressingJoyRun = ZInput.GetButtonDown("JoyRun");

    return isPressingRun || isPressingJoyRun;
  }

  private void OnAnchorKeyPress()
  {
    if (GetAnchorKeyUp())
    {
      _isHoldingAnchor = false;
      return;
    }

    var isAnchorKeyDown = GetAnchorKeyDown();
    if (!isAnchorKeyDown)
    {
      if (_isHoldingAnchor)
      {
        _isHoldingAnchor = false;
      }

      return;
    }

    if (_isHoldingAnchor)
    {
      Logger.LogDebug(
        "Anchor key skipped due to update already fired currently pending");
      return;
    }

    Logger.LogDebug("Anchor Keydown is pressed");
    var flag = HaveControllingPlayer();
    if (flag && Player.m_localPlayer.IsAttached() &&
        Player.m_localPlayer.m_attachPoint &&
        Player.m_localPlayer.m_doodadController != null)
    {
      Logger.LogDebug("toggling vehicleShip anchor");
      ToggleAnchor();
      _isHoldingAnchor = true;
    }
    else
    {
      Logger.LogDebug("Player not controlling ship, skipping");
    }
  }

  private void OnControllingWithHotKeyPress()
  {
    var hasControllingPlayer = HaveControllingPlayer();

    // Edge case but the player could be detached. This guard allows the next anchor click if returning to the controls.
    if (!hasControllingPlayer)
    {
      if (_isHoldingAnchor)
      {
        _isHoldingAnchor = false;
      }

      return;
    }

    OnAnchorKeyPress();
    OnFlightControls();
  }


/*
 * Toggle the ship anchor and emit the event to other players so their client can update
 */
  public void ToggleAnchor() => SendSetAnchor(!isAnchored);


  private void ToggleAutoAscend()
  {
    if (IsNotFlying)
    {
      _isAscending = false;
      _isDescending = false;
      return;
    }

    if (!ValheimRaftPlugin.Instance.FlightVerticalToggle.Value) return;

    if (!_isHoldingAscend && _isAscending)
    {
      _isAscending = false;
      _isDescending = false;
      return;
    }

    _isAscending = true;
    _isDescending = false;
  }

  public void SyncRudder(float rudder)
  {
    m_nview.InvokeRPC(nameof(RPC_Rudder), rudder);
  }


  internal void RPC_Rudder(long sender, float value)
  {
    m_rudderValue = value;
  }

  /// <summary>
  /// Setter method for anchor, directly calling this before invoking ZDO call will cause de-syncs so this should only be used in the RPC
  /// </summary>
  /// <param name="isEnabled"></param>
  /// <returns></returns>
  private VehicleMovementFlags HandleSetAnchor(bool isEnabled)
  {
    if (isEnabled)
    {
      _isAscending = false;
      _isDescending = false;
      // only stops speed if the anchor is dropped.
      vehicleSpeed = Ship.Speed.Stop;
    }

    return isEnabled
      ? (MovementFlags | VehicleMovementFlags.IsAnchored)
      : (MovementFlags & ~VehicleMovementFlags.IsAnchored);
  }

  public void SendSetAnchor(bool state)
  {
    if (_isHoldingAnchor)
    {
      Logger.LogDebug(
        $"skipped due to IsUpdatingAnchorState: {_isHoldingAnchor}");
      return;
    }

    if (HasPendingAnchor)
    {
      // Might need to rethink this if it's heavy performance hit. Maybe a coroutine if calling cancel invoke is constant.
      CancelInvoke(nameof(DelayedAnchor));
      HasPendingAnchor = false;
    }

    SetAnchor(state);
    if (state)
    {
      SendSpeedChange(DirectionChange.Stop);
    }

    m_nview.InvokeRPC(nameof(RPC_SetAnchor), state);
  }

  public void SendToggleOceanSway()
  {
    m_nview.InvokeRPC(0, nameof(RPC_SetOceanSway), !HasOceanSwayDisabled);
  }

  public void RPC_SetOceanSway(long sender, bool state)
  {
    if (!m_nview) return;
    var isOwner = m_nview.IsOwner();
    if (isOwner)
    {
      var zdo = m_nview.GetZDO();
      zdo.Set(VehicleZdoVars.VehicleOceanSway, state);
    }

    SyncShip();
  }

  /// <summary>
  /// SyncZDOs if they are out of alignment
  /// </summary>
  private void SyncShip()
  {
    if (ZNetView.m_forceDisableInit) return;
    SyncAnchor();
    SyncOceanSway();
  }

  private void SyncOceanSway()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!m_nview)
    {
      return;
    }

    var zdo = m_nview.GetZDO();
    if (zdo == null) return;

    var isEnabled = zdo.GetBool(VehicleZdoVars.VehicleOceanSway, false);
    HasOceanSwayDisabled = isEnabled;
  }

  private void SyncAnchor()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!isActiveAndEnabled) return;
    if (m_nview == null)
    {
      MovementFlags = VehicleMovementFlags.None;
      return;
    }

    if (m_nview.isActiveAndEnabled != true) return;

    var newFlags =
      (VehicleMovementFlags)m_nview.GetZDO()
        .GetInt(VehicleZdoVars.VehicleFlags, (int)MovementFlags);

    if (MovementFlags != newFlags)
    {
      Logger.LogDebug(
        $"newFlags do not match currentFlags {MovementFlags} newFlags:{newFlags}");
      MovementFlags = newFlags;
    }
  }

  /// <summary>
  /// Method to be called only from a direct setter (as a fallback) or RPC_SetAnchor
  /// </summary>
  /// <param name="state"></param>
  /// <param name="hasOverride"></param>
  public void SetAnchor(bool state, bool hasOverride = false)
  {
    var newFlags = HandleSetAnchor(state);
    Logger.LogDebug(
      $"Setting anchor to: {state} the new movementFlag should be {newFlags}");

    if (m_nview.IsOwner() || hasOverride)
    {
      var zdo = m_nview.GetZDO();
      zdo.Set(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
    }

    MovementFlags = newFlags;
    isAnchored = state;
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    SetAnchor(state);
  }

  private void OnControlsHandOff()
  {
    if (!Player.m_localPlayer || !ShipInstance?.Instance)
    {
      return;
    }

    // the person controlling the ship should control physics
    var playerOwner = Player.m_localPlayer.GetOwner();
    m_nview.GetZDO().SetOwner(playerOwner);
    Logger.LogDebug("Changing ship owner to " + playerOwner +
                    $", name: {Player.m_localPlayer.GetPlayerName()}");
    SyncVehicleBounds();
    var attachTransform = lastUsedWheelComponent.AttachPoint;
    Player.m_localPlayer.StartDoodadControl(lastUsedWheelComponent);
    if (attachTransform != null)
    {
      Player.m_localPlayer.AttachStart(attachTransform, null,
        hideWeapons: false, isBed: false,
        onShip: true, m_attachAnimation, detachOffset);
      ShipInstance.Instance.m_controlGuiPos =
        lastUsedWheelComponent.wheelTransform;
    }
  }

  private void RPC_RequestResponse(long sender, bool granted)
  {
    if (!Player.m_localPlayer || !ShipInstance?.Instance)
    {
      return;
    }

    if (granted)
    {
      OnControlsHandOff();
    }
    else
    {
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
    }
  }

  public void ApplyControlls(Vector3 dir) => ApplyControls(dir);

  /// <summary>
  /// Updates based on the controls provided
  /// </summary>
  /// <param name="dir"></param>
  public void ApplyControls(Vector3 dir)
  {
    var isForward = (double)dir.z > 0.5;
    var isBackward = (double)dir.z < -0.5;

    if (isForward && !m_forwardPressed)
    {
      SendSpeedChange(DirectionChange.Forward);
    }

    if (isBackward && !m_backwardPressed)
    {
      SendSpeedChange(DirectionChange.Backward);
    }

    var fixedDeltaTime = Time.fixedDeltaTime;
    var num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(m_rudderValue));
    m_rudder = dir.x * num;
    m_rudderValue += m_rudder * m_rudderSpeed * fixedDeltaTime;
    m_rudderValue = Mathf.Clamp(m_rudderValue, -1f, 1f);
    // deadzone logic to allow rudder to be centered.
    if (Time.fixedDeltaTime - m_sendRudderTime > 0.2f)
    {
      // allows updating rudder but zeros it out quickly in a deadzone.
      if (m_rudderValue is >= -0.1f and < 0.1f)
      {
        m_rudderValue = 0.0f;
      }

      m_sendRudderTime = Time.time;
      SyncRudder(m_rudderValue);
    }

    m_forwardPressed = isForward;
    m_backwardPressed = isBackward;
  }

  public void SendSpeedChange(DirectionChange directionChange)
  {
    switch (directionChange)
    {
      case DirectionChange.Forward:
        SetForward();
        break;
      case DirectionChange.Backward:
        SetBackward();
        break;
      case DirectionChange.Stop:
      default:
        vehicleSpeed = Ship.Speed.Stop;
        break;
    }

    m_nview.InvokeRPC(0, nameof(RPC_SpeedChange), (int)vehicleSpeed);
  }

  internal void RPC_SpeedChange(long sender, int speed)
  {
    if (isAnchored && PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
    {
      MovementFlags = HandleSetAnchor(false);
    }

    vehicleSpeed = (Ship.Speed)speed;
  }


  private void SetForward()
  {
    if (isAnchored && !PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
    {
      vehicleSpeed = Ship.Speed.Stop;
      return;
    }

    switch (vehicleSpeed)
    {
      case Ship.Speed.Stop:
        vehicleSpeed = Ship.Speed.Slow;
        break;
      case Ship.Speed.Slow:
        vehicleSpeed = Ship.Speed.Half;
        break;
      case Ship.Speed.Half:
        vehicleSpeed = Ship.Speed.Full;
        break;
      case Ship.Speed.Back:
        vehicleSpeed = Ship.Speed.Stop;
        break;
      case Ship.Speed.Full:
        break;
      default:
        Logger.LogWarning($"Recieved a unknown Ship.Speed, {vehicleSpeed}");
        break;
    }
  }

  private void SetBackward()
  {
    if (isAnchored && !PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
    {
      vehicleSpeed = Ship.Speed.Stop;
      return;
    }

    switch (vehicleSpeed)
    {
      case Ship.Speed.Stop:
        vehicleSpeed = Ship.Speed.Back;
        break;
      case Ship.Speed.Slow:
        vehicleSpeed = Ship.Speed.Stop;
        break;
      case Ship.Speed.Half:
        vehicleSpeed = Ship.Speed.Slow;
        break;
      case Ship.Speed.Full:
        vehicleSpeed = Ship.Speed.Half;
        break;
      case Ship.Speed.Back:
        break;
    }
  }

  public void FireReleaseControl(Player player)
  {
    if (m_nview == null) return;
    if (!m_nview.IsValid()) return;
    m_nview.InvokeRPC(nameof(RPC_ReleaseControl), player.GetPlayerID());
    if (AttachPoint != null)
    {
      player.AttachStop();
    }
  }

  public bool HaveValidUser()
  {
    var user = GetUser();
    if (!ShipInstance?.Instance) return false;
    return user != 0L && IsPlayerInBoat(user);
  }

  private long GetUser()
  {
    if (!m_nview) return 0L;
    return !m_nview.IsValid()
      ? 0L
      : m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }
}