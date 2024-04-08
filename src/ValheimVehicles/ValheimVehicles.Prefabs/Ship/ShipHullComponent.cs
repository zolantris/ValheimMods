using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Util;
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
  private static readonly int ShipZdoId = "ValheimVehicleShipId".GetStableHashCode();

  public int zdoParentId;

  private ZNetView? _netview;

  private ZNetView ShipHullNetView
  {
    get
    {
      if (_netview == null)
      {
        _netview = GetComponent<ZNetView>();
      }

      return _netview;
    }
  }

  private void Awake()
  {
    zdoParentId = GetParentZdoId();
  }

  public void SetParentZdoId(int parentZdoId)
  {
    if (ShipHullNetView == null)
    {
      Logger.LogError("NetView null for ShipHullComponent.");
      return;
    }

    ShipHullNetView.GetZDO().Set(ShipZdoId, parentZdoId);
    zdoParentId = parentZdoId;
  }

  public int GetParentZdoId()
  {
    if (ShipHullNetView == null)
    {
      return 0;
    }

    var zdo = ShipHullNetView.GetZDO();

    return zdo?.GetInt(ShipZdoId) ?? 0;
  }
}