// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public interface IPowerSource : IPowerNode
  {
    float GetFuelLevel();
    float GetFuelCapacity();
    bool IsRunning { get; }

    void AddFuel(float amount);
    float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding);
    void SetRunning(bool val);
    void SetFuelCapacity(float val);
    void SetFuelConsumptionRate(float val);
    void UpdateFuelEfficiency();
  }
}