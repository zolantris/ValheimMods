using System;
using System.Reflection;
using Jotunn;
using Properties;

namespace ValheimRAFT.Util;

public abstract class SentryMetrics
{
  public static void ApplyMetrics()
  {
    // var assemblyEntry = Assembly.GetEntryAssembly();
    // string Dsn = "";
    // if (assemblyEntry == null)
    // {
    //   Logger.LogError("No assembly entry");
    //   // return;
    // }
    // else
    // {
    //   Dsn = assemblyEntry.GetCustomAttribute<SentryDSN>()
    //     .Dsn;
    // }
    //
    // Dsn = Assembly.GetAssembly().GetCustomAttribute<SentryDSN>().Dsn;

    // todo to remove this from hardcoded config
    // var Dsn =
    //   "https://e720adb5b1a1fdb40d073635eb76817d@o243490.ingest.us.sentry.io/4506613652586496";
    // Logger.LogDebug($"DSN FOR sentry {Dsn}");
    // SentryUnityWrapperPlugin.RegisterPluginAsync(
    //   new Config(ValheimRaftPlugin.ModGuid, ValheimRaftPlugin.ModName, ValheimRaftPlugin.Version,
    //     Dsn));
    //
    //
    // var dsn =
    //   "https://45e6b5f08cfdf76cae86a36cc3bdffd1@o243490.ingest.us.sentry.io/4506635619467264";
    // SentryUnityWrapperPlugin.RegisterPluginAsync(
    //   new Config("zolantris.ValheimVehicles", "ValheimVehicles", ValheimRaftPlugin.Version,
    //     dsn));
  }
}