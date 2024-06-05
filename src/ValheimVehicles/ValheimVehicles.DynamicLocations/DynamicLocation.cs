using System;
using ValheimVehicles.Vehicles.Components;

public struct DynamicLocation
{
  // similar to vector2d. Will be serialized to vector
  public Tuple<int, int> zoneId;
  public Tuple<float, float, float> position;
  public PlayerSpawnController.LocationTypes locationType;
}