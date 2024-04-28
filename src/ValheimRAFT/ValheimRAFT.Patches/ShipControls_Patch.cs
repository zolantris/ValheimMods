using HarmonyLib;
using ValheimVehicles.Propulsion.Rudder;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ShipControls_Patch
{
  [HarmonyPatch(typeof(ShipControlls), "Awake")]
  [HarmonyPrefix]
  private static bool ShipControlls_Awake(Ship __instance)
  {
    return !__instance.GetComponentInParent<RudderWheelComponent>();
  }

  [HarmonyPatch(typeof(ShipControlls), "Interact")]
  [HarmonyPrefix]
  private static void Interact(ShipControlls __instance, Humanoid character)
  {
    if (character == Player.m_localPlayer && __instance.isActiveAndEnabled)
    {
      var baseRoot = __instance.GetComponentInParent<MoveableBaseRootComponent>();
      if (baseRoot != null)
      {
        baseRoot.ComputeAllShipContainerItemWeight();
      }

      PatchSharedData.PlayerLastUsedControls = __instance;
      __instance.m_ship.m_controlGuiPos.position = __instance.transform.position;
    }
  }

  [HarmonyPatch(typeof(ShipControlls), "GetHoverText")]
  [HarmonyPrefix]
  public static bool GetRudderHoverText(ShipControlls __instance, ref string __result)
  {
    var baseRoot = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if (!baseRoot)
    {
      return true;
    }

    var shipStatsText = "";

    if (ValheimRaftPlugin.Instance.ShowShipStats.Value)
    {
      var shipMassToPush = ValheimRaftPlugin.Instance.MassPercentageFactor.Value;
      shipStatsText += $"\nsailArea: {baseRoot.GetTotalSailArea()}";
      shipStatsText += $"\ntotalMass: {baseRoot.TotalMass}";
      shipStatsText +=
        $"\nshipMass(no-containers): {baseRoot.ShipMass}";
      shipStatsText += $"\nshipContainerMass: {baseRoot.ShipContainerMass}";
      shipStatsText +=
        $"\ntotalMassToPush: {shipMassToPush}% * {baseRoot.TotalMass} = {baseRoot.TotalMass * shipMassToPush / 100f}";
      shipStatsText +=
        $"\nshipPropulsion: {baseRoot.GetSailingForce()}";

      // final formatting
      shipStatsText = $"<color=white>{shipStatsText}</color>";
    }

    var isAnchored =
      baseRoot.shipController.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags
        .IsAnchored);
    var anchoredStatus = isAnchored
      ? "[<color=red><b>$valheim_vehicles_wheel_use_anchored</b></color>]"
      : "";
    var anchorText =
      isAnchored
        ? "$valheim_vehicles_wheel_use_anchor_disable_detail"
        : "$valheim_vehicles_wheel_use_anchor_enable_detail";
    var anchorKey =
      ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set"
        ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
        : ZInput.instance.GetBoundKeyString("Run");
    __result =
      Localization.instance.Localize(
        $"[<color=yellow><b>$KEY_Use</b></color>] <color=white><b>$valheim_vehicles_wheel_use</b></color> {anchoredStatus}\n[<color=yellow><b>{anchorKey}</b></color>] <color=white>{anchorText}</color> {shipStatsText}");

    return false;
  }

  [HarmonyPatch(typeof(ShipControlls), "RPC_RequestRespons")]
  [HarmonyPrefix]
  private static bool ShipControlls_RPC_RequestRespons(ShipControlls __instance, long sender,
    bool granted)
  {
    if (__instance != PatchSharedData.PlayerLastUsedControls)
    {
      PatchSharedData.PlayerLastUsedControls.RPC_RequestRespons(sender, granted);
      return false;
    }

    return true;
  }
}