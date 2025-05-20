// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  public abstract class PowerDataBase
  {
    public string NetworkId;
    public int PrefabHash;
  }

  public class PowerSourceData : PowerDataBase
  {
    public float Fuel;
    public float MaxFuel;
    public float OutputRate;
  }

  public class PowerStorageData : PowerDataBase
  {
    public float StoredEnergy;
    public float MaxCapacity;
  }

  public class PowerPylonData : PowerDataBase
  {
    public float Range;

    public PowerPylonData(string networkId, float range, int prefabHash)
    {
      NetworkId = networkId;
      Range = range;
      PrefabHash = prefabHash;
    }
  }

  /// <summary>
  /// Will not work for non-valheim
  /// </summary>
  public class PowerConduitData : PowerDataBase
  {
    public bool IsCharging;
    public readonly List<long> PlayerIds = new();
    public readonly List<Player> Players = new();

    public Player AddPlayer(long id)
    {
      var player = Player.GetPlayer(id);
      if (!PlayerIds.Contains(id)) PlayerIds.Add(id);
      if (!Players.Contains(player)) Players.Add(player);
      return player;
    }

    public void RemovePlayer(long id)
    {
      var player = Player.GetPlayer(id);
      if (!PlayerIds.Contains(id)) PlayerIds.Add(id);
      if (!Players.Contains(player)) Players.Add(player);
      Players.RemoveAll(p => !p || p.GetPlayerID() == id);
    }

    public void SanitizePlayers()
    {
      Players.RemoveAll(p => !p);
    }
  }

  public class PowerNetworkSimData
  {
    public readonly List<(PowerSourceData source, ZDO zdo)> Sources = new();
    public readonly List<(PowerStorageData storage, ZDO zdo)> Storages = new();
    public readonly List<(PowerConduitData conduit, ZDO zdo)> Conduits = new();
    public readonly List<(PowerPylonData pylon, ZDO zdo)> Pylons = new();
  }
}