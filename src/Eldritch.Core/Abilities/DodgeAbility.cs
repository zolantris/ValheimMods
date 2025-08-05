using System.Collections;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class DodgeAbility
  {
    private readonly DodgeAbilityConfig _config;
    private readonly CoroutineHandle _dodgeCoroutine;
    private readonly Transform _owner;
    private float _elapsed;
    private bool _isDodging;
    private float _lastDodgeTime = -Mathf.Infinity;
    private MonoBehaviour _monoBehaviour;
    private Vector3 _start, _end;

    public DodgeAbility(MonoBehaviour monoBehaviour, DodgeAbilityConfig config, Transform owner)
    {
      _dodgeCoroutine = new CoroutineHandle(monoBehaviour);
      _monoBehaviour = monoBehaviour;
      _config = config;
      _owner = owner;
    }

    public bool IsDodging => _dodgeCoroutine.IsRunning;

    public bool TryDodge(Vector3 inputDir)
    {
      if (_isDodging || Time.time < _lastDodgeTime + _config.cooldown || inputDir == Vector3.zero)
        return false;

      inputDir.y = 0;
      inputDir.Normalize();
      var angle = Vector3.SignedAngle(_owner.forward, inputDir, Vector3.up);
      var dist = _config.sideDistance;
      if (Mathf.Abs(angle) < 45f)
        dist = _config.forwardDistance;
      else if (Mathf.Abs(angle) > 135f)
        dist = _config.backwardDistance;

      _start = _owner.position;
      _end = _start + inputDir * dist + Vector3.up * _config.jumpHeight;

      _isDodging = true;
      _lastDodgeTime = Time.time;
      _dodgeCoroutine.Start(DodgeRoutine());
      return true;
    }

    public void CancelDodge()
    {
      _dodgeCoroutine.Stop();
    }

    private IEnumerator DodgeRoutine()
    {
      var elapsed = 0f;
      var duration = _config.dodgeDuration;
      while (elapsed < duration)
      {
        var t = elapsed / duration;
        var arc = Mathf.Sin(Mathf.PI * t) * _config.jumpHeight;
        var basePos = Vector3.Lerp(_start, _end, t);
        basePos.y += arc;
        _owner.position = basePos;
        elapsed += Time.deltaTime;
        yield return null;
      }
      _owner.position = _end; // Snap to end at the finish
      _isDodging = false;
    }
  }
}