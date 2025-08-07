using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class DodgeAbility
  {
    private readonly CoroutineHandle _dodgeCoroutine;
    private readonly Transform _owner;
    private readonly Rigidbody _rb;
    public readonly DodgeAbilityConfig config;
    private float _lastDodgeTime = -Mathf.Infinity;
    private bool _prevIsKinematic;
    private bool _prevUseGravity;

    private Vector3 _start, _end;

    public DodgeAbility(MonoBehaviour monoBehaviour, DodgeAbilityConfig config, Transform owner, Rigidbody rb)
    {
      if (rb == null || monoBehaviour == null || owner == null)
      {
        throw new Exception($"Invalid monoBehaviour {monoBehaviour} or owner object {owner}, or rigidbody {rb}");
      }

      this.config = config;
      _owner = owner;
      _rb = rb;
      _dodgeCoroutine = new CoroutineHandle(monoBehaviour);
    }

    public bool CanDodge => !IsDodging && !_rb.isKinematic && Time.time > _lastDodgeTime + config.cooldown;

    public bool IsDodging => _dodgeCoroutine.IsRunning;

    public bool TryDodge(Vector2 input, Action onDodgeComplete = null)
    {
      if (!CanDodge || input == Vector2.zero)
        return false;

      var worldDir = _owner.right * input.x + _owner.forward * input.y;
      if (worldDir.sqrMagnitude > 0.01f)
        worldDir.Normalize();

      var angle = Vector3.SignedAngle(_owner.forward, worldDir, Vector3.up);
      var dist = config.sideDistance;
      if (Mathf.Abs(angle) < 45f)
        dist = config.forwardDistance;
      else if (Mathf.Abs(angle) > 135f)
        dist = config.backwardDistance;

      _start = _owner.position;
      _end = _start + worldDir * dist;

      // Draw full arc
      var arcSegments = 20;
      var prevPoint = _start;
      for (var i = 1; i <= arcSegments; i++)
      {
        var t = i / (float)arcSegments;
        var arc = Mathf.Sin(Mathf.PI * t) * config.jumpHeight;
        var point = Vector3.Lerp(_start, _end, t);
        point.y += arc;
        Debug.DrawLine(prevPoint, point, Color.yellow, 10.0f);
        prevPoint = point;
      }

      _lastDodgeTime = Time.time;
      Debug.Log(
        $"[Dodge] input: {input}, worldDir: {worldDir}, forward: {_owner.forward}, right: {_owner.right}, angle: {angle}, dist: {dist}, _start: {_start}, _end: {_end}"
      );

      Debug.DrawRay(_start, _owner.forward * 2, Color.blue, 10.0f); // forward
      Debug.DrawRay(_start, _owner.right * 2, Color.red, 10.0f); // right
      Debug.DrawRay(_start, worldDir * dist, Color.yellow, 10.0f); // intended dodge

      // Debug visual
      Debug.DrawLine(_start, _end, Color.cyan, 10.0f);
      Debug.DrawRay(_start, Vector3.up * 0.5f, Color.green, 10.0f);
      Debug.DrawRay(_end, Vector3.up * 0.5f, Color.red, 10.0f);

      _dodgeCoroutine.Start(DodgeRoutine(_start, _end, onDodgeComplete));
      return true;
    }

    private IEnumerator DodgeRoutine(Vector3 start, Vector3 end, [CanBeNull] Action onDodgeComplete = null)
    {
      _prevUseGravity = _rb.useGravity;
      _prevIsKinematic = _rb.isKinematic;
      _rb.useGravity = false;
      _rb.isKinematic = true;

      var elapsed = 0f;
      var duration = config.dodgeDuration;

      while (elapsed < duration)
      {
        var t = elapsed / duration;
        var arc = Mathf.Sin(Mathf.PI * t) * config.jumpHeight;
        var basePos = Vector3.Lerp(start, end, t);
        basePos.y += arc;
        _rb.MovePosition(basePos);
        elapsed += Time.deltaTime;
        yield return null;
      }
      _rb.MovePosition(end);
      _rb.rotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);

      // Restore Rigidbody state
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
      _rb.velocity = Vector3.zero;

      // must zero out velocity and then do it again on fixed update to prevent any bounce when dodging.
      yield return new WaitForFixedUpdate();
      _rb.rotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);
      _rb.velocity = Vector3.zero;
      onDodgeComplete?.Invoke();
    }

    public void CancelDodge()
    {
      _dodgeCoroutine.Stop();
      _rb.useGravity = _prevUseGravity;
      _rb.isKinematic = _prevIsKinematic;
    }
  }
}