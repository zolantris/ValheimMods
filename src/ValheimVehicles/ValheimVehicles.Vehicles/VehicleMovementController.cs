using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : MonoBehaviour
{
  public IVehicleShip ShipInstance { get; set; }
  public Vector3 detachOffset = new(0f, 0.5f, 0f);

  public Transform AttachPoint { get; set; }

  public const string m_attachAnimation = "Standing Torch Idle right";
  public RudderWheelComponent lastUsedWheelComponent;
  public ZNetView vehicleNetview;

  private bool hasRegister = false;

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
    vehicleNetview = GetComponent<ZNetView>();
    if (!vehicleNetview) return;
    InitializeRPC();
  }

  /**
   * Will not be supported in v3.x.x
   */
  public void DEPRECATED_InitializeRudderWithShip(IVehicleShip vehicleShip,
    RudderWheelComponent rudderWheel, Ship ship)
  {
    vehicleNetview = ship.m_nview;
    ship.m_controlGuiPos = rudderWheel.transform;
    var rudderAttachPoint = rudderWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint;
    }

    InitializeRPC();
  }

  private void InitializeRPC()
  {
    if (vehicleNetview != null && hasRegister)
    {
      UnRegisterRPCListeners();
    }

    if (vehicleNetview != null && !hasRegister)
    {
      RegisterRPCListeners();
      hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    vehicleNetview.Unregister(nameof(RPC_RequestControl));
    vehicleNetview.Unregister(nameof(RPC_RequestResponse));
    vehicleNetview.Unregister(nameof(RPC_ReleaseControl));
    hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    vehicleNetview.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    vehicleNetview.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    vehicleNetview.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);
    hasRegister = true;
  }

  public void InitializeRudderWithShip(IVehicleShip vehicleShip, RudderWheelComponent rudderWheel)
  {
    ShipInstance = vehicleShip;
    ShipInstance.Instance.m_controlGuiPos = transform;

    var rudderAttachPoint = rudderWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }

    vehicleNetview = vehicleShip.Instance.m_nview;
  }

  private void OnDestroy()
  {
    if (!hasRegister) return;
    UnRegisterRPCListeners();
  }

  // public bool IsValid()
  // {
  //   return this;
  // }
  //
  // public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  // {
  //   return false;
  // }

  // public bool Interact(Humanoid character, bool repeat, bool alt)
  // {
  //   if (character == Player.m_localPlayer && isActiveAndEnabled)
  //   {
  //     var baseVehicle = GetComponentInParent<BaseVehicleController>();
  //     if (baseVehicle != null)
  //     {
  //       baseVehicle.ComputeAllShipContainerItemWeight();
  //     }
  //     else
  //     {
  //       var baseRoot = GetComponentInParent<MoveableBaseRootComponent>();
  //       if (baseRoot != null)
  //       {
  //         baseRoot.ComputeAllShipContainerItemWeight();
  //       }
  //     }
  //
  //     lastUsedWheelComponent = this;
  //   }
  //
  //   if (repeat)
  //   {
  //     return false;
  //   }
  //
  //   if (vehicleNetview == null) return false;
  //
  //   if (!vehicleNetview.IsValid())
  //   {
  //     return false;
  //   }
  //
  //   if (!InUseDistance(character))
  //   {
  //     return false;
  //   }
  //
  //   var player = character as Player;
  //
  //
  //   var playerOnShipViaShipInstance =
  //     ShipInstance?.Instance?.GetComponentsInChildren<Player>() ?? null;
  //
  //   /*
  //    * <note /> This logic allows for the player to just look at the Raft and see if the player is a child within it.
  //    */
  //   if (playerOnShipViaShipInstance != null)
  //     foreach (var player1 in playerOnShipViaShipInstance)
  //     {
  //       Logger.LogDebug(
  //         $"Interact PlayerId {player1.GetPlayerID()}, currentPlayerId: {player.GetPlayerID()}");
  //       if (player1.GetPlayerID() != player.GetPlayerID()) continue;
  //       vehicleNetview.InvokeRPC(nameof(RPC_RequestControl), player.GetPlayerID());
  //       return true;
  //     }
  //
  //   if (player == null || player.IsEncumbered())
  //   {
  //     return false;
  //   }
  //
  //   var playerOnShip = player.GetStandingOnShip();
  //
  //   if (playerOnShip == null)
  //   {
  //     Logger.LogDebug("Player is not on Ship");
  //     return false;
  //   }
  //
  //   vehicleNetview.InvokeRPC(nameof(RPC_RequestControl), player.GetPlayerID());
  //   return false;
  // }

  public void FireRequestControl(long playerId, Transform attachTransform)
  {
    vehicleNetview.InvokeRPC(nameof(RPC_RequestControl), [playerId, attachTransform]);
  }

  // public Component GetControlledComponent()
  // {
  //   return ShipInstance?.Instance;
  // }

  // public Vector3 GetPosition()
  // {
  //   return base.transform.position;
  // }
  //
  // public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  // {
  //   ShipInstance?.Instance.ApplyControls(moveDir);
  // }
  //
  // public string GetHoverName()
  // {
  //   return Localization.instance.Localize(m_hoverText);
  // }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var attachTransform = lastUsedWheelComponent.AttachPoint;

    var isOwner = vehicleNetview.IsOwner();
    var isInBoat = ShipInstance.IsPlayerInBoat(playerID);
    if (!isOwner || !isInBoat) return;

    var isValidUser = false;
    if (GetUser() == playerID || !HaveValidUser())
    {
      vehicleNetview.GetZDO().Set(ZDOVars.s_user, playerID);
      isValidUser = true;
    }

    vehicleNetview.InvokeRPC(sender, nameof(RPC_RequestResponse), isValidUser);
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (vehicleNetview.IsOwner() && GetUser() == playerID)
    {
      vehicleNetview.GetZDO().Set(ZDOVars.s_user, 0L);
    }
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
    if (!vehicleNetview.IsValid()) return;
    vehicleNetview.InvokeRPC(nameof(RPC_ReleaseControl), player.GetPlayerID());
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
    return !vehicleNetview.IsValid() ? 0L : vehicleNetview.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }
}