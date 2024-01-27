using System.Collections.Generic;
using HarmonyLib;

namespace ValheimRAFT.Patches;

public static class PatchSharedData
{
  /*
   * previously this was a single piece. But this would be inaccurate in multiplayer if multiple people tried to add something.
   */
  // public static Dictionary<string, Piece> PlayerLastRayPiece = new();
  public static Piece PlayerLastRayPiece;

  /*
   * todo remove this
   * not used, probably can removed unless the yawoffset is needed for piece placing
   */
  public static readonly float YawOffset = 0f;

  public static ShipControlls PlayerLastUsedControls;
  // public static Dictionary<string, ShipControlls> PlayerLastUsedControls;
  //
  // public static ShipControlls GetLastUsedControls(string playerId)
  // {
  //   return PlayerLastUsedControls.GetValueSafe<string, ShipControlls>(playerId);
  // }

  public static bool m_disableCreateDestroy;
}