using DynamicLocations.Controllers;
using HarmonyLib;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

public class DynamicLocations_Game_LogoutPatch
{
  /// <summary>
  /// Removes logouts that are done off the ship
  /// </summary>
  [HarmonyPatch(typeof(Game), nameof(Game.ContinueLogout))]
  [HarmonyPrefix]
  private static void Game_OnContinueLogout()
  {
    if (Player.m_localPlayer == null) return;
    var playerZdoid = Player.m_localPlayer.GetZDOID();
    if (playerZdoid == ZDOID.None) return;

    var onboardData =
      VehicleOnboardController.GetOnboardCharacterData(Player.m_localPlayer
        .GetZDOID());
    if (onboardData == null || onboardData.OnboardController == null)
    {
      PlayerSpawnController.Instance?.SyncLogoutPoint(null, true);
      return;
    }

    if (onboardData.OnboardController == null) return;

    PlayerSpawnController.Instance?.SyncLogoutPoint(
      onboardData?.OnboardController?.vehicleShip?.NetView?.GetZDO());
  }
}