namespace ValheimVehicles.Interfaces;

/// <summary>
/// T is the SerializeableConfig, TConfig is the interface of the config. This should be shared with Components so that a component can easily grab it's config and sync it.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TConfig"></typeparam>
public interface ISerializableConfig<T, TConfig>
{
  public void ApplyFrom(TConfig config);
  public void ApplyTo(TConfig config);
  public void Save(ZDO zdo, T config);
  public T Load(ZDO zdo, TConfig config);
  public void Serialize(ZPackage package);
  public T Deserialize(ZPackage package);
}