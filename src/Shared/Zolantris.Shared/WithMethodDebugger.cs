using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Zolantris.Shared
{

  [AttributeUsage(AttributeTargets.Method)]
  public class MeasureTimeAttribute : Attribute
  {
  }

  public static class TimerUtility
  {
    public static void ExecuteWithTiming(Action action,
      [System.Runtime.CompilerServices.CallerMemberName]
      string methodName = "")
    {
#if DEBUG
      var stopwatch = Stopwatch.StartNew();
      action();
      stopwatch.Stop();
      BatchedLogger.Instance.Log(
        $"[{methodName}] ran in: {stopwatch.ElapsedMilliseconds} ms");
#endif
    }

    public static void MeasureTimeWithAttribute(object instance,
      string methodName)
    {
      var method = instance.GetType().GetMethod(methodName);
      if (method is not null &&
          method.GetCustomAttribute<MeasureTimeAttribute>() is not null)
      {
        ExecuteWithTiming(() => method.Invoke(instance, null), methodName);
      }
      else
      {
        method?.Invoke(instance, null);
      }
    }
  }

  public class BatchedLogger : MonoBehaviour
  {
    private static BatchedLogger? _instance;
    private static readonly Queue<string> _logQueue = new();
    private float _timer;
    public static bool IsLoggingEnabled { get; set; } = true;

    [UsedImplicitly]
    public static float BatchIntervalFrequencyInSeconds { get; set; } =
      3f; // Adjust as necessary

    public static BatchedLogger Instance
    {
      get
      {
        if (_instance is null)
        {
          var loggerObject = new GameObject("Logger");
          _instance = loggerObject.AddComponent<BatchedLogger>();
          DontDestroyOnLoad(loggerObject);
        }

        return _instance;
      }
    }

#if DEBUG
    private void Update()
    {
      _timer += Time.deltaTime;

      if (_timer >= BatchIntervalFrequencyInSeconds)
      {
        FlushLogs();
        _timer = 0f;
      }
    }
#endif

    public void Log(string message)
    {
      _logQueue.Enqueue(message);
    }

    private void FlushLogs()
    {
      while (_logQueue.Count > 0)
      {
        LoggerProvider.LogInfoDebounced(_logQueue.Dequeue());
      }
    }
  }
}