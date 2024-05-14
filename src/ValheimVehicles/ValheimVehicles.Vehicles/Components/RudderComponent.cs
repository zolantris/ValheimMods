using UnityEngine;

namespace ValheimVehicles.Vehicles.Components;

public class RudderComponent : MonoBehaviour
{
  public Transform PivotPoint;
  public float maxRotation = 45;
  public float minRotation = -45;
  public float initialRotation = 0;
}