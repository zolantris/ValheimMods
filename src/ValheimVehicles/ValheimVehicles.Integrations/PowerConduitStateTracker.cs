// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.Integrations.PowerSystem
{
  public static class PowerConduitStateTracker
  {
    private static readonly Dictionary<ZDOID, PowerConduitData> _conduits = new();

    public static bool TryGet(ZDOID id, out PowerConduitData data)
    {
      return _conduits.TryGetValue(id, out data);
    }

    public static void Set(ZDOID id, PowerConduitData data)
    {
      _conduits[id] = data;
    }

    public static void AddOrCreate(ZDOID id, bool isCharging)
    {
      if (!_conduits.ContainsKey(id))
      {
        _conduits[id] = new PowerConduitData();
      }
    }

    public static void Remove(ZDOID id)
    {
      _conduits.Remove(id);
    }

    public static IEnumerable<KeyValuePair<ZDOID, PowerConduitData>> All => _conduits;

    public static void Clear()
    {
      _conduits.Clear();
    }
  }
}