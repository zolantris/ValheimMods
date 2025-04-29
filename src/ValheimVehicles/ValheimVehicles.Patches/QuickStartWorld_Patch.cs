using System.Linq;
using HarmonyLib;
using ValheimVehicles.QuickStartWorld.Config;

namespace ValheimVehicles.QuickStartWorld.Patches;

public class QuickStartWorld_Patch
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
    if (!QuickStartWorldConfig.QuickStartEnabled.Value ||
        QuickStartWorldConfig.QuickStartWorldName.Value == "" ||
        QuickStartWorldConfig.QuickStartWorldPlayerName.Value == "") return;
    ConnectOrHostServer();
  }

  public static void ConnectOrHostServer()
  {
    if (FejdStartup.instance == null) return;
    var worldList = SaveSystem.GetWorldList();
    var playerProfiles = SaveSystem.GetAllPlayerProfiles();
    var world = worldList.FirstOrDefault((x) =>
      x.m_name == QuickStartWorldConfig.QuickStartWorldName.Value);
    var player = playerProfiles.FirstOrDefault((x) =>
      x.m_playerName == QuickStartWorldConfig.QuickStartWorldPlayerName.Value);

    if (world == null || player == null) return;

    ZSteamMatchmaking.instance.StopServerListing();
    // ZNet.m_onlineBackend = OnlineBackendType.Steamworks;
    ZNet.m_onlineBackend = QuickStartWorldConfig.ServerOnlineBackendType.Value;
    Game.SetProfile(player.m_filename, FileHelpers.FileSource.Local);

    // joins an already hosted server.
    if (QuickStartWorldConfig.IsJoinServer.Value)
    {
      ZNet.SetServerHost(QuickStartWorldConfig.JoinServerUrl.Value, QuickStartWorldConfig.JoinServerPort.Value, QuickStartWorldConfig.ServerOnlineBackendType.Value);
      FejdStartup.instance.LoadMainScene();
    }
    else
    {
      ZNet.SetServer(true, QuickStartWorldConfig.IsOpenServer.Value, QuickStartWorldConfig.IsPublicServer.Value,
        world.m_name, QuickStartWorldConfig.QuickStartWorldPassword.Value,
        world);
      FejdStartup.instance.LoadMainScene();
    }
  }
#endif
}