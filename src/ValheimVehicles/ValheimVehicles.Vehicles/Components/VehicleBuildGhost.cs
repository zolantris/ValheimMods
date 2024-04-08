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
  private bool _isInitialized = false;

  public GameObject? GetPlaceholderInstance()
  {
    return _placeholderInstance;
  }

  private void Awake()
  {
    GetIsInitialized();
    ShouldRenderPlaceholder();
  }

  private void Start()
  {
    GetIsInitialized();
    ShouldRenderPlaceholder();
  }

  private void OnEnable()
  {
    GetIsInitialized();
    ShouldRenderPlaceholder();
  }

  public void DisableVehicleGhost()
  {
    var netView = GetComponent<ZNetView>();
    netView.GetZDO().Set(IsVehicleInitialized, true);
    _isInitialized = true;
    if (_placeholderInstance != null && _placeholderInstance.name.Contains("Cube"))
    {
      Destroy(_placeholderInstance);
    }
    else
    {
      DestroyGhostPiece();
    }
  }

  private static GameObject CreateCubePrimitive()
  {
    return GameObject.CreatePrimitive(PrimitiveType.Cube);
  }

  private void OnInitialized()
  {
    if (!_isInitialized) return;

    if ((bool)_placeholderInstance)
    {
      DestroyGhostPiece();
    }

    placeholderComponent = null;
    _placeholderInstance = null;
  }

  private bool GetIsInitialized()
  {
    if (_isInitialized) return true;

    var netView = GetComponent<ZNetView>();
    if (!(bool)netView) return false;
    var zdo = netView.GetZDO();
    if (zdo == null) return false;
    _isInitialized = zdo.GetBool(IsVehicleInitialized);
    return _isInitialized;
  }

  private void DestroyGhostPiece()
  {
    if (!_placeholderInstance) return;
    var wnt = _placeholderInstance.GetComponent<WearNTear>();

    if ((bool)wnt)
      wnt.Destroy();
    else if (_placeholderInstance)
    {
      Destroy(_placeholderInstance);
    }
  }

  public void UpdatePlaceholder()
  {
    if ((bool)_placeholderInstance)
    {
      DestroyGhostPiece();
      _placeholderInstance = null;
    }

    if (!(bool)placeholderComponent)
    {
      return;
    }

    _placeholderInstance =
      Instantiate(placeholderComponent, transform);
  }

  public void ShouldRenderPlaceholder()
  {
    if (GetIsInitialized())
    {
      OnInitialized();
    }
  }
}