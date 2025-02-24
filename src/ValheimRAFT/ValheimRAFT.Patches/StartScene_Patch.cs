using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.ModSupport;

namespace ValheimRAFT.Patches;

/// <summary>
/// Meant only to be applied if DebugRemoveStartMenuBackground = true
/// </summary>
public class StartScene_Patch
{
  [HarmonyPatch(typeof(MenuScene), "Awake")]
  [HarmonyPrefix]
  public static bool MenuScene_Awake(MenuScene __instance)
  {
    // destroy is pretty heavy and will break ui layering.
    // Object.Destroy(__instance.gameObject);

    // top level root objects
    var staticObj = GameObject.Find("Static");
    var backgroundSceneObj = GameObject.Find("Backgroundscene");

    // most of the valheim terrain is here
    if (staticObj != null)
    {
      staticObj.SetActive(false);
    }

    // grass is inside the BackgroundSceneObject
    if (backgroundSceneObj != null)
    {
      var grassRoot = backgroundSceneObj.transform.Find("grassroot");
      if (grassRoot != null)
      {
        grassRoot.gameObject.SetActive(false);
      }
    }
    
    return false;
  }

  // do nothing, this breaks character selection but makes it really easy to just load a empty black scene for valheim.
  [HarmonyPatch(typeof(FejdStartup), "UpdateCamera")]
  [HarmonyPrefix]
  public static bool FejdStartup_UpdateCamera_Patch()
  {
    return false;
  }

  // AudioMan returns a NRE if the StartScene is nuked
  [HarmonyPatch(typeof(AudioMan), "Update")]
  [HarmonyPrefix]
  public static bool AudioMan_Update(AudioMan __instance)
  {
    return __instance.GetActiveAudioListener() != null;
  }
}