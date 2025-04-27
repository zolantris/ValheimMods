using UnityEngine;

namespace ValheimVehicles.Interfaces;

public interface IValheimShip
{
  public void UpdateRudder(float dt, bool haveControllingPlayer);
  public float GetWindAngle();
  public float GetWindAngleFactor();
  public Ship.Speed GetSpeedSetting();
  public float GetRudder();
  public float GetRudderValue();
  public float GetShipYawAngle();
  public bool IsPlayerInBoat(ZDOID zdoId);
  public bool IsPlayerInBoat(Player zdoId);
  public bool IsPlayerInBoat(long playerID);
  public Transform m_controlGuiPos { get; set; }
  public bool IsOwner();

  public void ApplyControls(Vector3 dir);

  // valheim has a misspelling
  public void ApplyControlls(Vector3 dir);
  public void Forward();
  public void Backward();
  public void Stop();
  public void UpdateControls(float dt);
  public void UpdateControlls(float dt);
}