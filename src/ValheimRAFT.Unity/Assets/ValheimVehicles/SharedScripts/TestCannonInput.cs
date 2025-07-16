// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonTestInput : MonoBehaviour
  {
    public TargetController TargetController;
    [SerializeField] public CannonDirectionGroup firingGroup = CannonDirectionGroup.Forward;

    private void Start()
    {
      UpdateAllValues();
    }


    private void Update()
    {
      var isSpacePressed = Input.GetKeyDown(KeyCode.Space);
      var isKey1MousePress = Input.GetMouseButtonDown(1);
      if (isSpacePressed || isKey1MousePress)
      {
        var randomVelocityModifier = CannonController.GetRandomCannonVelocity;
        var randomArcModifier = CannonController.GetRandomCannonArc;
        TargetController.StartManualGroupFiring(firingGroup, randomVelocityModifier, randomArcModifier);
      }
    }

    private void OnEnable()
    {
      UpdateAllValues();
    }

    public void UpdateAllValues()
    {
      TargetController = GetComponent<TargetController>();

      var wanderers = FindObjectsOfType<RandomWanderer>(true);
      if (wanderers.Length > 0)
      {
        foreach (var randomWanderer in wanderers)
        {
          if (randomWanderer.gameObject.name.Contains("player"))
          {
            TargetController.AddPlayer(randomWanderer.transform);
          }
        }
      }
    }
  }
}