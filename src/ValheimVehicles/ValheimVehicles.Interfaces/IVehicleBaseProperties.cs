#region

  using UnityEngine;

#endregion

  namespace ValheimVehicles.Interfaces;

public interface IVehicleBaseProperties : IVehicleSharedProperties

{
  public bool IsLandVehicle { get; }
  public BoxCollider? FloatCollider { get; }
  public Rigidbody? MovementControllerRigidbody { get; }
  public Transform ControlGuiPosition { get; set; }
  public int PersistentZdoId { get; }
}