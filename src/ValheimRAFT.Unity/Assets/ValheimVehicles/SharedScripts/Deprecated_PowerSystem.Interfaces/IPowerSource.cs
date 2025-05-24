// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public interface IPowerSource : IPowerNode
  {
    float GetFuelLevel();
    float GetFuelCapacity();
    bool IsRunning { get; }

    void AddFuel(float amount);
    float RequestAvailablePower(float deltaTime, float totalDemand, bool isDemanding);
    void CommitEnergyUsed(float energyUsed);
    void SetRunning(bool val);
    void SetFuelCapacity(float val);
    void SetFuelConsumptionRate(float val);
    void UpdateFuelEfficiency();
  }
}