using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ValheimVehicles.Controllers;

public class VehicleObject
{
  public string itemId;
  public string prefabName;
  public float prefabHealth;
  public float prefabState;
}

public class BaseVehicleItemStorage
{
  public string vehicleId;
  public string vehicleName;
  public Dictionary<string, VehicleObject> registeredItems = new();

  public void setItemRegistry(VehicleObject item)
  {
    registeredItems.Add(item.itemId, item);
  }

  public List<KeyValuePair<string, VehicleObject>> GetVehicleItems()
  {
    return registeredItems.ToList();
  }
}