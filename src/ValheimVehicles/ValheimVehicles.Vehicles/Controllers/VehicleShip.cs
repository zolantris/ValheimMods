using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jotunn.Extensions;
using Jotunn.Managers;
using Registry;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public static class VehicleShipHelpers
{
  public static GameObject GetOrFindObj(GameObject returnObj, GameObject searchObj,
    string objectName)
  {
    if ((bool)returnObj)
    {
      return returnObj;
    }

    var gameObjTransform = searchObj.transform.FindDeepChild(objectName);
    if (!gameObjTransform)
    {
      return returnObj;
    }

    returnObj = gameObjTransform.gameObject;
    return returnObj;
  }
}

/*
 * Acts as a Delegate component between the ship physics and the controller
 */
public class VehicleShip : ValheimBaseGameShip, IValheimShip, IVehicleShip
{
  public GameObject RudderObject { get; set; }
  public const float MinimumRigibodyMass = 1000;

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;

  public GameObject GhostContainer() =>
    VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
      PrefabNames.GhostContainer);

  public GameObject PiecesContainer() =>
    VehicleShipHelpers.GetOrFindObj(_piecesContainer, transform.parent.gameObject,
      PrefabNames.PiecesContainer);

  // floating mechanics
  private bool _hasBoatSway = true;

  public static readonly List<VehicleShip> AllVehicles = [];

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;
  public float TargetHeight => MovementController.TargetHeight;

  public bool isCreative = false;

  public static bool HasVehicleDebugger = false;

  public void SetCreativeMode(bool val)
  {
    isCreative = val;
  }

  public static bool CustomShipPhysicsEnabled = false;

  // The determines the directional movement of the ship 
  public GameObject ColliderParentObj;

  public IWaterVehicleController VehicleController => _controller;

  public GameObject? ShipEffectsObj;
  public VehicleShipEffects? ShipEffects;

  private WaterVehicleController _controller;
  public ZSyncTransform m_zsyncTransform;

  public ZNetView NetView => m_nview;

  public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

  public VehicleMovementController MovementController
  {
    get => m_shipControlls;
    set => m_shipControlls = value;
  }

  private Ship.Speed VehicleSpeed => MovementController.GetSpeedSetting();
  private float _shipRotationOffset = 0f;
  private GameObject _shipRotationObj = new();

  public Transform ShipDirection { get; set; } = null!;

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

  public Transform m_controlGuiPos { get; set; }


  public VehicleShip Instance => this;

  public BoxCollider FloatCollider
  {
    get => m_floatcollider;
    set => m_floatcollider = value;
  }

  public BoxCollider BlockingCollider { get; set; }
  public BoxCollider OnboardCollider { get; set; }

  public Transform ControlGuiPosition
  {
    get => m_controlGuiPos;
    set => m_controlGuiPos = value;
  }

  public static void OnAllowFlight(object sender, EventArgs eventArgs)
  {
    foreach (var vehicle in AllVehicles)
    {
      vehicle.MovementController.OnFlightChangePolling();
    }
  }

  /// <summary>
  ///  Removes player from boat if not null, disconnects can make the player null
  /// </summary>
  private void RemovePlayersBeforeDestroyingBoat()
  {
    foreach (var mPlayer in m_players)
    {
      if (!mPlayer) continue;
      BaseVehicleController.AddDynamicParentForVehicle(mPlayer.m_nview, VehicleController.Instance);
      mPlayer?.transform?.SetParent(null);
    }
  }

  public new void OnTriggerEnter(Collider collider)
  {
    var playerSpawnController = collider.GetComponentInParent<PlayerSpawnController>();
    playerSpawnController?.SyncLogoutPoint();

    var pieceComponent = collider.GetComponentInParent<Piece>();
    if ((bool)pieceComponent)
    {
      Logger.LogDebug($"collider was a piece, {collider.name} {pieceComponent.name}");
    }

    base.OnTriggerEnter(collider);
  }

  public new void OnTriggerExit(Collider collider)
  {
    var component = collider.GetComponent<Player>();
    if ((bool)component)
    {
      var playerSpawnController = component.GetComponent<PlayerSpawnController>();
      playerSpawnController?.SyncLogoutPoint();
      BaseVehicleController.RemoveDynamicParentForVehicle(component.m_nview);
      m_players.Remove(component);
      Logger.LogDebug("Player over board, players left " + m_players.Count);
      if (component == Player.m_localPlayer)
      {
        s_currentShips.Remove(this);
      }
    }

    var component2 = collider.GetComponent<Character>();
    if ((bool)component2)
    {
      component2.InNumShipVolumes--;
    }
  }

  private static void UpdateShipSounds(VehicleShip vehicleShip)
  {
    if (vehicleShip?.ShipEffects == null) return;
    vehicleShip.ShipEffects.m_inWaterSoundRoot.SetActive(ValheimRaftPlugin.Instance
      .EnableShipInWaterSounds.Value);
    vehicleShip.ShipEffects.m_wakeSoundRoot.SetActive(ValheimRaftPlugin.Instance
      .EnableShipWakeSounds.Value);
    // this one is not a gameobject so have to select the gameobject
    vehicleShip.ShipEffects.m_sailSound.gameObject.SetActive(ValheimRaftPlugin.Instance
      .EnableShipInWaterSounds.Value);
  }

  private static void UpdateAllShipSounds()
  {
    foreach (var vehicleShip in AllVehicles)
    {
      UpdateShipSounds(vehicleShip);
    }
  }

  public static void UpdateAllShipSounds(object sender, EventArgs eventArgs)
  {
    UpdateAllShipSounds();
  }

  /// <summary>
  /// Unloads the Boat Pieces properly
  /// </summary>
  ///
  /// <description>calling cleanup must be done before Unity starts garbage collecting otherwise positions, ZNetViews and other items may be destroyed</description>
  /// 
  public void UnloadAndDestroyPieceContainer()
  {
    if (!(bool)_vehiclePiecesContainerInstance) return;
    RemovePlayersBeforeDestroyingBoat();
    _controller.CleanUp();
    Destroy(_controller.gameObject);
  }

  public void OnDestroy()
  {
    AllVehicles.Remove(this);
    UnloadAndDestroyPieceContainer();

    if (MovementController.isActiveAndEnabled)
    {
      Destroy(MovementController.gameObject);
    }

    if ((bool)_shipRotationObj)
    {
      Destroy(_shipRotationObj);
    }

    // also destroys the sailcloth
    if ((bool)m_sailObject)
    {
      Destroy(m_sailObject);
    }

    if ((bool)m_mastObject)
    {
      Destroy(m_mastObject);
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
    if (m_players.Count != 0 && m_shipControlls)
    {
      return m_shipControlls.HaveValidUser();
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
    var colliderParentObj = transform.Find("colliders");
    var floatColliderObj =
      colliderParentObj.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      colliderParentObj.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      colliderParentObj.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    ShipDirection = floatColliderObj.Find(PrefabNames.VehicleShipMovementOrientation);
    BlockingCollider = blockingColliderObj.GetComponent<BoxCollider>();
    FloatCollider = floatColliderObj.GetComponent<BoxCollider>();
    OnboardCollider = onboardColliderObj.GetComponent<BoxCollider>();

    if (!MovementController)
    {
      MovementController = GetComponent<VehicleMovementController>();
    }

    if (MovementController)
    {
      MovementController.ShipInstance = this;
    }

    _impactEffect = GetComponent<ImpactEffect>();

    if (!(bool)m_body)
    {
      m_body = GetComponent<Rigidbody>();
    }

    UpdateVehicleSpeedThrottle();


    if (!(bool)m_zsyncTransform)
    {
      m_zsyncTransform = GetComponent<ZSyncTransform>();
    }

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
      m_sailCloth = m_sailObject.AddComponent<Cloth>();
    }

    if (!(bool)ShipEffectsObj)
    {
      ShipEffects = GetComponent<VehicleShipEffects>();
      ShipEffectsObj = ShipEffects.gameObject;
    }
  }

  public void ToggleShipEffects()
  {
    if (TargetHeight > 0f)
    {
      ShipEffectsObj?.SetActive(false);
    }
    else
    {
      ShipEffectsObj.SetActive(true);
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

  private new void Awake()
  {
    m_nview = GetComponent<ZNetView>();

    if (AllVehicles.Contains(this))
    {
      Logger.LogDebug("VehicleShip somehow already registered this component");
    }
    else
    {
      AllVehicles.Add(this);
    }


    AwakeSetupShipComponents();

    base.Awake();

    var excludedLayers = LayerMask.GetMask("piece_nonsolid");
    m_body.excludeLayers = excludedLayers;

    Logger.LogDebug($"called Awake in {name}, m_body {m_body}");
    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    FixShipRotation();


    InitializeWaterVehicleController();

    if (HasVehicleDebugger && _controller)
    {
      InitializeVehicleDebugger();
    }

    UpdateShipSounds(this);
  }

  public override void OnEnable()
  {
    base.OnEnable();
    InitializeWaterVehicleController();
    if (HasVehicleDebugger && _controller)
    {
      InitializeVehicleDebugger();
    }
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)m_nview || m_nview.GetZDO() == null || m_nview.m_ghost || (bool)_controller ||
        !isActiveAndEnabled) return;
    var sector = ZoneSystem.instance.GetZone(transform.position);
    var zdo = m_nview.GetZDO();
    zdo.SetPosition(transform.position);
    zdo.SetSector(sector);
  }

  public void FixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_body || !(bool)m_floatcollider)
    {
      return;
    }

    FixShipRotation();

    if (!_hasBoatSway)
    {
      return;
    }

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

  private void InitHull()
  {
    var pieceCount = _controller.GetPieceCount();
    if (pieceCount != 0 || !_controller.m_nview)
    {
      return;
    }

    if (_controller.BaseVehicleInitState != BaseVehicleController.InitializationState.Created)
    {
      return;
    }

    var prefab = PrefabManager.Instance.GetPrefab(PrefabNames.ShipHullCenterWoodPrefabName);
    if (!prefab) return;

    var hull = Instantiate(prefab, transform.position, transform.rotation);
    if (hull == null) return;

    var hullNetView = hull.GetComponent<ZNetView>();
    _controller.AddNewPiece(hullNetView);
    _controller.SetInitComplete();
  }

  public void InitializeVehicleDebugger()
  {
    if (VehicleDebugHelpersInstance != null) return;
    VehicleDebugHelpersInstance = gameObject.AddComponent<VehicleDebugHelpers>();

    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_floatcollider,
      lineColor = Color.green,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_blockingcollider,
      lineColor = Color.blue,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_onboardcollider,
      lineColor = Color.yellow,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.VehicleObj = gameObject;
    VehicleDebugHelpersInstance.VehicleShipInstance = this;
  }

  /*
   * Only initializes the controller if the prefab is enabled (when zdo is initialized this happens)
   */
  private void InitializeWaterVehicleController()
  {
    if (!(bool)m_nview || m_nview.GetZDO() == null || m_nview.m_ghost || (bool)_controller) return;

    enabled = true;

    var ladders = GetComponentsInChildren<Ladder>();
    foreach (var ladder in ladders)
      ladder.m_useDistance = 10f;

    var colliderParentBoxCollider =
      ColliderParentObj.gameObject.AddComponent<BoxCollider>();
    colliderParentBoxCollider.enabled = false;

    var vehiclePiecesContainer = VehiclePiecesPrefab.VehiclePiecesContainer;
    if (!vehiclePiecesContainer) return;

    _vehiclePiecesContainerInstance = Instantiate(vehiclePiecesContainer, null);
    _vehiclePiecesContainerInstance.transform.position = transform.position;
    _vehiclePiecesContainerInstance.transform.rotation = transform.rotation;

    _controller = _vehiclePiecesContainerInstance.AddComponent<WaterVehicleController>();
    _controller.InitializeShipValues(Instance);

    m_mastObject.transform.SetParent(_controller.transform);
    m_sailObject.transform.SetParent(_controller.transform);

    // sets back to unparented to prevent rigidbody from controlling parent physics

    InitHull();
  }

  /**
   * TODO this could be set to false for the ship as an override to allow the ship to never remove itself
   */
  public new bool CanBeRemoved()
  {
    return m_players.Count == 0;
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

  ///
  /// <summary>Paints / Visualizes GUI for the area it is updating on raft</summary>
  ///
  /// todo create this
  /// Lines created
  /// - purple = front left
  /// - blue = front right
  /// - orange = back left
  /// - yellow = back right
  ///
  public void DebugForceAtVectorPoint()
  {
  }

  /**
   *  Shows velocity numbers with maximum and minimum velocity numbers for raft propulsion debugging
   */
  public void UpdateVelocityHud()
  {
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
        _controller.m_balanceForce);
    var backUpwardsForce =
      GetUpwardsForce(TargetHeight,
        back.y + backForce.y,
        _controller.m_balanceForce);
    var leftUpwardsForce =
      GetUpwardsForce(TargetHeight,
        left.y + leftForce.y,
        _controller.m_balanceForce);
    var rightUpwardsForce =
      GetUpwardsForce(TargetHeight,
        right.y + rightForce.y,
        _controller.m_balanceForce);
    var centerUpwardsForce = GetUpwardsForce(TargetHeight,
      centerpos2.y + m_body.velocity.y, _controller.m_liftForce);


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
    if (!(bool)m_body) return 0f;

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
    _controller.SyncRigidbodyStats(flight);

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
    /*
     * this may be unstable and require a getter each time...highly doubt it though.
     */
    if (!_impactEffect)
    {
      _impactEffect = GetComponent<ImpactEffect>();
    }

    if ((bool)_impactEffect)
    {
      if (_impactEffect.m_nview.name.Contains(PrefabNames.WaterVehicleShip))
      {
        return;
      }

      _impactEffect.m_interval = 0.5f;
      _impactEffect.m_minVelocity = 0.1f;
      _impactEffect.m_damages.m_damage = GetDamageFromImpact();
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
    if (MovementController.HasOceanSwayDisabled)
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
        !MovementController.IsAnchored) return false;

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
    m_zsyncTransform.m_useGravity =
      TargetHeight == 0f;
    m_body.useGravity = TargetHeight == 0f;
  }

  public void EnforceCreativeMode()
  {
    if (!_controller) return;
    if ((!_controller.isCreative || m_body.rotation.eulerAngles.x == 0) &&
        m_body.rotation.eulerAngles.z == 0) return;
    var rotationWithoutTilt = Quaternion.Euler(0, m_body.rotation.eulerAngles.y, 0);
    m_body.rotation = rotationWithoutTilt;
  }

  /// <summary>
  /// Only Updates for the controlling player. Only players are synced
  /// </summary>
  public void VehiclePhysicsFixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_nview || m_nview.m_zdo == null ||
        !(bool)ShipDirection) return;

    /*
     * creative mode should not allow movement and applying force on a object will cause errors when the object is kinematic
     */
    if (_controller.isCreative || m_body.isKinematic)
    {
      EnforceCreativeMode();
      return;
    }

    UpdateGravity();

    var hasControllingPlayer = HaveControllingPlayer();

    // Sets values based on m_speed
    MovementController.UpdateShipRudderTurningSpeed();
    MovementController.UpdateShipSpeed(hasControllingPlayer, m_players.Count);

    //base ship direction controls
    MovementController.UpdateControls(Time.fixedDeltaTime);
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
      m_rudderRotationMax * (0f - MovementController.m_rudderValue), 0f);
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


  /**
   * In theory, we can just make the sailComponent and mastComponent parents of the masts/sails of the ship. This will make any mutations to those parents in sync with the sail changes
   */
  private void SyncVehicleRotationDependentItems()
  {
    if (!_controller.isActiveAndEnabled) return;

    foreach (var mast in _controller.m_mastPieces.ToList())
    {
      if (!(bool)mast)
      {
        _controller.m_mastPieces.Remove(mast);
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

    foreach (var rudder in _controller.m_rudderPieces.ToList())
    {
      if (!(bool)rudder)
      {
        _controller.m_rudderPieces.Remove(rudder);
        continue;
      }

      if (!rudder.PivotPoint)
      {
        Logger.LogError("No pivot point detected for rudder");
        continue;
      }

      var newRotation = Quaternion.Slerp(
        rudder.PivotPoint.localRotation,
        Quaternion.Euler(0f, m_rudderRotationMax * (0f - MovementController.GetRudderValue()) * 2,
          0f), 0.5f);
      rudder.PivotPoint.localRotation = newRotation;
    }

    foreach (var wheel in _controller._steeringWheelPieces.ToList())
    {
      if (!(bool)wheel)
      {
        _controller._steeringWheelPieces.Remove(wheel);
      }
      else if ((bool)wheel.wheelTransform)
      {
        wheel.wheelTransform.localRotation = Quaternion.Slerp(
          wheel.wheelTransform.localRotation,
          Quaternion.Euler(
            m_rudderRotationMax * (0f - MovementController.m_rudderValue) *
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

  /// <summary>
  /// Sets the speed of the ship with rudder speed added to it.
  /// </summary>
  /// Does not apply for stopped or anchored states
  /// 
  private void ApplyRudderForce()
  {
    if (VehicleSpeed == Ship.Speed.Stop ||
        MovementController.IsAnchored) return;

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
                            (0f - MovementController.m_rudderValue) *
                            Time.fixedDeltaTime);

    AddForceAtPosition(
      steerOffsetForce,
      steerOffset, ForceMode.VelocityChange);

    var steerForce = ShipDirection.forward *
                     (m_backwardForce * rudderForce *
                      (1f - Mathf.Abs(MovementController.m_rudderValue)));

    var directionMultiplier =
      ((VehicleSpeed != Ship.Speed.Back) ? 1 : (-1));
    steerForce *= directionMultiplier;

    // todo see if this is necessary. This logic is from the Base game Ship
    if (VehicleSpeed is Ship.Speed.Back or Ship.Speed.Slow)
    {
      steerForce += ShipDirection.right * m_stearForce * (0f - MovementController.m_rudderValue) *
                    directionMultiplier;
    }

    if (TargetHeight > 0)
    {
      transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    AddForceAtPosition(steerForce * Time.fixedDeltaTime, steerOffset,
      ForceMode.VelocityChange);
  }

  private static void ApplySailForce(VehicleShip instance, bool isFlying = false)
  {
    if (!instance || !instance?.m_body || !instance?.ShipDirection) return;

    var sailArea = 0f;

    if ((bool)instance._controller)
    {
      sailArea = instance._controller.GetSailingForce();
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

    if (instance.MovementController.IsAnchored)
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

  /**
    * Compatibility method remappings
    * delegates a bunch of required ship logic to MovementController
    */
  public Ship.Speed GetSpeedSetting() => VehicleSpeed;

  public float GetRudder() => MovementController.GetRudder();
  public float GetRudderValue() => MovementController.GetRudderValue();

  public void ApplyControlls(Vector3 dir) => MovementController.ApplyControls(dir);
  public void ApplyControls(Vector3 dir) => MovementController.ApplyControls(dir);

  public void Forward()
  {
    MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Forward);
  }

  public void Backward() =>
    MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Backward);

  public void Stop() =>
    MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Stop);

  public void UpdateControls(float dt) => MovementController.UpdateControls(dt);

  public void UpdateControlls(float dt) => MovementController.UpdateControls(dt);
}