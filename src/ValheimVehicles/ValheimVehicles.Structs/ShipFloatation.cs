using UnityEngine;

namespace ValheimVehicles.Vehicles.Structs;

public struct ShipFloatation
{
  public Vector3 ShipBack;
  public Vector3 ShipForward;
  public Vector3 ShipLeft;
  public Vector3 ShipRight;
  public bool IsAboveBuoyantLevel;
  public float BuoyancySpeedMultiplier;
  public bool IsInvalid; // for floating values that are in extreme negatives.
  public float CurrentDepth; // a positive number for below water. A negative number above the water
  public float WaterLevelLeft;
  public float WaterLevelRight;
  public float WaterLevelForward;
  public float WaterLevelBack;
  public float LowestWaterHeight;
  public float GroundLevelLeft;
  public float GroundLevelRight;
  public float GroundLevelForward;
  public float GroundLevelBack;
  public float GroundLevelCenter;
  // averages
  public float AverageWaterHeight;
  public float AverageGroundLevel;
  // max values
  public float MaxWaterHeight;
  public float MaxGroundLevel;
}