using BepInEx.Configuration;
using DirectWorld.Patches;

namespace DirectWorld.Config;

/// <summary>
/// Configuration for DirectWorld - rapid world startup and server joining without UI interaction.
/// Debug-only for now.
/// </summary>
public class DirectWorldConfig
{
  private const string DirectWorldSection = "DirectWorld";
#if DEBUG
  public static ConfigEntry<string> DirectWorldName { get; private set; } =
    null!;

  public static ConfigEntry<bool> DirectWorldEnabled { get; private set; } =
    null!;

  public static ConfigEntry<string>
    DirectWorldPassword { get; private set; } =
    null!;


  public static ConfigEntry<string>
    DirectWorldPlayerName { get; private set; } = null!;

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
  public static void OnDirectWorldEnabled()
  {
    if (FejdStartup.instance == null) return;

    if (ZNet.instance != null) return;
    if (ZNetScene.instance != null) return;

    if (DirectWorldEnabled.Value)
    {
      DirectWorld_Patch.DirectPlayExtended(FejdStartup.instance);
    }
  }
#endif

  public static void BindConfig(ConfigFile config, object? configSync)
  {
#if DEBUG
    ServerOnlineBackendType = config.Bind(DirectWorldSection, "ServerOnlineBackendType", OnlineBackendType.Steamworks, new ConfigDescription("For setting the server type."));

    DirectWorldName = config.Bind(DirectWorldSection, "DirectWorldName",
      "",
      new ConfigDescription("Name of the world to load"));

    DirectWorldPassword = config.Bind(DirectWorldSection,
      "DirectWorldPassword",
      "",
      new ConfigDescription("Password for the world"));

    IsOpenServer = config.Bind(DirectWorldSection,
      "IsOpenServer",
      false,
      new ConfigDescription("Allow other players to connect to the hosted world"));

    IsPublicServer = config.Bind(DirectWorldSection,
      "IsPublicServer",
      false,
      new ConfigDescription("List hosted world publicly"));

    IsServer = config.Bind(DirectWorldSection,
      "IsServer",
      false,
      new ConfigDescription("Set if server is public"));

    IsJoinServer = config.Bind(DirectWorldSection,
      "IsJoinServer",
      false,
      new ConfigDescription("Join a server instead of hosting"));

    JoinServerUrl = config.Bind(DirectWorldSection,
      "JoinServerUrl",
      "",
      new ConfigDescription("IP or URL of remote server"));

    JoinServerPort = config.Bind(DirectWorldSection,
      "JoinServerPort",
      2456,
      new ConfigDescription("Port of remote server"));

    DirectWorldEnabled = config.Bind(DirectWorldSection,
      "DirectWorldEnabled",
      false,
      new ConfigDescription("Enable DirectWorld auto-loading"));

    DirectWorldPlayerName = config.Bind(DirectWorldSection,
      "DirectWorldPlayerName",
      "",
      new ConfigDescription("Character/player name to use"));

    DirectWorldEnabled.SettingChanged += (sender, args) => OnDirectWorldEnabled();
#endif
  }
}
