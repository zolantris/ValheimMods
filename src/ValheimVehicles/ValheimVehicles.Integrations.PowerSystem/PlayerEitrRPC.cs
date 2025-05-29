using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations.PowerSystem;

public static class PlayerEitrRPC
{
  public static bool hasRegistered = false;


  // public static void Unregister()
  // {
  //   hasRegistered = false;
  // }

  private const string RPC_AddEitr_Name = "ValheimVehicles_PowerSystem_RPC_AddEitr";
  private const string RPC_UseEitr_Name = "ValheimVehicles_PowerSystem_RPC_UseEitr";

  public static void Register()
  {
    if (hasRegistered) return;
    ZRoutedRpc.instance.Register<ZPackage>(RPC_AddEitr_Name, RPC_AddEitr);
    ZRoutedRpc.instance.Register<ZPackage>(RPC_UseEitr_Name, RPC_UseEitr);
    hasRegistered = true;
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
    ZRoutedRpc.instance.InvokeRoutedRPC(playerId, RPC_AddEitr_Name, pkg);
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
    ZRoutedRpc.instance.InvokeRoutedRPC(playerPeerId, RPC_UseEitr_Name, pkg);
  }

  private static void RPC_UseEitr(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var amount = pkg.ReadSingle();
    if (!Player.m_localPlayer) return;
#if DEBUG
    LoggerProvider.LogDebug($"Added Eitr for player {Player.m_localPlayer.GetPlayerName()}");
#endif
    Player.m_localPlayer.UseEitr(amount);
  }

  private static void RPC_AddEitr(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var amount = pkg.ReadSingle();
    if (!Player.m_localPlayer) return;
#if DEBUG
    LoggerProvider.LogDebug($"Added Eitr for player {Player.m_localPlayer.GetPlayerName()}");
#endif
    Player.m_localPlayer.AddEitr(amount);
  }
}