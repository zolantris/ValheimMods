// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using Eldritch.Core;
using UnityEngine;

namespace Eldritch.Valheim
{
  public enum XenoHitboxType
  {
    Tail,
    Arm,
    Blood
  }

  [RequireComponent(typeof(Collider))]
  public sealed class XenoAttackHitbox : MonoBehaviour
  {
    [SerializeField] private XenoHitboxType type = XenoHitboxType.Tail;
    [SerializeField] private XenoDroneAI controller;
    [SerializeField] private LayerMask hittableLayers; // e.g., Player, Characters, Pieces
    [SerializeField] private bool useAttackWindow = true;

    private Collider _col;

    private void Awake()
    {
      _col = GetComponent<Collider>();
      _col.isTrigger = true; // ensure trigger
      if (!controller) controller = GetComponentInParent<XenoDroneAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
      // Early-outs: disabled, no controller, layer not hittable, attack window closed
      if (!enabled || controller == null) return;
      if ((1 << other.gameObject.layer & hittableLayers.value) == 0) return;
      if (useAttackWindow && !controller.IsAttackWindowOpen(type)) return;

      controller.HandleHit(type, this, other);
    }
  }
}