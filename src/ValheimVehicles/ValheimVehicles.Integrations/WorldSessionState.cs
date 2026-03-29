using ValheimVehicles.Components;
using ZdoWatcher;
namespace ValheimVehicles.Integrations;

public static class WorldSessionState
{
  private static long _activeWorldKey = 0L;

  public static void OnSessionTeardown()
  {
    ClearWorldScopedState();
    _activeWorldKey = 0L;
  }

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

  /// <summary>
  /// Critical to clear any world scoped state here. If we don't, then when a player leaves a world and joins another, they will have the old world's state which may cause issues or at least pollute memory.
  /// </summary>
  /// todo track all world scoped state and clear it here. This is just the first one that came to mind and is the most likely to cause issues if not cleared.
  /// 
  private static void ClearWorldScopedState()
  {
    ZdoWatchController.Instance.Reset();
    VehicleManager.VehicleInstances.Clear();
  }
}