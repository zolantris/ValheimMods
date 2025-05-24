using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Integrations.PowerSystem;

public static class PlayerEitrRPC
{
  public static bool hasRegistered = false;
  public static void Register()
  {
    if (hasRegistered) return;
    ZRoutedRpc.instance.Register<float>(RPC_AddEitr_Name, RPC_AddEitr);
    hasRegistered = true;
  }
  private const string RPC_AddEitr_Name = nameof(RPC_AddEitr);

  public static void Request_AddEitr(long playerId, float amount)
  {
    var player = Player.GetPlayer(playerId);
    if (!player) return;
    Request_AddEitr(player, amount);
  }

  public static void Request_AddEitr(Player player, float amount)
  {
    if (!player || amount <= 0f)
    {
      LoggerProvider.LogWarning("Player is null or amount is <= 0");
      return;
    }

    var netView = player.m_nview;
    if (!netView || !netView.IsValid()) return;
    if (netView.IsOwner())
    {
      var zdoOwner = netView.GetZDO().GetOwner();
      netView.InvokeRPC(zdoOwner, RPC_AddEitr_Name, amount);
    }
    else
    {
      player.AddEitr(amount);
    }
  }

  private static void RPC_AddEitr(long sender, float amount)
  {
    if (!Player.m_localPlayer) return;
#if DEBUG
    LoggerProvider.LogDebug($"Added Eitr for player {Player.m_localPlayer.GetPlayerName()}");
#endif
    Player.m_localPlayer.AddEitr(amount);
  }
}