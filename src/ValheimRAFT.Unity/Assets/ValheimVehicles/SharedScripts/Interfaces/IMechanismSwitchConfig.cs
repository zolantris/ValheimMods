// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.Interfaces
{
  public interface IMechanismSwitchConfig
  {
    MechanismAction SelectedAction { get; set; }
    SwivelComponent? TargetSwivel { get; set; }
    int TargetSwivelId { get; set; }
  }
}