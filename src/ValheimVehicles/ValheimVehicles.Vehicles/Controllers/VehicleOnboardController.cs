using System;
using System.Collections.Generic;
using System.Linq;
using DynamicLocations;
using DynamicLocations.Controllers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.Patches;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// A Controller placed directly on the VehicleOnboardCollider GameObject, meant to detect collisions only on that component
///
/// TODO in multiplayer make sure that not only the host, but all clients add all Characters that are players to the VehiclePieces controller. This way there is no jitters
/// </summary>
public class VehicleOnboardController : MonoBehaviour
{
  public VehicleMovementController MovementController = null!;
  public IVehicleShip? VehicleInstance => MovementController?.ShipInstance;

  [UsedImplicitly]
  public static readonly Dictionary<ZDOID, WaterZoneCharacterData>
    CharacterOnboardDataItems =
      new();

  private static readonly Dictionary<ZDOID, Player> DelayedExitSubscriptions =
    [];

  private static bool _hasExitSubscriptionDelay = false;

  public Collider? OnboardCollider => MovementController?.OnboardCollider;

  public VehiclePiecesController? PiecesController =>
    MovementController?.ShipInstance?.VehiclePiecesController;

  private void Awake()
  {
    MovementController = GetComponentInParent<VehicleMovementController>();
    InvokeRepeating(nameof(ValidateCharactersAreOnShip), 1f, 30f);
  }

  public List<Player> GetPlayersOnShip()
  {
    var playerList = new List<Player>();
    var characterList = CharacterOnboardDataItems.Values
      .ToList();
    foreach (var characterOnboardDataItem in characterList)
    {
      if (characterOnboardDataItem == null) continue;
      if (characterOnboardDataItem.character == null) continue;
      if (!characterOnboardDataItem.character.IsPlayer()) continue;
      var piecesController = characterOnboardDataItem?.OnboardController
        ?.PiecesController;
      if (!piecesController && characterOnboardDataItem?.zdoId != null)
      {
        CharacterOnboardDataItems.Remove(characterOnboardDataItem.zdoId);
        continue;
      }

      if (piecesController == PiecesController)
      {
        var player = characterOnboardDataItem.character as Player;
        if (player == null) continue;
        playerList.Add(player);
      }
    }

    return playerList;
  }

  private bool IsValidCharacter(Character character)
  {
    return character != null && character.enabled;
  }

  private void ValidateCharactersAreOnShip()
  {
    var itemsToRemove = new List<Character>();
    var keysToRemove = new List<ZDOID>();

    foreach (var keyValuePair in CharacterOnboardDataItems)
    {
      // todo maybe add a check to see if character is connected.
      if (!IsValidCharacter(keyValuePair.Value.character))
      {
        keysToRemove.Add(keyValuePair.Key);
        continue;
      }

      if (keyValuePair.Value.character.transform.root
            .GetComponentInParent<VehiclePiecesController>() == null)
      {
        itemsToRemove.Add(keyValuePair.Value.character);
      }
    }

    foreach (var zdoid in keysToRemove)
    {
      RemoveByZdoid(zdoid);
    }

    foreach (var character in itemsToRemove)
    {
      RemoveCharacter(character);
    }
  }

  private static void RemoveByZdoid(ZDOID zdoid)
  {
    CharacterOnboardDataItems.Remove(zdoid);
  }

  private static void RemoveCharacter(Character character)
  {
    var zdoid = character.GetZDOID();
    RemoveByZdoid(zdoid);
  }

  private void AddCharacter(Character character)
  {
    var zdoid = character.GetZDOID();
    var onboardDataItem = new WaterZoneCharacterData(character, this);
    CharacterOnboardDataItems.Add(zdoid, onboardDataItem);
  }

  public static bool IsCharacterOnboard(Character character)
  {
    return CharacterOnboardDataItems.ContainsKey(character.GetZDOID());
  }

  public static void UpdateUnderwaterState(Character character, bool? val)
  {
    var characterData = GetOnboardCharacterData(character);
    characterData?.UpdateUnderwaterStatus(val);
  }

  public static WaterZoneCharacterData? GetOnboardCharacterData(
    Character character)
  {
    return GetOnboardCharacterData(character.GetZDOID());
  }

  public static WaterZoneCharacterData? GetOnboardCharacterData(
    ZDOID zdoid)
  {
    if (CharacterOnboardDataItems.TryGetValue(zdoid,
          out var data))
    {
      if (data.OnboardController == null)
      {
        CharacterOnboardDataItems.Remove(zdoid);
        return null;
      }

      return data;
    }

    return null;
  }

  public static bool GetCharacterVehicleMovementController(ZDOID zdoid,
    out VehicleOnboardController? controller)
  {
    controller = null;
    if (CharacterOnboardDataItems.TryGetValue(zdoid, out var data))
    {
      if (data.OnboardController == null)
      {
        data.OnboardController =
          VehiclePiecesController
            .GetPieceControllerFromPlayer(data.character.gameObject)?
            .VehicleInstance?.OnboardController;
      }

      if (data.OnboardController == null)
      {
        CharacterOnboardDataItems.Remove(zdoid);
        return false;
      }

      controller = data.OnboardController;
      return true;
    }

    controller = null;
    return false;
  }

  /// <summary>
  /// Starts the updater only for server or client hybrid but not client only
  /// </summary>
  private void Start()
  {
    if (ZNet.instance == null) return;
    if (ZNet.instance.IsDedicated())
    {
      StartCoroutine(RemovePlayersRoutine());
      return;
    }

    if (!ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
    {
      StartCoroutine(RemovePlayersRoutine());
    }
  }

  private void OnEnable()
  {
    StartCoroutine(RemovePlayersRoutine());
  }

  private void OnDisable()
  {
    // protect character so it removes this list on unmount of onboard controller
    foreach (var character in CharacterOnboardDataItems.Values.ToList())
    {
      if (character.OnboardController == this)
      {
        CharacterOnboardDataItems.Remove(character.zdoId);
      }
    }

    StopCoroutine(nameof(RemovePlayersRoutine));
  }

  public VehicleMovementController GetMovementController() =>
    MovementController;

  public void SetMovementController(VehicleMovementController val)
  {
    MovementController = val;
  }

  public void OnTriggerEnter(Collider collider)
  {
    if (MovementController == null) return;
    OnEnterVehicleBounds(collider);
    HandleCharacterHitVehicleBounds(collider, false);
  }

  // todo might refactor this, but water zone controllers will be from the controller instead of delegated here.
  // public static void OnTriggerEnterReceived(Collider collider)
  // {
  //   OnEnterVehicleBounds(collider);
  // }
  //
  // public static void OnTriggerExitReceived()
  // {
  // }

  public void OnTriggerExit(Collider collider)
  {
    if (MovementController == null) return;
    OnExitVehicleBounds(collider);
    HandleCharacterHitVehicleBounds(collider, true);
  }

  public void HandleCharacterHitVehicleBounds(Collider collider, bool isExiting)
  {
    var character = collider.GetComponent<Character>();
    if (!(bool)character) return;

    var characterZdo = character.GetZDOID();
    var exists =
      CharacterOnboardDataItems.TryGetValue(characterZdo,
        out var characterInstance);

    if (isExiting)
    {
      if (!exists) return;
      RemoveCharacter(character);
      character.InNumShipVolumes--;
      WaterZoneUtils.UpdateDepthValues(character);
      return;
    }

    // do not increment or add character if already exists in object. This could be a race condition
    if (!exists)
    {
      AddCharacter(character);
      character.InNumShipVolumes++;
    }
    else if (characterInstance != null)
    {
      if (characterInstance.OnboardController != this ||
          characterInstance.OnboardController != null &&
          characterInstance.OnboardController.transform.parent == null)
      {
        characterInstance.OnboardController = this;
      }
    }

    WaterZoneUtils.UpdateDepthValues(character, LiquidType.Water);
  }

  /// <summary>
  /// Gets the PlayerComponent and adds/removes it based on exiting state
  /// </summary>
  /// <param name="collider"></param>
  /// <returns></returns>
  private Player? GetPlayerComponent(Collider collider)
  {
    if (MovementController.ShipInstance?.Instance == null) return null;
    var playerComponent = collider.GetComponent<Player>();
    if (!playerComponent) return null;

#if DEBUG
    Logger.LogDebug("Player collider hit OnboardTriggerCollider");
#endif

    return playerComponent;
  }

  private void RemovePlayerOnShip(Player player)
  {
    var isPlayerInList = MovementController.m_players.Contains(player);
    player.m_doodadController = null;

    if (isPlayerInList)
    {
      MovementController.m_players.Remove(player);
    }
    else
    {
      Logger.LogWarning(
        $"Player {player.GetPlayerName()} detected leaving ship, but not within the ship's player list");
    }

    player.transform.SetParent(null);
  }

  private void SetPlayerOnShip(Player player)
  {
    var piecesTransform = MovementController.ShipInstance?.Instance
      ?.VehiclePiecesController?
      .transform;


    if (!piecesTransform)
    {
      Logger.LogDebug("Unable to get piecesControllerTransform.");
      return;
    }

    var isPlayerInList = MovementController.m_players.Contains(player);

    MovementController.AssignShipControls(player);

    player.transform.SetParent(piecesTransform);

    if (!isPlayerInList)
    {
      MovementController.m_players.Add(player);
    }
    else
    {
      Logger.LogWarning(
        "Player detected entering ship, but they are already added within the list of ship players");
    }
  }

  /// <summary>
  /// Protects against the vehicle smashing the player out of the world on spawn.
  /// </summary>
  /// <param name="character"></param>
  public void OnEnterVolatileVehicle(Character character)
  {
    if (PiecesController == null) return;
    if (!PiecesController.IsActivationComplete)
    {
      character.m_body.isKinematic = true;
    }
  }

  public static void OnVehicleReady()
  {
    foreach (var characterOnboardDataItem in CharacterOnboardDataItems)
    {
      if (characterOnboardDataItem.Value.character.m_body.isKinematic)
      {
        characterOnboardDataItem.Value.character.m_body.isKinematic = false;
      }
    }
  }

  public static void RPC_PlayerOnboardSync()
  {
  }

  public void OnEnterVehicleBounds(Collider collider)
  {
    var playerInList = GetPlayerComponent(collider);
    if (playerInList == null) return;

    Logger.LogDebug(
      $"Player: {playerInList.GetPlayerName()} on-board, total onboard {MovementController.m_players.Count}");

    // All clients should do this
    SetPlayerOnShip(playerInList);

    var vehicleZdo = MovementController
      .ShipInstance?.NetView?.GetZDO();

    if (playerInList == Player.m_localPlayer && vehicleZdo != null)
    {
      ValheimBaseGameShip.s_currentShips.Add(MovementController);
      PlayerSpawnController.Instance?.SyncLogoutPoint(vehicleZdo);
    }
  }

  public void DebounceExitVehicleBounds()
  {
    _hasExitSubscriptionDelay = true;
    var localList = DelayedExitSubscriptions.ToList();

    // allows new items to be added while this is running
    DelayedExitSubscriptions.Clear();

    foreach (var delayedExitSubscription in localList)
    {
      RemovePlayerOnShip(delayedExitSubscription.Value);

      var remainingPlayers = MovementController.m_players.Count;
      Logger.LogDebug(
        $"Player: {delayedExitSubscription.Value.GetPlayerName()} over-board, players remaining {remainingPlayers}");

      var vehicleZdo = MovementController
        .ShipInstance?.NetView?.GetZDO();

      if (delayedExitSubscription.Value == Player.m_localPlayer &&
          vehicleZdo != null)
      {
        PlayerSpawnController.Instance?.SyncLogoutPoint(vehicleZdo, true);
      }
    }

    _hasExitSubscriptionDelay = false;
  }


  public void OnExitVehicleBounds(Collider collider)
  {
    var playerInList = GetPlayerComponent(collider);
    if (playerInList == null)
    {
      return;
    }

    var playerZdoid = playerInList.GetZDOID();
    if (!DelayedExitSubscriptions.ContainsKey(playerZdoid))
    {
      DelayedExitSubscriptions.Add(playerZdoid, playerInList);
    }

    if (!_hasExitSubscriptionDelay)
    {
      _hasExitSubscriptionDelay = true;
      Invoke(nameof(DebounceExitVehicleBounds), 0.5f);
    }
  }

  /// <summary>
  /// Coroutine to update players if they logout or desync, this will remove them every 30 seconds
  /// </summary>
  /// <returns></returns>
  private IEnumerator<WaitForSeconds?> RemovePlayersRoutine()
  {
    while (isActiveAndEnabled)
    {
      yield return new WaitForSeconds(15);

      var playersOnboard = MovementController?.ShipInstance
        ?.VehiclePiecesController?
        .GetComponentsInChildren<Player>();
      List<Player> validPlayers = [];

      if (playersOnboard == null) continue;

      foreach (var player in playersOnboard)
      {
        if (player == null || !player.isActiveAndEnabled) continue;
        validPlayers.Add(player);
      }

      if (MovementController != null)
      {
        MovementController.m_players = validPlayers;
        if (validPlayers.Count == 0)
        {
          MovementController.SendDelayedAnchor();
        }
      }

      yield return new WaitForSeconds(15);
    }
  }
}