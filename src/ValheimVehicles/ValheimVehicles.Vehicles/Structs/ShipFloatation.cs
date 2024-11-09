using UnityEngine;

namespace ValheimVehicles.Vehicles.Structs;

public struct ShipFloatation
{
  public Vector3 ShipBack;
  public Vector3 ShipForward;
  public Vector3 ShipLeft;
  public Vector3 ShipRight;
  public bool IsAboveBuoyantLevel;
  public float CurrentDepth;
  public float WaterLevelLeft;
  public float WaterLevelRight;
  public float WaterLevelForward;
  public float WaterLevelBack;
  public float AverageWaterHeight;
  public float GroundLevelLeft;
  public float GroundLevelRight;
  public float GroundLevelForward;
  public float GroundLevelBack;
  public float GroundLevelCenter;
}