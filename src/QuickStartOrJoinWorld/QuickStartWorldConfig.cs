using BepInEx.Configuration;
using QuickStartOrJoinWorld.Patches;

namespace QuickStartOrJoinWorld.Config;

/// <summary>
/// For configuring quickstart worlds meant for debug an BETA variants
/// This mod is super helpful for starting and/or connecting to a world/server without any UI input.
/// 
/// The Config file is debug-only for now.
/// </summary>
public class QuickStartWorldConfig
{
  private const string QuickStartSection = "QuickStartWorld";
#if DEBUG
  public static ConfigEntry<string> QuickStartWorldName { get; private set; } =
    null!;

  public static ConfigEntry<bool> QuickStartEnabled { get; private set; } =
    null!;

  public static ConfigEntry<string>
    QuickStartWorldPassword { get; private set; } =
    null!;


  public static ConfigEntry<string>
    QuickStartWorldPlayerName { get; private set; } = null!;

  public static ConfigEntry<string>
    JoinServerUrl { get; private set; } =
    null!;

  public static ConfigEntry<OnlineBackendType>
    ServerOnlineBackendType { get; private set; } =
    null!;


  public static ConfigEntry<int>
    JoinServerPort { get; private set; } =
    null!;

  public static ConfigEntry<bool>
    IsPublicServer { get; private set; } =
    null!;

  public static ConfigEntry<bool>
    IsServer { get; private set; } =
    null!;

  public static ConfigEntry<bool>
    IsJoinServer { get; private set; } =
    null!;


  public static ConfigEntry<bool>
    IsOpenServer { get; private set; } =
    null!;

#endif

#if DEBUG
  public static void OnQuickStartEnabled()
  {
    if (FejdStartup.instance == null) return;

    // these cannot be valid otherwise it could trigger while playing the main game.
    if (ZNet.instance != null) return;
    if (ZNetScene.instance != null) return;

    if (QuickStartEnabled.Value)
    {
      QuickStartWorld_Patch.DirectPlayExtended(FejdStartup.instance);
    }
  }
#endif

  public static void BindConfig(ConfigFile config, object? configSync)
  {
#if DEBUG
    ServerOnlineBackendType = config.Bind(QuickStartSection, "ServerOnlineBackendType", OnlineBackendType.Steamworks, new ConfigDescription("For setting the server type."));

    QuickStartWorldName = config.Bind(QuickStartSection, "QuickStartWorldName",
      "",
      new ConfigDescription("Set the quick start World Name"));

    QuickStartWorldPassword = config.Bind(QuickStartSection,
      "QuickStartWorldPassword",
      "",
      new ConfigDescription("Set the quick start world password"));

    IsOpenServer = config.Bind(QuickStartSection,
      "IsOpenServer",
      false,
      new ConfigDescription("Set if hosted server is opened allowing other players to connect to the server."));

    IsPublicServer = config.Bind(QuickStartSection,
      "IsPublicServer",
      false,
      new ConfigDescription("Set the hosted server is public and listed."));

    IsServer = config.Bind(QuickStartSection,
      "IsServer",
      false,
      new ConfigDescription("Set if server is public"));

    IsJoinServer = config.Bind(QuickStartSection,
      "IsJoinServer",
      false,
      new ConfigDescription("Join a server instead of hosting a server automatically."));

    JoinServerUrl = config.Bind(QuickStartSection,
      "JoinServerUrl",
      "",
      new ConfigDescription("Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url."));

    JoinServerPort = config.Bind(QuickStartSection,
      "JoinServerPort",
      2456,
      new ConfigDescription("Set the join server port."));

    QuickStartEnabled = config.Bind(QuickStartSection,
      "QuickStartEnabled",
      false,
      new ConfigDescription("Enable Quick start"));

    QuickStartWorldPlayerName = config.Bind(QuickStartSection,
      "QuickStartWorldPlayerName",
      "",
      new ConfigDescription("Quick start player name. Must be valid to start the quick start"));

    QuickStartEnabled.SettingChanged += (sender, args) => OnQuickStartEnabled();
#endif
  }
}

