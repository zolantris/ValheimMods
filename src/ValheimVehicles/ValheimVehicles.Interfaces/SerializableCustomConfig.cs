using System.Collections.Generic;
namespace ValheimVehicles.Interfaces;

/// <summary>
/// T is the SerializeableConfig, TConfig is the interface of the config. This should be shared with Components so that a component can easily grab it's config and sync it.
/// </summary>
public interface ISerializableConfig<T, TComponent>
{
  void ApplyFrom(TComponent component);
  void ApplyTo(TComponent component);
  T Load(ZDO zdo, TComponent component);
  T LoadByKeys(ZDO zdo, List<string> keys);
  T LoadByKey(string key);
  void Save(ZDO zdo, T config);
  void Serialize(ZPackage pkg);
  T Deserialize(ZPackage pkg);

  /// <summary>
  /// Returns a deterministic hash for comparing config equality.
  /// Should be stable across machines.
  /// </summary>
  int GetStableHashCode();
}