using UnityEngine;
using ValheimVehicles.Vehicles;

namespace Components;

/// <summary>
/// Workaround Compatibility class for older ships. May be removed
/// </summary>
/// <param name="ship"></param>
public class CompatVehicleShip(Ship ship) : IVehicleShip
{
  public bool IsPlayerInBoat(ZDOID zdoId)
  {
    throw new System.NotImplementedException();
  }

  public bool IsPlayerInBoat(Player zdoId)
  {
    throw new System.NotImplementedException();
  }

  public bool IsPlayerInBoat(long playerID)
  {
    throw new System.NotImplementedException();
  }

  public GameObject RudderObject
  {
    get => ship.m_rudderObject;
    set { }
  }

  public IWaterVehicleController VehicleController { get; }

  public BoxCollider FloatCollider
  {
    get => ship.m_floatCollider;
    set { }
  }

  public Transform? ShipDirection { get; }

  public Transform ControlGuiPosition
  {
    get => ship.m_controlGuiPos;
    set { }
  }

  public VehicleShip Instance { get; }

  public Ship DeprecatedShipInstance => ship;
}