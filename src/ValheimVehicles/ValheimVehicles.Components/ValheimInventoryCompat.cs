using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Components;

/// <summary>
/// A workaround for an inventory patch done by some mods that can break ammo decrementing of a single value. First call seems to be thrown out when it should not be.
/// </summary>
public static class ValheimInventoryCompat
{
  public static void RemoveItemWithRemainder(Inventory inventory, string name, int amount, out int remainder)
  {
    foreach (var itemData in inventory.m_inventory)
    {
      if (itemData.m_shared.m_name == name && itemData.m_worldLevel >= Game.m_worldLevel)
      {
        var num = Mathf.Min(itemData.m_stack, amount);
        itemData.m_stack -= num;
        amount -= num;
        if (amount <= 0)
          break;
      }
    }
    remainder = Mathf.Max(0, amount);
    if (remainder > 0)
    {
      LoggerProvider.LogWarning("Did not remove all ammo requested.");
    }

    inventory.m_inventory.RemoveAll((Predicate<ItemDrop.ItemData>)(x => x.m_stack <= 0));
    inventory.Changed();
  }
}