using UnityEngine;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IVehicleShip
{
  public GameObject RudderObject { get; set; }
  public IWaterVehicleController VehicleController { get; }
  public BoxCollider FloatCollider { get; set; }
  public Transform? ShipDirection { get; }
  public Transform ControlGuiPosition { get; set; }
  public VehicleShip Instance { get; }
  public ZNetView NetView { get; }
}