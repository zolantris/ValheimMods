using UnityEngine;

namespace ValheimVehicles.Controllers;

public class VehicleBoundsController
{
  public static void AlignBoxColliderToMesh(BoxCollider boxCollider,
    MeshRenderer meshRenderer)
  {
    // Step 1: Get the mesh's world-space bounds
    var bounds =
      meshRenderer.bounds; // This is the axis-aligned world-space bounds

    // Step 2: Transform bounds center into local space
    var localCenter =
      boxCollider.transform.InverseTransformPoint(bounds.center);

    // Step 3: Adjust for the lossy scale
    var localSize = Vector3.Scale(bounds.size,
      InvertVector(boxCollider.transform.lossyScale));

    // Step 4: Apply the local center and size to the BoxCollider
    boxCollider.center = localCenter;
    boxCollider.size = localSize;
  }

  private static Vector3 InvertVector(Vector3 v)
  {
    return new Vector3(1 / v.x, 1 / v.y, 1 / v.z);
  }
}