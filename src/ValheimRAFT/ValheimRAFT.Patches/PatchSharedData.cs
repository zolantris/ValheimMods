using System.Collections.Generic;
using HarmonyLib;

namespace ValheimRAFT.Patches;

public static class PatchSharedData
{
  /// <summary>
  /// for controlling the local player's last placed piece
  /// </summary>
  public static Piece? PlayerLastRayPiece;

  /*
   * todo remove this
   * not used, probably can removed unless the yawoffset is needed for piece placing
   */
  public static readonly float YawOffset = 0;

  public static ShipControlls PlayerLastUsedControls;

  // stops zone destroys of items outside of zones...
  // WARNING DEBUG ONLY
  public static bool m_disableCreateDestroy;
}