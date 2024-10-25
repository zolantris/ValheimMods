using System;
using System.Collections.Generic;
using DynamicLocations;
using DynamicLocations.Controllers;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// A Controller placed directly on the VehicleOnboardCollider GameObject, meant to detect collisions only on that component
/// </summary>
public class VehicleOnboardController : MonoBehaviour
{
  internal VehicleMovementController MovementController = null!;

  public static Dictionary<ZDOID, Character?> CharactersOnboard = new();

  public static Dictionary<ZDOID, VehicleOnboardController?>
    CharacterOnVehiclePieces = new();

  public Collider onboardCollider => MovementController.OnboardCollider;

  private void Awake()
  {
    MovementController = GetComponentInParent<VehicleMovementController>();
    InvokeRepeating(nameof(ValidateCharactersAreOnShip), 1f, 30f);
  }

  private void ValidateCharactersAreOnShip()
  {
    var itemsToRemove = new List<Character>();
    var keysToRemove = new List<ZDOID>();

    foreach (var keyValuePair in CharactersOnboard)
    {
      if (keyValuePair.Value == null || keyValuePair.Value.enabled != true)
      {
        keysToRemove.Add(keyValuePair.Key);
        continue;
      }


      if (keyValuePair.Value.transform.root
            .GetComponentInParent<VehiclePiecesController>() == null)
      {
        itemsToRemove.Add(keyValuePair.Value);
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
    CharactersOnboard.Remove(zdoid);
    CharacterOnVehiclePieces.Remove(zdoid);
  }

  private static void RemoveCharacter(Character character)
  {
    var zdoid = character.GetZDOID();
    CharactersOnboard.Remove(zdoid);
    CharacterOnVehiclePieces.Remove(zdoid);
  }

  private void AddCharacter(Character character)
  {
    var zdoid = character.GetZDOID();
    CharactersOnboard.Add(zdoid, character);
    CharacterOnVehiclePieces.Add(zdoid, this);
  }

  public static bool CharacterIsOnboard(Character character)
  {
    return CharactersOnboard.ContainsKey(character.GetZDOID());
  }

  public static bool GetCharacterVehicleMovementController(ZDOID zdoid,
    out VehicleOnboardController controller)
  {
    return CharacterOnVehiclePieces.TryGetValue(zdoid, out controller);
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

    if (isExiting)
    {
      if (CharactersOnboard.ContainsKey(character.GetZDOID()))
      {
        RemoveCharacter(character);
        character.InNumShipVolumes--;
      }

      return;
    }

    if (CharactersOnboard.ContainsKey(character.GetZDOID())) return;
    AddCharacter(character);
    character.InNumShipVolumes++;
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

  public void OnExitVehicleBounds(Collider collider)
  {
    var playerInList = GetPlayerComponent(collider);
    if (playerInList == null)
    {
      return;
    }

    RemovePlayerOnShip(playerInList);

    var remainingPlayers = MovementController.m_players.Count;
    Logger.LogDebug(
      $"Player: {playerInList.GetPlayerName()} over-board, players remaining {remainingPlayers}");

    var vehicleZdo = MovementController
      .ShipInstance?.NetView?.GetZDO();

    if (playerInList == Player.m_localPlayer && vehicleZdo != null)
    {
      // Todo figure out why I had this enabled, it looks like it could cause a ton of issues.
      // ValheimBaseGameShip.s_currentShips.Remove(_movementController);
      PlayerSpawnController.Instance?.SyncLogoutPoint(vehicleZdo, true);
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