using System.Reflection;
namespace ValheimVehicles.Compat;

public static class ValheimRAFTApi
{
  private static object _hostContext;

  public static void RegisterHost(object host)
  {
    _hostContext = host;
  }

  public static string GetPluginVersion()
  {
    var method = _hostContext?.GetType().GetMethod("GetVersion", BindingFlags.Static | BindingFlags.Public
    );
    var result = method?.Invoke(_hostContext, null) as string;
    return result ?? ValheimVehiclesPlugin.Version;
  }

  public static void CallSomethingOnHost()
  {
    var method = _hostContext?.GetType().GetMethod("SomePublicMethod");
    method?.Invoke(_hostContext, null);
  }
}