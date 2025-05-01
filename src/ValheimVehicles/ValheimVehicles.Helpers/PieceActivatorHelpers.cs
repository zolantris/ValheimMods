using UnityEngine;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.Helpers;

public static class PieceActivatorHelpers
{
  /// <summary>
  /// Gets the RaycastPieceActivator which is used for Swivels and VehiclePiecesController components. These components are responsible for activation and parenting of vehicle pieces and will always exist above the current piece in transform hierarchy.
  /// </summary>
  public static IRaycastPieceActivator? GetRaycastPieceActivator(
    Transform obj)
  {
    var pieceActivator = obj.GetComponentInParent<IRaycastPieceActivator>();
    return pieceActivator;
  }

  public static IRaycastPieceActivator? GetRaycastPieceActivator(
    GameObject obj)
  {
    var pieceActivator = obj.GetComponentInParent<IRaycastPieceActivator>();
    return pieceActivator;
  }

  /// <summary>
  /// Gets the activator host. If it exists.
  /// </summary>
  public static IPieceActivatorHost? GetPieceActivatorHost(
    GameObject obj)
  {
    var activatorHost = obj.GetComponentInParent<IPieceActivatorHost>();
    return activatorHost;
  }
}