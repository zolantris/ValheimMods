using BepInEx.Configuration;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.QuickStartWorld.Patches;
using Zolantris.Shared;

namespace ValheimVehicles.QuickStartWorld.Config;

/// <summary>
/// For configuring quickstart worlds meant for debug an BETA variants of valheimRAFT
/// TODO move this into its own mod. This is super helpful for starting and/or connecting to a world/server without any UI input.
/// 
/// The Config file is debug-only for now.
/// </summary>
public class QuickStartWorldConfig : BepInExBaseConfig<QuickStartWorldConfig>
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

  public override void OnBindConfig(ConfigFile config)
  {
#if DEBUG
    ServerOnlineBackendType = config.BindUnique(QuickStartSection, "ServerOnlineBackendType", OnlineBackendType.Steamworks, ConfigHelpers.CreateConfigDescription("For setting the server type."));

    QuickStartWorldName = config.BindUnique(QuickStartSection, "QuickStartWorldName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start World Name",
        false, false));

    // QuickStartProfileBackend
    QuickStartWorldPassword = config.BindUnique(QuickStartSection,
      "QuickStartWorldPassword",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start world password",
        false, false));

    IsOpenServer = config.BindUnique(QuickStartSection,
      "IsOpenServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set if hosted server is opened allowing other players to connect to the server.",
        false, false));

    IsPublicServer = config.BindUnique(QuickStartSection,
      "IsPublicServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set the hosted server is public and listed.",
        false, false));

    IsServer = config.BindUnique(QuickStartSection,
      "IsServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Set if server is public",
        false, false));

    IsJoinServer = config.BindUnique(QuickStartSection,
      "IsJoinServer",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Join a server instead of hosting a server automatically.",
        false, false));

    JoinServerUrl = config.BindUnique(QuickStartSection,
      "JoinServerUrl",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.",
        false, false));

    JoinServerPort = config.BindUnique(QuickStartSection,
      "JoinServerPort",
      2456,
      ConfigHelpers.CreateConfigDescription(
        "Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.",
        false, false));

    QuickStartEnabled = config.BindUnique(QuickStartSection,
      "QuickStartEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable Quick start",
        false, false));

    QuickStartWorldPlayerName = config.BindUnique(QuickStartSection,
      "QuickStartWorldPlayerName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Quick start player name. Must be valid to start the quick start",
        false, false));

    QuickStartEnabled.SettingChanged += (sender, args) => OnQuickStartEnabled();
#endif
  }
}