using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Interfaces;

public interface IMechanismActionSetter
{
  public void SetMechanismAction(MechanismAction action);
  public MechanismAction SelectedAction { get; set; }
}