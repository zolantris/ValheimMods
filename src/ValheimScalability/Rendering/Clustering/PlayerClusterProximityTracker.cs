using System.Collections;
using UnityEngine;
using ValheimScalability.BepInExConfig;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Attached to Player by Harmony. Only Player.m_localPlayer performs recurring work.
/// </summary>
public sealed class PlayerClusterProximityTracker : MonoBehaviour
{
  private Player _player;
  private Coroutine _coroutine;

  private void Awake()
  {
    _player =
      GetComponent<Player>();
  }

  private void OnEnable()
  {
    if (_coroutine == null)
    {
      _coroutine =
        StartCoroutine(
          ProximityCoroutine());
    }
  }

  private IEnumerator ProximityCoroutine()
  {
    while (this &&
           _player &&
           Player.m_localPlayer != _player)
    {
      yield return null;
    }

    while (this &&
           _player)
    {
      if (Player.m_localPlayer == _player &&
          !Application.isBatchMode &&
          ValheimScalabilityConfig.Mode ==
            ClusterPresentationMode.Adaptive)
      {
        SectorMeshClusterManager
          .EnsureAttached()?
          .UpdateLocalPlayerProximity(
            _player.transform.position,
            Camera.main);
      }

      yield return
        new WaitForSecondsRealtime(
          Mathf.Max(
            0.05f,
            ValheimScalabilityConfig.ProximityPollInterval));
    }
  }

  private void OnDisable()
  {
    if (_coroutine == null)
    {
      return;
    }

    StopCoroutine(
      _coroutine);

    _coroutine =
      null;
  }

  private void OnDestroy()
  {
    if (Player.m_localPlayer ==
        _player)
    {
      SectorMeshClusterManager.Instance?
        .ClearLocalPlayerProximity();
    }
  }
}
