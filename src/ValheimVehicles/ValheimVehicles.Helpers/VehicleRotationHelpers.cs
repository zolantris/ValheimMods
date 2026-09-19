using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Patches;

namespace ValheimVehicles.Helpers;

public class VehicleRotationHelpers
{
  /// <summary>
  /// Uses RelativeEuler but allows Vector3 shorthand.
  /// </summary>
  /// <remarks>
  /// Must be a separate function name rather than an overload because
  /// Harmony errors with the current setup.
  /// </remarks>
  public static Quaternion RelativeEulerFromVector(Vector3 eulerAngles)
  {
    return RelativeEuler(
      eulerAngles.x,
      eulerAngles.y,
      eulerAngles.z);
  }

  /// <summary>
  /// Creates an Euler rotation and applies the current vehicle-relative
  /// rotation when placement is occurring on a vehicle.
  /// </summary>
  public static Quaternion RelativeEuler(float x, float y, float z)
  {
    return ApplyVehicleRelativeRotation(
      Quaternion.Euler(x, y, z));
  }

  /// <summary>
  /// Applies vehicle-relative rotation to an already-created Quaternion.
  ///
  /// This exists separately from RelativeEuler so Harmony transpilers can
  /// preserve vanilla Quaternion.Euler calls and transform their result
  /// afterward.
  /// </summary>
  public static Quaternion ApplyVehicleRelativeRotation(
    Quaternion rotation)
  {
    if (!PatchSharedData.PlayerLastRayPiece)
      return rotation;

    var vehiclePiecesController =
      PatchSharedData.PlayerLastRayPiece
        .GetComponentInParent<VehiclePiecesController>();

    if (vehiclePiecesController)
    {
      return vehiclePiecesController.transform.rotation * rotation;
    }

    return rotation;
  }
}