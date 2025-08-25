using TMPro;
using UnityEngine;
using ValheimVehicles.Prefabs;
namespace ValheimVehicles.ValheimVehicles.Integrations;

public class TMPProHelpers
{
  public static void Init()
  {
    var liberation = LoadValheimVehicleAssets.LiberationSansFontAsset;
    if (liberation)
    {
      TMP_Settings.defaultFontAsset = liberation;

      // Optional: set global fallbacks too
      if (TMP_Settings.fallbackFontAssets == null)
        TMP_Settings.fallbackFontAssets = new System.Collections.Generic.List<TMP_FontAsset>();

      if (!TMP_Settings.fallbackFontAssets.Contains(liberation))
        TMP_Settings.fallbackFontAssets.Add(liberation);

      Debug.Log("[TMP] Default font set to LiberationSans");
    }
  }
}