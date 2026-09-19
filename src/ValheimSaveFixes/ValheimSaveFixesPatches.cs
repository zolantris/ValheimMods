using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Splatform;

namespace ValheimSaveFixes.Patches;

[HarmonyPatch]
internal static class SaveMountFixes
{
  /*
   * PlayerProfile disk write
   *
   * Game.SavePlayerProfile() eventually calls:
   *
   *   PlayerProfile.Save()
   *     -> SavePlayerToDisk()
   *
   * SavePlayerToDisk has its own CloudStorageSupported check/mount.
   * Therefore suppress that mount when THIS profile is Local.
   */

  private static bool ShouldMountCloudForProfileDisk(
    PlayerProfile profile)
  {
    if (!FileHelpers.CloudStorageSupported)
      return false;

    if (profile == null)
      return true;

    return profile.m_fileSource.IsCloud();
  }

  [HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> SavePlayerToDisk_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        nameof(ShouldMountCloudForProfileDisk));

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced &&
          instruction.Calls(cloudSupportedGetter))
      {
        /*
         * Original:
         *
         *   call FileHelpers.get_CloudStorageSupported()
         *
         * Replacement:
         *
         *   ldarg.0
         *   call ShouldMountCloudForProfileDisk(PlayerProfile)
         *
         * Mutating the original instruction avoids directly touching
         * Harmony labels/blocks and therefore avoids the Label/mscorlib
         * compile problem.
         */

        instruction.opcode = OpCodes.Ldarg_0;
        instruction.operand = null;

        yield return instruction;

        yield return new CodeInstruction(
          OpCodes.Call,
          replacement);

        replaced = true;
        continue;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        "[ValheimSaveFixes] Failed to patch cloud mount check in PlayerProfile.SavePlayerToDisk");
    }
  }

  /*
   * New world / character creation
   *
   * Vanilla selects Local when:
   *
   *   !CloudStorageSupportedAndEnabled || forceLocal
   *
   * but then checks CloudStorageSupported when deciding whether to mount.
   *
   * That means Steam storage can be mounted even though the new object
   * is explicitly going to Local storage.
   */

  private static bool ShouldMountCloudForCreation(bool forceLocal)
  {
    return !forceLocal &&
           FileHelpers.CloudStorageSupportedAndEnabled;
  }

  [HarmonyPatch(
    typeof(FejdStartup),
    nameof(FejdStartup.OnNewWorldDone),
    typeof(bool))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnNewWorldDone_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCreationCloudCheck(
      instructions,
      nameof(FejdStartup.OnNewWorldDone));
  }

  [HarmonyPatch(
    typeof(FejdStartup),
    nameof(FejdStartup.OnNewCharacterDone),
    typeof(bool))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnNewCharacterDone_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCreationCloudCheck(
      instructions,
      nameof(FejdStartup.OnNewCharacterDone));
  }

  private static IEnumerable<CodeInstruction> ReplaceCreationCloudCheck(
    IEnumerable<CodeInstruction> instructions,
    string methodName)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        nameof(ShouldMountCloudForCreation));

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced &&
          instruction.Calls(cloudSupportedGetter))
      {
        /*
         * Mutate the ORIGINAL instruction instead of creating a
         * replacement CodeInstruction.
         *
         * This preserves Harmony labels / exception blocks without
         * directly referencing System.Reflection.Emit.Label.
         *
         * Original:
         *
         *   call FileHelpers.get_CloudStorageSupported()
         *
         * Becomes:
         *
         *   ldarg.1
         *   call ShouldMountCloudForCreation(bool)
         */

        instruction.opcode = OpCodes.Ldarg_1;
        instruction.operand = null;

        yield return instruction;
        yield return new CodeInstruction(
          OpCodes.Call,
          replacement);

        replaced = true;
        continue;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        $"[ValheimSaveFixes] Failed to patch cloud mount check in {methodName}");
    }
  }

  /*
   * Manual save
   *
   * Menu.OnManualSave() should only perform its outer Steam/platform
   * storage mount when at least one object involved in the save actually
   * resides in Cloud storage.
   */

  private static bool ShouldMountCloudForManualSave()
  {
    if (!FileHelpers.CloudStorageSupported)
      return false;

    var world = ZNet.m_world;
    var profile = Game.instance?.GetPlayerProfile();

    /*
     * Unknown lifecycle state:
     * preserve vanilla behavior rather than assuming Local.
     */
    if (world == null || profile == null)
      return true;

    return world.m_fileSource.IsCloud() ||
           profile.m_fileSource.IsCloud();
  }

  [HarmonyPatch(typeof(Menu), nameof(Menu.OnManualSave))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnManualSave_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCloudSupportedCheck(
      instructions,
      nameof(ShouldMountCloudForManualSave),
      "Menu.OnManualSave");
  }

  /*
   * Player profile saving
   *
   * This is REQUIRED.
   *
   * ZNet.RPC_Save() does:
   *
   *   Game.instance.SavePlayerProfile(true);
   *   Save(...);
   *
   * so the character save happens BEFORE the world save.
   *
   * Vanilla Game.SavePlayerProfile() performs its own platform storage
   * mount based on CloudStorageSupported, even when the profile itself
   * is Local.
   *
   * This was the mount still killing your manual save.
   */

  private static bool ShouldMountCloudForPlayerSave()
  {
    if (!FileHelpers.CloudStorageSupported)
      return false;

    var profile = Game.instance?.GetPlayerProfile();

    /*
     * Preserve vanilla behavior if called during an unexpected state.
     */
    if (profile == null)
      return true;

    return profile.m_fileSource.IsCloud();
  }

  [HarmonyPatch(
    typeof(Game),
    nameof(Game.SavePlayerProfile),
    typeof(bool),
    typeof(bool))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> SavePlayerProfile_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCloudSupportedCheck(
      instructions,
      nameof(ShouldMountCloudForPlayerSave),
      "Game.SavePlayerProfile");
  }

  /*
   * World saving
   *
   * SaveWorldThread() independently checks CloudStorageSupported and
   * attempts another mount before writing the world.
   *
   * Local worlds do not need that mount.
   */

  private static bool ShouldMountCloudForWorldSave()
  {
    if (!FileHelpers.CloudStorageSupported)
      return false;

    var world = ZNet.m_world;

    /*
     * Preserve vanilla behavior in an unexpected state.
     */
    if (world == null)
      return true;

    return world.m_fileSource.IsCloud();
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> SaveWorldThread_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCloudSupportedCheck(
      instructions,
      nameof(ShouldMountCloudForWorldSave),
      "ZNet.SaveWorldThread");
  }

  /*
   * Generic replacement for no-argument cloud predicates.
   *
   * IMPORTANT:
   *
   * We mutate the existing CodeInstruction instead of constructing
   * another instruction and copying labels/blocks.
   *
   * This both:
   *
   *   1. Preserves Harmony branch labels and exception blocks.
   *   2. Avoids the mscorlib/System.Reflection.Emit.Label compile issue
   *      this project's reference setup currently produces.
   */

  private static IEnumerable<CodeInstruction> ReplaceCloudSupportedCheck(
    IEnumerable<CodeInstruction> instructions,
    string replacementMethodName,
    string patchedMethodName)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        replacementMethodName);

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced &&
          instruction.Calls(cloudSupportedGetter))
      {
        /*
         * Original:
         *
         *   call FileHelpers.get_CloudStorageSupported()
         *
         * Replacement:
         *
         *   call ShouldMountCloudForX()
         *
         * Same stack signature: () -> bool.
         *
         * Mutating preserves labels and exception blocks automatically.
         */
        instruction.opcode = OpCodes.Call;
        instruction.operand = replacement;

        replaced = true;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        $"[ValheimSaveFixes] Failed to patch cloud mount check in {patchedMethodName}");
    }
  }

  /*
   * FileHelpers.Unmount reference-counter protection
   *
   * Vanilla immediately performs:
   *
   *   --m_depotReferenceCounter;
   *
   * The counter is uint.
   *
   * If an Unmount happens while it is already zero, it wraps to
   * uint.MaxValue and poisons all future reference-count bookkeeping.
   */

  [HarmonyPatch(
    typeof(FileHelpers),
    nameof(FileHelpers.Unmount),
    typeof(UnmountMode))]
  [HarmonyPrefix]
  private static bool FileHelpers_Unmount_Prefix(
    uint ___m_depotReferenceCounter)
  {
    if (___m_depotReferenceCounter != 0)
      return true;

    ZLog.LogError(
      "[ValheimSaveFixes] Blocked FileHelpers.Unmount() because the depot reference counter was already 0.");

    return false;
  }
}