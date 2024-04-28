using UnityEngine;

namespace ValheimVehicles.Vehicles;

public interface IVehicleShip
{
  public bool IsPlayerInBoat(ZDOID zdoId);
  public bool IsPlayerInBoat(Player zdoId);
  public bool IsPlayerInBoat(long playerID);

  public GameObject RudderObject { get; set; }
  public IWaterVehicleController VehicleController { get; }
  public BoxCollider FloatCollider { get; set; }
  public Transform? ShipForwardRotation { get; }

  public Transform ControlGuiPosition { get; set; }
  public VehicleShip Instance { get; }
}