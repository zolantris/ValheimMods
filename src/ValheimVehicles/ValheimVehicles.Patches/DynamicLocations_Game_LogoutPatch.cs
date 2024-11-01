using DynamicLocations.Controllers;
using HarmonyLib;
using ValheimVehicles.Vehicles.Controllers;

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
    var onboardData =
      VehicleOnboardController.GetOnboardCharacterData(Player.m_localPlayer
        .GetZDOID());
    if (onboardData == null || onboardData.OnboardController == null)
    {
      PlayerSpawnController.Instance?.SyncLogoutPoint(null, true);
      return;
    }

    PlayerSpawnController.Instance?.SyncLogoutPoint(
      onboardData?.OnboardController?.VehicleInstance?.NetView?.GetZDO());
  }
}