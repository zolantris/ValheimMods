using System.Collections.Generic;
using UnityEngine;
namespace ValheimVehicles.ValheimVehicles.Vehicles.Structs;

public struct ActivationPieceData
{
  // list of frozen rigidbodies that were non-kinematic;
  private List<Rigidbody> frozenRigidbodies;
  public List<Rigidbody> rigidbodies;
  // public List<Rigidbody> rigibodies;
  public ZNetView netView;
  public GameObject gameObject;
  public int vehicleId;
  public Vector3 localPosition;

  public ActivationPieceData(ZNetView netView, int vehicleId, Vector3 localPosition)
  {
    this.netView = netView;
    this.vehicleId = vehicleId;
    this.localPosition = localPosition;
    rigidbodies = [];
    frozenRigidbodies = [];

    netView.GetComponentsInChildren(true, rigidbodies);

    var wnt = netView.GetComponent<WearNTear>();
    if (wnt) wnt.enabled = false;

    gameObject = netView.gameObject;
  }

  public void UnFreezeRigidbodies()
  {
    if (rigidbodies.Count <= 0) return;

    foreach (var rigidbody in frozenRigidbodies)
    {
      if (rigidbody == null) continue;
      rigidbody.isKinematic = false;
    }

    frozenRigidbodies.Clear();
  }

  /// <summary>
  /// Freezes a kinematic piece influence by Gravity so that it does not move until it is placed on the vehicle. These pieces will always be unfrozen whenever the vehicle is activated.
  /// </summary>
  public void FreezeRigidbodies()
  {
    if (rigidbodies.Count <= 0) return;
    foreach (var rigidbody in rigidbodies)
    {
      if (rigidbody == null) continue;
      if (!rigidbody.isKinematic)
      {
        if (!frozenRigidbodies.Contains(rigidbody))
        {
          frozenRigidbodies.Add(rigidbody);

        }
        rigidbody.isKinematic = true;
      }
    }
  }
}