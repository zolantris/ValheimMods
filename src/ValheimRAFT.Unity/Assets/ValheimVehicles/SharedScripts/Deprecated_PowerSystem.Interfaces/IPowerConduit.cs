// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem.Interfaces
{
  public interface IPowerConduit : IPowerNode
  {
    bool IsDemanding { get; }
    float RequestPower(float deltaTime); // Charging mode
    float SupplyPower(float deltaTime); // Draining mode
  }
}