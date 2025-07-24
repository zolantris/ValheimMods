#region

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using DynamicLocations.Controllers;
  using JetBrains.Annotations;
  using UnityEngine;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Patches;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.UI;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.Controllers;

  /// <summary>
  /// A Controller placed directly on the VehicleOnboardCollider GameObject, meant to detect collisions only on that component
  ///
  /// TODO in multiplayer make sure that not only the host, but all clients add all Characters that are players to the VehiclePieces controller. This way there is no jitters
  /// </summary>
  public class VehicleOnboardController : MonoBehaviour, IVehicleSharedProperties, IDeferredTrigger
  {


    // todo Possibly localize these lists and have a delegate to select correct list or skip immediately
    [UsedImplicitly]
    public static readonly Dictionary<ZDOID, WaterZoneCharacterData>
      CharacterOnboardDataItems =
        new();

    private static readonly Dictionary<ZDOID, Player> DelayedExitSubscriptions =
      [];

    public List<Player> m_localPlayers = [];


    public bool HasPlayersOnboard => m_localPlayers.Count > 0;
    private static bool _hasExitSubscriptionDelay = false;

    public BoxCollider OnboardCollider = null!;

    private const float _maxStayTimer = 2f;
    public float _disableTime = 0f;

    public bool isReadyForCollisions { get; set; }
    public bool isRebuildingCollisions
    {
      get;
      set;
    }

    private Rigidbody onboardRigidbody;

    public List<Player> GetLocalPlayersSafe()
    {
      m_localPlayers.RemoveAll(x => x == null);
      return m_localPlayers;
    }

    private void Awake()
    {
      OnboardController = this;
      OnboardCollider = GetComponent<BoxCollider>();
      InvokeRepeating(nameof(ValidateCharactersAreOnShip), 1f, 30f);

      if (OnboardCollider)
      {
        OnboardCollider.includeLayers = LayerHelpers.OnboardLayers;
      }
    }

    private void Start()
    {
      isReadyForCollisions = MovementController != null;
      Invoke(nameof(UpdateReadyForCollisions), 0.1f);
    }

    public void UpdateReadyForCollisions()
    {
      CancelInvoke(nameof(UpdateReadyForCollisions));
      if (!MovementController || !PiecesController)
      {
        isReadyForCollisions = false;
        Invoke(nameof(UpdateReadyForCollisions), 0.1f);
        return;
      }

      isReadyForCollisions = true;
    }

    /// <summary>
    /// For all Players and Characters on vehicle.
    /// </summary>
    /// <returns></returns>
    public List<Character> GetCharactersOnShip()
    {
      var localOnboardCharacterList = new List<Character>();
      var characterList = CharacterOnboardDataItems.Values
        .ToList();
      foreach (var characterOnboardDataItem in characterList)
      {
        if (characterOnboardDataItem == null) continue;
        if (characterOnboardDataItem.OnboardController == null || characterOnboardDataItem.character == null)
        {
          CharacterOnboardDataItems.Remove(characterOnboardDataItem.zdoId);
          continue;
        }

        var piecesController = characterOnboardDataItem.OnboardController
          .PiecesController;
        if (piecesController == null)
        {
          CharacterOnboardDataItems.Remove(characterOnboardDataItem.zdoId);
          continue;
        }

        if (piecesController == PiecesController)
        {
          var character = characterOnboardDataItem.character;
          if (character == null) continue;
          localOnboardCharacterList.Add(character);
        }
      }

      return localOnboardCharacterList;
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
          itemsToRemove.Add(keyValuePair.Value.character);
      }

      foreach (var zdoid in keysToRemove) RemoveByZdoid(zdoid);

      foreach (var character in itemsToRemove) RemoveCharacter(character);
    }

    private static void RemoveByZdoid(ZDOID zdoid)
    {
      CharacterOnboardDataItems.Remove(zdoid);
    }

    public void TryAddPlayerIfMissing(Player player)
    {
      AddPlayerToLocalShip(player);
      AddCharacter(player);
    }

    private void RemoveCharacter(Character character)
    {
      var zdoid = character.GetZDOID();
      RemoveByZdoid(zdoid);

      var player = m_localPlayers
        .FirstOrDefault(x => x.GetZDOID() == zdoid);
      if (player != null)
        m_localPlayers.Remove(player);

      if (PiecesController != null)
      {
        PiecesController.RemoveTempPiece(character.m_nview);
      }

      character.InNumShipVolumes--;
      WaterZoneUtils.UpdateDepthValues(character);
    }

    public void AddCharacter(Character character)
    {
      var zdoid = character.GetZDOID();
      var exists =
        CharacterOnboardDataItems.TryGetValue(zdoid,
          out var characterInstance);

      if (PiecesController != null)
      {
        PiecesController.AddTemporaryPiece(character.m_nview);
      }

      if (!exists)
      {
        var onboardDataItem = new WaterZoneCharacterData(character, this);
        CharacterOnboardDataItems.Add(zdoid, onboardDataItem);
        character.InNumShipVolumes++;
      }
      else if (characterInstance != null)
      {
        if (characterInstance.OnboardController != this ||
            characterInstance.OnboardController != null &&
            characterInstance.OnboardController.transform.parent == null)
          characterInstance.OnboardController = this;
      }
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
        if (data.character == null)
        {
          CharacterOnboardDataItems.Remove(zdoid);
          return false;
        }

        if (data.OnboardController == null)
        {
          var piecesController = VehiclePiecesController.GetVehiclePiecesController(data.character.gameObject);
          if (piecesController != null)
          {
            data.OnboardController = piecesController.OnboardController;
          }
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

    private Coroutine? _removePlayersCoroutineInstance;

    /// <summary>
    /// Starts the updater only for server or client hybrid but not client only
    /// </summary>
    private void StartRemovePlayerCoroutine()
    {
      if (ZNet.instance == null) return;
      if (ZNet.instance.IsDedicated())
      {
        _removePlayersCoroutineInstance = StartCoroutine(RemovePlayersRoutine());
        return;
      }

      if (!ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
        _removePlayersCoroutineInstance = StartCoroutine(RemovePlayersRoutine());
    }

    private void OnEnable()
    {
      Invoke(nameof(UpdateReadyForCollisions), 0.1f);
      StartRemovePlayerCoroutine();
    }

    private void OnDisable()
    {
      CancelInvoke(nameof(DebounceExitVehicleBounds));

      // protect character so it removes this list on unmount of onboard controller
      foreach (var character in CharacterOnboardDataItems.Values.ToList())
        if (character.OnboardController == this)
          CharacterOnboardDataItems.Remove(character.zdoId);

      if (_removePlayersCoroutineInstance != null)
        StopCoroutine(_removePlayersCoroutineInstance);
    }

    public void OnTriggerEnter(Collider collider)
    {
      if (!IsReady()) return;
      if (collider.gameObject.layer == LayerHelpers.ItemLayer)
      {
        HandleItemHitVehicle(collider);
        return;
      }
      OnPlayerEnterVehicleBounds(collider);
      HandleCharacterHitVehicleBounds(collider, false);
    }

    public void HandleItemHitVehicle(Collider collider)
    {
      if (collider == null) return;
      var itemNetView = collider.GetComponentInParent<ZNetView>();
      if (itemNetView == null) return;
      if (PiecesController == null || PiecesController.m_tempPieces.Contains(itemNetView)) return;
      PiecesController.AddTemporaryPiece(itemNetView, true);
    }

    public void HandleItemLeaveVehicle(Collider collider)
    {
      if (collider == null) return;
      var itemNetView = collider.GetComponentInParent<ZNetView>();
      if (itemNetView == null) return;
      if (PiecesController == null || !PiecesController.m_tempPieces.Contains(itemNetView)) return;
      PiecesController.RemoveTempPiece(itemNetView);
    }

    /// <summary>
    /// Meant to be run only when rebuilding. This is heavier computation. But it will prevent problems like the player getting hit by damage if they are not onboard.
    /// </summary>
    /// <param name="collider"></param>
    public void OnTriggerStay(Collider collider)
    {
      if (!isRebuildingCollisions) return;
      if (collider.gameObject.layer == LayerHelpers.ItemLayer)
      {
        return;
      }

      HandlePlayerExitVehicleBounds(collider);
      HandleCharacterHitVehicleBounds(collider, false);
    }

    public void OnTriggerExit(Collider collider)
    {
      if (!IsReady()) return;
      if (collider.gameObject.layer == LayerHelpers.ItemLayer)
      {
        HandleItemLeaveVehicle(collider);
        return;
      }
      HandlePlayerExitVehicleBounds(collider);
      HandleCharacterHitVehicleBounds(collider, true);
    }

    /// <summary>
    /// For bounds updates this must be called
    /// </summary>
    /// todo to see if we need to add a cast to ensure the player is onboard.
    public void OnBoundsRebuild()
    {
      isRebuildingCollisions = false;
      _disableTime = Time.fixedTime + _maxStayTimer;
    }

    public void UpdateReloadingTime()
    {
      isRebuildingCollisions = Time.fixedTime < _disableTime;
      if (!isRebuildingCollisions)
      {
        _disableTime = 0f;
      }
    }

    /// <summary>
    /// Same logic as (VehicleRamAOE,VehicleOnboardController)
    /// </summary>
    /// todo share logic
    /// <returns></returns>
    public bool IsReady()
    {
      if (!isReadyForCollisions) return false;
      if (isRebuildingCollisions)
      {
        UpdateReloadingTime();
      }

      return !isRebuildingCollisions;
    }


    /// <summary>
    /// For restoring any ignore colliders. May want just track ignored colliders per character collider at this rate.
    /// </summary>
    /// 
    /// Todo more collision logic here might be needed
    /// <param name="collider"></param>
    public void RestoreCollisionDetection(Collider collider)
    {
      if (PiecesController != null &&
          PiecesController.m_convexHullAPI.convexHullMeshColliders.Count > 0)
        foreach (var piecesControllerConvexHullMesh in
                 PiecesController.m_convexHullAPI.convexHullMeshColliders)
          Physics.IgnoreCollision(piecesControllerConvexHullMesh, collider,
            false);

      if (MovementController != null && MovementController.LandMovementController != null)
      {
        MovementController.LandMovementController.treadsLeftMovingComponent.convexHullComponent.convexHullMeshColliders.ForEach((x) =>
        {
          if (x == null) return;
          Physics.IgnoreCollision(collider, x, false);
        });
        MovementController.LandMovementController.treadsRightMovingComponent.convexHullComponent.convexHullMeshColliders.ForEach((x) =>
        {
          if (x == null) return;
          Physics.IgnoreCollision(collider, x, false);
        });
      }
    }

    public void HandleCharacterHitVehicleBounds(Collider collider, bool isExiting)
    {
      var character = collider.GetComponent<Character>();
      if (character == null) return;

      RestoreCollisionDetection(collider);

      if (isExiting)
      {
        RemoveCharacter(character);
        return;
      }

      // do not increment or add character if already exists in object. This could be a race condition
      AddCharacter(character);

      WaterZoneUtils.UpdateDepthValues(character, LiquidType.Water);
    }

    /// <summary>
    /// Gets the PlayerComponent and adds/removes it based on exiting state
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    private Player? GetPlayerComponent(Collider collider)
    {
      if (MovementController == null) return null;
      if (Manager == null) return null;
      var playerComponent = collider.GetComponent<Player>();
      if (!playerComponent) return null;

#if DEBUG
      Logger.LogDebug("Player collider hit OnboardTriggerCollider");
#endif

      return playerComponent;
    }

    /// <summary>
    /// Restores the blocking behavior if this mod is controlling / unblocking camera
    /// </summary>
    public static void RestorePlayerBlockingCamera(Player player, bool canBypass = false)
    {
      if (!canBypass && !PhysicsConfig.removeCameraCollisionWithObjectsOnBoat.Value) return;
      if (Player.m_localPlayer == player && GameCamera.instance != null &&
          GameCamera.instance.m_blockCameraMask == 0)
        GameCamera.instance.m_blockCameraMask =
          GameCamera_WaterPatches.BlockingWaterMask;
    }

    public static void AddOrRemovePlayerBlockingCameraWhileControlling(Player player, bool isControlling)
    {
      if (isControlling)
        RemovePlayerBlockingCameraWhileOnboard(player, true);
      else
        RestorePlayerBlockingCamera(player, true);
    }

    public static void AddOrRemovePlayerBlockingCamera(Player player)
    {
      if (WaterZoneUtils.IsOnboard(player))
        RemovePlayerBlockingCameraWhileOnboard(player);
      else
        RestorePlayerBlockingCamera(player);
    }

    /// <summary>
    /// Prevents jitters. Likely most people will want this feature enabled especially for complicated boats.
    /// </summary>
    /// Does not remove changes if the feature is disabled. Players will need to reload. This prevents breaking other mods that might mess with camera.
    public static void RemovePlayerBlockingCameraWhileOnboard(Player player, bool canBypass = false)
    {
      if (!canBypass && !PhysicsConfig.removeCameraCollisionWithObjectsOnBoat.Value) return;
      if (Player.m_localPlayer == player && GameCamera.instance != null &&
          GameCamera.instance.m_blockCameraMask != 0)
        GameCamera.instance.m_blockCameraMask = 0;
    }

    private void RemovePlayerOnShip(Player player)
    {
      var isPlayerInList = m_localPlayers.Contains(player);
      if (isPlayerInList)
      {
        m_localPlayers.Remove(player);
        if (Player.m_localPlayer == player && MovementController != null)
          ValheimBaseGameShip.s_currentShips.Remove(MovementController);
      }
      else
      {
        Logger.LogWarning(
          $"Player {player.GetPlayerName()} detected leaving ship, but not within the ship's player list");
      }

      if (player.m_doodadController != null)
      {
        var controller = player.m_doodadController.GetControlledComponent();
        // controlling null component means we should remove the player anyways.
        if (controller == null)
        {
          player.m_doodadController = null;
        }
        else
        {
          // must be same manager.
          var vehicleManager = controller.GetComponent<VehicleManager>();
          if (vehicleManager != null && vehicleManager == Manager)
          {
            player.m_doodadController = null;
          }
        }
      }

      RestorePlayerBlockingCamera(player);
      UpdateCameraZoom(player, true);

      if (player == Player.m_localPlayer)
      {
        if (VehicleGui.hasConfigPanelOpened)
        {
          VehicleGui.SetConfigPanelState(false);
        }
      }

      player.transform.SetParent(null);
    }

    public void UpdateCameraZoom(Player player, bool isLeaving)
    {
      if (!CameraConfig.CameraZoomOverridesEnabled.Value || CameraConfig.VehicleCameraZoomMaxDistance.Value == 0 || Player.m_localPlayer != player || GameCamera.instance == null)
      {
        return;
      }

      if (isLeaving)
      {
        GameCamera.instance.m_maxDistance = Mathf.Max(GameCamera_CullingPatches.originalMaxDistance, GameCamera_CullingPatches.minimumMaxDistance);
        return;
      }

      var distance = Mathf.Lerp(CameraConfig.cameraZoomMultiplier, Mathf.Pow(CameraConfig.cameraZoomMultiplier, 2), CameraConfig.VehicleCameraZoomMaxDistance.Value);
      GameCamera.instance.m_maxDistance = distance;
    }

    public void AddPlayerToLocalShip(Player player)
    {
      if (PiecesController == null) return;

      var piecesTransform = PiecesController.transform;

      if (!piecesTransform)
      {
        LoggerProvider.LogDebug("Unable to get piecesControllerTransform.");
        return;
      }

      var isPlayerInList = m_localPlayers.Contains(player);
      RemovePlayerBlockingCameraWhileOnboard(player);
      UpdateCameraZoom(player, false);
      player.transform.SetParent(piecesTransform);

      if (!isPlayerInList)
        m_localPlayers.Add(player);
    }

    /// <summary>
    /// Protects against the vehicle smashing the player out of the world on spawn.
    /// </summary>
    /// <param name="character"></param>
    public void OnEnterVolatileVehicle(Character character)
    {
      if (PiecesController == null) return;
      if (!PiecesController.IsActivationComplete)
        character.m_body.isKinematic = true;
    }

    public static void OnVehicleReady()
    {
      foreach (var characterOnboardDataItem in CharacterOnboardDataItems)
        if (characterOnboardDataItem.Value.character.m_body.isKinematic)
          characterOnboardDataItem.Value.character.m_body.isKinematic = false;
    }

    public static void RPC_PlayerOnboardSync()
    {
    }

    public void OnPlayerEnterVehicleBounds(Collider collider)
    {
      var playerInList = GetPlayerComponent(collider);
      if (playerInList == null) return;

      // todo add a remove step...or maybe let it not remove but defend in a bubble further out.
      if (PiecesController != null && PiecesController.targetController != null)
      {
        PiecesController.targetController.AddPlayer(playerInList.transform);
      }

      // All clients should do this
      AddPlayerToLocalShip(playerInList);
      if (Player.m_localPlayer == playerInList)
        ValheimBaseGameShip.s_currentShips.Add(MovementController);

      LoggerProvider.LogDebug(
        $"Player: {playerInList.GetPlayerName()} on-board, total onboard {m_localPlayers.Count}");

      var vehicleZdo = MovementController?.m_nview != null
        ? MovementController.m_nview.GetZDO()
        : null;

      if (playerInList == Player.m_localPlayer && vehicleZdo != null)
        if (PlayerSpawnController.Instance != null)
          PlayerSpawnController.Instance.SyncLogoutPoint(vehicleZdo);
    }

    public void RemoveLogoutPoint(
      KeyValuePair<ZDOID, Player> delayedExitSubscription)
    {
      if (MovementController == null ||
          MovementController?.m_nview == null) return;
      var vehicleZdo = MovementController
        .m_nview.GetZDO();
      if (delayedExitSubscription.Value == Player.m_localPlayer &&
          vehicleZdo != null && PlayerSpawnController.Instance != null)
        PlayerSpawnController.Instance.SyncLogoutPoint(vehicleZdo, true);
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
        var remainingPlayers = m_localPlayers.Count;
        LoggerProvider.LogDebug(
          $"Player: {delayedExitSubscription.Value.GetPlayerName()} over-board, players remaining {remainingPlayers}");
        RemoveLogoutPoint(delayedExitSubscription);
      }

      _hasExitSubscriptionDelay = false;
    }


    public void HandlePlayerExitVehicleBounds(Collider collider)
    {
      var playerInList = GetPlayerComponent(collider);
      if (playerInList == null) return;

      var playerZdoid = playerInList.GetZDOID();
      if (!DelayedExitSubscriptions.ContainsKey(playerZdoid))
        DelayedExitSubscriptions.Add(playerZdoid, playerInList);

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

        if (PiecesController == null) continue;

        var playersOnboard = PiecesController.GetComponentsInChildren<Player>();
        List<Player> validPlayers = [];

        if (playersOnboard == null) continue;

        foreach (var player in playersOnboard)
        {
          if (player == null || !player.isActiveAndEnabled) continue;
          validPlayers.Add(player);
        }

        if (MovementController != null)
        {
          m_localPlayers = validPlayers;
          if (validPlayers.Count == 0) MovementController.SendDelayedAnchor();
        }

        yield return new WaitForSeconds(15);
      }
    }

  #region IVehicleSharedProperties

    public VehiclePiecesController? PiecesController
    {
      get;
      set;
    } = null!;
    public VehicleMovementController? MovementController
    {
      get;
      set;
    } = null!;
    public VehicleConfigSyncComponent? VehicleConfigSync
    {
      get;
      set;
    } = null!;
    public VehicleOnboardController? OnboardController
    {
      get;
      set;
    } = null!;
    public VehicleLandMovementController? LandMovementController
    {
      get;
      set;
    } = null!;
    public VehicleManager Manager
    {
      get;
      set;
    } = null!;

    public ZNetView m_nview
    {
      get;
      set;
    } = null!;

    public bool IsControllerValid => Manager.IsControllerValid;

    public bool IsInitialized => Manager.IsInitialized;

    public bool IsDestroying => Manager.IsDestroying;

  #endregion

  }