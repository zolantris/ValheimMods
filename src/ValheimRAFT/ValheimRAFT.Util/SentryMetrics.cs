using System;
using System.Reflection;
using Jotunn;
using Properties;
using SentryUnityWrapper;

namespace ValheimRAFT.Util;

public class SentryMetrics
{
  public static void ApplyMetrics()
  {
    var Dsn = Assembly.GetEntryAssembly()
      .GetCustomAttribute<SentryDSN>()
      .ConfigurationLocation;

    Logger.LogDebug($"DSN FOR sentry {Dsn}");
    SentryUnityWrapperPlugin.RegisterPluginAsync(
      new(ValheimRaftPlugin.BepInGuid, ValheimRaftPlugin.ModName, ValheimRaftPlugin.Version,
        Dsn));
  }
  // SentryMetrics()
  // {
  //   string @namespace = "SentryUnityWrapper";
  //   string @pluginClass = "SentryUnityWrapperPlugin";
  //   string method = "RegisterPluginAsync";
  //   string options = "Options";
  //
  //   var Dsn =
  //     "https://e720adb5b1a1fdb40d073635eb76817d@o243490.ingest.sentry.io/4506613652586496";
  //   var myClassType = Type.GetType(String.format("{0}.{1}", @namespace, @class));
  //   object instance =
  //     pluginClass == null
  //       ? null
  //       : Activator.CreateInstance(myClassType); //Check if exists, instantiate if so.
  //   var myMethodExists = myClassType.GetMethod(method) != null;
  //
  //   if (typeof(SentryUnityWrapper))
  //     RegisterPluginAsync(
  //       new Options(BepInGuid, ModName, Version, Dsn));
  // }
}