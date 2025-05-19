using System.Reflection;
namespace ValheimVehicles.Compat;

public static class ValheimRAFT_API
{
  private static object _hostContext;

  /// <summary>
  /// These values come from ValheimRAFTPlugin but we cannot reference that directly so it's copied for now.
  ///
  /// todo move these values to a shared lib.
  /// </summary>
  public const string Author = "zolantris";
  public const string ModName = "ValheimRAFT";
  public const string ModNameBeta = "ValheimRAFTBETA";

  private static string? _cachedPluginVersion = null;
  private static string? _cachedPluginName = null;

  ///
  /// <summary>
  /// These folder names are matched for the CustomTexturesGroup
  /// </summary>
  /// 
  public static string[] possibleModFolderNames =
  [
    $"{Author}-{ModName}", $"zolantris-{ModName}", $"Zolantris-{ModName}",
    ModName, $"{Author}-{ModNameBeta}", $"zolantris-{ModNameBeta}",
    $"Zolantris-{ModNameBeta}",
    ModNameBeta
  ];

  public static void RegisterHost(object host)
  {
    _hostContext = host;
  }

  public static string GetPluginName()
  {
    if (_cachedPluginName != null) return _cachedPluginName;
    var method = _hostContext?.GetType().GetMethod("GetPluginName", BindingFlags.Static | BindingFlags.Public
    );
    var result = method?.Invoke(_hostContext, null) as string;
    _cachedPluginName = result ?? null;
    return result ?? ValheimVehiclesPlugin.ModName;
  }

  public static string GetPluginVersion()
  {
    if (_cachedPluginVersion != null) return _cachedPluginVersion;
    var method = _hostContext?.GetType().GetMethod("GetPluginVersion", BindingFlags.Static | BindingFlags.Public
    );
    var result = method?.Invoke(_hostContext, null) as string;
    _cachedPluginVersion = result ?? null;
    return result ?? ValheimVehiclesPlugin.Version;
  }

  public static void CallSomethingOnHost()
  {
    var method = _hostContext?.GetType().GetMethod("SomePublicMethod");
    method?.Invoke(_hostContext, null);
  }
}