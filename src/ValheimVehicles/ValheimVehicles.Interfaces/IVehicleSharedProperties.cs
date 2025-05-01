namespace ValheimVehicles.Interfaces;

public interface IVehicleSharedProperties : IVehicleControllers
{
  ZNetView? NetView { get; set; }
}