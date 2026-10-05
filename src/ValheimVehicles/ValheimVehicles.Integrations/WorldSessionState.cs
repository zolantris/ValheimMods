using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.ValheimVehicles.Patches;
using ZdoWatcher;
namespace ValheimVehicles.Integrations;

public static class WorldSessionState
{
  private static long _activeWorldKey = 0L;

  /// <summary>
  /// Clears every static registry keyed by ZDOID or pooled ZDO references. Must run when a new ZDOMan is created (before any ZDOs load or deserialize).
  /// </summary>
  /// <remarks>
  /// Valheim reassigns ZDOIDs on every world load (ZDOID.SetID(++m_loadID)) and ZDOID.Reset() in the ZDOMan constructor remaps user keys. Any ZDOID kept from a previous session in the same process points at an unrelated object in the new session.
  /// Stale ids in m_allPieces caused unrelated world objects (trees, LocationProxies) to be stamped onto the vehicle origin after a logout/rejoin without restarting the game.
  /// </remarks>
  public static void ResetZdoScopedRegistries()
  {
    VehiclePiecesController.m_allPieces.Clear();
    VehiclePiecesController.m_dynamicObjects.Clear();
    VehiclePiecesController.m_pendingPieces.Clear();
    VehiclePiecesController.m_pendingTempPieces.Clear();
    VehiclePiecesController.VehicleParentIdCache.Clear();

    BasePieceActivatorComponent.m_pendingPieces.Clear();
    BasePieceActivatorComponent.m_pendingTempPieces.Clear();

    SwivelComponentBridge.AllSwivelPieces.Clear();
    SwivelComponentBridge.ZdoToComponent.Clear();

    VehicleOnboardController.CharacterOnboardDataItems.Clear();
    VehicleOnboardController.DelayedExitSubscriptions.Clear();

    PersistentIdHelper.ClearMBParentCache();
  }

  public static void OnSessionTeardown()
  {
    ClearWorldScopedState();
    _activeWorldKey = 0L;
  }

  public static bool canClearWorldScopeState = false;

  public static void EnsureWorldScope(long newWorldKey)
  {
    if (newWorldKey == 0)
    {
      return;
    }

    if (_activeWorldKey == newWorldKey)
    {
      return;
    }

    ClearWorldScopedState();
    _activeWorldKey = newWorldKey;
  }

  public static void ResetStaticEntries()
  {
    VehicleOnboardController.CharacterOnboardDataItems.Clear();
    ShieldGenerator_Patches.Reset();
  }

  /// <summary>
  /// Critical to clear any world scoped state here. If we don't, then when a player leaves a world and joins another, they will have the old world's state which may cause issues or at least pollute memory.
  /// </summary>
  /// todo track all world scoped state and clear it here. This is just the first one that came to mind and is the most likely to cause issues if not cleared.
  /// 
  private static void ClearWorldScopedState()
  {
    if (!canClearWorldScopeState) return;
    ZdoWatchController.Instance.Reset();
    VehicleManager.VehicleInstances.Clear();
  }
}