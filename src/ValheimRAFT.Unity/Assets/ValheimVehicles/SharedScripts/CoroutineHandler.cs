// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CoroutineHandle
  {
    private Coroutine _coroutine;
    private MonoBehaviour _owner;

    public bool IsRunning => _coroutine != null;

    public Coroutine Instance => _coroutine;

    public CoroutineHandle(MonoBehaviour owner)
    {
      _owner = owner;
    }

    public void Start(IEnumerator routine)
    {
      Stop();
      _coroutine = _owner.StartCoroutine(Wrap(routine));
    }

    public void Stop()
    {
      if (_coroutine != null)
      {
        _owner.StopCoroutine(_coroutine);
        _coroutine = null;
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
        _coroutine = null;
      }
    }
  }
}