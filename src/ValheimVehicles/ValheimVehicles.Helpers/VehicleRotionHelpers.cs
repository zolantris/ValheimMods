using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Helpers;

public class VehicleRotionHelpers
{
  /// <summary>
  /// Uses RelativeEuler but allows Vector3 shorthand
  /// </summary>
  /// must be a separate function name (not an overload) otherwise Harmony errors with current setup
  /// <param name="eulerAngles"></param>
  /// <returns></returns>
  public static Quaternion RelativeEulerFromVector(Vector3 eulerAngles)
  {
    var x = eulerAngles.x;
    var y = eulerAngles.y;
    var z = eulerAngles.z;
    return RelativeEuler(x, y, z);
  }


  /// <summary>
  /// Relative rotation based on the boat
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <param name="z"></param>
  /// <returns></returns>
  public static Quaternion RelativeEuler(float x, float y, float z)
  {
    var rot = Quaternion.Euler(x, y, z);
    if (!PatchSharedData.PlayerLastRayPiece) return rot;

    var bvc = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<VehiclePiecesController>();
    if (bvc)
    {
      return bvc.transform.rotation * rot;
    }

    if (!ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value) return rot;

    var mbr = PatchSharedData.PlayerLastRayPiece
      .GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      return mbr.transform.rotation * rot;
    }

    return rot;
  }
}