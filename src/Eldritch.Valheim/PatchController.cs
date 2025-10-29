using HarmonyLib;
using Zolantris.Shared;

namespace Eldritch.Valheim;

public static class PatchController
{
  private static Harmony? _harmonyInstance;

  public static void Apply(string harmonyGuid)
  {
    _harmonyInstance = new Harmony(harmonyGuid);

    HarmonyHelper.TryPatchAll(_harmonyInstance,
      typeof(Patch_Humanoid),
      typeof(Patch_Character)
    );
  }

  public static void UnpatchSelf()
  {
    _harmonyInstance?.UnpatchSelf();
  }
}