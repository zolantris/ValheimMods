using System.Collections;
using System.Collections.Generic;
using BepInEx;
using DynamicLocations.Controllers;
using UnityEngine;

namespace DynamicLocations.Interfaces;

/// <summary>
/// This interface is provided for other mod APIs, however I highly recommend using ModLoginApi class directly as the class has some critical guards
/// </summary>
internal interface IModLoginAPI
{
  // Required for determining the mod that is being integrated.
  // This information is used to allow players to manually disable specific integrations if they destabilize the mod.
  BaseUnityPlugin Plugin { get; set; }

  // Will ignore values provided to IsLoginZdo and LoginPrefabHashCode
  public bool UseDefaultCallbacks { get; }

  // Must be a value in miliseconds between 2000 and 20000; Anything above that value will be reset back to the default of the DynamicLocationsApi. 
  public int MovementTimeout { get; }

  // will automatically freeze the player until after LoginIntegration completes. 
  public bool ShouldFreezePlayer { get; }

  public int LoginPrefabHashCode { get; }

  // Sets the order of calls via numbers
  public int Priority { get; }

  // runs before a mod guid
  public List<string> RunBeforePlugins { get; }

  // runs after a mod guid
  public List<string> RunAfterPlugins { get; }

  /// <summary>
  /// Moves the user to the ZDO
  /// - Can be set unimplemented if UseDefaultCallback is provided
  /// - This logic must be a Coroutine as there could be asynchronous logic required to wait until the DynamicLocation is ready and/or safe to move the player to.
  /// - the instanceof playerSpawnController is exposed as an API, allowing the integration to call MovePlayerToLoginPoint when needed or use completely custom logic.
  /// </summary>
  /// <returns></returns>
  public IEnumerator OnLoginMoveToZDO(ZDO zdo, Vector3? offset,
    PlayerSpawnController playerSpawnController);

  /// <summary>
  /// Identifier for the prefab that will contain the DynamicLocationZDO
  /// - Prefab "wood_floor" would have it's equivalent ZDO with "wood_floor".GetHashCode()
  /// </summary>
  /// <optimizations>
  /// Default LoginPrefabHashCode as a pre-computed variable { get; } = PrefabStringName.GetHashCode()
  /// </optimizations>
  /// <returns></returns>
  public bool OnLoginMatchZdoPrefab(ZDO zdo);
}