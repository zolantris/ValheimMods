using System.Collections;
using System.Collections.Generic;
using BepInEx;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;
using UnityEngine;

namespace DynamicLocations.API;

/// <summary>
/// Can be used with minimal config effort in other mods
/// </summary>
public class DefaultLoginIntegration : IModLoginAPI
{
  public PluginInfo PluginInfo { get; }
  public bool UseDefaultCallbacks => true;
  public int MovementTimeout => 5000;
  public bool ShouldFreezePlayer => false;
  public int LoginPrefabHashCode { get; }
  public int Priority => 999;
  public List<string> RunBeforePlugins { get; } = [];
  public List<string> RunAfterPlugins { get; } = [];

  /// <summary>
  /// This is the base requirement for LoginIntegration for a specific ZDO. 
  /// </summary>
  /// <param name="pluginInfo"></param>
  /// <param name="loginPrefabHashCode"></param>
  private DefaultLoginIntegration(PluginInfo pluginInfo,
    int loginPrefabHashCode)
  {
    PluginInfo = pluginInfo;
    LoginPrefabHashCode = loginPrefabHashCode;
  }

  public IEnumerator OnLoginMoveToZDO(ZDO zdo, Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    throw new System.NotImplementedException();
  }

  public bool OnLoginMatchZdoPrefab(ZDO zdo)
  {
    // should never be 0
    if (LoginPrefabHashCode == 0) return false;
    return zdo.GetPrefab() == LoginPrefabHashCode;
  }
}