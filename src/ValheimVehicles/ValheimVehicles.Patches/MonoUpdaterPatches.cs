using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Patches;

/// <summary>
/// These patches are critical to get Physics to sync properly across the ship
/// </summary>
public class MonoUpdaterPatches
{
  private const string VehiclePiecesControllerUpdate =
    "MonoUpdaters.Update.VehiclePiecesController";

  private const string VehiclePiecesControllerFixedUpdate =
    "MonoUpdaters.FixedUpdate.VehiclePiecesController";

  private const string ValheimAoeFixedUpdate =
    "MonoUpdaters.FixedUpdate.ValheimAoe";

  private const string VehiclePiecesControllerLateUpdate =
    "MonoUpdaters.LateUpdate.VehiclePiecesController";

  private const string VehicleMovementControllerFixedUpdate =
    "MonoUpdaters.FixedUpdate.VehicleMovementController";

  private const string VehicleMovementControllerLateUpdate =
    "MonoUpdaters.LateUpdate.VehicleMovementController";

  private const string VehicleShipEffectsLateUpdate =
    "MonoUpdaters.LateUpdate.VehicleShipEffects";

  /// <summary>
  /// For Synchronizing updates with other updaters across the Valheim Game
  /// </summary>
  /// <includes>
  /// - VehiclePiecesController -> has a critical update that syncs Physics and needs other logic like ZSyncTransform to be fired before it to not create Jitters
  /// </includes>
  /// <skips> VehicleMovementController.CustomUpdate as the method currently does nothing
  /// </skips>
  /// <param name="__instance"></param>
  [HarmonyPatch(typeof(MonoUpdaters), nameof(MonoUpdaters.Update))]
  [HarmonyPostfix]
  public static void Update(MonoUpdaters __instance)
  {
    var deltaTime = Time.deltaTime;
    var time = Time.time;

    __instance.m_update.CustomUpdate(
      VehiclePiecesController.MonoUpdaterInstances,
      VehiclePiecesControllerUpdate,
      deltaTime, time);

    if (!Mathf.Approximately(Time.deltaTime, deltaTime))
      Logger.LogError("DeltaTime is out of sync even with synchronous calls");

    if (!Mathf.Approximately(Time.time, time))
      Logger.LogError("Time is out of sync even with synchronous calls");
  }


  [HarmonyPatch(typeof(MonoUpdaters), nameof(MonoUpdaters.FixedUpdate))]
  [HarmonyPostfix]
  public static void FixedUpdate(MonoUpdaters __instance)
  {
    var deltaTime = Time.fixedDeltaTime;
    __instance.m_update.CustomFixedUpdate(
      VehicleMovementController.MonoUpdaterInstances,
      VehicleMovementControllerFixedUpdate,
      deltaTime);

    __instance.m_update.CustomFixedUpdate(
      VehiclePiecesController.MonoUpdaterInstances,
      VehiclePiecesControllerFixedUpdate,
      deltaTime);

    __instance.m_update.CustomFixedUpdate(
      ValheimAoe.Instances,
      ValheimAoeFixedUpdate,
      deltaTime);

    if (!Mathf.Approximately(Time.fixedDeltaTime, deltaTime))
      Logger.LogError("DeltaTime is out of sync even with synchronous calls");
  }

  [HarmonyPatch(typeof(MonoUpdaters), nameof(MonoUpdaters.LateUpdate))]
  [HarmonyPostfix]
  public static void LateUpdate(MonoUpdaters __instance)
  {
    var deltaTime = Time.deltaTime;

    __instance.m_update.CustomLateUpdate(VehicleShipEffects.MonoUpdaters,
      VehicleShipEffectsLateUpdate, deltaTime);

    __instance.m_update.CustomLateUpdate(
      VehicleMovementController.MonoUpdaterInstances,
      VehicleMovementControllerLateUpdate,
      deltaTime);

    __instance.m_update.CustomLateUpdate(
      VehiclePiecesController.MonoUpdaterInstances,
      VehiclePiecesControllerLateUpdate,
      deltaTime);

    if (!Mathf.Approximately(Time.deltaTime, deltaTime))
      Logger.LogError("DeltaTime is out of sync even with synchronous calls");
  }
}