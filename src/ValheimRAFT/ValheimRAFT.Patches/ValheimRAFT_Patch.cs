using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

/**
 * <description>Only provided for v2.0.0 compatibility, not used to apply patches</description>
 * <deprecated>Use ValheimRAFT.Patches and the associated patch </deprecated>
 */
public class ValheimRAFT_Patch
{
  /// <summary>
  ///  Used in Planbuild when targeting BepIn.Sarcen.ValheimRAFT
  /// </summary>
  /// <deprecated>Use ValheimRAFT.Patches.Player_Patch.PlacedPiece and the associated patch </deprecated>
  /// <param name="player"></param>
  /// <param name="gameObject"></param>
  public static void PlacedPiece(Player player, GameObject gameObject)
  {
    Logger.LogWarning(
      "Deprecated ValheimRAFT method PlacedPiece called, please use ValheimRAFT.Patches.Player_Patch.PlacedPiece");
    Player_Patch.PlacedPiece(gameObject);
  }
}