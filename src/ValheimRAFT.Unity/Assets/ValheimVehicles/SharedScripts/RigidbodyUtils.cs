#region

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{

  public static class RigidbodyUtils
  {
    /// <summary>
    /// Moves the Rigidbody’s GameObject pivot to the world center of mass (or an override),
    /// while adjusting all child objects to maintain their world positions. Automatically restores
    /// all components (including external references) that depended on the Rigidbody.
    /// </summary>
    /// <param name="rbObject">The GameObject containing the Rigidbody to recenter.</param>
    /// <param name="overrideCenter">Optional world position override. If null, the Rigidbody's world center of mass is used.</param>
    public static void RecenterRigidbodyPivot(GameObject rbObject, Vector3? overrideCenter = null)
    {
      if (!rbObject) return;
      var rb = rbObject.GetComponent<Rigidbody>();
      if (!rb) return;

      var rbTransform = rb.transform;

      // Compute the target center position
      var tempWcM = rb.centerOfMass;
      rb.ResetCenterOfMass();
      var worldCOM = overrideCenter ?? rb.worldCenterOfMass;
      var delta = rbTransform.position - worldCOM; // Difference needed to move the pivot

      // --- Step 1: Cache Rigidbody properties before removing it ---
      RigidbodyData rbData = new(rb);

      // Track components that reference this Rigidbody (including external objects)
      // var dependentComponents = FindAllComponentsWithRigidbody(rb);

      // Remove the Rigidbody immediately
      // Object.DestroyImmediate(rb);

      // --- Step 2: Move the Rigidbody Transform ---

      // --- Step 3: Adjust all children so they maintain world-space positions ---
      List<Transform> children = new();
      List<ConfigurableJoint> joints = new();

      foreach (Transform child in rbTransform)
      {
        if (child.TryGetComponent(out ConfigurableJoint joint))
          joints.Add(joint);

        children.Add(child);
        child.transform.parent = null;
      }

      foreach (var child in children)
      {
        child.position += delta;
      }

      // Update ConfigurableJoints
      foreach (var joint in joints)
      {
        joint.connectedBody = null;
      }

      foreach (var joint in joints)
      {
        joint.connectedAnchor += delta;
        joint.connectedBody = rb;
      }

      rbTransform.position = worldCOM;

      foreach (var child in children)
      {
        child.SetParent(rb.transform);
      }
      rb.centerOfMass = tempWcM;
      // --- Step 4: Start a Coroutine to Re-add the Rigidbody in the Next Frame ---
      // rbObject.GetComponent<MonoBehaviour>().StartCoroutine(ReAddRigidbodyAfterFrame(rbObject, rbData, dependentComponents));
    }

    /// <summary>
    /// Coroutine that waits a frame before re-adding the Rigidbody to allow Unity to fully unregister it.
    /// </summary>
    private static IEnumerator ReAddRigidbodyAfterFrame(GameObject rbObject, RigidbodyData rbData, List<Component> dependentComponents)
    {
      yield return null; // Wait until next frame

      // --- Step 5: Re-add Rigidbody and Restore Properties ---
      var newRB = rbObject.AddComponent<Rigidbody>();
      rbData.ApplyTo(newRB);

      // --- Step 6: Restore Rigidbody References (Local + External) ---
      RestoreRigidbodyReferences(dependentComponents, newRB);
    }

    /// <summary>
    /// Finds all components in the scene that reference the given Rigidbody.
    /// This includes both local components and external objects.
    /// </summary>
    private static List<Component> FindAllComponentsWithRigidbody(Rigidbody rb)
    {
      List<Component> referencingComponents = new();

      // Search all GameObjects in the scene
      foreach (var go in Object.FindObjectsOfType<GameObject>(true))
      {
        if (go == rb.gameObject) continue; // Skip the Rigidbody's own GameObject

        var allComponents = go.GetComponents<Component>();

        foreach (var component in allComponents)
        {
          if (component == null) continue;

          // Use reflection to check for fields that store a Rigidbody reference
          var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

          foreach (var field in fields)
          {
            if (field.FieldType == typeof(Rigidbody))
            {
              var fieldRB = field.GetValue(component) as Rigidbody;
              if (fieldRB == rb)
              {
                referencingComponents.Add(component);
                break; // No need to check more fields in this component
              }
            }
          }
        }
      }

      return referencingComponents;
    }

    /// <summary>
    /// Restores the Rigidbody reference in all components that previously referenced it.
    /// </summary>
    private static void RestoreRigidbodyReferences(List<Component> components, Rigidbody newRB)
    {
      foreach (var component in components)
      {
        var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
          if (field.FieldType == typeof(Rigidbody))
          {
            field.SetValue(component, newRB);
          }
        }
      }
    }

    /// <summary>
    /// Helper struct to cache and restore Rigidbody properties.
    /// </summary>
    private struct RigidbodyData
    {
      public Vector3 Velocity;
      public Vector3 AngularVelocity;
      public float Mass;
      public float Drag;
      public float AngularDrag;
      public bool UseGravity;
      public RigidbodyConstraints Constraints;
      public CollisionDetectionMode CollisionMode;
      public RigidbodyInterpolation Interpolation;
      public float MaxAngularVelocity;
      public float MaxDepenetrationVelocity;

      public RigidbodyData(Rigidbody rb)
      {
        Velocity = rb.velocity;
        AngularVelocity = rb.angularVelocity;
        Mass = rb.mass;
        Drag = rb.drag;
        AngularDrag = rb.angularDrag;
        UseGravity = rb.useGravity;
        Constraints = rb.constraints;
        CollisionMode = rb.collisionDetectionMode;
        Interpolation = rb.interpolation;
        MaxAngularVelocity = rb.maxAngularVelocity;
        MaxDepenetrationVelocity = rb.maxDepenetrationVelocity;
      }

      public void ApplyTo(Rigidbody rb)
      {
        rb.velocity = Velocity;
        rb.angularVelocity = AngularVelocity;
        rb.mass = Mass;
        rb.drag = Drag;
        rb.angularDrag = AngularDrag;
        rb.useGravity = UseGravity;
        rb.constraints = Constraints;
        rb.collisionDetectionMode = CollisionMode;
        rb.interpolation = Interpolation;
        rb.maxAngularVelocity = MaxAngularVelocity;
        rb.maxDepenetrationVelocity = MaxDepenetrationVelocity;
      }
    }
  }
}