// using System.Collections;
// using System.Linq;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;
//
// [TestFixture]
// public class MovingTreadComponentTests
// {
//   private GameObject tankGameObject;
//   private MovingTreadComponent movingTreadComponent;
//
//   [SetUp]
//   public void SetUp()
//   {
//     // Create a new GameObject and add the MovingTreadComponent
//     tankGameObject = new GameObject();
//     movingTreadComponent = tankGameObject.AddComponent<MovingTreadComponent>();
//   }
//
//   [TearDown]
//   public void TearDown()
//   {
//     // Clean up after each test (destroy the created GameObject)
//     Object.Destroy(tankGameObject);
//   }
//
//   [Test]
//   public void TestTreadInitialization()
//   {
//     // Ensure the component is properly initialized
//     Assert.That(movingTreadComponent, Is.Not.Null);
//     Assert.That(0 == movingTreadComponent._movingTreads.Count);
//     Assert.That(0 == movingTreadComponent.treadTargetPoints.Count);
//   }
//
//   [Test]
//   public void TestTreadMovement()
//   {
//     // Simulate some setup (like moving treads)
//     movingTreadComponent.InitTreads(); // Initialize treads
//
//     // Check the initial number of treads and positions
//     Assert.That(movingTreadComponent._movingTreads.Count == 0); // Should be 0 at this point
//     Assert.That(movingTreadComponent.treadTargetPoints.Count == 0); // Add proper check for positions
//
//     // Mock a forward movement
//     movingTreadComponent.isForward = true;
//     movingTreadComponent.speedMultiplier = 1f;
//
//     // Run the FixedUpdate() for a few frames (simulate physics)
//     movingTreadComponent.FixedUpdate();
//
//     var rb = movingTreadComponent._movingTreads.First();
//
//     // need to find out exact value.
//     Assert.That(Mathf.Approximately(movingTreadComponent.treadProgress[rb], 0.1f));
//   }
//
//   [Test]
//   public void TestTreadRotationClockwise()
//   {
//     // Initialize treads
//     movingTreadComponent.InitTreads();
//
//     // Set the direction to forward (clockwise)
//     movingTreadComponent.isForward = true;
//
//     // Run the FixedUpdate() to simulate movement
//     movingTreadComponent.FixedUpdate();
//
//     // Check if the treads are moving in the expected direction
//     // You can inspect the rotation of the rigidbodies to ensure the clockwise movement is happening
//     // Use assertions to check that the treads rotate properly (if there's a specific angle you're expecting)
//     Assert.That(movingTreadComponent._movingTreads[0].transform.rotation.eulerAngles, Vector3.zero); // Just an example
//   }
//
//   [UnityTest]
//   public IEnumerator TestTreadMovementOverTime()
//   {
//     // Initialize the treads
//     movingTreadComponent.InitTreads();
//     movingTreadComponent.isForward = true;
//
//     // Start with speedMultiplier to ensure movement
//     movingTreadComponent.speedMultiplier = 1f;
//
//     // Simulate multiple fixed updates (frame time)
//     for (var i = 0; i < 10; i++)
//     {
//       movingTreadComponent.FixedUpdate();
//
//       // Assert the progress is changing (incrementing between 0 and 1)
//       foreach (var progress in movingTreadComponent.treadProgress.Values)
//       {
//         Assert.That(progress is >= 0f and <= 1f);
//       }
//
//       yield return null; // Wait for the next frame (FixedUpdate)
//     }
//   }
// }