using System;
using UnityEngine;

namespace ValheimVehicles.Vehicles.Components;

/**
 * @description Valheim Requires a Ghost placeholder that must be shown to place an item. This component is required to show and place a vehicle.
 */
public class VehicleBuildGhost : MonoBehaviour
{
  private GameObject? _placeholderInstance;
  public GameObject? placeholderComponent;
  public static readonly int IsVehicleInitialized = "IsVehicleInitialized".GetStableHashCode();

  public GameObject? GetPlaceholderInstance()
  {
    return _placeholderInstance;
  }

  public void DisableVehicleGhost()
  {
    var netView = GetComponent<ZNetView>();
    netView.GetZDO().Set(IsVehicleInitialized, true);

    if (_placeholderInstance != null && _placeholderInstance.name.Contains("Cube"))
    {
      Destroy(_placeholderInstance);
    }
    else
    {
      _placeholderInstance = null;
    }
  }

  private static GameObject CreateCubePrimitive()
  {
    return GameObject.CreatePrimitive(PrimitiveType.Cube);
  }

  private bool IsInitialized()
  {
    var netView = GetComponent<ZNetView>();
    if (!(bool)netView) return false;
    var zdo = netView.GetZDO();
    if (zdo == null) return false;
    return zdo.GetBool(IsVehicleInitialized);
  }

  public void UpdatePlaceholder()
  {
    if (IsInitialized())
    {
      if (_placeholderInstance != null)
      {
        Destroy(_placeholderInstance);
      }

      placeholderComponent = null;
      _placeholderInstance = null;
      return;
    }

    if (_placeholderInstance != null)
    {
      Destroy(_placeholderInstance);
      _placeholderInstance = null;
    }

    if (placeholderComponent == null)
    {
      return;
    }

    _placeholderInstance = Instantiate(placeholderComponent, transform);
  }
}