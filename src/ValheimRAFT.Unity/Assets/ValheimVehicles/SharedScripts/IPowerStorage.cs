// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Interfaces
{
  public interface IPowerStorage : IPowerNode
  {
    float ChargeLevel { get; }
    float Capacity { get; }
    float CapacityRemaining { get; }
    float Charge(float amount);
    float Discharge(float amount);
    bool IsCharging { get; }
  }
}