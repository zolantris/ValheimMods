namespace ValheimVehicles.Interfaces;

public interface IPowerSource
{
  float GetFuelLevel();
  float GetFuelCapacity();
  bool IsRunning { get; }
  void Refuel(float amount);
}