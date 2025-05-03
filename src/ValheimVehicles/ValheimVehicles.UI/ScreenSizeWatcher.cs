using System;
using System.Collections;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.UI;

public class ScreenSizeWatcher : SingletonBehaviour<ScreenSizeWatcher>
{
  public static event Action<Vector2Int>? OnScreenSizeChanged;

  private Vector2Int _lastSize;

  private void Start()
  {
    _lastSize = new Vector2Int(Screen.width, Screen.height);
  }

  private Coroutine _updateCoroutine;

  private void OnEnable()
  {
    _updateCoroutine = StartCoroutine(UpdateScreenSizeCoroutine());
  }

  private void OnDisable()
  {
    StopCoroutine(_updateCoroutine);
  }

  private void SyncScreenSize()
  {
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
      yield return new WaitForSeconds(3f);
      yield return new WaitForEndOfFrame();
      SyncScreenSize();
    }
  }
}