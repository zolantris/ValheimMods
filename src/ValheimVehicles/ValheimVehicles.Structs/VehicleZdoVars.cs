using System.Collections.Generic;

namespace ValheimVehicles.Vehicles;

public static class VehicleZdoVars
{
  public const string ZdoKeyBaseVehicleInitState =
    "ValheimVehicles_BaseVehicle_Initialized";

  public static string CustomMeshId =
    "ValheimVehicles_CustomMesh";

  public static string CustomMeshScale = "ValheimVehicles_CustomMeshSize";

  public static string CustomMeshPrimitiveType =
    "ValheimVehicles_CustomMeshPrimitiveType";

  public static string IsLandVehicle = "ValheimVehicles_IsLandVehicle";

  public static readonly int MBCultivatableParentIdHash =
    "MBCultivatableParentId".GetStableHashCode();

  // todo ZDO.GetHashZDOID is likely deprecated.
  public static readonly KeyValuePair<int, int> MBCultivatableParentHash =
    ZDO.GetHashZDOID("MBCultivatableParent");

  public static readonly int TempPieceParentId =
    "VehicleTempPieceParentId".GetStableHashCode();

  // todo remove this flag set as it is deprecated
  public static readonly int DEPRECATED_VehicleFlags =
    "VehicleFlags".GetStableHashCode();

  public static readonly string VehicleAnchorState = "VehicleAnchorState";

  public static readonly int VehicleTargetHeight =
    "VehicleTargetHeight".GetStableHashCode();

  public static readonly int VehicleOceanSway =
    "VehicleOceanSway".GetStableHashCode();

  public static readonly int VehicleTreadWidth =
    "VehicleTreadWidth".GetStableHashCode();


  // todo ZDO.GetHashZDOID is likely deprecated.
  public static readonly KeyValuePair<int, int> MBParentHash =
    ZDO.GetHashZDOID("MBParent");

  public static readonly int MBParentIdHash = "MBParentId".GetStableHashCode();

  /// <summary>
  /// This is the main positional hash for an object within the vehicle. This position is relative to the vehicle and coordinates are in local position after parented within the vehicle.
  /// </summary>
  public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

  /// <summary>
  /// This is the local rotation hash for an object within the vehicle. This rotation is relative to the vehicle and coordinates are in local rotation after parented within the vehicle.
  /// </summary>
  public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MBRotationVecHash =
    "MBRotationVec".GetStableHashCode();

  public static readonly int MBPieceCount = "MBPieceCount".GetStableHashCode();

  public const string VehicleParentIdHash = "VehicleParentIdHash";
  public const string VehicleParentId = "VehicleParentId";
}