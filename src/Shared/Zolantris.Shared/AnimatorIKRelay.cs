using UnityEngine;
using Zolantris.Shared.Interfaces;

namespace Zolantris.Shared
{
  public class AnimatorIKRelay : MonoBehaviour
  {
    public IAnimatorIKRelayReceiver relayReceiver;

    public void SetReceiver(IAnimatorIKRelayReceiver receiver)
    {
      relayReceiver = receiver;
    }

    private void OnAnimatorIK(int layerIndex)
    {
      if (relayReceiver == null) return;
      relayReceiver.OnAnimatorIKRelay(layerIndex);
    }
  }
}