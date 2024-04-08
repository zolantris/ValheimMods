# SentryUnityPlugin

This plugin is a wrapper around sentry-unity. It allows BepInEx plugins to
register scoped packages to Sentry and (Hopefully) report separately to
different providers based on the error types encountered.

Scopes should not conflict with other packages and there is steps to prevent
registering with the same mod guid.

All binaries are owned by Sentry and taken from their (
sentry-unity)[https://github.com/getsentry/sentry-unity] package
releases.

## Features

- The Version will follow Sentry-Unity SDK releases for minor and major
  versions. IE if the Sentry-Unity is `1.8.0` this package will be `1.8.0` but
  also
  could be `1.8.11` etc.
- register any plugins during runtime and then
  initialize the sdk per package with a different scope.
- Developers are required to provider their own Dsn route
- Has limited configuration for both scope and init config. This allows other
  mods to utilize sentry as well.

## Todos

- give users the option to remove registered sentry packages to prevent abuse of
  mod-owners.
- Ensure scope only access and game scope access to avoid having other mods
  report issues in a sentry instance.
- Add a Config option to directly disable sentry logs for a specific registered
  package. Or all logs.
    - These values would need to be output and parsed from a a string

## Integration Recommendations

1. Make sure your packages does not require users to have sentry. Some users
   will not want to download this package.
2. Make sure to be clear to your users what sentry logs are being collected. (
   data on the game only)

Example code for making **sentry** optionally required.

```csharp

  public static void ApplyMetricIfAvailable()
  {
    string @namespace = "SentryUnityWrapper";
    string @pluginClass = "SentryUnityWrapperPlugin";
    if (ValheimRaftPlugin.Instance.EnableMetrics.Value &&
        Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper"))
    {
      if (Type.GetType($"{@namespace}.{@pluginClass}") != null)
      {
        SentryMetrics.ApplyMetrics();
      }
    }
  }

  public static void ApplyMetrics()
  {
    var Dsn = Assembly.GetEntryAssembly()
      .GetCustomAttribute<SentryDSN>()
      .ConfigurationLocation;

    Logger.LogDebug($"DSN FOR sentry {Dsn}");
    SentryUnityWrapperPlugin.RegisterPluginAsync(
      new(ValheimRaftPlugin.BepInGuid, ValheimRaftPlugin.ModName, ValheimRaftPlugin.Version,Dsn));
  }
```

## Resources

Sentry icon is licensed under open source and provided
on https://iconduck.com/icons/27934/sentry.