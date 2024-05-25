using HarmonyLib;
using ValheimRAFT.Util;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ZDO_Patch
{
  [HarmonyPatch(typeof(ZDO), "Deserialize")]
  [HarmonyPostfix]
  private static void ZDO_Deserialize(ZDO __instance, ZPackage pkg)
  {
    ZDOLoaded(__instance);
  }

  [HarmonyPatch(typeof(ZDO), "Load")]
  [HarmonyPostfix]
  private static void ZDO_Load(ZDO __instance, ZPackage pkg, int version)
  {
    ZDOLoaded(__instance);
  }

  private static void ZDOLoaded(ZDO zdo)
  {
    ZDOPersistentID.Instance.Register(zdo);
    MoveableBaseRootComponent.InitZDO(zdo);
    BaseVehicleController.InitZdo(zdo);
    PlayerSpawnController.InitZdo(zdo);
  }

  [HarmonyPatch(typeof(ZDO), "Reset")]
  [HarmonyPrefix]
  private static void ZDO_Reset(ZDO __instance)
  {
    ZDOUnload(__instance);
  }

  public static void ZDOUnload(ZDO zdo)
  {
    MoveableBaseRootComponent.RemoveZDO(zdo);
    BaseVehicleController.RemoveZDO(zdo);
    ZDOPersistentID.Instance.Unregister(zdo);
  }
}