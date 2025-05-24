using System.Collections;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public static class PowerNetworkRebuildScheduler
{
  private static float _lastTriggerTime;
  private static float _firstTriggerTime;
  private static bool _rebuildRequested;
  private static Coroutine? _rebuildCoroutine;

  private const float MinInactivitySeconds = 1f;
  private const float MaxDelaySeconds = 3f;

  public static void Trigger()
  {
    _lastTriggerTime = Time.realtimeSinceStartup;

    if (!_rebuildRequested)
    {
      _firstTriggerTime = _lastTriggerTime;
      _rebuildRequested = true;

      if (_rebuildCoroutine == null && PowerNetworkController.Instance != null)
      {
        _rebuildCoroutine = PowerNetworkController.Instance.StartCoroutine(RebuildWatcher());
      }
    }
  }

  private static IEnumerator RebuildWatcher()
  {
    LoggerProvider.LogDebug("[PowerNetworkRebuildScheduler] Debounced rebuild coroutine started.");

    while (_rebuildRequested)
    {
      yield return null;

      var now = Time.realtimeSinceStartup;
      var sinceLast = now - _lastTriggerTime;
      var sinceFirst = now - _firstTriggerTime;

      if (sinceLast >= MinInactivitySeconds || sinceFirst >= MaxDelaySeconds)
      {
        _rebuildRequested = false;
        _rebuildCoroutine = null;

        LoggerProvider.LogInfo("[PowerNetworkRebuildScheduler] Executing network rebuild.");
        PowerSystemClusterManager.RebuildClusters();

        // Updates the power nodes with the latest simulation. This is for all rendered nodes and will only be accurate on clients.
        PowerNetworkController.UpdateAllPowerNodes();

        // for creating a network of IPowerNodes using our new logic
        PowerNetworkControllerIntegration.SimulateAllNetworks(PowerNetworkController.AllPowerNodes);
        yield break;
      }
    }
  }
}