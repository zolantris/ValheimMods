// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public interface IPowerSource : IPowerNode
  {
    float GetFuelLevel();
    float GetFuelCapacity();
    bool IsRunning { get; }
    void Refuel(float amount);
    float RequestAvailablePower(float deltaTime, bool isNetworkDemanding);
  }
}