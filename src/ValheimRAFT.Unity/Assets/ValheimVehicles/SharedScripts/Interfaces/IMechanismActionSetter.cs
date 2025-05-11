// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Interfaces
{
  public interface IMechanismActionSetter
  {
    public MechanismAction SelectedAction { get; set; }
    public void SetMechanismAction(MechanismAction action);
  }
}