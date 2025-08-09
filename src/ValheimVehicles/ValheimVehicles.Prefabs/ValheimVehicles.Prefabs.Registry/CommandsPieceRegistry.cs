using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Prefabs.Registry;

public class CommandsPieceRegistry : GuardedRegistry<CommandsPieceRegistry>
{
  private static GameObject CreateToggleObj(string objName)
  {
    var obj = new GameObject
    {
      name = objName
    };
    return obj;
  }

  public class CommandPieceController : MonoBehaviour
  {

  }

  private static void RegisterCommandPiece(string objName, string pieceName, string pieceDescription)
  {
    var pieceObj = CreateToggleObj(objName);

    pieceObj.AddComponent<CommandPieceController>();

    // todo might need to add a netview here.

    // Create and add custom pieces
    var config = new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = VehicleHammerTableCategories.Tools,
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Anchor),
      Name = pieceName,
      Description = pieceDescription
    };

    var piece = new CustomPiece(pieceObj, false, config);
    var success = PieceManager.Instance.AddPiece(piece);

    if (!success)
    {
      LoggerProvider.LogError($"Could not register Piece {objName} successfully.");
    }
  }

  /// <summary>
  /// Registers all vehicle boolean commands.
  /// </summary>
  public override void OnRegister()
  {
    // TODO internationalize strings.
    RegisterCommandPiece("Vehicle_CreativeModeToggle", "Toggle Edit-Mode", "Runs Vehicle command <creative>. This will toggle the nearest vehicle's creative mode, then delete itself.");

    // TODO internationalize strings.
    RegisterCommandPiece("Vehicle_DebugMenu", "Toggle Debug Menu", "Toggles the vehicle debug menu which allows for easy running common vehicle commands.");
  }
}