using UnityEngine;
using UnityEngine.Serialization;

namespace ValheimVehicles.SharedScripts
{
    public class ChildCollisionDetector : MonoBehaviour
    {
        // Reference to the parent component
        private ParentCollisionListener parentCollisionListener;
        public bool hasCollisionStayListener = false;

        private void Start()
        {
            // Find the parent component and store a reference
            parentCollisionListener =
                GetComponentInParent<ParentCollisionListener>();
        }

        // This is where the collision detection will occur
        private void OnCollisionEnter(Collision collision)
        {
            // Call a method on the parent component to notify it about the collision
            if (parentCollisionListener != null)
            {
                parentCollisionListener.OnChildCollisionEnter(collision);
            }
        }

        // Optionally, you can also listen for ongoing collisions
        private void OnCollisionStay(Collision collision)
        {
            if (!hasCollisionStayListener) return;
            if (parentCollisionListener != null)
            {
                parentCollisionListener.OnChildCollisionStay(collision);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (parentCollisionListener != null)
            {
                parentCollisionListener.OnChildCollisionExit(collision);
            }
        }
    }
}

