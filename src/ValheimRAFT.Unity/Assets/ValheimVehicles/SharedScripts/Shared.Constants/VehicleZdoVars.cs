// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#if !TEST && !UNITY_2022
using System.Collections.Generic;
#endif
namespace ValheimVehicles.Shared.Constants
{

#if UNITY_2022
  public static class StringExtensions {
    public static int GetStableHashCode(this string str) => str.GetHashCode();
  }
#endif
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

    public const string ToggleSwitchAction = "ValheimVehicles_ToggleSwitchAction";

    public const string RopeConnections = "MBRopeAnchor_Ropes"; // connection points for the rope prefabs

    // For Components that are within a Swivel
    public const string SwivelParentId = "SwivelParentId";
    public const string Mechanism_Swivel_TargetId = "Mechanism_swivelTargetId";

    // for swivels
    public const string SwivelSyncPosition = "SwivelSync_Position";
    public const string SwivelSyncRotation = "SwivelSync_Rotation";
    public const string SwivelSyncVelocity = "SwivelSync_Velocity";
    public const string SwivelSyncAngularVelocity = "SwivelSync_AngularVelocity";

    // for power systems
    public const string PowerSystem_IsActive = "PowerSystem_IsActive";
    public const string PowerSystem_NetworkId = "PowerSystem_NetworkId";
    public const string PowerSystem_Energy = "PowerSystem_StoredEnergy";
    public const string PowerSystem_Fuel = "PowerSystem_StoredFuel";
    public const string PowerSystem_FuelOutputRate = "PowerSystem_FuelOutputRate";
    public const string PowerSystem_FuelType = "PowerSystem_FuelType";
    public const string PowerSystem_EnergyCapacity = "PowerSystem_StoredEnergyCapacity";
    public const string PowerSystem_StoredFuelCapacity = "PowerSystem_StoredFuelCapacity";

    // power system booleans
    public const string PowerSystem_IsForceDeactivated = "PowerSystem_ForceDeactivated"; // generators/storage
    public const string PowerSystem_IsRunning = "PowerSystem_IsRunning"; // generators

    public const string PowerSystem_IsDemanding = "PowerSystem_IsDemanding"; // consumers and storage

    public const string PowerSystem_BasePowerConsumption = "PowerSystem_BasePowerConsumption";
    public const string PowerSystem_Intensity_Level = "PowerSystem_ConsumerLevel"; // levels of power set for the consumer IE engine speed etc.

    public static string CustomMeshId =
      "ValheimVehicles_CustomMesh";

    public static string CustomMeshScale = "ValheimVehicles_CustomMeshSize";

    public static string CustomMeshPrimitiveType =
      "ValheimVehicles_CustomMeshPrimitiveType";

    public static readonly int MBCultivatableParentIdHash =
      "MBCultivatableParentId".GetStableHashCode();

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

    public static readonly int MBParentId = "MBParentId".GetStableHashCode();

    /// <summary>
    /// This is the main positional hash for an object within the vehicle. This position is relative to the vehicle and coordinates are in local position after parented within the vehicle.
    /// </summary>
    public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

    /// <summary>
    /// This is the local rotation hash for an object within the vehicle. This rotation is relative to the vehicle and coordinates are in local rotation after parented within the vehicle.
    /// </summary>
    /// todo DEPRECATED this probably can be removed as we use the Vec3 hash instead.
    public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

    public static readonly int MBRotationVecHash =
      "MBRotationVec".GetStableHashCode();

    public static readonly int MBPieceCount = "MBPieceCount".GetStableHashCode();


#if !TEST && !UNITY_2022
    // todo ZDO.GetHashZDOID is likely deprecated.
    public static readonly KeyValuePair<int, int> MBCultivatableParentHash =
      ZDO.GetHashZDOID("MBCultivatableParent");
    // todo ZDO.GetHashZDOID is likely deprecated.
    public static readonly KeyValuePair<int, int> MBParentHash =
      ZDO.GetHashZDOID("MBParent");
#endif
  }
}