// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Zolantris.Shared
{
  public class CoroutineHandle
  {
    [CanBeNull] private readonly MonoBehaviour _owner;

    public CoroutineHandle(MonoBehaviour owner)
    {
      _owner = owner;
    }

    public bool IsValid(MonoBehaviour? instance)
    {
      if (instance == null) return false;
      if (!_owner) return false;
      if (instance != _owner) return false;
      return true;
    }

    public bool IsRunning => Instance != null;

    public Coroutine Instance
    {
      get;
      private set;
    }

    /// <summary>
    /// Only allow a single instance of the routine to run. By default it will restart the current routine.
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
        yield return routine;
      }
      finally
      {
        Instance = null;
      }
    }
  }
}