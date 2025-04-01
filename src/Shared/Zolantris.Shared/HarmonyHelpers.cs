using System;
using HarmonyLib;
namespace Zolantris.Shared;

public static class HarmonyHelper
{
#if DEBUG
  private const bool IsDebug = true;
#else
  private const bool IsDebug = false;
#endif
  /// <summary>
  /// Safely patches all annotated methods in the specified class. This will ensure the mod does not
  /// </summary>
  /// <param name="harmonyInstance">Harmony instance.</param>
  /// <param name="type">Class containing Harmony patch methods.</param>
  public static void TryPatchAll(Harmony harmonyInstance, Type type)
  {
    try
    {
      harmonyInstance.PatchAll(type);
      if (IsDebug)
      {
        Jotunn.Logger.LogDebug($"[Harmony] Successfully patched: {type.Name}");
      }
    }
    catch (Exception ex)
    {
      Jotunn.Logger.LogError($"[Harmony] Failed to patch: {type.Name}\n{ex}");
    }
  }

  public static void TryPatchAll(Harmony harmonyInstance, params Type[] patchTypes)
  {
    foreach (var type in patchTypes)
    {
      TryPatchAll(harmonyInstance, type);
    }
  }
}