// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public class PowerNetworkSimData
  {
    public readonly List<(PowerSourceData source, ZDO zdo)> Sources = new();
    public readonly List<(PowerStorageData storage, ZDO zdo)> Storages = new();
    public readonly List<(PowerConsumerData consumer, ZDO zdo)> Consumers = new();
    public readonly List<(PowerConduitData conduit, ZDO zdo)> Conduits = new();
    public readonly List<(PowerPylonData pylon, ZDO zdo)> Pylons = new();
  }
}