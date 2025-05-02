namespace ValheimVehicles.Interfaces;

public interface ISerializableConfig<T>
{
  public void Save(ZDO zdo, T config);
  public T Load(ZDO zdo);
  public void Serialize(ZPackage package);
  public T Deserialize(ZPackage package);
}