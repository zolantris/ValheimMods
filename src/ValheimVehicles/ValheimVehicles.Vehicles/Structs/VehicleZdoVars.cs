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

  public static readonly int VehicleMovingPiece =
    "VehicleMovingPiece".GetStableHashCode();

  public static readonly int VehicleMovingPieceOffsetHash =
    "VehicleMovingPieceOffsetHash".GetStableHashCode();

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

  public static readonly KeyValuePair<int, int> MBParentHash =
    ZDO.GetHashZDOID("MBParent");

  public static readonly int MBParentIdHash = "MBParentId".GetStableHashCode();

  public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

  public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MBRotationVecHash =
    "MBRotationVec".GetStableHashCode();

  public static readonly int MBPieceCount = "MBPieceCount".GetStableHashCode();

  public const string VehicleParentIdHash = "VehicleParentIdHash";
  public const string VehicleParentId = "VehicleParentId";
}