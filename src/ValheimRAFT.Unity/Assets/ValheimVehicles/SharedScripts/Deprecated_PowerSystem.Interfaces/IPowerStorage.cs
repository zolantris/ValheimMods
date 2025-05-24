// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Interfaces
{
  public interface IPowerStorage : IPowerNode
  {
    float ChargeLevel { get; }
    float Energy { get; }
    float CapacityRemaining { get; }
    float Charge(float amount);
    void CommitDischarge(float amount);
    float PeekDischarge(float amount);
    float Discharge(float amount);
    bool IsCharging { get; }
    void SetCapacity(float val);
    void SetActive(bool val);
  }
}