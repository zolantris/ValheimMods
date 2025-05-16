using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerHoverComponent : MonoBehaviour, Hoverable, Interactable
{
  private PowerSourceComponentIntegration _powerSourceComponent;
  private PowerStorageComponentIntegration _powerStorageComponent;
  private bool HasPowerStorage = false;
  private bool HasPowerSource = false;
  public static int AddManyCount = 10;
  public static int AddOneCount = 1;

  public void Start()
  {
    _powerSourceComponent = GetComponent<PowerSourceComponentIntegration>();
    _powerStorageComponent = GetComponent<PowerStorageComponentIntegration>();

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

  public bool TryAddFuel(Humanoid user, int amountToAdd)
  {
#if DEBUG
    if (Time.fixedTime > _lastSearchTime + _NearbyChestSearchDebounce)
    {
      _lastSearchTime = Time.fixedTime;
      UpdateNearbyChests();
    }
    var originalAmount = amountToAdd;
    if (!user.IsPlayer()) return false;
    var player = user.GetComponent<Player>();

    if (_NearByContainers.Count > 0)
    {
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
          inventory.RemoveItem(EitrInventoryItem_TokenId, itemsToUse);
          _powerSourceComponent.AddFuelOrRPC(itemsToUse);
        }
      }
      if (amountToAdd < 0)
      {
        player.Message(MessageHud.MessageType.Center, $"{ModTranslations.Interact_AddedFromContainer} ({originalAmount}) ({ModTranslations.PowerSource_FuelNameEitr})");
        return true;
      }
    }
#endif
    var playerInventory = player.GetInventory();
    var items = playerInventory.CountItemsByName([EitrInventoryItem_TokenId]);
    if (items < amountToAdd)
    {
      player.Message(MessageHud.MessageType.Center, $"{ModTranslations.PowerSource_NotEnoughFuel} \n({ModTranslations.PowerSource_FuelNameEitr})");
      return false;
    }

    try
    {

      playerInventory.RemoveItem(EitrInventoryItem_TokenId, amountToAdd);
      _powerSourceComponent.AddFuelOrRPC(amountToAdd);
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error when removing {EitrInventoryItem_TokenId} from inventory, \n {e.Message} \n {e.StackTrace}");
    }

    return true;
  }

  public bool PowerSourceInteract(Humanoid user, bool hold, bool alt)
  {
    if (!HasPowerSource) return false;
    if (!hold)
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


  public string GetHoverText()
  {
    var outString = "";
    if (HasPowerSource)
    {
      outString += $"Power Source: {MathUtils.RoundToHundredth(_powerSourceComponent.GetFuelLevel())}/{_powerSourceComponent.GetFuelCapacity()}\n";
    }
    if (HasPowerStorage)
    {
      outString += $"Power Storage: {MathUtils.RoundToHundredth(_powerStorageComponent.ChargeLevel)}/{_powerStorageComponent.Capacity}";
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
  public string GetHoverName()
  {
    return "Power Source";
  }
}