// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.ZDOConfigs
{
  public interface INetworkedZDOConfig<TComponent>
  {
    void Load(ZDO zdo, TComponent component);
    void Save(ZDO zdo, TComponent component);
  }
}