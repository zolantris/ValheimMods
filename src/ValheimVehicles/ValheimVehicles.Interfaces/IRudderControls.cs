using UnityEngine;

namespace ValheimVehicles.Interfaces;

public interface IRudderControls
{
  public string m_hoverText { get; set; }

  public IVehicleShip ShipInstance { get; }

  public float MaxUseRange { get; set; }

  // might be safer to directly make this a getter
  public Transform AttachPoint { get; set; }
}