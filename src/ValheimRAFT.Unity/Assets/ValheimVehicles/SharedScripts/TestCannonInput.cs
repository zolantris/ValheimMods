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

    private void Awake()
    {
      TargetController= GetComponent<TargetController>();
    }

    private void Update()
    {
      var isSpacePressed = Input.GetKeyDown(KeyCode.Space);
      var isKey1MousePress = Input.GetMouseButtonDown(1);
      if (isSpacePressed || isKey1MousePress)
      {
        TargetController.StartFiring();
      }
    }
  }
}