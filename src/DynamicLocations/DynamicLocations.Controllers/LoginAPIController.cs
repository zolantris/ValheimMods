using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using DynamicLocations.API;
using DynamicLocations.Config;
using DynamicLocations.Interfaces;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Controllers;

/// <summary>
/// This controller will take the Input of DynamicLoginIntegration instances and then finalize the callbacks through here.
/// </summary>
/// <warning>Do not override / patch this, please create a class extending IModeLoginAPI or use the default DynamicLocations.API.LoginIntegrations</warning>
public class LoginAPIController
{
  private PlayerSpawnController? _playerSpawnController =>
    PlayerSpawnController.Instance;

  // static
  public static readonly Dictionary<string, DynamicLoginIntegration>
    LoginIntegrations =
      new();

  public static readonly Dictionary<string, DynamicLoginIntegration>
    DisabledLoginIntegrations =
      new();

  public static List<DynamicLoginIntegration> loginIntegrationsByPriority = [];

  private static string? GetModIntegrationId(
    DynamicLoginIntegration integration)
  {
    if (integration.Guid != "") return integration.Guid;
    Logger.LogWarning(
      "Invalid guid detected, make sure the BepInPlugin guid is a valid number");
    return null;
  }

  public static List<DynamicLoginIntegration> OrderItems(
    List<DynamicLoginIntegration> items)
  {
    // Create a dictionary for quick lookup of items by name
    var itemLookup = items.ToDictionary(i => i.Guid);
    // Prepare a result list
    var result = new List<DynamicLoginIntegration>();

    // A set to track which items are already placed in the result
    var placed = new HashSet<string>();

    // First, add items that have no dependencies
    foreach (var item in items.OrderBy(i => i.Guid))
    {
      AddItemWithDependencies(item, itemLookup, result, placed);
    }

    return result;
  }

  private static void AddItemWithDependencies(DynamicLoginIntegration item,
    Dictionary<string, DynamicLoginIntegration> itemLookup,
    List<DynamicLoginIntegration> result,
    HashSet<string> placed)
  {
    // Check if this item has already been placed
    if (placed.Contains(item.Guid))
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
    placed.Add(item.Guid);

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
               .DisabledLoginApiIntegrations)
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
               DynamicLocationsConfig.DisabledLoginApiIntegrations)
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

    loginIntegrationsByPriority = OrderItems(LoginIntegrations.Values.ToList());

    foreach (var item in loginIntegrationsByPriority)
    {
      Logger.LogInfo(
        $"item ----> name:{item.Name}, guid: {item.Guid}, priority: {item.Priority}");
    }
  }

  public static bool AddLoginApiIntegration(
    DynamicLoginIntegration loginIntegration)
  {
    var pluginGuid = GetModIntegrationId(loginIntegration);
    if (pluginGuid == null) return false;

    if (!LoginIntegrations.ContainsKey(pluginGuid))
    {
      LoginIntegrations.Add(pluginGuid, loginIntegration);
      UpdateIntegrations();
      return true;
    }

    if (loginIntegration.LoginPrefabHashCode == 0)
    {
      Logger.LogWarning(
        $"LoginIntegration provided invalid prefab identifier, the hashcode was {loginIntegration.LoginPrefabHashCode}.");
    }

    // todo might be easier/cleaner to provide a prefab name and then convert to stablehashcode and use jotunn prefabmanager.instance.getPrefab()
    if (DynamicLocationsConfig.IsDebug)
    {
      if (!ZNetScene.instance.m_namedPrefabs.TryGetValue(
            loginIntegration.LoginPrefabHashCode, out var prefab))
      {
        Logger.LogError(
          $"Prefab not found for stableHashCode {loginIntegration.LoginPrefabHashCode}");
      }
      else
      {
        Logger.LogDebug(
          $"Found prefab for stableHashCode {loginIntegration.LoginPrefabHashCode} name {prefab.name}");
      }
    }


    Logger.LogError(
      "Could not integrate component due to collision in registered plugin GUID and plugin_version, this ModAPI plugin will not be loaded. Make sure your plugin only creates 1 instance of ModLoginApi and that your plugin GUID or plugin.Name_plugin_Version are unique.");
    return false;
  }

  private static void LogResults(DynamicLoginIntegration? selectedIntegration)
  {
    if (!DynamicLocationsConfig.IsDebug) return;
    Logger.LogDebug(
      selectedIntegration != null
        ? $"Successfully handled ModApi {selectedIntegration.Name} matched"
        : "No matches found for registered integrations");
    // if (selectedIntegration != null)
    // {
    //   Logger.LogWarning("Dynamic location not handled but ModApi matched");
    // }
    // else
    // {
    //
    // }
  }

  public static IEnumerator RunAllIntegrations_OnLoginMoveToZdo(ZDO zdo,
    Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    // early exit so apis do not need to null check unless they do not exit properly.
    if (playerSpawnController == null) yield break;

    IEnumerator? handled = null;
    DynamicLoginIntegration? selectedIntegration = null;

    foreach (var loginIntegration in loginIntegrationsByPriority)
    {
      var isZdoMatch = loginIntegration.OnLoginMatchZdoPrefab(zdo);
      if (isZdoMatch == false) continue;

      selectedIntegration = loginIntegration;
      handled =
        loginIntegration.API_OnLoginMoveToZDO(zdo, offset,
          playerSpawnController);
      break;
    }

    yield return handled;

    // this checks to see if the handler is not an enumerator
    if (selectedIntegration != null)
    {
      yield return new WaitUntil(() =>
        selectedIntegration.IsComplete);
    }

    LogResults(selectedIntegration);
  }
}