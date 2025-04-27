using BepInEx.Configuration;
using ComfyLib;

using ValheimVehicles.Config;

namespace ValheimVehicles.ModSupport;

/// <summary>
/// Whole file is debug only related.
/// </summary>
public static class QuickStartWorldConfig
{
  public static ConfigFile? Config { get; private set; }

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
    if (ZNet.instance.IsServer()) return;
    if (ZNet.instance.IsDedicated()) return;
    if (ZNetScene.instance != null) return;

    if (QuickStartEnabled.Value)
    {
      QuickStartWorld_Patch.DirectPlayExtended(FejdStartup.instance);
    }
  }
#endif

  public static void BindConfig(ConfigFile config)
  {
#if DEBUG
    Config = config;

    ServerOnlineBackendType = config.Bind(QuickStartSection, "ServerOnlineBackendType", OnlineBackendType.Steamworks);

    QuickStartWorldName = config.Bind(QuickStartSection, "QuickStartWorldName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start World Name",
        false, false));

    // QuickStartProfileBackend
    QuickStartWorldPassword = config.Bind(QuickStartSection,
      "QuickStartWorldPassword",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start world password",
        false, false));

    IsOpenServer = config.Bind(QuickStartSection,
      "IsOpenServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set if hosted server is opened allowing other players to connect to the server.",
        false, false));

    IsPublicServer = config.Bind(QuickStartSection,
      "IsPublicServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set the hosted server is public and listed.",
        false, false));

    IsServer = config.Bind(QuickStartSection,
      "IsServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set if server is public",
        false, false));

    IsJoinServer = config.Bind(QuickStartSection,
      "IsJoinServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Join a server instead of hosting a server automatically.",
        false, false));

    JoinServerUrl = config.Bind(QuickStartSection,
      "JoinServerUrl",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.",
        false, false));

    JoinServerPort = config.Bind(QuickStartSection,
      "JoinServerPort",
      2456,
      ConfigHelpers.CreateConfigDescription(
        "Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.",
        false, false));

    QuickStartEnabled = config.Bind(QuickStartSection,
      "QuickStartEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable Quick start",
        false, false));

    QuickStartWorldPlayerName = config.Bind(QuickStartSection,
      "QuickStartWorldPlayerName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Quick start player name. Must be valid to start the quick start",
        false, false));

    QuickStartEnabled.SettingChanged += (sender, args) => OnQuickStartEnabled();
#endif
  }
}