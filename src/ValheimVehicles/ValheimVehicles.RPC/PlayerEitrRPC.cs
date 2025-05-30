using System.Collections;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.RPC;

public static class PlayerEitrRPC
{
  public static RPCEntity AddEitr_RPCInstance;
  public static RPCEntity UseEitr_RPCInstance;

  public static void RegisterAll()
  {
    AddEitr_RPCInstance = RPCManager.RegisterRPC(nameof(RPC_AddEitr), RPC_AddEitr);
    UseEitr_RPCInstance = RPCManager.RegisterRPC(nameof(RPC_UseEitr), RPC_UseEitr);
  }

  public static void Request_AddEitr(long playerId, float amount)
  {
    if (playerId == 0 || amount <= 0f)
    {
      LoggerProvider.LogWarning("Player is null or amount is <= 0");
      return;
    }
    if (Player.m_localPlayer && Player.m_localPlayer.GetPlayerID() == playerId)
    {
      // if the player is local player, we can just use the local player's eitr.
      Player.m_localPlayer.AddEitr(amount);
      return;
    }
    var pkg = new ZPackage();
    pkg.Write(amount);

    AddEitr_RPCInstance.Send(playerId, pkg);
  }

  public static void Request_UseEitr(long playerPeerId, float amount)
  {
    // skip invoking on self.
    if (Player.m_localPlayer && Player.m_localPlayer.GetOwner() == playerPeerId)
    {
      // if the player is local player, we can just use the local player's eitr.
      Player.m_localPlayer.UseEitr(amount);
      return;
    }

    var pkg = new ZPackage();
    pkg.Write(amount);

    UseEitr_RPCInstance.Send(playerPeerId, pkg);
  }

  private static IEnumerator RPC_UseEitr(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var amount = pkg.ReadSingle();
    if (!Player.m_localPlayer) yield break;
#if DEBUG
    LoggerProvider.LogDebug($"Used Eitr for player {Player.m_localPlayer.GetPlayerName()}");
#endif
    Player.m_localPlayer.UseEitr(amount);
  }

  private static IEnumerator RPC_AddEitr(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var amount = pkg.ReadSingle();
    if (!Player.m_localPlayer) yield break;
#if DEBUG
    LoggerProvider.LogDebug($"Added Eitr for player {Player.m_localPlayer.GetPlayerName()}");
#endif
    Player.m_localPlayer.AddEitr(amount);
  }
}