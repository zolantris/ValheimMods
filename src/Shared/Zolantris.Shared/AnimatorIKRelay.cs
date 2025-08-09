using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared.Interfaces;
namespace Zolantris.Shared
{
  public class AnimatorIKRelay : MonoBehaviour
  {
    [CanBeNull] public IAnimatorIKRelayReceiver relayReceiver;
    private void LateUpdate()
    {
      relayReceiver?.OnAnimatorOverride();
    }

    private void OnAnimatorIK(int layerIndex)
    {
      relayReceiver?.OnAnimatorIKRelay(layerIndex);
    }

    public void SetReceiver(IAnimatorIKRelayReceiver receiver)
    {
      relayReceiver = receiver;
    }
  }
}