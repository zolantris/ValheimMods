using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Zolantris.Shared
{
  public class CoroutineHandle
  {
    [CanBeNull] private readonly MonoBehaviour _owner;

    public CoroutineHandle(MonoBehaviour owner)
    {
      _owner = owner;
    }

    public MonoBehaviour GetOwner()
    {
      return _owner;
    }

    public bool IsRunning => Instance != null;

    public Coroutine Instance { get; private set; }

    public bool IsValid(MonoBehaviour? instance)
    {
      if (instance == null) return false;
      if (!_owner) return false;
      if (instance != _owner) return false;
      return true;
    }

    /// <summary>
    ///   Only allow a single instance of the routine to run. By default it will
    ///   restart the current routine.
    /// </summary>
    public void Start(IEnumerator routine, bool shouldStop = true)
    {
      if (shouldStop)
      {
        Stop();
      }
      if (IsRunning)
      {
        return;
      }
      if (_owner != null)
      {
        Instance = _owner.StartCoroutine(Wrap(routine));
      }
    }

    public void Stop()
    {
      if (Instance != null)
      {
        if (_owner != null)
        {
          _owner.StopCoroutine(Instance);
        }
        Instance = null;
      }
    }

    private IEnumerator Wrap(IEnumerator routine)
    {
      try
      {
        while (true)
        {
          try
          {
            if (!routine.MoveNext())
              break;
          }
          catch (Exception ex)
          {
            LoggerProvider.LogError($"[CoroutineHandle] Exception in coroutine: {ex}");
            break;
          }
          yield return routine.Current;
        }
      }
      finally
      {
        Instance = null;
      }
    }
  }
}