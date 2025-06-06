using System.Collections.Generic;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
using ValheimVehicles.RPC;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

public partial class PowerConduitData : IPowerComputeZdoSync
{
  // for integration
  public PowerConduitData(ZDO zdo) : base(zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;
    Mode = GetVariant(PrefabHash);
    Load();
  }

  /// <summary>
  /// Todo might have to abstract this to a peer system if the player is accessible via server and just send the increment from the player.
  /// </summary>
  public class PlayerEitrDataBridge : PlayerEitrData
  {
    public PlayerEitrDataBridge(long playerId, float eitr, float eitrCapacity, PowerConduitData conduitData) : base(playerId, eitr, eitrCapacity, conduitData)
    {
      Request_UseEitr = PlayerEitrRPC.Request_UseEitr;
      Request_AddEitr = PlayerEitrRPC.Request_AddEitr;
    }
  }

  public void SanitizePlayerDataDictionary()
  {
    var keysToRemove = new List<long>();
    foreach (var playerEitrData in PlayerPeerToData)
    {
      if (PlayerPeerToData.ContainsKey(playerEitrData.Key))
      {
        keysToRemove.Add(playerEitrData.Key);
      }
    }
    keysToRemove.ForEach(x => PlayerPeerToData.Remove(x));
  }

  public void ClearData()
  {
    PlayerPeerToData.Clear();
  }

  public bool AddOrUpdate(long playerId, float currentEitr, float maxEitr)
  {
    SanitizePlayerDataDictionary();

    var playerData = new PlayerEitrDataBridge(playerId, currentEitr, maxEitr, this);

    if (PlayerPeerToData.ContainsKey(playerId))
    {
      PlayerPeerToData[playerData.PlayerId] = playerData;
    }
    else
    {
      PlayerPeerToData.Add(playerData.PlayerId, playerData);
    }

    return true;
  }

  public void RemovePlayer(long playerPeerId)
  {
    if (!PlayerPeerToData.Remove(playerPeerId))
    {
      SanitizePlayerDataDictionary();
    }
  }

  /// <summary>
  /// Main simulation method for Drain Mode.
  /// </summary>
  public float DrainSimulate(float requestedEnergy)
  {
    TryRemoveEitrFromPlayers(requestedEnergy);

    return 0f;
  }

  /// <summary>
  /// Main simulation method for Charge Mode. Not ready.
  /// TODO implement this. 
  /// </summary>
  public float ChargeSimulate(float availableEnergy)
  {
    return 0f;
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();
    });
  }

  public static float GetAverageEitr(List<PlayerEitrData> playersWithinZone, Dictionary<long, PlayerEitrData> playerDataById)
  {
    var total = 0f;
    var count = 0;

    foreach (var player in playersWithinZone)
    {
      if (player.Eitr < 0.5f)
      {
        playerDataById.Remove(player.PlayerId);
        continue;
      }
      total += player.Eitr;
      count++;
    }

    return count > 0 ? total / count : 0f;
  }

  public bool HasPlayersWithEitr => GetAllPlayerEitr() > 1f;
}