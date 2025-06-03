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

    public CoroutineHandle(MonoBehaviour owner)
    {
      _owner = owner;
    }

    public void Start(IEnumerator routine)
    {
      Stop(); // Only one at a time.
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

    // The magic: ensures _coroutine is null at the end, even on yield break.
    private IEnumerator Wrap(IEnumerator routine)
    {
      yield return routine; // Whatever exit path, this runs after.
      _coroutine = null;
    }
  }
}