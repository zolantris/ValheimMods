using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class DodgeAbility
  {
    private readonly DodgeAbilityConfig _config;
    private readonly CoroutineHandle _dodgeCoroutine;
    private readonly Transform _owner;
    private readonly Rigidbody _rb;
    private float _lastDodgeTime = -Mathf.Infinity;
    private bool _prevIsKinematic;
    private bool _prevUseGravity;

    private Vector3 _start, _end;

    public DodgeAbility(MonoBehaviour monoBehaviour, DodgeAbilityConfig config, Transform owner, Rigidbody rb)
    {
      _config = config;
      _owner = owner;
      _rb = rb;
      _dodgeCoroutine = new CoroutineHandle(monoBehaviour);
    }

    public bool CanDodge => !IsDodging && Time.time > _lastDodgeTime + _config.cooldown;

    public bool IsDodging
    {
      get;
      private set;
    }

    public bool TryDodge(Vector3 inputDir, [CanBeNull] Action onDodgeComplete = null)
    {
      if (!CanDodge || inputDir == Vector3.zero)
        return false;

      // Direction logic
      inputDir.y = 0;
      if (inputDir == Vector3.zero)
        inputDir = _owner.forward;
      inputDir.Normalize();

      var angle = Vector3.SignedAngle(_owner.forward, inputDir, Vector3.up);

      var dist = _config.sideDistance;
      if (Mathf.Abs(angle) < 45f)
        dist = _config.forwardDistance;
      else if (Mathf.Abs(angle) > 135f)
        dist = _config.backwardDistance;

      _start = _owner.position;
      _end = _start + inputDir * dist;

      // Save and override Rigidbody state
      _prevIsKinematic = _rb.isKinematic;
      _prevUseGravity = _rb.useGravity;
      _rb.velocity = Vector3.zero;
      _rb.useGravity = false;
      _rb.isKinematic = true;

      IsDodging = true;
      _lastDodgeTime = Time.time;
      _dodgeCoroutine.Start(DodgeRoutine(onDodgeComplete));

      // Debug visual
      Debug.DrawLine(_start, _end, Color.cyan, 1.0f);
      Debug.DrawRay(_start, Vector3.up * 0.5f, Color.green, 1.0f);
      Debug.DrawRay(_end, Vector3.up * 0.5f, Color.red, 1.0f);

      return true;
    }

    private IEnumerator DodgeRoutine([CanBeNull] Action onDodgeComplete = null)
    {
      var elapsed = 0f;
      var duration = _config.dodgeDuration;
      while (elapsed < duration)
      {
        var t = elapsed / duration;
        var arc = Mathf.Sin(Mathf.PI * t) * _config.jumpHeight;
        var basePos = Vector3.Lerp(_start, _end, t);
        basePos.y += arc;
        _rb.MovePosition(basePos);
        elapsed += Time.deltaTime;
        yield return null;
      }
      _rb.MovePosition(_end);

      // Restore Rigidbody state
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
      IsDodging = false;

      onDodgeComplete?.Invoke();
    }

    public void CancelDodge()
    {
      _dodgeCoroutine.Stop();
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
      IsDodging = false;
    }
  }
}