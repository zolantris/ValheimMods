using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using DynamicLocations.Config;
using DynamicLocations.Controllers;
using DynamicLocations.Structs;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;

namespace DynamicLocations.API;

/// <summary>
/// Can be used with minimal config effort in other mods
/// </summary>
public class DynamicLoginIntegration
{
  public int LoginPrefabHashCode => Config.LoginPrefabHashCode;
  internal string Guid => Config.Guid;
  internal string Name => Config.Name;
  internal string Version => Config.Version;
  public bool ShouldFreezePlayer => Config.ShouldFreezePlayer;
  public List<string> RunBeforePlugins => Config.RunBeforePlugins;
  public List<string> RunAfterPlugins => Config.RunAfterPlugins;

  public int Priority =>
    Config.Priority > 0 ? Config.Priority : 999;

  public int MovementTimeoutMs => GetMovementTimeout();

  private const int DefaultMovementTimeoutMs = 5000;
  private DebugSafeTimer timerInstance;

  // State values
  private bool _isRunningOnLoginMoveToZdo = false;
  private bool _hasCompleted = false;
  private Coroutine? _onLoginMoveToZdoCoroutine = null;

  public bool IsComplete => _hasCompleted && !_isRunningOnLoginMoveToZdo &&
                            _onLoginMoveToZdoCoroutine == null;

  public IntegrationConfig Config { get; set; }

  /// <summary>
  /// This is the base requirement for LoginIntegration for a specific ZDO. 
  /// </summary>
  /// <param name="plugin">This is the BaseUnityPlugin used to extract Plugin Guid, Version, and Name for mod debugging.</param>
  protected DynamicLoginIntegration(IntegrationConfig config)
  {
    Config = config;
  }

  private int GetMovementTimeout()
  {
    return Config.MovementTimeout is >= 2000 and <= 20000
      ? Config.MovementTimeout
      : DefaultMovementTimeoutMs;
  }

  public static IntegrationConfig CreateConfig(BaseUnityPlugin plugin,
    string zdoTargetPrefabName)
  {
    var integrationConfig = new IntegrationConfig(plugin, zdoTargetPrefabName);
    return integrationConfig;
  }


  /// <summary>
  /// To be called when starting the OnLoginMoveToZDO.
  /// </summary>
  private void OnStart()
  {
    timerInstance =
      DebugSafeTimer.StartNew(PlayerSpawnController.Timers);
    PlayerSpawnController.ResetRoutine(ref _onLoginMoveToZdoCoroutine);
    _hasCompleted = false;
    _isRunningOnLoginMoveToZdo = true;
  }

  private void OnComplete()
  {
    timerInstance.Delete();
    PlayerSpawnController.ResetRoutine(ref _onLoginMoveToZdoCoroutine);
    _isRunningOnLoginMoveToZdo = false;
    _hasCompleted = true;
  }

  /// <summary>
  /// The method run by the APIController. This should never be overriden
  /// </summary>
  /// <param name="zdo"></param>
  /// <param name="offset"></param>
  /// <param name="playerSpawnController"></param>
  /// <returns></returns>
  protected internal IEnumerator API_OnLoginMoveToZDO(
    ZDO zdo,
    Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    OnStart();
    _onLoginMoveToZdoCoroutine =
      playerSpawnController.StartCoroutine(
        OnLoginMoveToZDO(zdo, offset,
          playerSpawnController));
    yield return _onLoginMoveToZdoCoroutine;
    yield return new WaitUntil(() =>
      _onLoginMoveToZdoCoroutine is null ||
      timerInstance.ElapsedMilliseconds >= GetMovementTimeout());
    OnComplete();

    yield return true;
  }

  /// <summary>
  /// This is meant to be overriden per mod if custom logic is needed
  /// </summary>
  /// <param name="zdo"></param>
  /// <param name="offset"></param>
  /// <param name="playerSpawnController"></param>
  /// <returns></returns>
  protected virtual IEnumerator OnLoginMoveToZDO(
    ZDO zdo,
    Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    if (DynamicLocationsConfig.IsDebug)
    {
      Logger.LogDebug(
        "Not overridden custom handler, running MovePlayerToZdo default call.");
    }

    var isMatch = OnLoginMatchZdoPrefab(zdo);
    if (!isMatch) yield break;
    yield return playerSpawnController.MovePlayerToZdo(zdo,
      offset ?? Vector3.zero);
    yield return new WaitUntil(() =>
      PlayerSpawnController.MoveToLogoutRoutine is null);
  }

  /// <summary>
  /// The main logic for determining if the ZDO matches.
  /// - Mods might override this if there is another key on the zdo that needs to be looked at and not the prefab hashcode.
  /// </summary>
  /// <param name="zdo"></param>
  /// <returns></returns>
  [UsedImplicitly]
  public virtual bool OnLoginMatchZdoPrefab(ZDO zdo)
  {
    if (LoginPrefabHashCode == 0)
    {
      Logger.LogError(
        $"Login Prefab Hash was zero, this likely means the mod {Config.Name} {Config.Version} has not properly been integrated for DynamicLoginIntegration");
      return false;
    }

    return zdo.GetPrefab() == LoginPrefabHashCode;
  }
}