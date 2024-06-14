using System.Collections.Generic;

namespace ValheimVehicles.Vehicles;

public static class VehicleZdoVars
{
  public const string ZdoKeyBaseVehicleInitState =
    "ValheimVehicles_BaseVehicle_Initialized";

  public static readonly int VehicleFlags = "VehicleFlags".GetStableHashCode();
  public static readonly int VehicleTargetHeight = "VehicleTargetHeight".GetStableHashCode();
  public static readonly int VehicleOceanSway = "VehicleOceanSway".GetStableHashCode();

  public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("MBParent");

  public static readonly int MBCharacterParentHash = "MBCharacterParent".GetStableHashCode();

  public static readonly int MBCharacterOffsetHash = "MBCharacterOffset".GetStableHashCode();

  public static readonly int MBParentIdHash = "MBParentId".GetStableHashCode();

  public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

  public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MBRotationVecHash = "MBRotationVec".GetStableHashCode();

  public static readonly int MBPieceCount = "MBPieceCount".GetStableHashCode();
}