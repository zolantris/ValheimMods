using UnityEngine;

namespace ValheimVehicles.Vehicles;

public interface IVehicleProperties
{
  public BoxCollider FloatCollider { get; set; }
}