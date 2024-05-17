using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestRunner;
using UnityEngine;
using UnityEngine.TestTools;
using ValheimVehicles.Vehicles;
using Debug = System.Diagnostics.Debug;

// using Assert = UnityEngine.Assertions.Assert; 

namespace ModTesting
{
  public class VehicleBoundsComponent
  {
    [Test]
    public static void Test1()
    {
      // var objectPosition = new Vector3(200, 0, 50);
      var parentGameObject = new GameObject();
      Assert.That(true);
    }


    // [Test]
    // public void CanTransformGlobalPositionWithLocalScale1()
    // {
    //   // setup
    //   // parent of collider GameObject and global positioned item
    //   var objectPosition = new Vector3(200, 0, 50);
    //   var parentGameObject = new GameObject
    //   {
    //     transform =
    //     {
    //       position = objectPosition
    //     }
    //   };
    //
    //   // collider GameObject transform
    //   var childColliderGameObject = new GameObject();
    //   var childColliderGameObjectLocalPosition = new Vector3(0.5f, 1, 2);
    //   var childColliderGameObjectLocalScale = new Vector3(1, 1, 1);
    //
    //   // collider
    //   var boxColliderCenterPosition = new Vector3(202, 1, 28);
    //   var boxColliderSize = new Vector3(2, 2, 2);
    //
    //
    //   childColliderGameObject.transform.SetParent(parentGameObject.transform);
    //   childColliderGameObject.transform.localPosition = childColliderGameObjectLocalPosition;
    //   childColliderGameObject.transform.localScale = childColliderGameObjectLocalPosition;
    //
    //   var boxCollider = childColliderGameObject.AddComponent<BoxCollider>();
    //   boxCollider.center = boxColliderCenterPosition;
    //   boxCollider.size = boxColliderSize;
    //
    //   var colliders = parentGameObject.GetComponentsInChildren<Collider>();
    //
    //   // Assert.IsTrue(colliders.Length == 1);
    //
    //   var firstCollider = colliders.FirstOrDefault();
    //
    //   var localBoundsCenter = Vector3.zero;
    //   var localBoundsSize = Vector3.one;
    //
    //
    //   // test
    //   // Debug.Assert(firstCollider != null, nameof(firstCollider) + " != null");
    //   var output = BaseVehicleController.TransformColliderGlobalBoundsToLocal(firstCollider,
    //     parentGameObject, localBoundsCenter, localBoundsSize);
    //   // Assert.Equals(output, true);
    // }
    //
    // [TestFixture]
    // public class TransformColliderGlobalBoundsToLocal
    // {
    //
    //   [UnityTest]
    //   public void CanTransformGlobalPositionWithLocalScale1()
    //   {
    //     // setup
    //     // parent of collider GameObject and global positioned item
    //     var objectPosition = new Vector3(200, 0, 50);
    //     var parentGameObject = new GameObject
    //     {
    //       transform =
    //       {
    //         position = objectPosition
    //       }
    //     };
    //
    //     // collider GameObject transform
    //     var childColliderGameObject = new GameObject();
    //     var childColliderGameObjectLocalPosition = new Vector3(0.5f, 1, 2);
    //     var childColliderGameObjectLocalScale = new Vector3(1, 1, 1);
    //
    //     // collider
    //     var boxColliderCenterPosition = new Vector3(202, 1, 28);
    //     var boxColliderSize = new Vector3(2, 2, 2);
    //
    //
    //     childColliderGameObject.transform.SetParent(parentGameObject.transform);
    //     childColliderGameObject.transform.localPosition = childColliderGameObjectLocalPosition;
    //     childColliderGameObject.transform.localScale = childColliderGameObjectLocalPosition;
    //
    //     var boxCollider = childColliderGameObject.AddComponent<BoxCollider>();
    //     boxCollider.center = boxColliderCenterPosition;
    //     boxCollider.size = boxColliderSize;
    //
    //     var colliders = parentGameObject.GetComponentsInChildren<Collider>();
    //
    //     // Assert.IsTrue(colliders.Length == 1);
    //
    //     var firstCollider = colliders.FirstOrDefault();
    //
    //     var localBoundsCenter = Vector3.zero;
    //     var localBoundsSize = Vector3.one;
    //
    //
    //     // test
    //     // Debug.Assert(firstCollider != null, nameof(firstCollider) + " != null");
    //     var output = BaseVehicleController.TransformColliderGlobalBoundsToLocal(firstCollider,
    //       parentGameObject, localBoundsCenter, localBoundsSize);
    //     // Assert.Equals(output, true);
    //   }
    // }
  }
}