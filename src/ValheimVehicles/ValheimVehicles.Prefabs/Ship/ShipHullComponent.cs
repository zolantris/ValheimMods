using System;
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
public class ShipHullComponent : MonoBehaviour
{
  private WaterVehicleController _waterVehicleController;
  private ValheimShip _shipInstance;

  private void GetShipZDO(ZNetView netView)
  {
    // netView.m_zdo.GetInt();
  }

  // private void OnDestroy()
  // {
  //   if ((bool)_waterVehicleController)
  //   {
  //     Destroy(_waterVehicleController);
  //   }
  // }

  /*
   * Most initialization logic can be called within Start as Awake will actually trigger when the vehicle ghost appears
   */
  private void Awake()
  {
    Logger.LogDebug("ShipHullPrefabComponent.Awake() called");
    _waterVehicleController = GetComponentInParent<WaterVehicleController>();

    Logger.LogDebug($"WaterVehicleController GETTER {_waterVehicleController}");

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

    // _waterVehicleController =
    /**
     * Skip ship initiation if the hull is already part of the ship
     */
    _shipInstance = gameObject.GetComponentInParent<ValheimShip>();
    if (!(bool)_shipInstance)
    {
      _shipInstance =
        // Makes the ship instance exist outside of the hull component to prevent issues
        // Adds the ship globally to the ValheimRaftPluginGO for now
        // TODO to make a VehicleComponent gameobject that is responsible for initializing all Vehicles
        gameObject.AddComponent<ValheimShip>();
      _shipInstance.transform.SetParent(null);


      // Adds the hull to the baseVehicle
      var waterVehicleController = _shipInstance.GetComponent<WaterVehicleController>();

      if ((bool)waterVehicleController)
      {
        waterVehicleController.baseVehicle.AddNewPiece(GetComponent<ZNetView>());
      }


      // var baseVehicle = _shipInstance.GetComponent<BaseVehicle>();
      // transform.SetParent(baseVehicle.transform);
    }
    // var waterVehicleNetView = waterVehicle.Init();

    // waterVehicleNetView.m_zdo.GetZDOID();
    // _waterVehicleController.transform.SetParent(null);
    // transform.SetParent(_waterVehicleController.transform);
    //
    // Logger.LogDebug($"Re-parented the watervehicle controller {_waterVehicleController}");
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