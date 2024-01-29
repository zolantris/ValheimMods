using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn;
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
  private static readonly KeyValuePair<int, int> ShipZdo = ZDO.GetHashZDOID("MBParent");
  private static readonly int ShipZdoId = "ValheimVehicleShipId".GetStableHashCode();

  private void Awake()
  {
    var nv = GetComponent<ZNetView>();

    if (nv == null)
    {
      return;
    }

    var zdo = nv.GetZDO();

    if (zdo == null)
    {
      return;
    }

    var zdoParent = zdo.GetZDOID(ShipZdo);
    var zdoParentId = zdo.GetInt(ShipZdoId);

    if (zdoParent != ZDOID.None || zdoParentId != 0)
    {
      return;
    }

    InitShip(nv);
  }

  private void InitShip(ZNetView netView)
  {
    var waterVehiclePrefab = PrefabController.WaterVehiclePrefab;

    waterVehiclePrefab.AddComponent<Transform>();
    /*
     * Sets the shipInstance to this current position/rotation, but the object will have no parent
     */
    var shipInstance =
      Instantiate(waterVehiclePrefab, null);
    shipInstance.transform.position = transform.position;
    shipInstance.transform.rotation = transform.rotation;

    if (shipInstance == null) return;

    var instanceController = shipInstance.GetComponentInChildren<WaterVehicleController>();

    if (instanceController == null) return;
    instanceController.AddNewPiece(netView);
    netView.GetZDO().Set(ShipZdoId, instanceController.PersistentZdoId);
  }
  // private WaterVehicleController _waterVehicleController;
  // private GameObject waterVehiclePrefabInstance;
  //
  // private void GetShipZDO(ZNetView netView)
  // {
  //   // netView.m_zdo.GetInt();
  // }
  //
  // // private void OnDestroy()
  // // {
  // //   if ((bool)_waterVehicleController)
  // //   {
  // //     Destroy(_waterVehicleController);
  // //   }
  // // }
  //
  // /*
  //  * Most initialization logic can be called within Start as Awake will actually trigger when the vehicle ghost appears
  //  */
  // private void Awake()
  // {
  //   Logger.LogDebug("ShipHullPrefabComponent.Awake() called");
  //   _waterVehicleController = GetComponentInParent<WaterVehicleController>();
  //
  //   Logger.LogDebug($"WaterVehicleController GETTER {_waterVehicleController}");
  //
  //   /*
  //    * early exits as this instance of water vehicle is not needed
  //    */
  //   if ((bool)_waterVehicleController || (bool)waterVehiclePrefabInstance)
  //   {
  //     // AddToVehicle();
  //     return;
  //   }
  //
  //   // var netView = GetComponent<ZNetView>();
  //   //
  //   // if (!netView)
  //   // {
  //   //   Logger.LogError("No netview provided to ShipHullPrefab, this is likely an error");
  //   //   // return;
  //   //   netView = gameObject.AddComponent<ZNetView>();
  //   // }
  //   waterVehiclePrefabInstance = Instantiate(PrefabController.GetWaterVehiclePrefab, null);
  //   waterVehiclePrefabInstance.transform.position = transform.position;
  //   waterVehiclePrefabInstance.transform.rotation = transform.rotation;
  //
  //   // FirstTimeCreation();
  //   // _waterVehicleController =
  //   /**
  //    * Skip ship initiation if the hull is already part of the ship
  //    */
  //
  //   // _vehicleShipInstance = gameObject.GetComponentInParent<VVShip>();
  //   // if ((bool)_vehicleShipInstance) return;
  //   // _vehicleShipInstance =
  //   //   // Makes the ship instance exist outside of the hull component to prevent issues
  //   //   // Adds the ship globally to the ValheimRaftPluginGO for now
  //   //   // TODO to make a VehicleComponent gameobject that is responsible for initializing all Vehicles
  //   //   gameObject.AddComponent<VVShip>();
  //   // _shipInstance.transform.SetParent(null);
  //
  //
  //   // var baseVehicle = _shipInstance.GetComponent<BaseVehicle>();
  //   // transform.SetParent(baseVehicle.transform);
  //   // var waterVehicleNetView = waterVehicle.Init();
  //
  //   // waterVehicleNetView.m_zdo.GetZDOID();
  //   // _waterVehicleController.transform.SetParent(null);
  //   // transform.SetParent(_waterVehicleController.transform);
  //   //
  //   // Logger.LogDebug($"Re-parented the watervehicle controller {_waterVehicleController}");
  //   // FirstTimeCreation();
  // }
  //
  // private void OnDestroy()
  // {
  //   if (waterVehiclePrefabInstance)
  //   {
  //     Logger.LogDebug($"called destroy of hull object, {_waterVehicleController.m_pieces.Count}");
  //     if (_waterVehicleController.m_hullPieces.Count == 0)
  //     {
  //       Destroy(_waterVehicleController.gameObject);
  //     }
  //     else
  //     {
  //       var zNetView = GetComponent<ZNetView>();
  //       _waterVehicleController.RemovePiece(zNetView);
  //     }
  //   }
  // }
  //
  // // private void AddToVehicle()
  // // {
  // //   _waterVehicleController =
  // //     waterVehiclePrefabInstance.GetComponent<WaterVehicleController>();
  // //
  // //   Logger.LogDebug($"AddToVehicle called, waterVehicleController {_waterVehicleController}");
  // //   if ((bool)_waterVehicleController)
  // //   {
  // //     var piece = GetComponent<Piece>();
  // //     if (!(bool)piece)
  // //     {
  // //       Logger.LogDebug("No netview for HullComponent");
  // //       return;
  // //     }
  // //
  // //     _waterVehicleController.AddNewPiece(piece);
  // //   }
  // // }
  //
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
  //       _waterVehicleController.AddNewPiece(netView);
  //     }
  //   }
  // }
}