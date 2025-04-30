#region

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static Toggle AddToggleRow(Transform parent, string label, bool initial, UnityAction<bool> onChanged)
    {
      var row = CreateRow(parent, label, out _);

      var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
      toggleGO.transform.SetParent(row.transform, false);
      var toggle = toggleGO.GetComponent<Toggle>();
      toggle.isOn = initial;
      toggle.onValueChanged.AddListener(onChanged);

      return toggle;
    }
  }
}