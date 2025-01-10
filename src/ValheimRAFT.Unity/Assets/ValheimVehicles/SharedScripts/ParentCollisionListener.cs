using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public class ParentCollisionListener : MonoBehaviour
  {
    // This method will be called when the child collides with something
    public virtual void OnChildCollisionEnter(Collision collision) {}
    
    public virtual void OnChildCollisionStay(Collision collision) {}

    public virtual void OnChildCollisionExit(Collision collision) {}
  }
}