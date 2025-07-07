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
    [SerializeField] public int firingGroup;

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
        TargetController.StartManualFiring(firingGroup);
      }
    }

    private void OnEnable()
    {
      UpdateAllValues();
    }

    public void UpdateAllValues()
    {
      TargetController= GetComponent<TargetController>();

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