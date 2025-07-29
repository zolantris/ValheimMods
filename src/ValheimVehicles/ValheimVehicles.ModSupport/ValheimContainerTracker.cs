using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.ModSupport;

public static class ValheimContainerTracker
{
  private static readonly HashSet<Container> _activeContainers = new();
  private static float lastSanitize = 0f;
  private static float sanitizeInterval = 5f;
  public static HashSet<Container> ActiveContainers => _activeContainers;
  public static Action<Container>? OnContainerAddSubscriptions = null;
  public static Action<Container>? OnContainerRemoveSubscriptions = null;

  public static void AddContainer(Container container)
  {
    _activeContainers.Add(container);
    SanitizeContainers();

    // actions
    OnContainerAddSubscriptions?.Invoke(container);
  }

  public static void RemoveContainer(Container container)
  {
    _activeContainers.Remove(container);
    SanitizeContainers();

    // actions
    OnContainerRemoveSubscriptions?.Invoke(container);
  }

  private static void SanitizeContainers()
  {
    try
    {
      if (Time.fixedTime > lastSanitize + sanitizeInterval)
      {
        lastSanitize = Time.fixedTime;
        _activeContainers.RemoveWhere(x => x == null);
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error sanitizing containers: {e}");
    }
  }

  /// <summary>
  /// Removes container items by Name and returns number removed.
  /// </summary>
  public static int RemoveItemsByName(Inventory inventory, string tokenId, int amount)
  {
    var removed = 0;

    try
    {
      while (amount > 0)
      {
        var item = inventory.GetItem(tokenId);
        if (item == null) break; // No more items to remove
        var take = Mathf.Min(item.m_stack, amount);
        if (take <= 0) break;
        if (inventory.RemoveItem(item, take))
        {
          removed += take;
          amount -= take;
        }
        else
        {
          break; // Defensive, should never fail here, but...
        }
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error removing items by name: {e}");
    }
    return removed;
  }
}