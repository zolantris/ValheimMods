using Sentry.Unity;

namespace SentryUnityWrapper;

public class Config
{
  private readonly SentryUnityOptions _sentryUnityWrapperConfigOptions = new()
  {
    Debug = false
  };

  public SentryUnityOptions GetSentryUnityOptions()
  {
    return _sentryUnityWrapperConfigOptions;
  }

  public string PluginName;
  public string PluginGuid;
  public string PluginVersion;
  public string GameName;

  public Config(string pluginGuid, string pluginName,
    string pluginVersion,
    string sentryDsn,
    bool enabled = true,
    string gameName = "Valheim")
  {
    PluginVersion = pluginVersion;
    GameName = gameName;
    PluginGuid = pluginGuid;
    PluginName = pluginName;
    _sentryUnityWrapperConfigOptions.Enabled = enabled;
    _sentryUnityWrapperConfigOptions.Dsn = sentryDsn;
  }
}