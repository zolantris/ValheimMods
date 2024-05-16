using System;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Vehicles;

/// <summary>
/// Compatibility Class made to remap a ship to VehicleShip and provide the same methods so VehicleShip requires minimal code changes
/// </summary>
public class VehicleShipCompat : IVehicleShip
{
  public Ship? ShipInstance;
  public VehicleShip? VehicleShipInstance;
  private bool _isValheimShip;
  private bool _isVehicleShip;

  public bool IsValheimShip => ShipInstance && _isValheimShip;
  public bool IsVehicleShip => VehicleShipInstance && _isVehicleShip;

  public bool IsMbRaft =>
    ShipInstance != null &&
    ShipInstance.gameObject.name.Contains(PrefabNames.MBRaft);

  public static VehicleShipCompat? InitFromUnknown(object? vehicleOrShip)
  {
    if (vehicleOrShip?.GetType() == typeof(VehicleShipCompat))
    {
      return vehicleOrShip as VehicleShipCompat;
    }

    var vehicleShip = vehicleOrShip as VehicleShip;
    if (vehicleShip != null)
    {
      return InitWithVehicleShip(vehicleShip);
    }

    var ship = vehicleOrShip as Ship;
    if (ship != null)
    {
      return InitWithShip(ship);
    }

    return null;
  }

  public bool IsOwner()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.IsOwner();
    }

    if (IsValheimShip)
    {
      return ShipInstance.IsOwner();
    }

    return false;
  }

  private static VehicleShipCompat InitWithVehicleShip(VehicleShip vehicleShip)
  {
    return new VehicleShipCompat()
    {
      VehicleShipInstance = vehicleShip,
      _isVehicleShip = true,
      _isValheimShip = false,
      m_controlGuiPos = vehicleShip.m_controlGuiPos,
    };
  }

  private static VehicleShipCompat InitWithShip(Ship ship)
  {
    return new VehicleShipCompat()
    {
      ShipInstance = ship,
      _isVehicleShip = false,
      _isValheimShip = true,
      m_controlGuiPos = ship.m_controlGuiPos,
    };
  }

  public bool IsPlayerInBoat(ZDOID zdoId)
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.IsPlayerInBoat(zdoId);
    }

    if (IsValheimShip)
    {
      return ShipInstance.IsPlayerInBoat(zdoId);
    }

    return false;
  }

  public bool IsPlayerInBoat(Player zdoId)
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.IsPlayerInBoat(zdoId);
    }

    if (IsValheimShip)
    {
      return ShipInstance.IsPlayerInBoat(zdoId);
    }

    return false;
  }

  public bool IsPlayerInBoat(long playerID)
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.IsPlayerInBoat(playerID);
    }

    if (IsValheimShip)
    {
      return ShipInstance.IsPlayerInBoat(playerID);
    }

    return false;
  }

  public float GetWindAngle()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetWindAngle();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetWindAngle();
    }

    return 0f;
  }

  public float GetWindAngleFactor()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetWindAngleFactor();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetWindAngleFactor();
    }

    return 0f;
  }

  public Ship.Speed GetSpeedSetting()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetSpeedSetting();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetSpeedSetting();
    }

    return Ship.Speed.Stop;
  }

  public float GetRudder()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetRudder();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetRudder();
    }

    return 0f;
  }

  public float GetRudderValue()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetRudderValue();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetRudderValue();
    }

    return 0f;
  }

  public float GetShipYawAngle()
  {
    if (IsVehicleShip)
    {
      return VehicleShipInstance.GetShipYawAngle();
    }

    if (IsValheimShip)
    {
      return ShipInstance.GetShipYawAngle();
    }

    return 0f;
  }

  public GameObject RudderObject { get; set; }
  public IWaterVehicleController VehicleController { get; }
  public BoxCollider FloatCollider { get; set; }
  public Transform? ShipDirection { get; }
  public Transform ControlGuiPosition { get; set; }
  public Transform m_controlGuiPos { get; set; }

  public VehicleShip? Instance
  {
    get => VehicleShipInstance;
  }
}