using HarmonyLib;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.Patches;

public class Piece_Patch
{
  [HarmonyPatch(typeof(Piece), "CanBeRemoved")]
  [HarmonyPostfix]
  public static void Swivel_CanRemove(Piece __instance, ref bool __result)
  {
    var pieceController = __instance.GetComponent<IPieceController>();
    if (pieceController != null && pieceController.CanRaycastHitPiece())
    {
      __result = true;
    }
  }
}