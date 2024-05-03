using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Extensions;
using Jotunn.Managers;
using Registry;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

internal static class VehicleShipHelpers
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
public class VehicleShip : ValheimBaseGameShip, IVehicleShip
{
  public GameObject RudderObject { get; set; }

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;

  public static bool CustomShipPhysicsEnabled = false;

  public GameObject GhostContainer =>
    VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
      PrefabNames.GhostContainer);

  public GameObject PiecesContainer =>
    VehicleShipHelpers.GetOrFindObj(_piecesContainer, transform.parent.gameObject,
      PrefabNames.PiecesContainer);

  // The determines the directional movement of the ship 
  public GameObject ColliderParentObj;

  public IWaterVehicleController VehicleController => _controller;

  public GameObject? ShipEffectsObj;
  public VehicleShipEffects? ShipEffects;

  private WaterVehicleController _controller;
  public ZSyncTransform m_zsyncTransform;

  public VehicleDebugHelpers VehicleDebugHelpersInstance { get; private set; }


  public VehicleMovementController MovementController
  {
    get => m_shipControlls;
    set => m_shipControlls = value;
  }

  private float _shipRotationOffset = 0f;
  private GameObject _shipRotationObj = new();

  public Transform? ShipDirection => ShipMovementOrientation;

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

  public VehicleShip Instance => this;

  public BoxCollider FloatCollider
  {
    get => m_floatcollider;
    set => m_floatcollider = value;
  }

  public Transform ShipMovementOrientation { get; set; }

  public BoxCollider BlockingCollider { get; set; }
  public BoxCollider OnboardCollider { get; set; }

  public Transform ControlGuiPosition
  {
    get => m_controlGuiPos;
    set => m_controlGuiPos = value;
  }

  public new void OnTriggerEnter(Collider collider)
  {
    base.OnTriggerEnter(collider);
  }

  public void OnDestroy()
  {
    if ((bool)_vehiclePiecesContainerInstance)
    {
      Destroy(_vehiclePiecesContainerInstance);
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

  public void UpdateShipRotation(float offset)
  {
    // _shipRotationOffset = offset;
    // if (!(bool)m_floatcollider) return;
    // if (m_floatcollider.transform.position != _shipRotationObj.transform.position)
    // {
    //   _shipRotationObj.transform.position = m_floatcollider.transform.position;
    // }
    //
    // var dynamicRotation = new Vector3(
    //   0,
    //   _shipRotationOffset,
    //   0);
    //
    // _shipRotationObj.transform.localRotation = Quaternion.Euler(dynamicRotation);
  }

  private static bool GetAnchorKey()
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

      // Logger.LogDebug($"AnchorKey: leftShift {isLeftShiftDown}, mainKey: {mainKeyString}");
      // Logger.LogDebug(
      //   $"AnchorKey isDown: {ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown()}");
      return buttonDownDynamic || isLeftShiftDown ||
             ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown();
    }

    var isPressingRun = ZInput.GetButtonDown("Run") || ZInput.GetButtonDown("JoyRun");
    var isPressingJoyRun = ZInput.GetButtonDown("JoyRun");

    // Logger.LogDebug(
    //   $"AnchorKey isPressingRun: {isPressingRun},isPressingJoyRun {isPressingJoyRun} ");

    return isPressingRun || isPressingJoyRun;
  }

  private void Update()
  {
    if (!GetAnchorKey()) return;
    Logger.LogDebug("Anchor Keydown is pressed");

    var flag = HaveControllingPlayer();
    if (flag && Player.m_localPlayer.IsAttached() && Player.m_localPlayer.m_attachPoint &&
        Player.m_localPlayer.m_doodadController != null)
    {
      Logger.LogDebug("toggling vehicleShip anchor");
      _controller.ToggleAnchor();
    }
    else
    {
      Logger.LogDebug("Player not controlling ship, skipping");
    }
  }

  public bool IsReady()
  {
    var netView = GetComponent<ZNetView>();
    return netView && netView.isActiveAndEnabled;
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

    BlockingCollider = blockingColliderObj.GetComponent<BoxCollider>();
    FloatCollider = floatColliderObj.GetComponent<BoxCollider>();
    OnboardCollider = onboardColliderObj.GetComponent<BoxCollider>();

    if (!(bool)m_body)
    {
      m_body = GetComponent<Rigidbody>();
    }

    if (!(bool)_shipRotationObj)
    {
      _shipRotationObj = new GameObject();
    }

    UpdateShipRotation(_shipRotationOffset);

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

  private void OnGUI()
  {
    if (myButtonStyle == null)
    {
      myButtonStyle = new GUIStyle(GUI.skin.button);
      myButtonStyle.fontSize = 50;
    }

    GUILayout.BeginArea(new Rect(300, 10, 150, 150), myButtonStyle);

    if (GUILayout.Button("Add forward force to ship"))
    {
      var directionForce = GetDirectionForce();
      var forceAmount = Vector3.forward;
      m_body.AddForceAtPosition(forceAmount, directionForce, ForceMode.Impulse);
    }

    var waterLevelOffset =
      GUILayout.TextField(m_waterLevelOffset.ToString(), 25);

    if (waterLevelOffset != null && float.TryParse(waterLevelOffset, out var offset))
    {
      m_waterLevelOffset = offset;
    }

    if (GUILayout.Button($"customphysics {CustomShipPhysicsEnabled}"))
    {
      CustomShipPhysicsEnabled = !CustomShipPhysicsEnabled;
    }

    if (GUILayout.Button("Rebuild bounds"))
    {
      if ((bool)_controller)
      {
        _controller.RebuildBounds();
      }
    }

    // called ShipHud, duplicate it and add some more ui stuff.
    if (GUILayout.Button("rotate90 ship"))
    {
      if (_shipRotationOffset > 360)
      {
        _shipRotationOffset = 0;
      }
      else
      {
        _shipRotationOffset += 90;
      }

      m_floatcollider.transform.Rotate(0, 90, 0);
      _controller.RebuildBounds();

      // Instance.transform.rotation = new Quaternion(Instance.transform.rotation.x,
      //   Instance.transform.rotation.y + 90, Instance.transform.rotation.z,
      //   Instance.transform.rotation.w);
    }

    // if (GUILayout.Button("rotate based on rudder dir"))
    // {
    //   if (_controller.m_rudderPieces.Count > 0)
    //   {
    //     var rudderPiece = _controller.m_rudderPieces.First();
    //     if (rudderPiece.transform.localRotation != shipRotationObj.transform.rotation)
    //     {
    //       shipRotationObj.transform.localRotation =
    //         Quaternion.Euler(0f, rudderPiece.transform.localRotation.y - 180f, 0f);
    //       m_body.centerOfMass = rudderPiece.transform.localPosition;
    //     }
    //   }
    //   // Instance.transform.rotation = new Quaternion(Instance.transform.rotation.x,
    //   //   Instance.transform.rotation.y + 90, Instance.transform.rotation.z,
    //   //   Instance.transform.rotation.w);
    //   // _controller.transform.rotation = new Quaternion(_controller.transform.rotation.x,
    //   //   _controller.transform.rotation.y - 90, _controller.transform.rotation.z,
    //   //   _controller.transform.rotation.w);
    // }

    if (GUILayout.Button("rotate based on steering"))
    {
      if (_controller.m_rudderWheelPieces.Count > 0)
      {
        var wheelPiece = _controller.m_rudderWheelPieces.First();
        m_body.centerOfMass = wheelPiece.transform.localPosition;
      }

      // FloatCollider.transform.Rotate(0, _shipRotationOffset, 0);
      // _controller.transform.SetParent(null);
      // Instance.transform.rotation = new Quaternion(Instance.transform.rotation.x,
      //   Instance.transform.rotation.y + 90, Instance.transform.rotation.z,
      //   Instance.transform.rotation.w);
      // _controller.transform.SetParent(transform);
    }

    GUILayout.EndArea();
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
      transform.rotation = Quaternion.Euler(transformedX, transform.rotation.y, transformedZ);
    }
  }

  private new void Awake()
  {
    AwakeSetupShipComponents();

    base.Awake();

    var excludedLayers = LayerMask.GetMask("piece", "piece_nonsolid");
    m_body.excludeLayers = excludedLayers;

    Logger.LogDebug($"called Awake in {name}, m_body {m_body}");
    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    FixShipRotation();


    InitializeWaterVehicleController();
  }

  public override void OnEnable()
  {
    base.OnEnable();
    InitializeWaterVehicleController();
  }

  // public void UpdateShipRotationObj(GameObject? go)
  // {
  //   if (!isActiveAndEnabled) return;
  //   if (go == null)
  //   {
  //     shipRotationObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
  //     // todo fix shipRotationObj calcs
  //     // shipRotationObj.transform.localPosition = Vector3.zero;
  //     // shipRotationObj.transform.SetParent(transform);
  //     // shipRotationObj.transform.localPosition = FloatColliderObj.transform.localPosition;
  //     return;
  //   }
  //
  //   shipRotationObj.transform.localRotation = Quaternion.Euler(
  //     go.transform.localRotation.eulerAngles.x, go.transform.localRotation.eulerAngles.y,
  //     go.transform.localRotation.eulerAngles.z);
  // }

  public void FixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_body || !(bool)m_floatcollider)
    {
      return;
    }

    if (CustomShipPhysicsEnabled)
    {
      // CustomPhysics();
      CustomFixedUpdate_Deprecated();
      return;
    }

    // todo remove this if unnecessary
    // ShipDirectionTransform.position = m_floatcollider.transform.position;
    // ShipDirectionTransform.rotation = Quaternion.Euler(
    //   m_floatcollider.transform.rotation.eulerAngles.x,
    //   ShipDirectionTransform.rotation.eulerAngles.y,
    //   m_floatcollider.transform.rotation.eulerAngles.z);

    TestFixedUpdate();
    // ValheimRaftCustomFixedUpdate();
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

    var prefab = PrefabManager.Instance.GetPrefab(PrefabNames.ShipHullPrefabName);
    if (!prefab) return;

    var hull = Instantiate(prefab, transform.position, transform.rotation);
    if (hull == null) return;

    var hullNetView = hull.GetComponent<ZNetView>();
    _controller.AddNewPiece(hullNetView);

    // todo This logic is unnecessary as InitPiece is called from zdo initialization of the PlaceholderItem
    //
    // var placeholderInstance = buildGhostInstance.GetPlaceholderInstance();
    // if (placeholderInstance == null) return;
    //
    // var hullNetView = placeholderInstance.GetComponent<ZNetView>();
    // hullNetView.transform.SetParent(null);
    //
    // AddNewPiece(hullNetView);
    // buildGhostInstance.DisableVehicleGhost();
    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    // var floor = ZNetScene.instance.GetPrefab("wood_floor");
    // for (var x = -1f; x < 1.01f; x += 2f)
    // {
    //   for (var z = -2f; z < 20.01f; z += 2f)
    //   {
    //     var pt = _controller.transform.TransformPoint(new Vector3(x,
    //       ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
    //     var obj = Instantiate(floor, pt, transform.rotation);
    //     var netview = obj.GetComponent<ZNetView>();
    //     _controller.AddNewPiece(netview);
    //   }
    // }

    _controller.SetInitComplete();
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

    if (VehicleDebugHelpersInstance == null && ValheimRaftPlugin.Instance.HasDebugBase.Value)
    {
      VehicleDebugHelpersInstance = gameObject.AddComponent<VehicleDebugHelpers>();
    }


    if (VehicleDebugHelpersInstance != null)
    {
      VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
      {
        collider = colliderParentBoxCollider,
        lineColor = Color.blue,
        parent = gameObject
      });

      VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
      {
        collider = _controller.m_floatcollider,
        lineColor = Color.green,
        parent = gameObject
      });
      VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
      {
        collider = _controller.m_blockingcollider,
        lineColor = Color.magenta,
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

    m_mastObject.transform.SetParent(_controller.transform);
    m_sailObject.transform.SetParent(_controller.transform);
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

  public Vector3 GetDirectionForce()
  {
    // Zero would would be +1 and 180 would be -1
    var vectorX = (float)Math.Cos(ShipDirection.localRotation.y);
    // VectorZ is going to be 0 force at 0 and 1 at 
    var vectorZ = (float)Math.Sin(ShipDirection.localRotation.y);

    /*
     * Computed sailSpeed based on the rudder settings.
     */
    switch (m_speed)
    {
      case Speed.Full:
        vectorX *= 0.4f;
        vectorZ *= 0.4f;
        break;
      case Speed.Half:
        vectorX *= 0.25f;
        vectorZ *= 0.25f;
        break;
      case Speed.Slow:
        // sailArea = Math.Min(0.1f, sailArea * 0.1f);
        vectorX *= 0.1f;
        vectorZ *= 0.1f;
        break;
      case Speed.Stop:
      case Speed.Back:
      default:
        vectorX *= 0f;
        vectorZ *= 0f;
        break;
    }

    var shipDirectionForce = new Vector3(vectorX, 0, vectorZ);
    return shipDirectionForce;
  }

  public void AddForceAtPosition(Vector3 force, Vector3 position,
    ForceMode forceMode)
  {
    m_body.AddForceAtPosition(force, position, forceMode);
    // var directionForce = GetDirectionForce();
    // var newForce = new Vector3(directionForce.x * force.x, force.y,
    //   directionForce.z * force.z);
    // m_body.AddForceAtPosition(newForce, position, forceMode);
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
    // either 90 or 270 degress so Sin 90 or Sin 270
    // if (Mathf.Abs((int)Mathf.Sin(shipRotationObj.transform.localEulerAngles.y +
    //                              direction.x * 90)) == 1)
    // {
    // }

    return m_floatcollider.extents.z;
  }

  public void CustomPhysics()
  {
    m_body.useGravity = _controller.m_targetHeight == 0f;

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

  public void TestFixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_nview || m_nview.m_zdo == null) return;

    /*
     * creative mode should not allows movement and applying force on a object will cause errors when the object is kinematic
     */
    if (_controller.isCreative)
    {
      return;
    }

    // This could be the spot that causes the raft to fly at spawn
    _controller.m_targetHeight =
      m_nview.m_zdo.GetFloat("MBTargetHeight", _controller.m_targetHeight);
    _controller.VehicleFlags =
      (WaterVehicleFlags)m_nview.m_zdo.GetInt("MBFlags",
        (int)_controller.VehicleFlags);

    // This could be the spot that causes the raft to fly at spawn
    m_zsyncTransform.m_useGravity =
      _controller.m_targetHeight == 0f;
    m_body.useGravity = _controller.m_targetHeight == 0f;

    var flag = HaveControllingPlayer();

    UpdateControls(Time.fixedDeltaTime);
    UpdateSail(Time.fixedDeltaTime);
    SyncVehicleMastsAndSails();
    UpdateRudder(Time.fixedDeltaTime, flag);
    if (m_players.Count == 0 ||
        _controller.VehicleFlags.HasFlag(WaterVehicleFlags
          .IsAnchored))
    {
      m_speed = Speed.Stop;
      m_rudderValue = 0f;
      if (!_controller.VehicleFlags.HasFlag(
            WaterVehicleFlags.IsAnchored))
      {
        _controller.VehicleFlags |=
          WaterVehicleFlags.IsAnchored;
        m_nview.m_zdo.Set("MBFlags", (int)_controller.VehicleFlags);
      }
    }

    if ((bool)m_nview && !m_nview.IsOwner()) return;

    if (m_body.isKinematic) return;
    // don't damage the ship lol
    // UpdateUpsideDmg(Time.fixedDeltaTime);

    if (!flag && (m_speed == Speed.Slow || m_speed == Speed.Back))
      m_speed = Speed.Stop;
    var worldCenterOfMass = m_body.worldCenterOfMass;
    var shipForward = ShipDirection.position +
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

    if (!isAboveBuoyantLevel)
    {
      _controller.UpdateStats(false);
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

      var num5 = Vector3.Dot(m_body.velocity, ShipDirection.forward);
      var num6 = Vector3.Dot(m_body.velocity, ShipDirection.right);
      var velocity = m_body.velocity;
      var value = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping *
                  currentDepthForceMultiplier;
      var value2 = num5 * num5 * Mathf.Sign(num5) * m_dampingForward * currentDepthForceMultiplier;
      var value3 = num6 * num6 * Mathf.Sign(num6) * m_dampingSideway * currentDepthForceMultiplier;

      velocity.y -= Mathf.Clamp(value, -1f, 1f);
      velocity -= ShipDirection.forward * Mathf.Clamp(value2, -1f, 1f);
      velocity -= ShipDirection.right * Mathf.Clamp(value3, -1f, 1f);

      if (velocity.magnitude > m_body.velocity.magnitude)
        velocity = velocity.normalized * m_body.velocity.magnitude;

      m_body.velocity = velocity;
      m_body.angularVelocity -=
        m_body.angularVelocity * m_angularDamping * currentDepthForceMultiplier;

      if (m_players.Count == 0 ||
          _controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(velocity);
        m_body.velocity = anchoredVelocity;
        m_body.angularVelocity = Vector3.zero;
      }

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

      ApplySailForce(this, num5);
      ApplyEdgeForce(Time.fixedDeltaTime);
      if (_controller.m_targetHeight > 0f)
      {
        var centerpos = ShipDirection.position;
        var centerforce = GetUpwardsForce(_controller.m_targetHeight,
          centerpos.y + m_body.velocity.y, _controller.m_liftForce);
        AddForceAtPosition(Vector3.up * centerforce, centerpos,
          ForceMode.VelocityChange);
      }
    }
    else if (_controller.m_targetHeight > 0f)
    {
      if (m_players.Count == 0 ||
          _controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(m_body.velocity);
        m_body.velocity = anchoredVelocity;
      }

      _controller.UpdateStats(true);

      var side1 = ShipDirection.position +
                  ShipDirection.forward * m_floatcollider.extents.z;
      var side2 = ShipDirection.position -
                  ShipDirection.forward * m_floatcollider.extents.z;
      var side3 = ShipDirection.position -
                  ShipDirection.right * m_floatcollider.extents.x;
      var side4 = ShipDirection.position +
                  ShipDirection.right * m_floatcollider.extents.x;
      var centerpos2 = ShipDirection.position;
      var corner1curforce = m_body.GetPointVelocity(side1);
      var corner2curforce = m_body.GetPointVelocity(side2);
      var corner3curforce = m_body.GetPointVelocity(side3);
      var corner4curforce = m_body.GetPointVelocity(side4);
      var side1force =
        GetUpwardsForce(_controller.m_targetHeight,
          side1.y + corner1curforce.y,
          _controller.m_balanceForce);
      var side2force =
        GetUpwardsForce(_controller.m_targetHeight,
          side2.y + corner2curforce.y,
          _controller.m_balanceForce);
      var side3force =
        GetUpwardsForce(_controller.m_targetHeight,
          side3.y + corner3curforce.y,
          _controller.m_balanceForce);
      var side4force =
        GetUpwardsForce(_controller.m_targetHeight,
          side4.y + corner4curforce.y,
          _controller.m_balanceForce);
      var centerforce2 = GetUpwardsForce(_controller.m_targetHeight,
        centerpos2.y + m_body.velocity.y, _controller.m_liftForce);

      /**
       * applies only center force to keep boat stable and not flip
       */
      // AddForceAtPosition(Vector3.up * centerforce2, side1,
      //   ForceMode.VelocityChange);
      // AddForceAtPosition(Vector3.up * centerforce2, side2,
      //   ForceMode.VelocityChange);
      // AddForceAtPosition(Vector3.up * centerforce2, side3,
      //   ForceMode.VelocityChange);
      // AddForceAtPosition(Vector3.up * centerforce2, side4,
      //   ForceMode.VelocityChange);
      // AddForceAtPosition(Vector3.up * centerforce2, centerpos2,
      //   ForceMode.VelocityChange);


      AddForceAtPosition(Vector3.up * side1force, side1,
        ForceMode.VelocityChange);
      AddForceAtPosition(Vector3.up * side2force, side2,
        ForceMode.VelocityChange);
      AddForceAtPosition(Vector3.up * side3force, side3,
        ForceMode.VelocityChange);
      AddForceAtPosition(Vector3.up * side4force, side4,
        ForceMode.VelocityChange);
      AddForceAtPosition(Vector3.up * centerforce2, centerpos2,
        ForceMode.VelocityChange);

      var dir = Vector3.Dot(m_body.velocity, ShipDirection.forward);
      ApplySailForce(this, dir);
    }
  }

  public new void UpdateSail(float deltaTime)
  {
    // base.UpdateSail(deltaTime);

    UpdateSailSize(deltaTime);
    var windDir = EnvMan.instance.GetWindDir();
    windDir = Vector3.Cross(Vector3.Cross(windDir, ShipDirection.up),
      ShipDirection.up);
    if (m_speed == Speed.Full || m_speed == Speed.Half)
    {
      float t = 0.5f + Vector3.Dot(ShipDirection.forward, windDir) * 0.5f;
      Quaternion to = Quaternion.LookRotation(
        -Vector3.Lerp(windDir, Vector3.Normalize(windDir - ShipDirection.forward), t),
        ShipDirection.up);
      m_mastObject.transform.rotation =
        Quaternion.RotateTowards(m_mastObject.transform.rotation, to, 30f * deltaTime);
    }
    else if (m_speed == Speed.Back)
    {
      Quaternion from =
        Quaternion.LookRotation(-ShipDirection.forward, ShipDirection.up);
      Quaternion to2 = Quaternion.LookRotation(-windDir, ShipDirection.up);
      to2 = Quaternion.RotateTowards(from, to2, 80f);
      m_mastObject.transform.rotation =
        Quaternion.RotateTowards(m_mastObject.transform.rotation, to2, 30f * deltaTime);
    }
  }

  public float GetShipYawAngle()
  {
    var mainCamera = Utils.GetMainCamera();
    if (mainCamera == null)
    {
      return 0f;
    }

    return 0f -
           Utils.YawFromDirection(
             mainCamera.transform.InverseTransformDirection(ColliderParentObj.transform.forward));
  }

  public float GetWindAngle()
  {
    Vector3 windDir = EnvMan.instance.GetWindDir();
    return 0f -
           Utils.YawFromDirection(ColliderParentObj.transform.InverseTransformDirection(windDir));
  }


  /**
   * In theory we can just make the sailComponent and mastComponent parents of the masts/sails of the ship. This will make any mutations to those parents in sync with the sail changes
   */
  private void SyncVehicleMastsAndSails()
  {
    if (!(bool)_controller) return;

    foreach (var mast in _controller.m_mastPieces.ToList())
    {
      if (!(bool)mast)
      {
        _controller.m_mastPieces.Remove(mast);
      }
      else if (mast.m_allowSailShrinking)
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
        Quaternion.Euler(0f, m_rudderRotationMax * (0f - m_rudderValue) * 2, 0f), 0.5f);
      rudder.PivotPoint.localRotation = newRotation;
    }

    foreach (var wheel in _controller.m_rudderWheelPieces.ToList())
    {
      if (!(bool)wheel)
      {
        _controller.m_rudderWheelPieces.Remove(wheel);
      }
      else if ((bool)wheel.wheelTransform)
      {
        wheel.wheelTransform.localRotation = Quaternion.Slerp(
          wheel.wheelTransform.localRotation,
          Quaternion.Euler(
            m_rudderRotationMax * (0f - m_rudderValue) *
            wheel.m_wheelRotationFactor, 0f, 0f), 0.5f);
      }
    }
  }

  internal Vector3 GetSailForce(float sailSize, float dt)
  {
    Vector3 windDir = EnvMan.instance.GetWindDir();
    float windIntensity = EnvMan.instance.GetWindIntensity();
    float num = Mathf.Lerp(0.25f, 1f, windIntensity);
    float windAngleFactor = GetWindAngleFactor();
    windAngleFactor *= num;
    Vector3 target = Vector3.Normalize(windDir + ShipDirection.forward) *
                     (windAngleFactor * m_sailForceFactor * sailSize);
    m_sailForce = Vector3.SmoothDamp(m_sailForce, target, ref m_windChangeVelocity, 1f, 99f);
    return m_sailForce;

    // for testing rotation
    // var unchangedWindForce = new Vector3(-0.0120766945f, -0.00563957961f, 0.0823633149f);
    // return unchangedWindForce;
  }

  public new float GetWindAngleFactor()
  {
    float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -ColliderParentObj.transform.forward);
    float num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
    float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
    return num2 * num3;
  }

  private static void ApplySailForce(VehicleShip instance, float num5)
  {
    var sailArea = 0f;

    if ((bool)instance._controller)
    {
      sailArea = instance._controller.GetSailingForce();
    }

    /*
     * Computed sailSpeed based on the rudder settings.
     */
    switch (instance.m_speed)
    {
      case Speed.Full:
        break;
      case Speed.Half:
        sailArea *= 0.5f;
        break;
      case Speed.Slow:
        // sailArea = Math.Min(0.1f, sailArea * 0.1f);
        // sailArea = 0.1f;
        sailArea = 0;
        break;
      case Speed.Stop:
      case Speed.Back:
      default:
        sailArea = 0f;
        break;
    }

    if (instance._controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
    {
      sailArea = 0f;
    }

    var sailForce = instance.GetSailForce(sailArea, Time.fixedDeltaTime);

    var position = instance.m_body.worldCenterOfMass;


    //  * Math.Max(0.5f, ValheimRaftPlugin.Instance.RaftSailForceMultiplier.Value)
    // set the speed, this may need to be converted to a vector for the multiplier
    instance.AddForceAtPosition(
      sailForce,
      position,
      ForceMode.VelocityChange);


    // steer offset will need to be size x or size z depending on location of rotation.
    var stearoffset = instance.ShipDirection.position -
                      instance.ShipDirection.forward *
                      instance.GetFloatSizeFromDirection(Vector3.forward);
    var num7 = num5 * instance.m_stearVelForceFactor;
    instance.AddForceAtPosition(
      instance.ShipDirection.right *
      (num7 * (0f - instance.m_rudderValue) * Time.fixedDeltaTime),
      stearoffset, ForceMode.VelocityChange);
    var stearforce = Vector3.zero;
    switch (instance.m_speed)
    {
      case Speed.Slow:
        stearforce += instance.ShipDirection.forward *
                      (instance.m_backwardForce * (1f - Mathf.Abs(instance.m_rudderValue)));
        break;
      case Speed.Back:
        stearforce += -instance.ShipDirection.forward *
                      (instance.m_backwardForce * (1f - Mathf.Abs(instance.m_rudderValue)));
        break;
    }

    if (instance.m_speed == Speed.Back || instance.m_speed == Speed.Slow)
    {
      float num6 = instance.m_speed != Speed.Back ? 1 : -1;
      stearforce += instance.ShipDirection.right *
                    (instance.m_stearForce * (0f - instance.m_rudderValue) * num6);
    }

    instance.AddForceAtPosition(stearforce * Time.fixedDeltaTime, stearoffset,
      ForceMode.VelocityChange);
  }
}