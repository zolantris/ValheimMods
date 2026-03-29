namespace ValheimVehicles.Interfaces;

public interface INetView
{
  ZNetView? m_nview { get; set; }
  ZDO? m_zdo { get; set; }
}