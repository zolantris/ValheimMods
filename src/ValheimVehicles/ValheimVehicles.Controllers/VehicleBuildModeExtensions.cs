using ValheimVehicles.Enums;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Controllers;

public static class VehicleBuildModeExtensions
{
  public static bool CanPlacePiece(
    this VehicleBuildMode mode,
    bool isContainer)
  {
    return mode switch
    {
      VehicleBuildMode.Expandable => true,
      VehicleBuildMode.Fixed => true,
      VehicleBuildMode.Disabled => false,
      // VehicleBuildMode.StorageOnly => isContainer,
      _ => false
    };
  }

  public static VehicleBuildMode GetNextMode(this VehicleBuildMode mode)
  {
    return mode switch
    {
      VehicleBuildMode.Expandable => VehicleBuildMode.Fixed,
      VehicleBuildMode.Fixed => VehicleBuildMode.Disabled,
      VehicleBuildMode.Disabled => VehicleBuildMode.Expandable,
      _ => VehicleBuildMode.Expandable
    };
  }

  public static string GetModeText(this VehicleBuildMode mode)
  {
    return mode switch
    {
      VehicleBuildMode.Expandable => ModTranslations.VehicleBuildMode_Expandable,
      VehicleBuildMode.Fixed => ModTranslations.VehicleBuildMode_Fixed,
      VehicleBuildMode.Disabled => ModTranslations.VehicleBuildMode_Disabled,
      _ => ModTranslations.VehicleBuildMode_Expandable
    };
  }

  public static string GetModeTextDescription(this VehicleBuildMode mode)
  {
    return mode switch
    {
      VehicleBuildMode.Expandable => ModTranslations.VehicleBuildMode_ExpandableDesc,
      VehicleBuildMode.Fixed => ModTranslations.VehicleBuildMode_FixedDesc,
      VehicleBuildMode.Disabled => ModTranslations.VehicleBuildMode_DisabledDesc,
      _ => ModTranslations.VehicleBuildMode_ExpandableDesc
    };
  }


  public static bool CanExpandBounds(
    this VehicleBuildMode mode)
  {
    return mode == VehicleBuildMode.Expandable;
  }

  public static bool ShouldPersistBounds(
    this VehicleBuildMode mode)
  {
    return mode == VehicleBuildMode.Expandable;
  }
}