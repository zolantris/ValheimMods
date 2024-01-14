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
  private WaterVehicleController _waterVehicleController;

  private void GetShipZDO(ZNetView netView)
  {
    // netView.m_zdo.GetInt();
  }

  private void Start()
  {
    Logger.LogDebug("called Awake for ShipHullPrefabComponent");
    _waterVehicleController = GetComponentInParent<WaterVehicleController>();

    Logger.LogDebug($"Awake parent {_waterVehicleController}");

    /*
     * early exits as this instance of water vehicle is not needed
     */
    if ((bool)_waterVehicleController)
    {
      return;
    }

    // var netView = GetComponent<ZNetView>();
    //
    // if (!netView)
    // {
    //   Logger.LogError("No netview provided to ShipHullPrefab, this is likely an error");
    //   // return;
    //   netView = gameObject.AddComponent<ZNetView>();
    // }

    _waterVehicleController = gameObject.AddComponent<WaterVehicleController>();
    // var waterVehicleNetView = waterVehicle.Init();

    // waterVehicleNetView.m_zdo.GetZDOID();
    _waterVehicleController.transform.SetParent(null);
    transform.SetParent(_waterVehicleController.transform);

    Logger.LogDebug($"Re-parented the watervehicle controller {_waterVehicleController}");
    // FirstTimeCreation();
  }

  // private void FirstTimeCreation()
  // {
  //   // if (m_baseRoot.GetPieceCount() != 0)
  //   // {
  //   //   return;
  //   // }
  //
  //   /*
  //    * @todo turn the original planks into a Prefab so boat floors can be larger
  //    */
  //   var floor = ZNetScene.instance.GetPrefab("wood_floor");
  //   for (var x = -1f; x < 1.01f; x += 2f)
  //   {
  //     for (var z = -2f; z < 2.01f; z += 2f)
  //     {
  //       var pt = base.transform.TransformPoint(new Vector3(x,
  //         ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
  //       var obj = Instantiate(floor, pt, transform.rotation);
  //       var netView = obj.GetComponent<ZNetView>();
  //       _waterVehicleController.baseVehicle.AddNewPiece(netView);
  //     }
  //   }
  // }
}