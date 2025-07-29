using System.Collections.Generic;
using TMPro;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.UI;

public static class DropdownHelpers
{
  /// <summary>
  /// Sets up options on a TMP_Dropdown and selects the first option cleanly.
  /// </summary>
  public static void SetupOptionsAndSelectFirst(TMP_Dropdown dropdown, List<TMP_Dropdown.OptionData> options)
  {
    if (dropdown == null || options == null || options.Count == 0)
    {
      LoggerProvider.LogWarning("[DropdownHelpers] Dropdown or options list is null/empty. Cannot setup options.");
      return;
    }

    dropdown.ClearOptions();
    dropdown.AddOptions(options);
    dropdown.RefreshShownValue();
    dropdown.value = 0;
    dropdown.captionText.text = options[0].text; // Ensure caption displays first item
  }
}