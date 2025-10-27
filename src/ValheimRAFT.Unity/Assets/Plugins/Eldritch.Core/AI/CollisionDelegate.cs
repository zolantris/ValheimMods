using UnityEngine;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{

  public class CollisionDelegate : MonoBehaviour
  {
    public XenoDroneAI ownerAI;

    public void SetOwnerAI(XenoDroneAI ai)
    {
      ownerAI = ai;
    }

    private void OnCollisionEnter(Collision other)
    {
      if (!ownerAI) return;
      ownerAI.OnCollisionEnter(other);
    }
    private void OnCollisionStay(Collision other)
    {
      if (!ownerAI) return;
      ownerAI.OnCollisionStay(other);
    }
  }
}