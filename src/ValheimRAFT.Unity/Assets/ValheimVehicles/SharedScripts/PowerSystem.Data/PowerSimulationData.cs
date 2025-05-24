// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public class PowerSimulationData
  {
    public readonly List<PowerSourceData> Sources = new();
    public readonly List<PowerStorageData> Storages = new();
    public readonly List<PowerConsumerData> Consumers = new();
    public readonly List<PowerConduitData> Conduits = new();
    public readonly List<PowerPylonData> Pylons = new();

    // Add contextual simulation state here
    public float DeltaTime { get; set; }
    public string NetworkId { get; set; } = string.Empty;
  }
}