// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  public enum MechanismAction
  {
    CommandsHud,
    CreativeMode,
    ColliderEditMode,
    SwivelEditMode,
    SwivelActivateMode,
    None,
    VehicleDock, // docks/undocks vehicle from nearest vehicle
    FireCannonGroup // Allows for firing Cannons of a specific group. Defaults to group 0 until support is created for this.
  }
}