// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.Integrations.Interfaces
{
  public interface INetworkedZDOConfig<TComponent>
  {
    void Load(ZDO zdo, TComponent component);
    void Save(ZDO zdo, TComponent component);
  }
}