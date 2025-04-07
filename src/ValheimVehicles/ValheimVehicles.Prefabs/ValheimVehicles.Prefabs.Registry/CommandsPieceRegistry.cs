using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
namespace ValheimVehicles.Prefabs.Registry;

public class CommandsPieceRegistry : GuardedRegistry
{
  private static void RegisterCreativeCommand()
  {
    // Create and add custom pieces
    var makeConfig = new PieceConfig();
    makeConfig.PieceTable = "_BlueprintTestTable";
    makeConfig.Category = "Make";
    var makePiece = new CustomPiece(VehicleHammerTableRegistry.GetPieceTable(), fixReference: false, makeConfig);
    PieceManager.Instance.AddPiece(makePiece);

    var placeConfig = new PieceConfig();
    placeConfig.PieceTable = "_BlueprintTestTable";
    placeConfig.Category = "Place";
    placeConfig.AllowedInDungeons = true;
    placeConfig.AddRequirement("Wood", 2);
    var placePiece = new CustomPiece(null, fixReference: false, placeConfig);
    PieceManager.Instance.AddPiece(placePiece);
  }

  public override void OnRegister()
  {
    RegisterCreativeCommand();
  }
}