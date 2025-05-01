#region

  using DynamicLocations.Controllers;
  using HarmonyLib;
  using ValheimVehicles.Controllers;

#endregion

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
    if (PlayerSpawnController.Instance == null) return;

    var onboardData =
      VehicleOnboardController.GetOnboardCharacterData(Player.m_localPlayer
        .GetZDOID());
    
    if (onboardData == null || onboardData.OnboardController == null || onboardData.OnboardController.NetView == null || onboardData.OnboardController.NetView.GetZDO() == null)
    {
      PlayerSpawnController.Instance.SyncLogoutPoint(null, true);
      return;
    }
    
    PlayerSpawnController.Instance.SyncLogoutPoint(
      onboardData.OnboardController.NetView.GetZDO());
  }
}