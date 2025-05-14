using ValheimVehicles.Integrations.Interfaces;

namespace ValheimVehicles.Integrations;

public class NoZDOConfig<T> : INetworkedZDOConfig<T>
{
  public void Save(ZDO zdo, T component) {}
  public void Load(ZDO zdo, T component) {}
}