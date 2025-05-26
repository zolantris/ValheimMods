// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.PowerSystem.Interfaces
{
  public interface INetworkedComponent
  {
    void UpdateNetworkedData();
    void SyncNetworkedData();
  }
}