using System;
using System.Collections;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.UI;

public class ScreenSizeWatcher : SingletonBehaviour<ScreenSizeWatcher>
{
  public static event Action<Vector2Int>? OnScreenSizeChanged;

  private Vector2Int _lastSize;
  private Coroutine? _updateCoroutine;

  private void Start()
  {
    _lastSize = new Vector2Int(Screen.width, Screen.height);
  }

  private void OnEnable()
  {
    _updateCoroutine = StartCoroutine(UpdateScreenSizeCoroutine());
  }

  private void OnDisable()
  {
    if (_updateCoroutine != null)
    {
      StopCoroutine(_updateCoroutine);
      _updateCoroutine = null;
    }
  }

  private void SyncScreenSize()
  {
    if (!isActiveAndEnabled) return;
    var currentSize = new Vector2Int(Screen.width, Screen.height);
    if (currentSize != _lastSize)
    {
      _lastSize = currentSize;
      OnScreenSizeChanged?.Invoke(currentSize);
    }
  }

  private IEnumerator UpdateScreenSizeCoroutine()
  {
    while (isActiveAndEnabled)
    {
      yield return new WaitForEndOfFrame();
      SyncScreenSize();
    }
  }
}