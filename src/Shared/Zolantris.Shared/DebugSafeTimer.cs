using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace Zolantris.Shared.Debug;

/// <summary>
/// Work around for debugging timeouts
/// </summary>
public class DebugSafeTimer
{
  private bool _isRunning;

  private float _elapsedTime;
  public float ElapsedMilliseconds => _elapsedTime * 1000;

  public static DebugSafeTimer StartNew()
  {
    var timer = new DebugSafeTimer();
    timer.Start();
    return timer;
  }

  private List<DebugSafeTimer>? _listRef = null;

  /// <summary>
  /// For running and updating within a list which then would be iterated from
  /// </summary>
  /// <param name="list"></param>
  /// <returns></returns>
  public static DebugSafeTimer StartNew(List<DebugSafeTimer> list)
  {
    var timer = new DebugSafeTimer();
    timer.Start();
    list.Add(timer);
    timer._listRef = list;
    return timer;
  }

  /// <summary>
  /// To be run in a Monobehavior
  /// </summary>
  /// <param name="list"></param>
  public static void UpdateTimersFromList(List<DebugSafeTimer> list)
  {
    if (list.Count == 0) return;
    foreach (var debugSafeTimer in list.ToArray())
    {
      if (debugSafeTimer != null)
      {
        debugSafeTimer.Update();
      }
      else
      {
        // This should be intended to remove if null
        if (debugSafeTimer == null)
        {
          list.Remove(debugSafeTimer!);
        }
      }
    }
  }

  [UsedImplicitly]
  public void Start()
  {
    if (_listRef != null && !_listRef.Contains(this))
    {
      _listRef.Add(this);
    }

    _isRunning = true;
  }

  [UsedImplicitly]
  public void Stop()
  {
    _isRunning = false;
  }

  [UsedImplicitly]
  public void Reset()
  {
    if (_listRef != null && _listRef.Contains(this))
    {
      _listRef.Remove(this);
    }

    _elapsedTime = 0;
    Stop();
  }

  [UsedImplicitly]
  public void Restart()
  {
    _elapsedTime = 0;
    Start();
  }

  [UsedImplicitly]
  public void Clear()
  {
    Reset();
    if (_listRef == null)
    {
      LoggerProvider.LogDebug("Called delete but listRef did not exist");
      return;
    }

    // should be fine to call without a Contains check
    _listRef?.Remove(this);
  }

  private void OnUpdateAutoExpire()
  {
    if (ElapsedMilliseconds > 20000)
    {
      Clear();
    }
  }

  /// <summary>
  /// To be run in a Monobehavior
  /// </summary>
  [UsedImplicitly]
  private void Update()
  {
    if (_isRunning)
    {
      _elapsedTime += Time.deltaTime;
    }

    OnUpdateAutoExpire();
  }
}