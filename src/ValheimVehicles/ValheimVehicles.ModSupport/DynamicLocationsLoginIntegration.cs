using System.Collections;
using Jotunn;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.ValheimVehicles.ModSupport;

public class DynamicLocationsLoginIntegration : ModAp
{
  public IEnumerator OnLoginMoveToZDO()
  {
    throw new System.NotImplementedException();
  }

  public int LoginPrefabHashCode { get; } = ValheimVehicles.Prefabs.PrefabNames
    .WaterVehicleShip
    .GetHashCode();

  public bool IsLoginZdo(ZDO zdo)
  {
    throw new System.NotImplementedException();
  }

  // Internal Methods

  private VehicleShip? GetVehicleFromZdo(ZDO zdo)
  {
    if (zdo.GetPrefab() != ValheimVehicles.Prefabs.PrefabNames.WaterVehicleShip
          .GetHashCode())
    {
      Logger.LogDebug("VehicleShip not detected for Login ZDO");
      return null;
    }

    var vehicleShipNetView = ZNetScene.instance.FindInstance(zdo);
    if (!vehicleShipNetView) return null;
    var vehicleShip = vehicleShipNetView.GetComponent<VehicleShip>();
    return vehicleShip;
  }
}