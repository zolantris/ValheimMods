using UnityEngine;

namespace ValheimVehicles.Vehicles;

public interface IVehicleShip
{
  public bool IsPlayerInBoat(ZDOID zdoId);
  public bool IsPlayerInBoat(Player zdoId);
  public bool IsPlayerInBoat(long playerID);

  public float GetWindAngle();
  public float GetWindAngleFactor();
  public Ship.Speed GetSpeedSetting();
  public float GetRudder();
  public float GetRudderValue();
  public float GetShipYawAngle();

  public GameObject RudderObject { get; set; }
  public IWaterVehicleController VehicleController { get; }
  public BoxCollider FloatCollider { get; set; }
  public Transform? ShipDirection { get; }

  public Transform ControlGuiPosition { get; set; }
  public Transform m_controlGuiPos { get; set; }
  public VehicleShip Instance { get; }
}