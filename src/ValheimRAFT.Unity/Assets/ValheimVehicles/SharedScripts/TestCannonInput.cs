// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.Structs;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonTestInput : MonoBehaviour
  {
    public TargetController targetController;
    [SerializeField] public CannonDirectionGroup firingGroup = CannonDirectionGroup.Forward;

    public List<CannonFireData> cannonFireDataList = new();

    private void Start()
    {
      UpdateAllValues();
      cannonFireDataList = CannonFireData.CreateListOfCannonFireDataFromTargetController(targetController, targetController.GetCannonManualFiringGroup(firingGroup), out _, out _);
    }


    private void Update()
    {
      var isSpacePressed = Input.GetKeyDown(KeyCode.Space);
      var isKey1MousePress = Input.GetMouseButtonDown(1);
      if (isSpacePressed || isKey1MousePress)
      {
        targetController.StartManualGroupFiring(cannonFireDataList, firingGroup);
      }
    }

    private void OnEnable()
    {
      UpdateAllValues();
    }

    public void UpdateAllValues()
    {
      targetController = GetComponent<TargetController>();

      var wanderers = FindObjectsOfType<RandomWanderer>(true);
      if (wanderers.Length > 0)
      {
        foreach (var randomWanderer in wanderers)
        {
          if (randomWanderer.gameObject.name.Contains("player"))
          {
            targetController.AddPlayer(randomWanderer.transform);
          }
        }
      }
    }
  }
}