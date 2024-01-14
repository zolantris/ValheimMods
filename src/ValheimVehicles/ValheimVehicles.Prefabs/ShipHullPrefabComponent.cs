using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs;

/*
 * Prefab controller for ShipHull
 *
 * Determines if it needs to initialize a WaterVehicle or if it is already connected to WaterVehicle
 */
public class ShipHullPrefabComponent : MonoBehaviour
{
  private WaterVehicle _waterVehicle;

  private void GetShipZDO(ZNetView netView)
  {
    // netView.m_zdo.GetInt();
  }

  private void Awake()
  {
    Logger.LogDebug("called Awake for ShipHullPrefabComponent");
    _waterVehicle = GetComponentInParent<WaterVehicle>();

    /*
     * early exits as this instance of water vehicle is not needed
     */
    if ((bool)_waterVehicle)
    {
      return;
    }

    var netView = GetComponent<ZNetView>();

    if (!netView)
    {
      Logger.LogError("No netview provided to ShipHullPrefab, this is likely an error");
      // return;
      netView = gameObject.AddComponent<ZNetView>();
    }

    _waterVehicle = gameObject.AddComponent<WaterVehicle>();
    // var waterVehicleNetView = waterVehicle.Init();

    // waterVehicleNetView.m_zdo.GetZDOID();
    transform.SetParent(_waterVehicle.transform);
  }

  private void FirstTimeCreation()
  {
    // if (m_baseRoot.GetPieceCount() != 0)
    // {
    //   return;
    // }

    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    var floor = ZNetScene.instance.GetPrefab("wood_floor");
    for (var x = -1f; x < 1.01f; x += 2f)
    {
      for (var z = -2f; z < 2.01f; z += 2f)
      {
        var pt = base.transform.TransformPoint(new Vector3(x,
          ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
        var obj = Instantiate(floor, pt, transform.rotation);
        var netView = obj.GetComponent<ZNetView>();
        _waterVehicle.baseVehicle.AddNewPiece(netView);
      }
    }
  }
}