using HarmonyLib;
using ValheimVehicles.Propulsion.Rudder;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ShipControls_Patch
{
  [HarmonyPatch(typeof(ShipControlls), "Awake")]
  [HarmonyPrefix]
  private static bool ShipControlls_Awake(ShipControlls __instance)
  {
    var isSteeringWheelParent = __instance.GetComponentInParent<SteeringWheelComponent>();
    var mbRoot = isSteeringWheelParent != null
      ? isSteeringWheelParent.GetComponentInParent<MoveableBaseRootComponent>()
      : null;

    if (!(bool)isSteeringWheelParent || !(bool)mbRoot) return true;

    __instance.m_nview = __instance.GetComponent<ZNetView>();
    __instance.m_ship = mbRoot.m_ship;

    return true;
  }

  [HarmonyPatch(typeof(ShipControlls), "RPC_RequestControl")]
  [HarmonyPrefix]
  private static bool ShipControlls_RequestControl(ShipControlls __instance, long sender,
    long playerID)
  {
    if (!__instance.m_nview.IsOwner() || !__instance.m_ship.IsPlayerInBoat(playerID))
      return false;
    if (__instance.GetUser() == playerID || !__instance.HaveValidUser())
    {
      __instance.m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
      __instance.m_nview.InvokeRPC(sender, "RequestRespons", (object)true);
    }
    else
      __instance.m_nview.InvokeRPC(sender, "RequestRespons", (object)false);

    return false;
    // var isSteeringWheelParent = __instance.GetComponentInParent<SteeringWheelComponent>();
    // var mbRoot = isSteeringWheelParent != null
    //   ? isSteeringWheelParent.GetComponentInParent<MoveableBaseRootComponent>()
    //   : null;
    //
    // if (!(bool)isSteeringWheelParent || !(bool)mbRoot) return true;
    //
    // __instance.m_nview = __instance.GetComponent<ZNetView>();
    // __instance.m_ship = mbRoot.m_ship;
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
    }
  }

  [HarmonyPatch(typeof(ShipControlls), "GetHoverText")]
  [HarmonyPrefix]
  public static bool GetRudderHoverText(ShipControlls __instance, ref string __result)
  {
    var mbShip = __instance.GetComponentInParent<MoveableBaseShipComponent>();
    if (!mbShip)
    {
      return true;
    }

    var isAnchored =
      mbShip.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags
        .IsAnchored);

    var hoverText = SteeringWheelComponent.GetHoverTextFromShip(mbShip.m_baseRoot.totalSailArea,
      mbShip.m_baseRoot.TotalMass, mbShip.m_baseRoot.ShipMass, mbShip.m_baseRoot.ShipContainerMass,
      mbShip.m_baseRoot.GetSailingForce(),
      isAnchored, SteeringWheelComponent.GetAnchorHotkeyString());

    __result = hoverText;

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