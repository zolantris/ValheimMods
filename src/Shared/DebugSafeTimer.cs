using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace Zolantris.Shared.Debug;

/// <summary>
/// Work around for debugging timeouts
/// </summary>
public class DebugSafeTimer
{
  private Stopwatch stopwatch;
  private bool isRunning = false;

  private float _elapsedTime = 0;
  public float ElapsedMilliseconds => _elapsedTime / 1000;

  public static DebugSafeTimer StartNew()
  {
    var timer = new DebugSafeTimer();
    timer.Start();
    return timer;
  }

  private List<DebugSafeTimer>? listRef = null;

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
    timer.listRef = list;
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

  public void Start()
  {
    isRunning = true;
  }

  public void Stop()
  {
    isRunning = false;
  }

  public void Reset()
  {
    _elapsedTime = 0;
    isRunning = false;
  }

  public void Restart()
  {
    _elapsedTime = 0;
    isRunning = true;
  }

  public void Delete()
  {
    isRunning = false;
    if (listRef == null)
    {
      Logger.LogWarning("Called delete but listRef did not exist");
      return;
    }

    // should be fine to call without a Contains check
    listRef?.Remove(this);
  }


  /// <summary>
  /// To be run in a Monobehavior
  /// </summary>
  private void Update()
  {
    if (isRunning)
    {
      _elapsedTime += Time.deltaTime;
    }
  }
}