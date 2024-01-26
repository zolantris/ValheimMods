using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Sentry;
using Sentry.Unity;
using UnityEngine;

namespace SentryUnityWrapper;

[BepInPlugin(BepInGuid, ModName, Version)]
public class SentryUnityWrapperPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.8.0";
  internal const string ModName = "SentryUnityWrapper";
  public const string BepInGuid = $"{Author}.{ModName}";
  private const string HarmonyGuid = $"{Author}.{ModName}";

  public const string ModDescription =
    "A Sentry.Unity wrapper for BepInEx mods. Sentry is logging service that supports scope friendly package logging, this interface aims to centralize Sentry setup and allow other mods to register logging with sentry through the supplied library";

  public const string CopyRight = "Copyright © 2024, GNU-v3 licensed";

  public static Dictionary<string, SentryClient> RegisteredPluginClients = new();
  public static Dictionary<string, Config> PendingPluginsToRegister = new();

  private bool _canAutoRegister = true;
  private float _autoRegisterTime = 10f;
  private static bool _hasCalledSentryInit = false;

  private void Awake()
  {
    StartAutoRegistry();
  }

  public void StartAutoRegistry()
  {
    StartCoroutine(InitializeSentryForRegisteredPlugins());
    StartCoroutine(StartCounting());
  }

  private IEnumerator StartCounting()
  {
    yield return new WaitForSeconds(_autoRegisterTime);
    _canAutoRegister = false;
  }

  private IEnumerator InitializeSentryForRegisteredPlugins()
  {
    while (_canAutoRegister)
    {
      yield return new WaitUntil(() => PendingPluginsToRegister.Count > 0);

      if (PendingPluginsToRegister.Count > 0)
      {
        RegisterPendingPlugins();
      }
    }
  }

  private static void RegisterPendingPlugins()
  {
    var pluginsToRegister = PendingPluginsToRegister.ToList();
    pluginsToRegister.ForEach((plugin) => { InitializeScopedLogging(plugin.Key, plugin.Value); });
  }

  public static Config CreateConfig(string pluginGuid, string modName,
    string pluginVersion,
    string sentryDsn,
    bool enabled = true,
    string gameName = "Valheim")
  {
    return new Config(pluginGuid, modName, pluginVersion, sentryDsn, enabled,
      gameName);
  }

  public static void RegisterPluginAsync(Config options)
  {
    PendingPluginsToRegister.Add(options.PluginGuid, options);
  }

  public static void RegisterClient(Config options)
  {
    InitializeScopedLogging(options.PluginGuid, options);
  }

  private void HandleCaptureEvent(SentryEvent sentryEvent)
  {
  }

  public static void BindToClient(string guid)
  {
    RegisteredPluginClients.TryGetValue(guid, out var client);
    if (client != null)
    {
      SentrySdk.BindClient(client);
    }
  }

  /**
   * This method will automatically register a plugin with it's guid
   */
  private static bool InitializeScopedLogging(string pluginGuid,
    Config options)
  {
    Debug.Log("called InitializeScopedLogging");
    RegisteredPluginClients.TryGetValue(pluginGuid, out var existingPlugin);

    if (existingPlugin != null)
    {
      Debug.LogError(
        $"A registered namespace for this sentry-plugin already exists for {pluginGuid}, please add a different namespace or contact the mod author to change their naming");
      PendingPluginsToRegister.Remove(options.PluginGuid);
      return false;
    }

    if (options.GetSentryUnityOptions().Dsn == "")
    {
      Debug.LogError(
        "No sentry Dsn provided. Please create a unity project within sentry and add the dsn from the projects settings");
      PendingPluginsToRegister.Remove(options.PluginGuid);
      return false;
    }

    SentrySdk.ConfigureScope(scope =>
    {
      scope.Release = options.PluginVersion;
      scope.SetTag("bepinex.plugin.guid", options.PluginGuid);
      scope.SetTag("pluginName", options.PluginName);
      scope.SetTag("gameName", options.GameName);
    });

    if (!_hasCalledSentryInit)
    {
      SentryUnity.Init(sentryUnityConfig =>
      {
        // A Sentry Data Source Name (DSN) is required.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
        sentryUnityConfig.Dsn = options.GetSentryUnityOptions().Dsn;

        sentryUnityConfig.CacheDirectoryPath =
          Path.Combine(Paths.PluginPath, options.PluginName);

        // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
        // This might be helpful, or might interfere with the normal operation of your application.
        // We enable it here for demonstration purposes when first trying Sentry.
        // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
        sentryUnityConfig.Debug = false;

        // This option is recommended. It enables Sentry's "Release Health" feature.
        sentryUnityConfig.AutoSessionTracking = true;

        // Enabling this option is recommended for client applications only. It ensures all threads use the same global scope.
        sentryUnityConfig.IsGlobalModeEnabled = false;

        // This option will enable Sentry's tracing features. You still need to start transactions and spans.
        sentryUnityConfig.EnableTracing = true;

        // Example sample rate for your transactions: captures 10% of transactions
        sentryUnityConfig.TracesSampleRate = 1.0;
      });
      _hasCalledSentryInit = true;
    }

    var clientOptions = new SentryUnityOptions()
    {
      // A Sentry Data Source Name (DSN) is required.
      // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
      // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
      Dsn = options.GetSentryUnityOptions().Dsn,
      CacheDirectoryPath = Path.Combine(Paths.PluginPath, options.PluginName),

      // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
      // This might be helpful, or might interfere with the normal operation of your application.
      // We enable it here for demonstration purposes when first trying Sentry.
      // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
      Debug = false,

      // This option is recommended. It enables Sentry's "Release Health" feature.
      AutoSessionTracking = true,

      // Enabling this option is recommended for client applications only. It ensures all threads use the same global scope.
      IsGlobalModeEnabled = false,

      // This option will enable Sentry's tracing features. You still need to start transactions and spans.
      EnableTracing = true,

      // Example sample rate for your transactions: captures 10% of transactions
      TracesSampleRate = 1.0,
    };

    // scopes based on plugin name being in the stack trace
    clientOptions.SetBeforeSend(@event =>
    {
      if (@event.Exception != null && @event.Exception.StackTrace.Contains(options.PluginName))
      {
        ZLog.Log($"StackTrace {@event.Exception.StackTrace}");
        return @event;
      }

      return null;
    });

    var client = new SentryClient(clientOptions);
    // client.CaptureEvent()


    // SentrySdk.CaptureException("")

    // Sentry.SentrySdk.CaptureEvent(SentrySdk.CaptureEvent());


    PendingPluginsToRegister.Remove(options.PluginGuid);
    RegisteredPluginClients.Add(options.PluginGuid, client);

    return true;
  }
}