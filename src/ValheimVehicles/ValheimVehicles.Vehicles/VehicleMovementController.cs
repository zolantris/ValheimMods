using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Enums;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : MonoBehaviour, IVehicleMovement
{
  public IVehicleShip ShipInstance { get; set; }
  public Vector3 detachOffset = new(0f, 0.5f, 0f);

  public VehicleMovementFlags MovementFlags { get; set; }

  // flying mechanics
  private bool _isAscending;
  private bool _isDescending;
  private bool _isHoldingDescend = false;
  private bool _isHoldingAscend = false;

  private const float InitialTargetHeight = 0f;
  public float TargetHeight { get; set; } = InitialTargetHeight;
  public Transform AttachPoint { get; set; }

  public const string m_attachAnimation = "Standing Torch Idle right";

  public SteeringWheelComponent lastUsedWheelComponent;

  public ZNetView NetView;
  private bool _hasRegister = false;


  // unfortunately the current approach does not allow increasing this beyond 1f otherwise it causes massive jitters when changing altitude.
  private float _maxVerticalOffset = 1f;
  public bool IsAnchored => MovementFlags.HasFlag(VehicleMovementFlags.IsAnchored);

  // public Quaternion VehicleRotation = Quaternion.Euler(Vector3.zero);
  //
  // public Vector3 Forward
  // {
  //   get => VehicleRotation * Vector3.forward;
  //   set => VehicleRotation = Quaternion.LookRotation(value);
  // }
  //
  // public Vector3 Back
  // {
  //   get => VehicleRotation * Vector3.back;
  //   set => VehicleRotation = Quaternion.LookRotation(value);
  // }
  //
  // public Vector3 Left
  // {
  //   get => VehicleRotation * Vector3.left;
  //   set => VehicleRotation = Quaternion.LookRotation(value);
  // }
  //
  // public Vector3 Right
  // {
  //   get => VehicleRotation * Vector3.right;
  //   set => VehicleRotation = Quaternion.LookRotation(value);
  // }


  private void Awake()
  {
    NetView = GetComponent<ZNetView>();
    if (!NetView) return;
    MovementFlags =
      (VehicleMovementFlags)NetView.GetZDO().GetInt(VehicleZdoVars.VehicleFlags,
        (int)MovementFlags);
    InitializeRPC();
  }

  private void Update()
  {
    OnControllingWithHotKeyPress();
    AutoVerticalFlightUpdate();
  }

  private void InitializeRPC()
  {
    if (NetView != null && _hasRegister)
    {
      UnRegisterRPCListeners();
    }

    if (NetView != null && !_hasRegister)
    {
      RegisterRPCListeners();
      _hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    NetView.Unregister(nameof(RPC_RequestControl));
    NetView.Unregister(nameof(RPC_RequestResponse));
    NetView.Unregister(nameof(RPC_ReleaseControl));
    _hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    NetView.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    NetView.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    NetView.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);
    NetView.Register(nameof(RPC_SetAnchor),
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
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

  public void InitializeRudderWithShip(IVehicleShip vehicleShip,
    SteeringWheelComponent steeringWheel)
  {
    ShipInstance = vehicleShip;
    ShipInstance.Instance.m_controlGuiPos = transform;

    var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }

    NetView = vehicleShip.Instance.m_nview;
  }

  private void OnDestroy()
  {
    if (!_hasRegister) return;
    UnRegisterRPCListeners();
  }

  public void FireRequestControl(long playerId, Transform attachTransform)
  {
    NetView.InvokeRPC(nameof(RPC_RequestControl), [playerId, attachTransform]);
  }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var attachTransform = lastUsedWheelComponent.AttachPoint;

    var isOwner = NetView.IsOwner();
    var isInBoat = ShipInstance.IsPlayerInBoat(playerID);
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

    var oldTargetHeight = TargetHeight;
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
      SetAnchor(false);
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
    var flag = ShipInstance.Instance.HaveControllingPlayer();
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
    if (!ShipInstance.Instance.HaveControllingPlayer()) return;
    OnAnchorKeyPress();
    OnFlightControls();
  }


  /*
   * Toggle the ship anchor and emit the event to other players so their client can update
   */
  public void ToggleAnchor()
  {
    SetAnchor(!IsAnchored);
  }


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

  /// <summary>
  /// Updates anchor locally and send the same value over network
  /// </summary>
  /// <param name="isEnabled"></param>
  public void SetAnchor(bool isEnabled)
  {
    if (isEnabled)
    {
      _isAscending = false;
      _isDescending = false;
    }

    // skips setting Flag if it already is set
    if (IsAnchored != isEnabled)
    {
      MovementFlags = isEnabled
        ? (MovementFlags & ~VehicleMovementFlags.IsAnchored)
        : (MovementFlags | VehicleMovementFlags.IsAnchored);
      NetView.m_zdo.Set(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
    }

    // always emits the setter to prevent desync
    SendSetAnchor(isEnabled);
  }

  public void SendSetAnchor(bool state)
  {
    NetView.InvokeRPC(nameof(RPC_SetAnchor), state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    MovementFlags = (state
      ? (MovementFlags | VehicleMovementFlags.IsAnchored)
      : (MovementFlags & ~VehicleMovementFlags.IsAnchored));
    NetView.GetZDO().Set(VehicleZdoVars.VehicleFlags, (int)MovementFlags);
  }

  private void RPC_RequestResponse(long sender, bool granted)
  {
    if (!Player.m_localPlayer)
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
    }
    else
    {
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
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
    return user != 0L && ShipInstance.IsPlayerInBoat(user);
  }

  private long GetUser()
  {
    return !NetView.IsValid() ? 0L : NetView.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }
}