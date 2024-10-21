using System.Collections;
using System.Collections.Generic;
using BepInEx;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;

namespace DynamicLocations.API;

public class LoginIntegration : IModLoginAPI
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
  private LoginIntegration(PluginInfo pluginInfo, int loginPrefabHashCode)
  {
    PluginInfo = pluginInfo;
    LoginPrefabHashCode = loginPrefabHashCode;
  }

  public IEnumerator OnLoginMoveToZDO(
    PlayerSpawnController playerSpawnController)
  {
    throw new System.NotImplementedException();
  }


  public bool IsLoginZdo(ZDO zdo)
  {
    throw new System.NotImplementedException();
  }
}