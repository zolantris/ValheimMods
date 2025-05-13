// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem.Interfaces
{
  public interface IPowerConsumer : IPowerNode
  {
    bool IsDemanding { get; }
    float RequestedPower(float deltaTime);
    void ApplyPower(float joules, float deltaTime);
  }
}