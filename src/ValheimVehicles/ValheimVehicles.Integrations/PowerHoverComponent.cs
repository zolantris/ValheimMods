using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.ModSupport;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using Zolantris.Shared;
using Zolantris.Shared.Debug;
namespace ValheimVehicles.Integrations;

public class PowerHoverComponent : MonoBehaviour, Hoverable, Interactable
{
  private PowerSourceBridge _powerSourceComponent;
  private PowerStorageBridge _powerStorageComponent;
  private bool HasPowerStorage = false;
  private bool HasPowerSource = false;
  public static int AddManyCount = 10;
  public static int AddOneCount = 1;

  public void Start()
  {
    _powerSourceComponent = GetComponent<PowerSourceBridge>();
    _powerStorageComponent = GetComponent<PowerStorageBridge>();

    HasPowerStorage = _powerStorageComponent != null;
    HasPowerSource = _powerSourceComponent != null;
  }

  public string EitrInventoryItem_TokenId = "$item_eitr";

  public List<Container> _NearByContainers = new();

  private static float _NearbyChestSearchRadius = 5f;
  private static float _NearbyChestSearchDebounce = 10f;
  private float _lastSearchTime;

  public void UpdateNearbyChests()
  {
    _NearByContainers.Clear();
    var colliders = Physics.OverlapSphere(transform.position, _NearbyChestSearchRadius, LayerMask.GetMask("piece"));
    foreach (var collider in colliders)
    {
      var container = collider.GetComponentInParent<Container>();
      if (container)
      {
        _NearByContainers.Add(container);
      }
    }

    var eitrNearby = 0;
    foreach (var nearByContainer in _NearByContainers)
    {
      eitrNearby += nearByContainer.GetInventory().CountItems(EitrInventoryItem_TokenId);
    }
    LoggerProvider.LogDebug($"Found {eitrNearby} items in {_NearByContainers.Count} containers");
  }

  public struct FuelToCommit
  {
    public Container? container;
    public Player? player;
    public int pendingPlayerAmount;
    public int pendingContainerAmount;
  }

  public static readonly Dictionary<string, Coroutine> PendingFuelPromises = new();
  public static readonly Dictionary<string, bool> PendingFuelPromisesResolutions = new();


  public void ClearPendingFuelPromises(string pendingPromiseId)
  {
    PendingFuelPromisesResolutions.Remove(pendingPromiseId);
    PendingFuelPromisesResolutions.Remove(pendingPromiseId);
  }

  public IEnumerator WaitForFuelToCommit(List<FuelToCommit> fuelPromise, string pendingPromiseId, string addedMessage, PowerSourceData sourceData)
  {
    var timer = DebugSafeTimer.StartNew();
    while (timer.ElapsedMilliseconds < 10000f && PendingFuelPromisesResolutions.TryGetValue(pendingPromiseId, out var isPending) && isPending)
      yield return null;
    if (timer.ElapsedMilliseconds >= 10000f && PendingFuelPromisesResolutions[pendingPromiseId])
    {
      ClearPendingFuelPromises(pendingPromiseId);

      Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, ModTranslations.PowerSource_Message_FailedToAdd);
      yield break;
    }

    foreach (var fuelToCommit in fuelPromise)
    {
      var container = fuelToCommit.container;
      var player = fuelToCommit.player;
      var pendingContainerAmount = fuelToCommit.pendingContainerAmount;
      var pendingPlayerAmount = fuelToCommit.pendingPlayerAmount;
      if (container != null)
      {
        ValheimContainerTracker.RemoveItemsByName(container.m_inventory, EitrInventoryItem_TokenId, pendingContainerAmount);
      }
      if (player != null && player.GetInventory() != null)
      {
        ValheimContainerTracker.RemoveItemsByName(player.m_inventory, EitrInventoryItem_TokenId, pendingPlayerAmount);
      }
    }

    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, addedMessage);
    ClearPendingFuelPromises(pendingPromiseId);

    sourceData?.Load();
  }

  public bool GetFuelFromContainers(int playerFuelItems, ref int amountToAdd, out List<FuelToCommit> fuelCommits)
  {
    fuelCommits = new List<FuelToCommit>();

    if (playerFuelItems >= amountToAdd || !PowerSystemConfig.PowerSource_AllowNearbyFuelingWithEitr.Value || _NearByContainers.Count <= 0) return false;

    foreach (var nearByContainer in _NearByContainers)
    {
      if (amountToAdd <= 0) break;
      var inventory = nearByContainer.GetInventory();
      if (inventory == null) continue;
      var quantity = inventory.CountItems(EitrInventoryItem_TokenId);
      if (quantity > 0)
      {
        var itemsToUse = Mathf.Min(quantity, amountToAdd);
        amountToAdd -= itemsToUse;

        fuelCommits.Add(new FuelToCommit
        {
          pendingContainerAmount = itemsToUse,
          container = nearByContainer
        });
      }
    }

    if (amountToAdd > 0)
    {
      fuelCommits.Clear();
      return false;
    }

    return true;
  }

  public string GetMessageAboutFuel(int addedFromPlayer, int addedFromContainer)
  {
    var message = "";
    if (addedFromContainer > 0)
    {
      message += $"{ModTranslations.PowerSource_Message_AddedFromContainer} ({addedFromContainer}) ({ModTranslations.PowerSource_FuelNameEitr})";
    }

    if (addedFromPlayer > 0)
    {
      message += $"{ModTranslations.PowerSource_Message_AddedFromPlayer} ({addedFromPlayer}) \n({ModTranslations.PowerSource_FuelNameEitr})";
    }

    return message;
  }

  /// <summary>
  /// We have to sync with server in order to not get a mismatch in data. This ensures the removal can happen after the server returns a response.
  /// </summary>
  /// <param name="user"></param>
  /// <param name="amountToAdd"></param>
  /// <param name="canRemove"></param>
  /// <returns></returns>
  public bool TryAddFuel(Humanoid user, int amountToAdd, bool canRemove = false)
  {
    if (PowerSystemConfig.PowerSource_AllowNearbyFuelingWithEitr.Value && Time.fixedTime > _lastSearchTime + _NearbyChestSearchDebounce)
    {
      _lastSearchTime = Time.fixedTime;
      UpdateNearbyChests();
    }
    var message = "";
    var originalAmountToAddForPlayer = amountToAdd;
    if (!user.IsPlayer()) return false;
    var player = user.GetComponent<Player>();

    List<FuelToCommit> fuelCommits = new();
    var fuelCommitId = Guid.NewGuid().ToString();

    var playerInventory = player.GetInventory();
    var playerFuelItems = playerInventory.CountItemsByName([EitrInventoryItem_TokenId]);

    var currentRemainingAmount = amountToAdd - playerFuelItems;
    var containerCommitAmount = currentRemainingAmount;

    var containerFuelItems = GetFuelFromContainers(playerFuelItems, ref currentRemainingAmount, out var containerFuelToCommit);

    if (!containerFuelItems && currentRemainingAmount > 0)
    {
      return false;
    }

    var addedMessage = GetMessageAboutFuel(Mathf.Min(playerFuelItems, originalAmountToAddForPlayer), containerCommitAmount);
    // todo translate this.

    fuelCommits.AddRange(containerFuelToCommit);
    fuelCommits.Add(new FuelToCommit
    {
      pendingPlayerAmount = Mathf.Min(playerFuelItems, amountToAdd),
      player = player
    });

    var netView = GetComponent<ZNetView>();
    if (!netView) return false;

    // fuel promise in-order to update fuel without causing upstream desync which deletes the fuel.
    PendingFuelPromisesResolutions[fuelCommitId] = true;
    PendingFuelPromises[fuelCommitId] = StartCoroutine(WaitForFuelToCommit(fuelCommits, fuelCommitId, addedMessage, _powerSourceComponent.Data));
    PowerSystemRPC.Request_AddFuelToSource(netView.GetZDO().m_uid, amountToAdd, fuelCommitId);

    return true;
  }

  public bool PowerSourceInteract(Humanoid user, bool hold, bool alt)
  {
    if (!HasPowerSource) return false;
    if (!alt)
    {
      TryAddFuel(user, AddOneCount);
      return true;
    }

    if (alt)
    {
      TryAddFuel(user, AddManyCount);
      return true;
    }

    return false;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (PowerSourceInteract(user, hold, alt))
      return true;

    return false;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void GetHoldText()
  {

  }

  /// <summary>
  /// Todo consider moving this to a static helper elsewhere.
  /// </summary>
  /// <param name="PowerConduit"></param>
  /// <returns></returns>
  public static string GetPowerConduitHoverText(PowerConduitData PowerConduit)
  {
    var baseString = PowerConduit.Mode == PowerConduitMode.Charge ? ModTranslations.PowerConduit_ChargePlate_Name : ModTranslations.PowerConduit_DrainPlate_Name;

    var stateText = PowerNetworkController.GetDrainMechanismActivationStatus(PowerConduit.IsActive, PowerConduit.HasPlayersWithEitr);
    baseString += "\n";
    baseString += $"[{stateText}]";

    return baseString;
  }


  public string GetHoverText()
  {
    var outString = "";
    if (HasPowerSource)
    {
      outString += $"{ModTranslations.PowerSource_Interact_AddOne}\n{ModTranslations.PowerSource_Interact_AddMany}\n";
      outString += $"{ModTranslations.Power_NetworkInfo_NetworkFuel}: {MathUtils.RoundToHundredth(_powerSourceComponent.Data.Fuel)}/{_powerSourceComponent.Data.FuelCapacity}";
    }
    if (HasPowerStorage)
    {
      if (outString != "")
      {
        outString += "\n";
      }
      outString += $"{ModTranslations.Power_NetworkInfo_NetworkPowerCapacity}: {MathUtils.RoundToHundredth(_powerStorageComponent.Data.Energy)}/{_powerStorageComponent.Data.EnergyCapacity}";
    }

    // Only need networkId from either of these.
    if (HasPowerStorage)
    {
      outString += "\n";
      outString += PowerNetworkController.GetNetworkPowerStatusString(_powerStorageComponent.NetworkId);
    }
    else if (HasPowerSource)
    {
      outString += "\nNetwork Data";
      outString += PowerNetworkController.GetNetworkPowerStatusString(_powerSourceComponent.NetworkId);
    }

    return outString;
  }

  /// <summary>
  /// Ignore this only for item hovering which cannot be done...yet.
  /// </summary>
  /// <returns></returns>
  public string GetHoverName()
  {
    return "Power Source";
  }
}