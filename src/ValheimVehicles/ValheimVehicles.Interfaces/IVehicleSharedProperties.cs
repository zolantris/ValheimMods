namespace ValheimVehicles.Interfaces;

public interface IVehicleSharedProperties : IVehicleControllers
{
  ZNetView? m_nview { get; set; }
}