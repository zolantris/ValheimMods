using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : MonoBehaviour, IVehicleMovement
{
  public IVehicleShip? ShipInstance { get; set; }
  public Vector3 detachOffset = new(0f, 0.5f, 0f);

  public VehicleMovementFlags MovementFlags { get; set; }

  internal bool m_forwardPressed;

  internal bool m_backwardPressed;

  internal float m_sendRudderTime;


  internal float m_rudder;
  public float m_rudderSpeed = 0.5f;
  internal float m_rudderValue;

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

  public SteeringWheelComponent lastUsedWheelComponent;

  public ZNetView NetView { get; set; }
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

  private void Awake()
  {
    NetView = GetComponent<ZNetView>();

    if (!NetView) return;
    if (!NetView.isActiveAndEnabled) return;

    InitializeRPC();
    SyncShip();

    if (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      InvokeRepeating(nameof(SyncTargetHeight), 2f, 2f);
    }
  }

  private void Start()
  {
    if (!NetView)
    {
      NetView = GetComponent<ZNetView>();
    }

    InitializeRPC();
    SyncShip();
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
        m_rudderSpeed = 1f;
        break;
      case Ship.Speed.Full:
        m_rudderSpeed = 0.5f;
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

  private void FixedUpdate()
  {
    UpdateShipRudderTurningSpeed();
  }

  /// <summary>
  /// Handles updating direction controls, update Controls is called within the FixedUpdate of VehicleShip
  /// </summary>
  /// <param name="dt"></param>
  public void UpdateControls(float dt)
  {
    if (NetView.IsOwner())
    {
      NetView.GetZDO().Set(ZDOVars.s_forward, (int)vehicleSpeed);
      NetView.GetZDO().Set(ZDOVars.s_rudder, m_rudderValue);
      return;
    }

    if (Time.time - m_sendRudderTime > 1f)
    {
      vehicleSpeed = (Ship.Speed)NetView.GetZDO().GetInt(ZDOVars.s_forward);
      m_rudderValue = NetView.GetZDO().GetFloat(ZDOVars.s_rudder);
    }
  }

  private void SetTargetHeight(float val)
  {
    switch (ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      case false:
        ShipInstance?.NetView.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, 0f);
        break;
      case true:
        ShipInstance?.NetView.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, val);
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
    if (NetView && !_hasRegister)
    {
      RegisterRPCListeners();
      _hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    // ship speed
    NetView.Unregister(nameof(RPC_SpeedChange));

    // anchor logic
    NetView.Unregister(nameof(RPC_SetAnchor));

    // rudder direction
    NetView.Unregister(nameof(RPC_Rudder));

    // boat sway
    NetView.Unregister(nameof(RPC_SetOceanSway));

    // steering
    NetView.Unregister(nameof(RPC_RequestControl));
    NetView.Unregister(nameof(RPC_RequestResponse));
    NetView.Unregister(nameof(RPC_ReleaseControl));

    _hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    // ship speed
    NetView.Register<int>(nameof(RPC_SpeedChange), RPC_SpeedChange);

    // anchor logic
    NetView.Register<bool>(nameof(RPC_SetAnchor), RPC_SetAnchor);

    // rudder direction
    NetView.Register<float>(nameof(RPC_Rudder), RPC_Rudder);

    // boat sway
    NetView.Register<bool>(nameof(RPC_SetOceanSway), RPC_SetOceanSway);

    // steering
    NetView.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    NetView.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    NetView.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);

    _hasRegister = true;
  }


  /**
   * Will not be supported in v3.x.x
   */
  public void DEPRECATED_InitializeRudderWithShip(SteeringWheelComponent steeringWheel, Ship ship)
  {
    NetView = ship.m_nview;
    ship.m_controlGuiPos = steeringWheel.transform;
    var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint;
    }

    InitializeRPC();
  }

  public void InitializeWheelWithShip(IVehicleShip vehicleShip,
    SteeringWheelComponent steeringWheel)
  {
    ShipInstance = vehicleShip;
    ShipInstance.Instance.m_controlGuiPos = transform;

    var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }

    NetView = GetComponent<ZNetView>();
  }

  private void OnDestroy()
  {
    if (_hasRegister)
    {
      UnRegisterRPCListeners();
    }

    CancelInvoke(nameof(SyncTargetHeight));
  }

  public void FireRequestControl(long playerId, Transform attachTransform)
  {
    NetView.InvokeRPC(nameof(RPC_RequestControl), [playerId, attachTransform]);
  }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var isOwner = NetView.IsOwner();
    var isInBoat = ShipInstance.Instance.IsPlayerInBoat(playerID);
    if (!isOwner || !isInBoat) return;

    var isValidUser = false;
    if (GetUser() == playerID || !HaveValidUser())
    {
      NetView.GetZDO().Set(ZDOVars.s_user, playerID);
      isValidUser = true;
    }

    NetView.InvokeRPC(sender, nameof(RPC_RequestResponse), isValidUser);
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (NetView.IsOwner() && GetUser() == playerID)
    {
      NetView.GetZDO().Set(ZDOVars.s_user, 0L);
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
      if (!ShipInstance.FloatCollider)
      {
        return;
      }

      TargetHeight = Mathf.Clamp(
        ShipInstance.FloatCollider.transform.position.y - _maxVerticalOffset,
        ZoneSystem.instance.m_waterLevel, 200f);

      if (ShipInstance.FloatCollider.transform.position.y - _maxVerticalOffset <=
          ZoneSystem.instance.m_waterLevel)
      {
        TargetHeight = 0f;
      }
    }

    NetView.GetZDO().Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
    ShipInstance.Instance.ToggleShipEffects();
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
      if (!ShipInstance.FloatCollider)
      {
        return;
      }

      TargetHeight = Mathf.Clamp(
        ShipInstance.FloatCollider.transform.position.y + _maxVerticalOffset,
        ZoneSystem.instance.m_waterLevel, 200f);
    }

    NetView.GetZDO().Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
    ShipInstance.Instance.ToggleShipEffects();
  }

  public void AutoAscendUpdate()
  {
    if (!_isAscending || _isHoldingAscend || _isHoldingDescend) return;
    TargetHeight = Mathf.Clamp(ShipInstance.FloatCollider.transform.position.y + _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    NetView.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
  }

  public void AutoDescendUpdate()
  {
    if (!_isDescending || _isHoldingDescend || _isHoldingAscend) return;
    TargetHeight = Mathf.Clamp(ShipInstance.FloatCollider.transform.position.y - _maxVerticalOffset,
      ZoneSystem.instance.m_waterLevel, 200f);
    NetView.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);
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
    var flag = ShipInstance?.Instance?.HaveControllingPlayer() ?? false;
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
    var hasControllingPlayer = ShipInstance?.Instance?.HaveControllingPlayer() ?? false;
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
    NetView.InvokeRPC(nameof(RPC_Rudder), rudder);
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
    NetView.InvokeRPC(0, nameof(RPC_SetAnchor), state);
    SendSpeedChange(DirectionChange.Stop);
  }

  public void SendToggleOceanSway()
  {
    NetView.InvokeRPC(0, nameof(RPC_SetOceanSway), !HasOceanSwayDisabled);
  }

  public void RPC_SetOceanSway(long sender, bool state)
  {
    var isOwner = NetView.IsOwner();
    if (isOwner)
    {
      var zdo = NetView.GetZDO();
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
    if (!NetView)
    {
      MovementFlags = VehicleMovementFlags.None;
      return;
    }

    var zdo = NetView.GetZDO();
    if (zdo == null) return;

    var isEnabled = zdo.GetBool(VehicleZdoVars.VehicleOceanSway, false);
    HasOceanSwayDisabled = isEnabled;
  }

  private void SyncAnchor()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!NetView)
    {
      MovementFlags = VehicleMovementFlags.None;
      return;
    }

    if (NetView?.isActiveAndEnabled != true) return;

    var newFlags =
      (VehicleMovementFlags)NetView.GetZDO()
        .GetInt(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
    MovementFlags = newFlags;
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    var isOwner = NetView.IsOwner();
    MovementFlags = HandleSetAnchor(state);
    if (isOwner)
    {
      var zdo = NetView.GetZDO();
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

    NetView.InvokeRPC(0, nameof(RPC_SpeedChange), (int)vehicleSpeed);
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
    if (!NetView.IsValid()) return;
    NetView.InvokeRPC(nameof(RPC_ReleaseControl), player.GetPlayerID());
    if (AttachPoint != null)
    {
      player.AttachStop();
    }
  }

  public bool HaveValidUser()
  {
    var user = GetUser();
    if (!ShipInstance?.Instance) return false;
    return user != 0L && ShipInstance.Instance.IsPlayerInBoat(user);
  }

  private long GetUser()
  {
    return !NetView.IsValid() ? 0L : NetView.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }
}