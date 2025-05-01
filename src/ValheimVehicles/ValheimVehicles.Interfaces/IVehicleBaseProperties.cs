using UnityEngine;
namespace ValheimVehicles.Interfaces;

public interface IVehicleBaseProperties : IVehicleControllers

{
  public bool IsLandVehicle { get; }
  public BoxCollider? FloatCollider { get; }
  public Rigidbody? MovementControllerRigidbody { get; }
  public Transform ControlGuiPosition { get; set; }
  public ZNetView? NetView { get; }
  public int PersistentZdoId { get; }
}