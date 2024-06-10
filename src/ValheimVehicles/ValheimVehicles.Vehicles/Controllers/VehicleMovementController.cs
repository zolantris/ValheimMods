using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Registry;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : ValheimBaseGameShip, IVehicleMovement, IValheimShip
{
  private bool _hasRegister = false;

  // unfortunately the current approach does not allow increasing this beyond 1f otherwise it causes massive jitters when changing altitude.
  private float _maxVerticalOffset = 1f;
  public bool IsAnchored => MovementFlags.HasFlag(VehicleMovementFlags.IsAnchored);

  public enum DirectionChange
  {
    Forward,
    Backward,
    Stop,
  }

  public SteeringWheelComponent lastUsedWheelComponent;

  public IVehicleShip? ShipInstance => vehicleShip;

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
  public float TargetHeight { get; private set; }
  public Transform AttachPoint { get; set; }
  public bool HasOceanSwayDisabled { get; set; }

  public const string m_attachAnimation = "Standing Torch Idle right";

  public GameObject RudderObject { get; set; }
  public const float MinimumRigibodyMass = 1000;

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;

  public bool isCreative => ShipInstance?.Instance?.isCreative ?? false;

  public static bool HasVehicleDebugger = false;
  public const float m_balanceForce = 0.03f;

  public const float m_liftForce = 20f;
  public static bool CustomShipPhysicsEnabled = false;

  // The top level netview is preserved but the lower level ones are kept for playering syncing for physics objects AND do not interact with top level netviews
  public ZNetView? ShipNetView => ShipInstance?.NetView;

  public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

  private Ship.Speed VehicleSpeed => GetSpeedSetting();
  private float _shipRotationOffset = 0f;
  private GameObject _shipRotationObj = new();

  public Transform ShipDirection { get; set; } = null!;

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

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

  private Rigidbody _movementControllerRigidbody;

  public Rigidbody m_body { get; set; }

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
          Math.Abs(Math.Min(ValheimRaftPlugin.Instance.VehicleRudderSpeedBack.Value,
            ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value));
        break;
      case Ship.Speed.Slow:
        _rudderForce = Math.Min(ValheimRaftPlugin.Instance.VehicleRudderSpeedSlow.Value,
          ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      case Ship.Speed.Half:
        _rudderForce = Mathf.Min(ValheimRaftPlugin.Instance.VehicleRudderSpeedHalf.Value,
          ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value);
        break;
      case Ship.Speed.Full:
        _rudderForce = Mathf.Min(ValheimRaftPlugin.Instance.VehicleRudderSpeedFull.Value,
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
    return netView && netView.isActiveAndEnabled;
  }

  public void UpdateVehicleSpeedThrottle()
  {
    // caps the vehicle speeds to these values.
    // m_body.maxAngularVelocity = ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value;
    // m_body.maxLinearVelocity = ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value * 1.2f;
  }

  public void AwakeSetupShipComponents()
  {
    vehicleShip = GetComponent<VehicleShip>();
    var vehicleCollidersParentObj = VehicleShip.GetVehicleMovementCollidersObj(transform);

    var floatColliderObj =
      vehicleCollidersParentObj.transform.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      vehicleCollidersParentObj.transform.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      vehicleCollidersParentObj.transform.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    ShipDirection = floatColliderObj.Find(PrefabNames.VehicleShipMovementOrientation);
    BlockingCollider = blockingColliderObj.GetComponent<BoxCollider>();
    FloatCollider = floatColliderObj.GetComponent<BoxCollider>();
    OnboardCollider = onboardColliderObj.GetComponent<BoxCollider>();

    _impactEffect = GetComponent<ImpactEffect>();

    if (!(bool)_impactEffect)
    {
      _impactEffect = gameObject.AddComponent<ImpactEffect>();
      _impactEffect.m_triggerMask = LayerMask.GetMask("Default", "character", "piece", "terrain",
        "static_solid", "Default_small", "character_net", "vehicle", LayerMask.LayerToName(29));
      _impactEffect.m_toolTier = 1000;
    }

    zsyncTransform = PrefabRegistryHelpers.GetOrAddMovementZSyncTransform(gameObject);

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

    if (!(bool)m_sailCloth)
    {
      m_sailCloth = gameObject.AddComponent<Cloth>();
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

    // var physicalLayers = LayerMask.GetMask("Default", "character", "piece", "terrain",
    //   "static_solid", "Default_small", "character_net", "vehicle", LayerMask.LayerToName(29));
    // m_body.includeLayers = physicalLayers;
    m_body.excludeLayers = excludedLayers;
  }

  private new void Awake()
  {
    AwakeSetupShipComponents();

    m_body = GetComponent<Rigidbody>();
    m_nview = GetComponent<ZNetView>();

    var excludedLayers = LayerMask.GetMask("piece", "piece_nonsolid");
    m_body.excludeLayers = excludedLayers;

    Logger.LogDebug($"called Awake in {name}, m_body {m_body}");
    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    FixShipRotation();
    if (!ShipNetView) return;

    InitializeRPC();
    SyncShip();

    if (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      InvokeRepeating(nameof(SyncTargetHeight), 2f, 2f);
    }

    base.Awake();
  }

  public new void Start()
  {
    base.Start();
    Invoke(nameof(UpdateRemovePieceCollisionExclusions), 5f);
    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    if (!m_body)
    {
      m_body = GetComponent<Rigidbody>();
    }

    InitializeRPC();
    SyncShip();
  }

  public void FixedUpdate()
  {
    if (!(bool)m_body || !(bool)m_floatcollider)
    {
      return;
    }

    UpdateShipRudderTurningSpeed();

    /*
     * creative mode should not allow movement and applying force on a object will cause errors when the object is kinematic
     */
    if (isCreative || m_body.isKinematic)
    {
      return;
    }

    FixShipRotation();

    VehiclePhysicsFixedUpdate();
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
    return Vector3.SmoothDamp(currentVelocity * 0.5f, Vector3.zero, ref zeroVelocity, 5f);
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
      return m_floatcollider.extents.x;
    }

    return m_floatcollider.extents.z;
  }

  public void CustomPhysics()
  {
    m_body.useGravity = TargetHeight == 0f;

    var waterLevelAtCenterShip = Floating.GetWaterLevel(m_floatcollider.center, ref m_previousBack);

    // above the water
    if (waterLevelAtCenterShip < m_body.centerOfMass.y)
    {
      return;
    }

    m_body.WakeUp();
    m_body.AddForceAtPosition(Vector3.up * 1f, m_body.worldCenterOfMass,
      ForceMode.VelocityChange);
  }

  public void UpdateShipBalancingForce()
  {
    var front = ShipDirection.position +
                ShipDirection.forward * m_floatcollider.extents.z;
    var back = ShipDirection.position -
               ShipDirection.forward * m_floatcollider.extents.z;
    var left = ShipDirection.position -
               ShipDirection.right * m_floatcollider.extents.x;
    var right = ShipDirection.position +
                ShipDirection.right * m_floatcollider.extents.x;

    var centerpos2 = ShipDirection.position;
    var frontForce = m_body.GetPointVelocity(front);
    var backForce = m_body.GetPointVelocity(back);
    var leftForce = m_body.GetPointVelocity(left);
    var rightForce = m_body.GetPointVelocity(right);

    var frontUpwardsForce =
      GetUpwardsForce(TargetHeight,
        front.y + frontForce.y,
        m_balanceForce);
    var backUpwardsForce =
      GetUpwardsForce(TargetHeight,
        back.y + backForce.y,
        m_balanceForce);
    var leftUpwardsForce =
      GetUpwardsForce(TargetHeight,
        left.y + leftForce.y,
        m_balanceForce);
    var rightUpwardsForce =
      GetUpwardsForce(TargetHeight,
        right.y + rightForce.y,
        m_balanceForce);
    var centerUpwardsForce = GetUpwardsForce(TargetHeight,
      centerpos2.y + m_body.velocity.y, m_liftForce);


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

  public void UpdateShipFlying()
  {
    UpdateVehicleStats(true);
    // early exit if anchored.
    if (UpdateAnchorVelocity(m_body.velocity))
    {
      return;
    }

    m_body.WakeUp();

    UpdateShipBalancingForce();

    if (!ValheimRaftPlugin.Instance.FlightHasRudderOnly.Value)
    {
      ApplySailForce(this, true);
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

  private void UpdateVehicleStats(bool flight)
  {
    ShipInstance?.VehiclePiecesController.Instance.SyncRigidbodyStats(flight);

    m_angularDamping = (flight ? 5f : 0.8f);
    m_backwardForce = 1f;
    m_damping = (flight ? 5f : 0.35f);
    m_dampingSideway = (flight ? 3f : 0.3f);
    m_force = 3f;
    m_forceDistance = 5f;
    m_sailForceFactor = (flight ? 0.2f : 0.05f);
    m_stearForce = (flight ? 0.2f : 1f);
    m_stearVelForceFactor = 1.3f;
    m_waterImpactDamage = 0f;

    if (!_impactEffect)
    {
      _impactEffect = GetComponent<ImpactEffect>();
    }

    if (!_impactEffect)
    {
      gameObject.AddComponent<ImpactEffect>();
    }

    if ((bool)_impactEffect)
    {
      _impactEffect.m_nview = m_nview;
      _impactEffect.m_body = m_body;
      _impactEffect.m_hitType = HitData.HitType.Boat;
      _impactEffect.m_interval = 0.5f;
      _impactEffect.m_minVelocity = 0.1f;
      _impactEffect.m_damages.m_blunt = GetDamageFromImpact();
    }
    else
    {
      Logger.LogDebug("No Ship ImpactEffect detected, this needs to be added to the custom ship");
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

    m_body.WakeUp();
    UpdateWaterForce(currentDepth, Time.fixedDeltaTime);

    var leftForce = new Vector3(shipLeft.x, waterLevelLeft, shipLeft.z);
    var rightForce = new Vector3(shipRight.x, waterLevelRight, shipRight.z);
    var forwardForce = new Vector3(shipForward.x, waterLevelForward, shipForward.z);
    var backwardForce = new Vector3(shipBack.x, waterLevelBack, shipBack.z);

    var fixedDeltaTime = Time.fixedDeltaTime;
    var deltaForceMultiplier = fixedDeltaTime * 50f;

    var currentDepthForceMultiplier = Mathf.Clamp01(Mathf.Abs(currentDepth) / m_forceDistance);
    var upwardForceVector = Vector3.up * m_force * currentDepthForceMultiplier;

    AddForceAtPosition(upwardForceVector * deltaForceMultiplier, worldCenterOfMass,
      ForceMode.VelocityChange);

    // todo rename variables for this section to something meaningful
    // todo abstract this to a method
    var deltaForward = Vector3.Dot(m_body.velocity, ShipDirection.forward);
    var deltaRight = Vector3.Dot(m_body.velocity, ShipDirection.right);
    var velocity = m_body.velocity;
    var deltaUp = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping *
                  currentDepthForceMultiplier;

    var deltaForwardClamp = deltaForward * deltaForward * Mathf.Sign(deltaForward) *
                            m_dampingForward *
                            currentDepthForceMultiplier;
    var deltaRightClamp = deltaRight * deltaRight * Mathf.Sign(deltaRight) * m_dampingSideway *
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
    var f = Mathf.Clamp((forwardForce.y - shipForward.y) * num7, 0f - num8, num8);
    var f2 = Mathf.Clamp((backwardForce.y - shipBack.y) * num7, 0f - num8, num8);
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
    UpdateVehicleStats(false);
    UpdateWaterForce(shipFloatation);
    ApplyEdgeForce(Time.fixedDeltaTime);
    if (HasOceanSwayDisabled)
    {
      transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
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
        !IsAnchored) return false;

    var anchoredVelocity = CalculateAnchorStopVelocity(velocity);
    var anchoredAngularVelocity = CalculateAnchorStopVelocity(m_body.angularVelocity);

    m_body.velocity = anchoredVelocity;
    m_body.angularVelocity = anchoredAngularVelocity;
    return true;
  }

  public ShipFloatation GetShipFloatationObj()
  {
    var worldCenterOfMass = m_body.worldCenterOfMass;
    var shipForward = ShipDirection!.position +
                      ShipDirection.forward * GetFloatSizeFromDirection(Vector3.forward);
    var shipBack = ShipDirection.position -
                   ShipDirection.forward * GetFloatSizeFromDirection(Vector3.forward);
    var shipLeft = ShipDirection.position -
                   ShipDirection.right * GetFloatSizeFromDirection(Vector3.right);
    var shipRight = ShipDirection.position +
                    ShipDirection.right * GetFloatSizeFromDirection(Vector3.right);
    var waterLevelCenter = Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter);

    var waterLevelLeft = Floating.GetWaterLevel(shipLeft, ref m_previousLeft);
    var waterLevelRight = Floating.GetWaterLevel(shipRight, ref m_previousRight);
    var waterLevelForward = Floating.GetWaterLevel(shipForward, ref m_previousForward);
    var waterLevelBack = Floating.GetWaterLevel(shipBack, ref m_previousBack);
    var averageWaterHeight =
      (waterLevelCenter + waterLevelLeft + waterLevelRight + waterLevelForward + waterLevelBack) /
      5f;
    var currentDepth = worldCenterOfMass.y - averageWaterHeight - m_waterLevelOffset;
    var isAboveBuoyantLevel = currentDepth > m_disableLevel;

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
    };
  }

  // Updates gravity and target height (which is used to compute gravity)
  public void UpdateGravity()
  {
    zsyncTransform.m_useGravity =
      TargetHeight == 0f;
    m_body.useGravity = TargetHeight == 0f;
  }

  public void UpdateShipCreativeModeRotation()
  {
    if (!isCreative) return;
    var rotationY = m_body.rotation.eulerAngles.y;

    if (PatchController.HasGizmoMod)
    {
      if (ValheimRaftPlugin.Instance.ComfyGizmoPatchCreativeHasNoRotation.Value)
      {
        rotationY = 0;
      }
      else
      {
        rotationY = ComfyGizmo_Patch.GetNearestSnapRotation(m_body.rotation.eulerAngles.y);
      }
    }

    var rotationWithoutTilt = Quaternion.Euler(0, rotationY, 0);
    m_body.rotation = rotationWithoutTilt;
  }

  /// <summary>
  /// Only Updates for the controlling player. Only players are synced
  /// </summary>
  public void VehiclePhysicsFixedUpdate()
  {
    if (!(bool)ShipInstance?.VehiclePiecesController.Instance || !(bool)m_nview ||
        m_nview.m_zdo == null ||
        !(bool)ShipDirection) return;

    UpdateGravity();

    var hasControllingPlayer = HaveControllingPlayer();

    // Sets values based on m_speed
    UpdateShipRudderTurningSpeed();
    UpdateShipSpeed(hasControllingPlayer, m_players.Count);

    //base ship direction controls
    UpdateControls(Time.fixedDeltaTime);
    UpdateSail(Time.fixedDeltaTime);

    // rudder direction
    UpdateRudder(Time.fixedDeltaTime, hasControllingPlayer);

    // raft pieces transforms
    SyncVehicleRotationDependentItems();

    /**
     * The remaining physics for ship logic must be done only one 1 client to prevent nasty desyncs.
     */
    // if (!m_nview.IsOwner()) return;
    if (!ValheimRaftPlugin.Instance.SyncShipPhysicsOnAllClients.Value && !m_nview.IsOwner()) return;


    var shipFloatation = GetShipFloatationObj();

    if (!shipFloatation.IsAboveBuoyantLevel)
    {
      UpdateShipFloatation(shipFloatation);
    }
    else if (TargetHeight > 0f)
    {
      UpdateShipFlying();
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
        b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * 6f) * 20f, 0f);
      }
      else if (VehicleSpeed == Ship.Speed.Back)
      {
        m_rudderPaddleTimer += dt;
        b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * -3f) * 40f, 0f);
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

    if ((bool)m_sailCloth)
    {
      if (speed == Ship.Speed.Stop || speed == Ship.Speed.Slow || speed == Ship.Speed.Back)
      {
        if (flag && m_sailCloth.enabled)
        {
          m_sailCloth.enabled = false;
        }
      }
      else if (flag)
      {
        if (!m_sailWasInPosition)
        {
          Utils.RecreateComponent(ref m_sailCloth);
        }
      }
      else
      {
        m_sailCloth.enabled = true;
      }
    }

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
          -Vector3.Lerp(windDir, Vector3.Normalize(windDir - ShipDirection.forward), t),
          ShipDirection.up);
        m_mastObject.transform.rotation =
          Quaternion.RotateTowards(m_mastObject.transform.rotation, to, 30f * deltaTime);
        break;
      }
      case Ship.Speed.Back:
      {
        var from =
          Quaternion.LookRotation(-ShipDirection.forward, ShipDirection.up);
        var to2 = Quaternion.LookRotation(-windDir, ShipDirection.up);
        to2 = Quaternion.RotateTowards(from, to2, 80f);
        m_mastObject.transform.rotation =
          Quaternion.RotateTowards(m_mastObject.transform.rotation, to2, 30f * deltaTime);
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
             mainCamera.transform.InverseTransformDirection(ShipDirection.forward));
  }

  public float GetWindAngle()
  {
    // moder power support
    var isWindPowerActive = IsWindControllActive();

    var windDir = isWindPowerActive ? ShipDirection.forward : EnvMan.instance.GetWindDir();
    return 0f -
           Utils.YawFromDirection(ShipDirection.InverseTransformDirection(windDir));
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

    foreach (var mast in ShipInstance.VehiclePiecesController.Instance.m_mastPieces.ToList())
    {
      if (!(bool)mast)
      {
        ShipInstance.VehiclePiecesController.Instance.m_mastPieces.Remove(mast);
        continue;
      }

      if (mast.m_allowSailShrinking)
      {
        if (mast.m_sailObject.transform.localScale != m_sailObject.transform.localScale)
          mast.m_sailCloth.enabled = false;
        mast.m_sailObject.transform.localScale = m_sailObject.transform.localScale;
        mast.m_sailCloth.enabled = m_sailCloth.enabled;
      }
      else
      {
        mast.m_sailObject.transform.localScale = Vector3.one;
        mast.m_sailCloth.enabled = !mast.m_disableCloth;
      }
    }

    foreach (var rudder in ShipInstance.VehiclePiecesController.Instance.m_rudderPieces.ToList())
    {
      if (!(bool)rudder)
      {
        ShipInstance.VehiclePiecesController.Instance.m_rudderPieces.Remove(rudder);
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

    foreach (var wheel in ShipInstance.VehiclePiecesController.Instance._steeringWheelPieces
               .ToList())
    {
      if (!(bool)wheel)
      {
        ShipInstance.VehiclePiecesController.Instance._steeringWheelPieces.Remove(wheel);
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
    var windIntensity = IsWindControllActive() ? 1f : EnvMan.instance.GetWindIntensity();
    var interpolatedWindIntensity = Mathf.Lerp(0.25f, 1f, windIntensity);

    var windAngleFactor = GetWindAngleFactor();
    windAngleFactor *= interpolatedWindIntensity;
    return windAngleFactor;
  }

  private float GetSailForceEnergy(float sailSize, float windAngleFactor)
  {
    return windAngleFactor * m_sailForceFactor * sailSize;
  }

  private Vector3 GetSailForce(float sailSize, float dt, bool isFlying)
  {
    var windDir = IsWindControllActive() ? Vector3.zero : EnvMan.instance.GetWindDir();
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

    m_sailForce = Vector3.SmoothDamp(m_sailForce, target, ref m_windChangeVelocity, 1f, 99f);

    return Vector3.ClampMagnitude(m_sailForce, 20f);
  }

  private new float GetWindAngleFactor()
  {
    if (IsWindControllActive()) return 1f;
    var num = Vector3.Dot(EnvMan.instance.GetWindDir(), -ShipDirection!.forward);
    var num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
    var num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
    return num2 * num3;
  }

  private Vector3 GetAdditiveSteerForce(float directionMultiplier)
  {
    return ShipDirection.right *
           (m_stearForce * (0f - m_rudderValue) * directionMultiplier);
  }

  /// <summary>
  /// Sets the speed of the ship with rudder speed added to it.
  /// </summary>
  /// Does not apply for stopped or anchored states
  /// 
  private void ApplyRudderForce()
  {
    if (VehicleSpeed == Ship.Speed.Stop ||
        IsAnchored) return;

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
    if (VehicleSpeed is Ship.Speed.Back or Ship.Speed.Slow)
    {
      steerForce += GetAdditiveSteerForce(directionMultiplier);
    }

    // Same logic as above, just separate to split out rudder custom speed potential divergence
    if (ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
    {
      if (
        (VehicleSpeed is Ship.Speed.Half &&
         ValheimRaftPlugin.Instance.VehicleRudderSpeedHalf.Value > 0) ||
        (VehicleSpeed is Ship.Speed.Full &&
         ValheimRaftPlugin.Instance.VehicleRudderSpeedFull.Value > 0))
      {
        steerForce += GetAdditiveSteerForce(directionMultiplier);
      }
    }

    if (TargetHeight > 0)
    {
      transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    AddForceAtPosition(steerForce * Time.fixedDeltaTime, steerOffset,
      ForceMode.VelocityChange);
  }

  private static void ApplySailForce(VehicleMovementController instance, bool isFlying = false)
  {
    if (!instance || !instance?.m_body || !instance?.ShipDirection) return;

    var sailArea = 0f;

    if (instance?.ShipInstance?.VehiclePiecesController != null)
    {
      sailArea = instance.ShipInstance.VehiclePiecesController.Instance.GetSailingForce();
    }

    /*
     * Computed sailSpeed based on the rudder settings.
     */
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

    if (instance.IsAnchored)
    {
      sailArea = 0f;
    }

    var sailForce = instance.GetSailForce(sailArea, Time.fixedDeltaTime, isFlying);

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


  public void UpdateControlls(float dt) => UpdateControls(dt);

  public void HandlePlayerHitVehicleBounds(Collider collider, bool isExiting)
  {
    if (ShipInstance?.Instance == null) return;
    var playerComponent = collider.GetComponent<Player>();

    if ((bool)playerComponent)
    {
      var containerCharacter = m_players.Contains(playerComponent);

      if (containerCharacter && isExiting)
      {
        Logger.LogDebug("Player over board, players left " + m_players.Count);
        m_players.Remove(playerComponent);
      }

      if (!containerCharacter && !isExiting)
      {
        Logger.LogDebug("Player onboard, total onboard " + m_players.Count);
        m_players.Add(playerComponent);
      }

      if (playerComponent != Player.m_localPlayer) return;

      if (isExiting)
      {
        PlayerSpawnController.Instance.SyncLogoutPoint();
        s_currentShips.Remove(this);
        Player.m_localPlayer.transform.SetParent(null);
      }

      if (!isExiting)
      {
        s_currentShips.Add(this);
        Player.m_localPlayer.transform.SetParent(ShipInstance.Instance.VehiclePiecesController
          .Instance
          .transform);
      }
    }
  }

  public void HandleCharacterHitVehicleBounds(Collider collider, bool isExiting)
  {
    var character = collider.GetComponent<Character>();
    if (!(bool)character) return;
    if (isExiting)
    {
      character.InNumShipVolumes--;
    }
    else
    {
      character.InNumShipVolumes++;
    }
  }

  public void OnTriggerEnter(Collider collider)
  {
    HandlePlayerHitVehicleBounds(collider, false);
    HandleCharacterHitVehicleBounds(collider, false);
  }

  public void OnTriggerExit(Collider collider)
  {
    HandlePlayerHitVehicleBounds(collider, true);
    HandleCharacterHitVehicleBounds(collider, true);
  }

  public Ship.Speed GetSpeedSetting() => vehicleSpeed;

  public float GetRudderValue() => m_rudderValue;
  public float GetRudder() => m_rudder;

  public void OnFlightChangePolling()
  {
    CancelInvoke(nameof(SyncTargetHeight));
    if (ValheimRaftPlugin.Instance.AllowFlight.Value)
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
  public void UpdateShipRudderTurningSpeed()
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
        Logger.LogError($"Speed value could not handle this variant, {vehicleSpeed}");
        m_rudderSpeed = 1f;
        break;
    }
  }

  public void UpdateShipSpeed(bool hasControllingPlayer, int playerCount)
  {
    if (IsAnchored && vehicleSpeed != Ship.Speed.Stop)
    {
      vehicleSpeed = Ship.Speed.Stop;
      // force resets rudder to 0 degree position
      m_rudderValue = 0f;
    }

    if (playerCount == 0 && !IsAnchored)
    {
      SendSetAnchor(true);
    }
    else if (!hasControllingPlayer && vehicleSpeed is Ship.Speed.Slow or Ship.Speed.Back)
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
      vehicleSpeed = (Ship.Speed)m_nview.GetZDO().GetInt(ZDOVars.s_forward);
      m_rudderValue = m_nview.GetZDO().GetFloat(ZDOVars.s_rudder);
    }
  }

  private void SetTargetHeight(float val)
  {
    switch (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      case false:
        ShipNetView?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, 0f);
        break;
      case true:
        ShipNetView?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, val);
        break;
    }
  }

  private void SyncTargetHeight()
  {
    if (!ShipInstance?.NetView) return;

    var zdoTargetHeight = ShipInstance!.NetView.m_zdo.GetFloat(
      VehicleZdoVars.VehicleTargetHeight,
      TargetHeight);
    TargetHeight = zdoTargetHeight;
  }

  private void InitializeRPC()
  {
    if (ShipNetView && !_hasRegister)
    {
      RegisterRPCListeners();
      _hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    // ship speed
    ShipNetView.Unregister(nameof(RPC_SpeedChange));

    // anchor logic
    ShipNetView.Unregister(nameof(RPC_SetAnchor));

    // rudder direction
    ShipNetView.Unregister(nameof(RPC_Rudder));

    // boat sway
    ShipNetView.Unregister(nameof(RPC_SetOceanSway));

    // steering
    ShipNetView.Unregister(nameof(RPC_RequestControl));
    ShipNetView.Unregister(nameof(RPC_RequestResponse));
    ShipNetView.Unregister(nameof(RPC_ReleaseControl));

    _hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    // ship speed
    ShipNetView.Register<int>(nameof(RPC_SpeedChange), RPC_SpeedChange);

    // anchor logic
    ShipNetView.Register<bool>(nameof(RPC_SetAnchor), RPC_SetAnchor);

    // rudder direction
    ShipNetView.Register<float>(nameof(RPC_Rudder), RPC_Rudder);

    // boat sway
    ShipNetView.Register<bool>(nameof(RPC_SetOceanSway), RPC_SetOceanSway);

    // steering
    ShipNetView.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    ShipNetView.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    ShipNetView.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);

    _hasRegister = true;
  }


  /**
   * Will not be supported in v3.x.x
   */
  public void DEPRECATED_InitializeRudderWithShip(SteeringWheelComponent steeringWheel, Ship ship)
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

  private void OnDestroy()
  {
    if (_hasRegister)
    {
      UnRegisterRPCListeners();
    }

    RemovePlayersBeforeDestroyingBoat();

    CancelInvoke(nameof(SyncTargetHeight));
  }

  public void FireRequestControl(long playerId, Transform attachTransform)
  {
    ShipNetView?.InvokeRPC(nameof(RPC_RequestControl), [playerId, attachTransform]);
  }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var isOwner = ShipNetView?.IsOwner() ?? false;
    var isInBoat = IsPlayerInBoat(playerID);
    if (!isOwner || !isInBoat) return;

    var isValidUser = false;
    if (GetUser() == playerID || !HaveValidUser())
    {
      ShipNetView?.GetZDO().Set(ZDOVars.s_user, playerID);
      isValidUser = true;
    }

    ShipNetView?.InvokeRPC(sender, nameof(RPC_RequestResponse), isValidUser);
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (ShipNetView == null) return;

    if (ShipNetView.IsOwner() && GetUser() == playerID)
    {
      ShipNetView.GetZDO().Set(ZDOVars.s_user, 0L);
    }
  }

  public void Descend()
  {
    if (MovementFlags.HasFlag(VehicleMovementFlags.IsAnchored))
    {
      SendSetAnchor(state: false);
    }

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      TargetHeight = 0f;
    }
    else
    {
      if (!FloatCollider)
      {
        return;
      }

      TargetHeight = Mathf.Clamp(
        FloatCollider.transform.position.y - _maxVerticalOffset,
        ZoneSystem.instance.m_waterLevel, 200f);

      if (FloatCollider.transform.position.y - _maxVerticalOffset <=
          ZoneSystem.instance.m_waterLevel)
      {
        TargetHeight = 0f;
      }
    }

    ShipNetView?.GetZDO()?.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
    ShipInstance?.Instance?.UpdateShipEffects();
  }

  public void Ascend()
  {
    if (IsAnchored)
    {
      SendSetAnchor(false);
    }

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      TargetHeight = 0f;
    }
    else
    {
      if (!FloatCollider)
      {
        return;
      }

      TargetHeight = Mathf.Clamp(
        FloatCollider.transform.position.y + _maxVerticalOffset,
        ZoneSystem.instance.m_waterLevel, 200f);
    }

    ShipNetView?.GetZDO().Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
    ShipInstance?.Instance?.UpdateShipEffects();
  }

  public void AutoAscendUpdate()
  {
    if (!_isAscending || _isHoldingAscend || _isHoldingDescend) return;
    TargetHeight = Mathf.Clamp(FloatCollider.transform.position.y + _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    ShipNetView?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
  }

  public void AutoDescendUpdate()
  {
    if (!_isDescending || _isHoldingDescend || _isHoldingAscend) return;
    TargetHeight = Mathf.Clamp(FloatCollider.transform.position.y - _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    ShipNetView?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
  }

  public void AutoVerticalFlightUpdate()
  {
    if (!ValheimRaftPlugin.Instance.AllowFlight.Value ||
        !ValheimRaftPlugin.Instance.FlightVerticalToggle.Value || IsAnchored) return;

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
    if (TargetHeight == 0f)
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

  public static void DEPRECATED_OnFlightControls(MoveableBaseShipComponent mbShip)
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


  public void OnFlightControls()
  {
    if (!ValheimRaftPlugin.Instance.AllowFlight.Value || IsAnchored) return;
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

  public static bool GetAnchorKey()
  {
    if (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "False" &&
        ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set")
    {
      var isLeftShiftDown = ZInput.GetButtonDown("LeftShift");
      var mainKeyString = ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.MainKey
        .ToString();
      var buttonDownDynamic =
        ZInput.GetButtonDown(mainKeyString);

      if (isLeftShiftDown)
      {
        Logger.LogDebug("LeftShift down");
      }

      if (buttonDownDynamic)
      {
        Logger.LogDebug($"Dynamic Anchor Button down: {mainKeyString}");
      }

      return buttonDownDynamic || isLeftShiftDown ||
             ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown();
    }

    var isPressingRun = ZInput.GetButtonDown("Run") || ZInput.GetButtonDown("JoyRun");
    var isPressingJoyRun = ZInput.GetButtonDown("JoyRun");

    return isPressingRun || isPressingJoyRun;
  }

  private void OnAnchorKeyPress()
  {
    if (!GetAnchorKey()) return;
    Logger.LogDebug("Anchor Keydown is pressed");
    var flag = HaveControllingPlayer();
    if (flag && Player.m_localPlayer.IsAttached() && Player.m_localPlayer.m_attachPoint &&
        Player.m_localPlayer.m_doodadController != null)
    {
      Logger.LogDebug("toggling vehicleShip anchor");
      ToggleAnchor();
    }
    else
    {
      Logger.LogDebug("Player not controlling ship, skipping");
    }
  }

  private void OnControllingWithHotKeyPress()
  {
    var hasControllingPlayer = HaveControllingPlayer();
    if (!hasControllingPlayer) return;
    OnAnchorKeyPress();
    OnFlightControls();
  }


  /*
   * Toggle the ship anchor and emit the event to other players so their client can update
   */
  public void ToggleAnchor() => SendSetAnchor(!IsAnchored);


  private void ToggleAutoAscend()
  {
    if (TargetHeight == 0f)
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
    ShipNetView.InvokeRPC(nameof(RPC_Rudder), rudder);
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
      // only stops speed if anchor is dropped.
      vehicleSpeed = Ship.Speed.Stop;
    }

    return isEnabled
      ? (MovementFlags | VehicleMovementFlags.IsAnchored)
      : (MovementFlags & ~VehicleMovementFlags.IsAnchored);
  }

  public void SendSetAnchor(bool state)
  {
    ShipNetView.InvokeRPC(0, nameof(RPC_SetAnchor), state);
    SendSpeedChange(DirectionChange.Stop);
  }

  public void SendToggleOceanSway()
  {
    ShipNetView.InvokeRPC(0, nameof(RPC_SetOceanSway), !HasOceanSwayDisabled);
  }

  public void RPC_SetOceanSway(long sender, bool state)
  {
    var isOwner = ShipNetView.IsOwner();
    if (isOwner)
    {
      var zdo = ShipNetView.GetZDO();
      zdo.Set(VehicleZdoVars.VehicleOceanSway, state);
    }

    Invoke(nameof(SyncShip), 0.25f);
  }

  private void SyncShip()
  {
    if (ZNetView.m_forceDisableInit) return;
    SyncAnchor();
    SyncOceanSway();
  }

  private void SyncOceanSway()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!ShipNetView)
    {
      MovementFlags = VehicleMovementFlags.None;
      return;
    }

    var zdo = ShipNetView.GetZDO();
    if (zdo == null) return;

    var isEnabled = zdo.GetBool(VehicleZdoVars.VehicleOceanSway, false);
    HasOceanSwayDisabled = isEnabled;
  }

  private void SyncAnchor()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!ShipNetView)
    {
      MovementFlags = VehicleMovementFlags.None;
      return;
    }

    if (ShipNetView?.isActiveAndEnabled != true) return;

    var newFlags =
      (VehicleMovementFlags)ShipNetView.GetZDO()
        .GetInt(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
    MovementFlags = newFlags;
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    var isOwner = ShipNetView.IsOwner();
    MovementFlags = HandleSetAnchor(state);
    if (isOwner)
    {
      var zdo = ShipNetView.GetZDO();
      zdo.Set(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
    }

    Invoke(nameof(SyncShip), 0.25f);
  }

  private void RPC_RequestResponse(long sender, bool granted)
  {
    if (!Player.m_localPlayer || !ShipInstance?.Instance)
    {
      return;
    }

    if (granted)
    {
      var attachTransform = lastUsedWheelComponent.AttachPoint;
      Player.m_localPlayer.StartDoodadControl(lastUsedWheelComponent);
      if (attachTransform != null)
      {
        Player.m_localPlayer.AttachStart(attachTransform, null, hideWeapons: false, isBed: false,
          onShip: true, m_attachAnimation, detachOffset);
        ShipInstance.Instance.m_controlGuiPos = lastUsedWheelComponent.wheelTransform;
      }

      // TODO this might not be a good idea. Restoring the polling in ValheimBaseGameShip might be a better approach.
      // set owner here is done because the person controlling the ship likely should be the one at the helm
      // NetView.GetZDO().SetOwner(Player.m_localPlayer.GetPlayerID());
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

    if (Time.time - m_sendRudderTime > 0.2f)
    {
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

    ShipNetView.InvokeRPC(0, nameof(RPC_SpeedChange), (int)vehicleSpeed);
  }

  internal void RPC_SpeedChange(long sender, int speed)
  {
    vehicleSpeed = (Ship.Speed)speed;
  }


  private void SetForward()
  {
    if (IsAnchored)
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
    }
  }

  private void SetBackward()
  {
    if (IsAnchored)
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
    if (!ShipNetView.IsValid()) return;
    ShipNetView.InvokeRPC(nameof(RPC_ReleaseControl), player.GetPlayerID());
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
    if (!ShipNetView) return 0L;
    return !ShipNetView.IsValid() ? 0L : ShipNetView.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }
}