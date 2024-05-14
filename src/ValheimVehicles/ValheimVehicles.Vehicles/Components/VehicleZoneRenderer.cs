using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.Vehicles.Components;

/**
 * A singleton that registers within the base-game ZoneController and controls all ship rendering.
 */
public class VehicleZoneRenderer : MonoBehaviour
{
  public static List<ZoneSystem> ActiveZones = [];
  public static VehicleZoneRenderer Instance;

  private void Awake()
  {
    Instance = this;
  }
}