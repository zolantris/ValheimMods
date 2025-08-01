using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Compat;

/// <summary>
/// Compatibility Class made to remap a ship to VehicleShip and provide the same methods so VehicleShip requires minimal code changes
/// </summary>
public class VehicleControllersCompat : IVehicleControllers, IValheimShip
{
  public Ship? ShipInstance;
  public VehicleManager? VehicleShipInstance;
  private bool _isValheimShip;
  private bool _isVehicleShip;

  public ZNetView? m_nview => GetNetView();

  public ZNetView? GetNetView()
  {
    if (IsVehicleShip) return VehicleShipInstance.m_nview;

    if (IsValheimShip) return ShipInstance.m_nview;

    return null;
  }

  public bool IsValheimShip => ShipInstance && _isValheimShip;
  public bool IsVehicleShip => VehicleShipInstance && _isVehicleShip;

  public bool IsMbRaft =>
    ShipInstance != null &&
    ShipInstance.gameObject.name.Contains(PrefabNames.MBRaft);

  public static VehicleControllersCompat? InitFromUnknown(object? vehicleOrShip)
  {
    if (vehicleOrShip?.GetType() == typeof(VehicleControllersCompat))
      return vehicleOrShip as VehicleControllersCompat;

    var vehicleShip = vehicleOrShip as VehicleManager;
    if (vehicleShip != null) return InitWithVehicleShip(vehicleShip);

    var ship = vehicleOrShip as Ship;
    if (ship != null) return InitWithShip(ship);

    return null;
  }

  public bool IsOwner()
  {
    if (IsVehicleShip) return VehicleShipInstance.MovementController.IsOwner();

    if (IsValheimShip) return ShipInstance.IsOwner();

    return false;
  }

  public void ApplyControls(Vector3 dir)
  {
    ApplyControlls(dir);
  }

  public void ApplyControlls(Vector3 dir)
  {
    if (IsVehicleShip)
    {
      VehicleShipInstance?.MovementController.ApplyControls(dir);
      return;
    }

    if (IsValheimShip) ShipInstance?.ApplyControlls(dir);
  }

  public void Forward()
  {
    if (IsVehicleShip)
    {
      VehicleShipInstance?.MovementController.SendSpeedChange(
        VehicleMovementController
          .DirectionChange.Forward);
      return;
    }

    if (IsValheimShip) ShipInstance?.Forward();
  }

  public void Backward()
  {
    if (IsVehicleShip)
    {
      VehicleShipInstance?.MovementController.SendSpeedChange(
        VehicleMovementController
          .DirectionChange.Backward);
      return;
    }

    if (IsValheimShip) ShipInstance?.Backward();
  }

  public void Stop()
  {
    if (IsVehicleShip)
    {
      VehicleShipInstance?.MovementController.SendSpeedChange(
        VehicleMovementController
          .DirectionChange.Stop);
      return;
    }

    if (IsValheimShip) ShipInstance?.Stop();
  }

  public void UpdateControls(float dt)
  {
    UpdateControlls(dt);
  }

  public void UpdateControlls(float dt)
  {
    if (IsVehicleShip)
    {
      VehicleShipInstance?.MovementController.UpdateControls(dt);
      return;
    }

    if (IsValheimShip) ShipInstance?.UpdateControlls(dt);
  }

  private static VehicleControllersCompat InitWithVehicleShip(VehicleManager vehicleManager)
  {
    return new VehicleControllersCompat
    {
      VehicleShipInstance = vehicleManager,
      _isVehicleShip = true,
      _isValheimShip = false,
      m_controlGuiPos = vehicleManager.m_controlGuiPos
    };
  }

  private static VehicleControllersCompat InitWithShip(Ship ship)
  {
    return new VehicleControllersCompat
    {
      ShipInstance = ship,
      _isVehicleShip = false,
      _isValheimShip = true,
      m_controlGuiPos = ship.m_controlGuiPos
    };
  }

  public bool IsPlayerInBoat(ZDOID zdoId)
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.IsPlayerInBoat(zdoId);

    if (IsValheimShip) return ShipInstance.IsPlayerInBoat(zdoId);

    return false;
  }

  public bool IsPlayerInBoat(Player zdoId)
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.IsPlayerInBoat(zdoId);

    if (IsValheimShip) return ShipInstance.IsPlayerInBoat(zdoId);

    return false;
  }

  public bool IsPlayerInBoat(long playerID)
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.IsPlayerInBoat(playerID);

    if (IsValheimShip) return ShipInstance.IsPlayerInBoat(playerID);

    return false;
  }

  public void UpdateRudder(float dt, bool haveControllingPlayer)
  {
    Logger.LogError(
      "Called UpdateRudder but not implemented in vehicle compat");
  }

  public float GetWindAngle()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetWindAngle();

    if (IsValheimShip) return ShipInstance.GetWindAngle();

    return 0f;
  }

  public float GetWindAngleFactor()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetWindAngleFactor();

    if (IsValheimShip) return ShipInstance.GetWindAngleFactor();

    return 0f;
  }

  public Ship.Speed GetSpeedSetting()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetSpeedSetting();

    if (IsValheimShip) return ShipInstance.GetSpeedSetting();

    return Ship.Speed.Stop;
  }

  public float GetRudder()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetRudder();

    if (IsValheimShip) return ShipInstance.GetRudder();

    return 0f;
  }

  public float GetRudderValue()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetRudderValue();

    if (IsValheimShip) return ShipInstance.GetRudderValue();

    return 0f;
  }

  public float GetShipYawAngle()
  {
    if (IsVehicleShip)
      return VehicleShipInstance.MovementController.GetShipYawAngle();

    if (IsValheimShip) return ShipInstance.GetShipYawAngle();

    return 0f;
  }

  public GameObject RudderObject { get; set; }

  public Rigidbody? MovementControllerRigidbody { get; }
  public bool IsLandVehicle
  {
    get;
  }
  public BoxCollider FloatCollider { get; set; }
  public Transform? ShipDirection { get; }
  public Transform ControlGuiPosition { get; set; }
  public Transform m_controlGuiPos { get; set; }

  public ZNetView? NetView
  {
    get
    {
      if (IsVehicleShip) return VehicleShipInstance.m_nview;

      if (IsValheimShip) return ShipInstance.m_nview;

      return null;
    }
  }

  public int PersistentZdoId => 0;

#region IVehicleControllers

  public VehiclePiecesController? PiecesController
  {
    get;
    set;
  }
  public VehicleMovementController? MovementController
  {
    get;
    set;
  }
  public VehicleConfigSyncComponent? VehicleConfigSync
  {
    get;
    set;
  }
  public VehicleOnboardController? OnboardController
  {
    get;
    set;
  }
  public VehicleLandMovementController? LandMovementController
  {
    get;
    set;
  }
  public VehicleManager Manager
  {
    get;
    set;
  }

#endregion

}