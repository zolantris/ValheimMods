using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

/// <summary>
/// A collider delgate componennt meant to send events upwards
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WaterMaskZoneColliderComponent : MonoBehaviour
{
  public WaterZoneController? controller = null!;

  private void Awake()
  {
    controller = GetComponentInParent<WaterZoneController>();
    if (controller == null)
    {
      Logger.LogWarning(
        "WaterMaskZone Requires a WaterZoneController as a parent");
    }
  }

  // private void OnTriggerEnter(Collider collider)
  // {
  //   controller?.OnTriggerEnterDelegate(collider);
  //   Logger.LogDebug("Item has entered cube area");
  // }
  //
  // private void OnTriggerExit(Collider collider)
  // {
  //   controller?.OnTriggerEnterDelegate(collider);
  //   Logger.LogDebug("Item has entered cube area");
  // }
}