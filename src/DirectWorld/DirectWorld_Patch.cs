using System.Linq;
using HarmonyLib;
using DirectWorld.Config;

namespace DirectWorld.Patches;

public class DirectWorld_Patch
{
#if DEBUG

  /// <summary>
  /// Extends CookieMilkX's mod in debug only but adds support for selection via configuration menu 
  /// </summary>
  /// <originalMod>https://thunderstore.io/c/valheim/p/CookiexMilk/DirectPlay/</originalMod>
  /// <param name="__instance"></param>
  /// <returns></returns>
  [HarmonyPatch(typeof(FejdStartup), "Start")]
  [HarmonyPostfix]
  public static void DirectPlayExtended(FejdStartup __instance)
  {
    if (!DirectWorldConfig.DirectWorldEnabled.Value ||
        DirectWorldConfig.DirectWorldName.Value == "" ||
        DirectWorldConfig.DirectWorldPlayerName.Value == "") return;
    ConnectOrHostServer();
  }

  public static void ConnectOrHostServer()
  {
    if (FejdStartup.instance == null) return;
    var worldList = SaveSystem.GetWorldList();
    var playerProfiles = SaveSystem.GetAllPlayerProfiles();
    var world = worldList.FirstOrDefault((x) =>
      x.m_name == DirectWorldConfig.DirectWorldName.Value);
    var player = playerProfiles.FirstOrDefault((x) =>
      x.m_playerName == DirectWorldConfig.DirectWorldPlayerName.Value);

    if (world == null || player == null) return;

    ZSteamMatchmaking.instance.StopServerListing();
    ZNet.m_onlineBackend = DirectWorldConfig.ServerOnlineBackendType.Value;
    Game.SetProfile(player.m_filename, FileHelpers.FileSource.Local);

    if (DirectWorldConfig.IsJoinServer.Value)
    {
      ZNet.SetServerHost(DirectWorldConfig.JoinServerUrl.Value, DirectWorldConfig.JoinServerPort.Value, DirectWorldConfig.ServerOnlineBackendType.Value);
      FejdStartup.instance.LoadMainScene();
    }
    else
    {
      ZNet.SetServer(true, DirectWorldConfig.IsOpenServer.Value, DirectWorldConfig.IsPublicServer.Value,
        world.m_name, DirectWorldConfig.DirectWorldPassword.Value,
        world);
      FejdStartup.instance.LoadMainScene();
    }
  }
#endif
}
