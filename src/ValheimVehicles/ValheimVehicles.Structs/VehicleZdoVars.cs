using System.Collections.Generic;

namespace ValheimVehicles.Structs;

/// <summary>
/// ZdoVars do not need to be a hashcode. Using a Hashcode makes it harder to see what the ZDO value relates to as well. So for any future ZDOVars they should be strings.
/// </summary>
public static class VehicleZdoVars
{
  public const string ZdoKeyBaseVehicleInitState =
    "ValheimVehicles_BaseVehicle_Initialized";

  public const string ModVersion = "ValheimVehicles_ModVersion";

  public const string VehicleFloatationHeight = "ValheimVehicles_VehicleFloatationHeight";
  public const string VehicleFloatationCustomModeEnabled = "ValheimVehicles_VehicleFloatationCustomModeEnabled";

  public static string ToggleSwitchAction = "ValheimVehicles_ToggleSwitchAction";

  public static string CustomMeshId =
    "ValheimVehicles_CustomMesh";

  public static string CustomMeshScale = "ValheimVehicles_CustomMeshSize";

  public static string CustomMeshPrimitiveType =
    "ValheimVehicles_CustomMeshPrimitiveType";

  /// <summary>
  ///  for vehicles that could be both submarines or watervehicles or airships this should be a required key to force flags.
  /// </summary>
  ///
  /// TODO make custom vehicles which are flagged under the "All" variant.
  public static string CustomVehicleVariant = "ValheimVehicles_CustomVehicleVariant";

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

  public static readonly int MBParentId = "MBParentId".GetStableHashCode();

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

  // For Components that are within a Swivel
  public const string SwivelParentId = "SwivelParentId";

  // for swivels
  public const string SwivelSyncPosition = "SwivelSync_Position";
  public const string SwivelSyncRotation = "SwivelSync_Rotation";
  public const string SwivelSyncVelocity = "SwivelSync_Velocity";
  public const string SwivelSyncAngularVelocity = "SwivelSync_AngularVelocity";

  // for power systems
  public const string Power_StoredPower = "PowerSystem_StoredPower";
  public const string Power_IsRunning = "PowerSystem_IsRunning";
  public const string Power_StoredFuel = "PowerSystem_StoredFuel";
}