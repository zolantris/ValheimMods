using System.Collections;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core
{
  public class SleepAnimation
  {
    public Vector3 angleA = new(0, -45, 45);
    public Vector3 angleB = new(0, 45, 60);
    private bool isSetup;

    private bool IsSleeping;

    private MonoBehaviour monoBehaviour;
    private CoroutineHandle NeckRoutine;
    private Transform neckTransform;
    public float speed = 1.0f; // seconds for one direction
    private float t;
    private bool toB = true;

    public void Setup(MonoBehaviour monoBehaviour, Transform neckTransform)
    {
      this.neckTransform = neckTransform;
      this.monoBehaviour = monoBehaviour;
      NeckRoutine = new CoroutineHandle(monoBehaviour);
      isSetup = true;
    }

    public void SetIsSleeping(bool val)
    {
      IsSleeping = val;
    }

    private IEnumerator MoveHeadLeftRight()
    {
      if (!isSetup) yield break;

      while (monoBehaviour.isActiveAndEnabled && IsSleeping)
      {
        yield return new WaitForEndOfFrame();
        LateUpdate_MoveHeadAround();
      }
    }

    public void StartTurningHead()
    {
      if (NeckRoutine.IsRunning) return;
      NeckRoutine.Start(MoveHeadLeftRight());
    }

    public void StopTurningHead()
    {
      if (!NeckRoutine.IsRunning) return;
      NeckRoutine.Stop();
      IsSleeping = false;
    }

    /// <summary>
    ///   Must be run on a Monobehavior that is after an Animator it's overriding.
    /// </summary>
    private void LateUpdate_MoveHeadAround()
    {
      if (!isSetup) return;

      var animatedRot = neckTransform.localRotation;
      float blend = 1; // 0=Animator, 1=Manual
      var manualRot = Quaternion.identity;
      neckTransform.localRotation = Quaternion.Slerp(animatedRot, manualRot, blend);

      t += Time.deltaTime / speed;
      if (t > 1f)
      {
        t = 0f;
        toB = !toB;
      }

      var from = toB ? angleA : angleB;
      var to = toB ? angleB : angleA;
      neckTransform.localRotation = Quaternion.Euler(Vector3.Lerp(from, to, t));
    }
  }
}