using HarmonyLib;
using ValheimVehicles.Prefabs;
namespace ValheimVehicles.ValheimVehicles.Patches;

[HarmonyPatch]
public class ZDO_Patch
{
  [HarmonyPatch(typeof(ZDO), nameof(ZDO.GetPrefab))]
  [HarmonyPostfix]
  private static void ZDO_GetPrefab_AliasMigrationPatch(ZDO __instance, ref int __result)
  {
    var aliasHash = PrefabRegistryController.ResolveAliasedHash(__instance.m_prefab);
    if (aliasHash != __instance.m_prefab)
    {
      __instance.SetPrefab(aliasHash);
      __result = aliasHash;
    }
  }
}