// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonTestInput : MonoBehaviour
  {
    private CannonController[] _cannons;

    private void Awake()
    {
      // Find all cannons in this hierarchy
      _cannons = GetComponentsInChildren<CannonController>(true);
    }

    private void Update()
    {
      var isSpacePressed = Input.GetKeyDown(KeyCode.Space);
      var isKey1MousePress = Input.GetMouseButtonDown(1);
      if (isSpacePressed || isKey1MousePress)
      {
        foreach (var cannon in _cannons)
        {
          cannon.Fire();
        }
      }
    }

    // Optional: Re-scan if cannons are added/removed at runtime
    public void RefreshCannons()
    {
      _cannons = GetComponentsInChildren<CannonController>(true);
    }
  }
}