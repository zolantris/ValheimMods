using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using DynamicLocations.Config;
using DynamicLocations.Interfaces;
using Jotunn;

namespace DynamicLocations.Controllers;

/// <summary>
/// This controller will take the Input of IModLoginAPI instances and then finalize the callbacks through here.
/// </summary>
/// <warning>Do not override / patch this, please create a class extending IModeLoginAPI or use the default DynamicLocations.API.LoginIntegrations</warning>
public class LoginAPIController : IModLoginAPI
{
  private readonly IModLoginAPI _loginAPI;
  public PluginInfo PluginInfo => _loginAPI.PluginInfo;
  public bool UseDefaultCallbacks => _loginAPI.UseDefaultCallbacks;

  private readonly PlayerSpawnController _playerSpawnController;

  /// <summary>
  /// miliseconds
  /// </summary>
  private const int DefaultMovementTimeout = 5000;

  public int MovementTimeout => GetMovementTimeout();

  public bool ShouldFreezePlayer => _loginAPI.ShouldFreezePlayer;

  public int LoginPrefabHashCode => _loginAPI.LoginPrefabHashCode;

  public int Priority =>
    _loginAPI.Priority > 0 ? _loginAPI.Priority : 999;

  public List<string> RunBeforePlugins { get; } = [];
  public List<string> RunAfterPlugins { get; } = [];


  // Non interface values.
  public string LoginPluginId;

  // static
  public static readonly Dictionary<string, IModLoginAPI> LoginIntegrations =
    new();

  public static readonly Dictionary<string, IModLoginAPI>
    DisabledLoginIntegrations =
      new();

  public static List<string> LoginIntegrationPriority = [];

  private static string? GetModIntegrationId(PluginInfo plugin)
  {
    if (plugin.Metadata.Name != "") return plugin.Metadata.GUID;
    Logger.LogWarning(
      "Invalid guid detected, make sure the BepInPlugin guid is a valid number");
    return null;
  }

  /// <summary>
  /// Adds/Removes integrations based on disabled integration keys
  /// </summary>
  /// todo make this a bit cleaner for looping.
  internal static void UpdateIntegrations()
  {
    foreach (var disableLoginApiIntegrationConfigItem in DynamicLocationsConfig
               .DisabledLoginApiIntegrations.Value)
    {
      if (LoginIntegrations.ContainsKey(disableLoginApiIntegrationConfigItem))
      {
        LoginIntegrations.Remove(disableLoginApiIntegrationConfigItem);
      }
    }

    foreach (var disabledIntegration in DisabledLoginIntegrations)
    {
      var isMatch = false;
      foreach (var disableLoginApiIntegrationConfigItem in
               DynamicLocationsConfig.DisabledLoginApiIntegrations.Value)
      {
        if (DisabledLoginIntegrations.ContainsKey(
              disableLoginApiIntegrationConfigItem))
        {
          isMatch = true;
        }
      }

      if (!isMatch)
      {
        LoginIntegrations.Add(disabledIntegration.Key,
          disabledIntegration.Value);
      }
    }
  }

  private bool AddLoginApiIntegration(
    IModLoginAPI loginAPI)
  {
    var pluginId = GetModIntegrationId(loginAPI.PluginInfo);
    if (pluginId == null) return false;

    if (!LoginIntegrations.ContainsKey(pluginId))
    {
      LoginIntegrations.Add(pluginId, loginAPI);
      LoginPluginId = pluginId;
      UpdateIntegrations();
      return true;
    }

    Logger.LogError(
      "Could not integrate component due to collision in registered plugin GUID and plugin_version, this ModAPI plugin will not be loaded. Make sure your plugin only creates 1 instance of ModLoginApi and that your plugin GUID or plugin.Name_plugin_Version are unique.");
    return false;
  }

  // Extends from the shared loginAPI, will protect against API incompatibility issues
  private LoginAPIController(IModLoginAPI loginAPI,
    PlayerSpawnController playerSpawnController)
  {
    AddLoginApiIntegration(loginAPI);
    _loginAPI = loginAPI;
    _playerSpawnController = playerSpawnController;
  }

  public IEnumerator OnLoginMoveToZDO(
    PlayerSpawnController playerSpawnController)
  {
    if (_loginAPI.UseDefaultCallbacks)
    {
      yield return _loginAPI.OnLoginMoveToZDO(playerSpawnController);
    }
    else
    {
      yield return _playerSpawnController.MovePlayerToLoginPoint();
      yield return null;
    }
  }

  public bool IsLoginZdo(ZDO zdo)
  {
    throw new System.NotImplementedException();
  }

  // guards
  private int GetMovementTimeout()
  {
    return _loginAPI.MovementTimeout is >= 5 and <= 20
      ? _loginAPI.MovementTimeout
      : DefaultMovementTimeout;
  }
}