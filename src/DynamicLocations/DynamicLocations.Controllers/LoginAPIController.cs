using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using DynamicLocations.Config;
using DynamicLocations.Interfaces;
using Jotunn;
using Jotunn.Managers;

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
  public string LoginPluginId = "Invalid Plugin";

  // static
  public static readonly Dictionary<string, IModLoginAPI> LoginIntegrations =
    new();

  public static readonly Dictionary<string, IModLoginAPI>
    DisabledLoginIntegrations =
      new();

  public static List<IModLoginAPI> LoginIntegrationPriority = [];

  private static string? GetModIntegrationId(PluginInfo plugin)
  {
    if (plugin.Metadata.Name != "") return plugin.Metadata.GUID;
    Logger.LogWarning(
      "Invalid guid detected, make sure the BepInPlugin guid is a valid number");
    return null;
  }

  public static List<IModLoginAPI> OrderItems(List<IModLoginAPI> items)
  {
    // Create a dictionary for quick lookup of items by name
    var itemLookup = items.ToDictionary(i => i.PluginInfo.Metadata.GUID);

    // Prepare a result list
    var result = new List<IModLoginAPI>();

    // A set to track which items are already placed in the result
    var placed = new HashSet<string>();

    // First, add items that have no dependencies
    foreach (var item in items.OrderBy(i => i.Priority))
    {
      AddItemWithDependencies(item, itemLookup, result, placed);
    }

    return result;
  }

  private static void AddItemWithDependencies(IModLoginAPI item,
    Dictionary<string, IModLoginAPI> itemLookup, List<IModLoginAPI> result,
    HashSet<string> placed)
  {
    // Check if this item has already been placed
    if (placed.Contains(item.PluginInfo.Metadata.GUID))
      return;

    // First, add the items this one must come after
    foreach (var after in item.RunAfterPlugins)
    {
      if (itemLookup.TryGetValue(after, out var afterItem))
      {
        AddItemWithDependencies(afterItem, itemLookup, result, placed);
      }
    }

    // Then, add this item itself
    result.Add(item);
    placed.Add(item.PluginInfo.Metadata.GUID);

    // Finally, add the items this one must come before
    foreach (var before in item.RunBeforePlugins)
    {
      if (itemLookup.TryGetValue(before, out var beforeItem))
      {
        AddItemWithDependencies(beforeItem, itemLookup, result, placed);
      }
    }
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

    LoginIntegrationPriority = OrderItems(LoginIntegrations.Values.ToList());

    foreach (var item in LoginIntegrationPriority)
    {
      Logger.LogInfo(
        $"item ----> name:{item.PluginInfo.Metadata.Name}, guid: {item.PluginInfo.Metadata.GUID}, priority: {item.Priority}");
    }
    //
    // var integrationOrder =
    //   LoginIntegrations.OrderBy(x => x.Value.Priority);


    // TODO add support for beforeEach and afterEach logic
    // foreach (var keyValuePair in integrationOrder)
    // {
    //   if ()
    // }

    // var listByRunAfter = new List<string>();
    // var listByRunBefore = new List<string>();

    // foreach (var integration in integrationOrder.ToArray())
    // {
    //   var currentIntegration = integration.Key;
    //   if (integration.Value.RunAfterPlugins.Count > 0)
    //   {
    //     // integrationOrder.Remove(integration);
    //
    //     // integrationOrder.FindAll(x => );
    //     integrationIndex = integrationOrder.IndexOf(integration)
    //     foreach (var valueRunAfterPlugin in integration.Value.RunAfterPlugins)
    //     {
    //       var isBeforeDependency = 
    //       // listByRunAfter.Add(integrationOrder);
    //     }
    //   }
    //
    //   if (integration.Value.RunBeforePlugins.Count > 0)
    //   {
    //   }
    // }
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
    _loginAPI = loginAPI;
    _playerSpawnController = playerSpawnController;

    // todo add more guards for other data types that could be invalid. Maybe add a Prefab.instance.getPrefab() to output the name returned
    if (loginAPI.LoginPrefabHashCode == 0)
    {
      Logger.LogWarning(
        $"LoginIntegration provided invalid prefab identifier, the hashcode was {loginAPI.LoginPrefabHashCode}.");
    }

    // todo might be easier/cleaner to provide a prefab name and then convert to stablehashcode and use jotunn prefabmanager.instance.getPrefab()
    if (DynamicLocationsConfig.IsDebug)
    {
      if (!ZNetScene.instance.m_namedPrefabs.TryGetValue(
            loginAPI.LoginPrefabHashCode, out var prefab))
      {
        Logger.LogError(
          $"Prefab not found for stableHashCode {loginAPI.LoginPrefabHashCode}");
      }
      else
      {
        Logger.LogDebug(
          $"Found prefab for stableHashCode {loginAPI.LoginPrefabHashCode} name {prefab.name}");
      }
    }

    AddLoginApiIntegration(loginAPI);
  }

  public IEnumerator OnLoginMoveToZDO(
    ZDO targetZdo,
    PlayerSpawnController playerSpawnController)
  {
    if (_loginAPI.UseDefaultCallbacks)
    {
      yield return _loginAPI.OnLoginMoveToZDO(playerSpawnController);
    }
    else
    {
      foreach (var loginAPI in LoginAPIController.LoginIntegrationPriority)
      {
        var isMatch = loginAPI.IsLoginZdo(targetZdo);
        var result = _playerSpawnController.MovePlayerToLogoutPoint();
        yield return result;
        yield break;
      }
    }
  }

  public bool IsLoginZdo(ZDO zdo)
  {
    return _loginAPI.IsLoginZdo(zdo);
  }

  // guards
  private int GetMovementTimeout()
  {
    return _loginAPI.MovementTimeout is >= 5 and <= 20
      ? _loginAPI.MovementTimeout
      : DefaultMovementTimeout;
  }
}