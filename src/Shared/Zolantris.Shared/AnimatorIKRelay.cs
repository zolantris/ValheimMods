using System;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared.Interfaces;

namespace Zolantris.Shared
{
  public class AnimatorIKRelay : MonoBehaviour
  {
    [CanBeNull] public IAnimatorIKRelayReceiver relayReceiver;
    public Transform neck;

    public void Awake()
    {
      neck = transform.Find("alien_xenos_drone_SK_Xenos_Drone_skeleton/XenosBiped_TrajectorySHJnt/XenosBiped_ROOTSHJnt/XenosBiped_Spine_01SHJnt/XenosBiped_Spine_02SHJnt/XenosBiped_Spine_03SHJnt/XenosBiped_Spine_TopSHJnt/XenosBiped_Neck_01SHJnt");
    }
    public void SetReceiver(IAnimatorIKRelayReceiver receiver)
    {
      relayReceiver = receiver;
    }

    private void OnAnimatorIK(int layerIndex)
    {
      relayReceiver?.OnAnimatorIKRelay(layerIndex);
    }
    
    public Vector3 angleA = new Vector3(0, -45, 0);
    public Vector3 angleB = new Vector3(0, 45, 0);
    public float speed = 1.0f; // seconds for one direction

    private bool toB = true;
    private float t = 0f;

    private void LateUpdate()
    {
      relayReceiver?.OnAnimatorOverride();
    
    }
  }
}