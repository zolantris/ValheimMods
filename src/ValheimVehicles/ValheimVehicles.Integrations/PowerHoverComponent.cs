using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
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

  public bool TryAddFuel(Humanoid user, int amountToAdd)
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

    var playerInventory = player.GetInventory();
    var playerFuelItems = playerInventory.CountItemsByName([EitrInventoryItem_TokenId]);
    if (playerFuelItems < amountToAdd)
    {
      if (PowerSystemConfig.PowerSource_AllowNearbyFuelingWithEitr.Value && _NearByContainers.Count > 0)
      {
        var originalAmountForContainers = amountToAdd;
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
        if (amountToAdd != originalAmountForContainers)
        {
          message += $"{ModTranslations.PowerSource_Message_AddedFromContainer} ({originalAmountForContainers - amountToAdd}) ({ModTranslations.PowerSource_FuelNameEitr})";
        }
        if (amountToAdd < 0)
        {
          player.Message(MessageHud.MessageType.TopLeft, message);
          return true;
        }
      }

      if (amountToAdd > 0 && playerFuelItems < amountToAdd)
      {
        player.Message(MessageHud.MessageType.Center, $"{ModTranslations.PowerSource_NotEnoughFuel} \n({ModTranslations.PowerSource_FuelNameEitr})");
        return false;
      }
    }

    try
    {
      message += $"{ModTranslations.PowerSource_Message_AddedFromPlayer} ({originalAmountToAddForPlayer}) \n({ModTranslations.PowerSource_FuelNameEitr})";

      playerInventory.RemoveItem(EitrInventoryItem_TokenId, amountToAdd);
      _powerSourceComponent.AddFuelOrRPC(amountToAdd);
      player.Message(MessageHud.MessageType.TopLeft, message);
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
    var baseString = $"{ModTranslations.PowerConduit_DrainPlate_Name}";

    if (PowerNetworkController.CanShowNetworkData || PowerSystemConfig.PowerNetwork_ShowAdditionalPowerInformationByDefault.Value)
    {
      var stateText = PowerNetworkController.GetDrainMechanismActivationStatus(PowerConduit.IsActive, PowerConduit.HasPlayersWithEitr);
      baseString += "\n";
      baseString += $"[{stateText}]";
    }

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