using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT.Util;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class ValheimRAFT_Patch
{
  public static float yawOffset;

  private static ShipControlls m_lastUsedControls;

  internal static Piece m_lastRayPiece;

  public static bool m_disableCreateDestroy;

  [HarmonyPatch(typeof(ShipControlls), "Awake")]
  [HarmonyPrefix]
  private static bool ShipControlls_Awake(Ship __instance)
  {
    return !__instance.GetComponentInParent<RudderComponent>();
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

      m_lastUsedControls = __instance;
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
      baseRoot.shipController.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored);
    var anchoredStatus = isAnchored ? "[<color=red><b>$mb_rudder_use_anchored</b></color>]" : "";
    var anchorText =
      isAnchored
        ? "$mb_rudder_use_anchor_disable_detail"
        : "$mb_rudder_use_anchor_enable_detail";
    var anchorKey =
      ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set"
        ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
        : ZInput.instance.GetBoundKeyString("Run");
    __result =
      Localization.instance.Localize(
        $"[<color=yellow><b>$KEY_Use</b></color>] <color=white><b>$mb_rudder_use</b></color> {anchoredStatus}\n[<color=yellow><b>{anchorKey}</b></color>] <color=white>{anchorText}</color> {shipStatsText}");

    return false;
  }

  [HarmonyPatch(typeof(ShipControlls), "RPC_RequestRespons")]
  [HarmonyPrefix]
  private static bool ShipControlls_RPC_RequestRespons(ShipControlls __instance, long sender,
    bool granted)
  {
    if (__instance != m_lastUsedControls)
    {
      m_lastUsedControls.RPC_RequestRespons(sender, granted);
      return false;
    }

    return true;
  }

  [HarmonyPatch(typeof(Ship), "Awake")]
  [HarmonyPostfix]
  private static void Ship_Awake(Ship __instance)
  {
    if ((bool)__instance.m_nview && __instance.m_nview.m_zdo != null &&
        __instance.name.StartsWith("MBRaft"))
    {
      var ladders = __instance.GetComponentsInChildren<Ladder>();
      for (var i = 0; i < ladders.Length; i++) ladders[i].m_useDistance = 10f;
      __instance.gameObject.AddComponent<MoveableBaseShipComponent>();
    }
  }

  [HarmonyPatch(typeof(Ship), "UpdateUpsideDmg")]
  [HarmonyPrefix]
  private static bool Ship_UpdateUpsideDmg(Ship __instance)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb && __instance.transform.up.y < 0f)
      __instance.m_body.rotation = Quaternion.Euler(new Vector3(
        __instance.m_body.rotation.eulerAngles.x, __instance.m_body.rotation.eulerAngles.y, 0f));
    return !mb;
  }

  [HarmonyPatch(typeof(Ship), "UpdateSail")]
  [HarmonyPostfix]
  private static void Ship_UpdateSail(Ship __instance)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !mb.m_baseRoot) return;
    for (var i = 0; i < mb.m_baseRoot.m_mastPieces.Count; i++)
    {
      var mast = mb.m_baseRoot.m_mastPieces[i];
      if (!mast)
      {
        mb.m_baseRoot.m_mastPieces.RemoveAt(i);
        i--;
      }
      else if (mast.m_allowSailRotation)
      {
        var newRotation = __instance.m_mastObject.transform.localRotation;
        mast.transform.localRotation = newRotation;
      }
    }
  }

  [HarmonyPatch(typeof(Ship), "UpdateSail")]
  [HarmonyPostfix]
  private static void Ship_UpdateSailSize(Ship __instance)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !mb.m_baseRoot) return;
    for (var j = 0; j < mb.m_baseRoot.m_mastPieces.Count; j++)
    {
      var mast = mb.m_baseRoot.m_mastPieces[j];
      if (!mast)
      {
        mb.m_baseRoot.m_mastPieces.RemoveAt(j);
        j--;
      }
      else if (mast.m_allowSailShrinking)
      {
        if (mast.m_sailObject.transform.localScale != __instance.m_sailObject.transform.localScale)
          mast.m_sailCloth.enabled = false;
        mast.m_sailObject.transform.localScale = __instance.m_sailObject.transform.localScale;
        mast.m_sailCloth.enabled = __instance.m_sailCloth.enabled;
      }
      else
      {
        mast.m_sailObject.transform.localScale = Vector3.one;
        mast.m_sailCloth.enabled = !mast.m_disableCloth;
      }
    }

    for (var i = 0; i < mb.m_baseRoot.m_rudderPieces.Count; i++)
    {
      var rudder = mb.m_baseRoot.m_rudderPieces[i];
      if (!rudder)
      {
        mb.m_baseRoot.m_rudderPieces.RemoveAt(i);
        i--;
      }
      else if ((bool)rudder.m_wheel)
      {
        rudder.m_wheel.localRotation = Quaternion.Slerp(rudder.m_wheel.localRotation,
          Quaternion.Euler(
            __instance.m_rudderRotationMax * (0f - __instance.m_rudderValue) *
            rudder.m_wheelRotationFactor, 0f, 0f), 0.5f);
      }
    }
  }

  private static Vector3 CalculateAnchorStopVelocity(Vector3 currentVelocity)
  {
    var zeroVelocity = Vector3.zero;
    return Vector3.SmoothDamp(currentVelocity * 0.5f, Vector3.zero, ref zeroVelocity, 5f);
  }

  [HarmonyPatch(typeof(Ship), "CustomFixedUpdate")]
  [HarmonyPrefix]
  private static bool Ship_FixedUpdate(Ship __instance)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !__instance.m_nview || __instance.m_nview.m_zdo == null) return true;

    /*
     * creative mode should not allows movement and applying force on a object will cause errors when the object is kinematic
     */
    if (mb.isCreative)
    {
      return false;
    }

    // This could be the spot that causes the raft to fly at spawn
    mb.m_targetHeight = __instance.m_nview.m_zdo.GetFloat("MBTargetHeight", mb.m_targetHeight);
    mb.m_flags =
      (MoveableBaseShipComponent.MBFlags)__instance.m_nview.m_zdo.GetInt("MBFlags",
        (int)mb.m_flags);

    // This could be the spot that causes the raft to fly at spawn
    mb.m_zsync.m_useGravity = mb.m_targetHeight == 0f;

    var flag = __instance.HaveControllingPlayer();

    __instance.UpdateControlls(Time.fixedDeltaTime);
    __instance.UpdateSail(Time.fixedDeltaTime);
    __instance.UpdateRudder(Time.fixedDeltaTime, flag);
    if (__instance.m_players.Count == 0 ||
        mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
    {
      __instance.m_speed = Ship.Speed.Stop;
      __instance.m_rudderValue = 0f;
      if (!mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
      {
        mb.m_flags |= MoveableBaseShipComponent.MBFlags.IsAnchored;
        __instance.m_nview.m_zdo.Set("MBFlags", (int)mb.m_flags);
      }
    }

    if ((bool)__instance.m_nview && !__instance.m_nview.IsOwner()) return false;
    __instance.UpdateUpsideDmg(Time.fixedDeltaTime);
    if (!flag && (__instance.m_speed == Ship.Speed.Slow || __instance.m_speed == Ship.Speed.Back))
      __instance.m_speed = Ship.Speed.Stop;
    var worldCenterOfMass = __instance.m_body.worldCenterOfMass;
    var vector = __instance.m_floatCollider.transform.position +
                 __instance.m_floatCollider.transform.forward * __instance.m_floatCollider.size.z /
                 2f;
    var vector2 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.forward * __instance.m_floatCollider.size.z /
                  2f;
    var vector3 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.right * __instance.m_floatCollider.size.x /
                  2f;
    var vector4 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.right * __instance.m_floatCollider.size.x /
                  2f;
    var waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref __instance.m_previousCenter);
    var waterLevel2 = Floating.GetWaterLevel(vector3, ref __instance.m_previousLeft);
    var waterLevel3 = Floating.GetWaterLevel(vector4, ref __instance.m_previousRight);
    var waterLevel4 = Floating.GetWaterLevel(vector, ref __instance.m_previousForward);
    var waterLevel5 = Floating.GetWaterLevel(vector2, ref __instance.m_previousBack);
    var averageWaterHeight =
      (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
    var currentDepth = worldCenterOfMass.y - averageWaterHeight - __instance.m_waterLevelOffset;
    if (!(currentDepth > __instance.m_disableLevel))
    {
      mb.UpdateStats(false);
      __instance.m_body.WakeUp();
      __instance.UpdateWaterForce(currentDepth, Time.fixedDeltaTime);
      var vector5 = new Vector3(vector3.x, waterLevel2, vector3.z);
      var vector6 = new Vector3(vector4.x, waterLevel3, vector4.z);
      var vector7 = new Vector3(vector.x, waterLevel4, vector.z);
      var vector8 = new Vector3(vector2.x, waterLevel5, vector2.z);
      var fixedDeltaTime = Time.fixedDeltaTime;
      var num3 = fixedDeltaTime * 50f;
      var num4 = Mathf.Clamp01(Mathf.Abs(currentDepth) / __instance.m_forceDistance);
      var vector9 = Vector3.up * __instance.m_force * num4;
      __instance.m_body.AddForceAtPosition(vector9 * num3, worldCenterOfMass,
        ForceMode.VelocityChange);
      var num5 = Vector3.Dot(__instance.m_body.velocity, __instance.transform.forward);
      var num6 = Vector3.Dot(__instance.m_body.velocity, __instance.transform.right);
      var velocity = __instance.m_body.velocity;
      var value = velocity.y * velocity.y * Mathf.Sign(velocity.y) * __instance.m_damping * num4;
      var value2 = num5 * num5 * Mathf.Sign(num5) * __instance.m_dampingForward * num4;
      var value3 = num6 * num6 * Mathf.Sign(num6) * __instance.m_dampingSideway * num4;
      velocity.y -= Mathf.Clamp(value, -1f, 1f);
      velocity -= __instance.transform.forward * Mathf.Clamp(value2, -1f, 1f);
      velocity -= __instance.transform.right * Mathf.Clamp(value3, -1f, 1f);
      if (velocity.magnitude > __instance.m_body.velocity.magnitude)
        velocity = velocity.normalized * __instance.m_body.velocity.magnitude;
      if (__instance.m_players.Count == 0 ||
          mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(velocity);
        velocity = anchoredVelocity;
      }

      __instance.m_body.velocity = velocity;
      __instance.m_body.angularVelocity -=
        __instance.m_body.angularVelocity * __instance.m_angularDamping * num4;
      var num7 = 0.15f;
      var num8 = 0.5f;
      var f = Mathf.Clamp((vector7.y - vector.y) * num7, 0f - num8, num8);
      var f2 = Mathf.Clamp((vector8.y - vector2.y) * num7, 0f - num8, num8);
      var f3 = Mathf.Clamp((vector5.y - vector3.y) * num7, 0f - num8, num8);
      var f4 = Mathf.Clamp((vector6.y - vector4.y) * num7, 0f - num8, num8);
      f = Mathf.Sign(f) * Mathf.Abs(Mathf.Pow(f, 2f));
      f2 = Mathf.Sign(f2) * Mathf.Abs(Mathf.Pow(f2, 2f));
      f3 = Mathf.Sign(f3) * Mathf.Abs(Mathf.Pow(f3, 2f));
      f4 = Mathf.Sign(f4) * Mathf.Abs(Mathf.Pow(f4, 2f));
      __instance.m_body.AddForceAtPosition(Vector3.up * f * num3, vector, ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * f2 * num3, vector2,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * f3 * num3, vector3,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * f4 * num3, vector4,
        ForceMode.VelocityChange);
      ApplySailForce(__instance, num5);
      __instance.ApplyEdgeForce(Time.fixedDeltaTime);
      if (mb.m_targetHeight > 0f)
      {
        var centerpos = __instance.m_floatCollider.transform.position;
        var centerforce = GetUpwardsForce(mb.m_targetHeight,
          centerpos.y + __instance.m_body.velocity.y, mb.m_liftForce);
        __instance.m_body.AddForceAtPosition(Vector3.up * centerforce, centerpos,
          ForceMode.VelocityChange);
      }
    }
    else if (mb.m_targetHeight > 0f)
    {
      if (__instance.m_players.Count == 0 ||
          mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(__instance.m_body.velocity);
        __instance.m_body.velocity = anchoredVelocity;
      }

      mb.UpdateStats(true);
      var side1 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.forward * __instance.m_floatCollider.size.z /
                  2f;
      var side2 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.forward * __instance.m_floatCollider.size.z /
                  2f;
      var side3 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.right * __instance.m_floatCollider.size.x /
                  2f;
      var side4 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.right * __instance.m_floatCollider.size.x /
                  2f;
      var centerpos2 = __instance.m_floatCollider.transform.position;
      var corner1curforce = __instance.m_body.GetPointVelocity(side1);
      var corner2curforce = __instance.m_body.GetPointVelocity(side2);
      var corner3curforce = __instance.m_body.GetPointVelocity(side3);
      var corner4curforce = __instance.m_body.GetPointVelocity(side4);
      var side1force =
        GetUpwardsForce(mb.m_targetHeight, side1.y + corner1curforce.y, mb.m_balanceForce);
      var side2force =
        GetUpwardsForce(mb.m_targetHeight, side2.y + corner2curforce.y, mb.m_balanceForce);
      var side3force =
        GetUpwardsForce(mb.m_targetHeight, side3.y + corner3curforce.y, mb.m_balanceForce);
      var side4force =
        GetUpwardsForce(mb.m_targetHeight, side4.y + corner4curforce.y, mb.m_balanceForce);
      var centerforce2 = GetUpwardsForce(mb.m_targetHeight,
        centerpos2.y + __instance.m_body.velocity.y, mb.m_liftForce);
      __instance.m_body.AddForceAtPosition(Vector3.up * side1force, side1,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * side2force, side2,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * side3force, side3,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * side4force, side4,
        ForceMode.VelocityChange);
      __instance.m_body.AddForceAtPosition(Vector3.up * centerforce2, centerpos2,
        ForceMode.VelocityChange);
      var dir = Vector3.Dot(__instance.m_body.velocity, __instance.transform.forward);
      ApplySailForce(__instance, dir);
    }

    return false;
  }


  /**
   * this is disabled for now, but in the future this calc will need to be overridden based on number of sails and ship weight/size
   */
  // [HarmonyPatch(typeof(Ship), "GetSailForce")]
  // private static class ChangeShipBaseSpeed
  // {
  //   private static bool Prefix(Ship __instance, ref Vector3 __result, float sailSize, float dt)
  //   {
  //     var windDir = EnvMan.instance.GetWindDir();
  //     var windIntensity = Mathf.Lerp(0.25f, 1f, EnvMan.instance.GetWindIntensity());
  //     var windIntensityAndAngleFactor = __instance.GetWindAngleFactor() * windIntensity;
  //     var forward = __instance.transform.forward;
  //
  //     var windDirAndForwardVector = Vector3.Normalize(windDir + forward);
  //
  //     var outputSailForce = Vector3.SmoothDamp(__instance.m_sailForce,
  //       windDirAndForwardVector * windIntensityAndAngleFactor * __instance.m_sailForceFactor *
  //       sailSize,
  //       ref __instance.m_windChangeVelocity, 1f, 1000f);
  //
  //     Logger.LogDebug(
  //       $"GetSailForce, m_sailForce {__instance.m_sailForce} m_windDir+forward {windDirAndForwardVector} windIntensity: {windIntensity}");
  //     Logger.LogDebug($"SailSize: {sailSize}");
  //     Logger.LogDebug(
  //       "Calcs for windDirAndForwardVector * windIntensityAndAngleFactor * __instance.m_sailForceFactor * sailSize");
  //
  //     __instance.m_sailForce = outputSailForce;
  //
  //     Logger.LogDebug($"Ship sailforce: {__instance.m_sailForce}");
  //     __result = __instance.m_sailForce;
  //
  //
  //     return false;
  //   }
  // }
  private static void ApplySailForce(Ship __instance, float num5)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();

    var sailArea = 0f;

    if (mb.m_baseRoot)
    {
      sailArea = mb.m_baseRoot.GetSailingForce();
    }

    /*
     * Computed sailSpeed based on the rudder settings.
     */
    switch (__instance.m_speed)
    {
      case Ship.Speed.Full:
        break;
      case Ship.Speed.Half:
        sailArea *= 0.5f;
        break;
      case Ship.Speed.Slow:
        sailArea = Math.Min(0.1f, sailArea * 0.1f);
        break;
      case Ship.Speed.Stop:
      case Ship.Speed.Back:
      default:
        sailArea = 0f;
        break;
    }

    if (mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
    {
      sailArea = 0f;
    }

    var sailForce = __instance.GetSailForce(sailArea, Time.fixedDeltaTime);

    var position = __instance.m_body.worldCenterOfMass;


    //  * Math.Max(0.5f, ValheimRaftPlugin.Instance.RaftSailForceMultiplier.Value)
    // set the speed, this may need to be converted to a vector for the multiplier
    __instance.m_body.AddForceAtPosition(
      sailForce,
      position,
      ForceMode.VelocityChange);

    var stearoffset = __instance.m_floatCollider.transform.position -
                      __instance.m_floatCollider.transform.forward *
                      __instance.m_floatCollider.size.z / 2f;
    var num7 = num5 * __instance.m_stearVelForceFactor;
    __instance.m_body.AddForceAtPosition(
      __instance.transform.right * num7 * (0f - __instance.m_rudderValue) * Time.fixedDeltaTime,
      stearoffset, ForceMode.VelocityChange);
    var stearforce = Vector3.zero;
    switch (__instance.m_speed)
    {
      case Ship.Speed.Slow:
        stearforce += __instance.transform.forward * __instance.m_backwardForce *
                      (1f - Mathf.Abs(__instance.m_rudderValue));
        break;
      case Ship.Speed.Back:
        stearforce += -__instance.transform.forward * __instance.m_backwardForce *
                      (1f - Mathf.Abs(__instance.m_rudderValue));
        break;
    }

    if (__instance.m_speed == Ship.Speed.Back || __instance.m_speed == Ship.Speed.Slow)
    {
      float num6 = __instance.m_speed != Ship.Speed.Back ? 1 : -1;
      stearforce += __instance.transform.right * __instance.m_stearForce *
                    (0f - __instance.m_rudderValue) * num6;
    }

    __instance.m_body.AddForceAtPosition(stearforce * Time.fixedDeltaTime, stearoffset,
      ForceMode.VelocityChange);
  }

  private static float GetUpwardsForce(float targetY, float currentY, float maxForce)
  {
    var dist = targetY - currentY;
    if (dist == 0f) return 0f;
    var force = 1f / (25f / (dist * dist));
    force *= dist > 0f ? maxForce : 0f - maxForce;
    return Mathf.Clamp(force, 0f - maxForce, maxForce);
  }

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
    ZDOPersistantID.Instance.Register(zdo);
    MoveableBaseRootComponent.InitZDO(zdo);
    BaseVehicle.InitZDO(zdo);
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
    BaseVehicle.RemoveZDO(zdo);
    ZDOPersistantID.Instance.Unregister(zdo);
  }

  [HarmonyPatch(typeof(ZNetView), "ResetZDO")]
  [HarmonyPrefix]
  private static bool ZNetView_ResetZDO(ZNetView __instance)
  {
    if (__instance.m_zdo == null) return false;

    return true;
  }

  [HarmonyPatch(typeof(ZNetView), "Awake")]
  [HarmonyPostfix]
  private static void ZNetView_Awake(ZNetView __instance)
  {
    if (__instance.m_zdo != null)
    {
      MoveableBaseRootComponent.InitPiece(__instance);
      BaseVehicle.InitPiece(__instance);
      CultivatableComponent.InitPiece(__instance);
    }
  }

  // [HarmonyPatch(typeof(Player), "FindClosestSnappoint")]
  // [HarmonyPrefix]
  // private static bool FindClosestSnapPointPrefix(Player __instance)
  // {
  //   Logger.LogDebug(
  //     $"LogPoint(SINGLE) before: {__instance.m_tempSnapPoints1.Count} {__instance.m_tempSnapPoints1}");
  //   Logger.LogDebug(
  //     $"LogPoint(SINGLE) before: {__instance.m_tempSnapPoints2.Count} {__instance.m_tempSnapPoints2}");
  //   return true;
  // }
  //
  // [HarmonyPatch(typeof(Player), "FindClosestSnappoint")]
  // [HarmonyPostfix]
  // private static void FindClosestSnapPointPostfix(Player __instance)
  // {
  //   Logger.LogDebug(
  //     $"LogPoint(SINGLE) after: {__instance.m_tempSnapPoints1.Count} {__instance.m_tempSnapPoints1}");
  //   Logger.LogDebug(
  //     $"LogPoint(SINGLE) after: {__instance.m_tempSnapPoints2.Count} {__instance.m_tempSnapPoints2}");
  // }
  //
  // [HarmonyPatch(typeof(Player), "FindClosestSnapPoints")]
  // [HarmonyPrefix]
  // private static bool FindClosestSnapPointsPrefix(Player __instance)
  // {
  //   Logger.LogDebug(
  //     $"LogPoints before: {__instance.m_tempSnapPoints1.Count} {__instance.m_tempSnapPoints1}");
  //   Logger.LogDebug(
  //     $"LogPoints before: {__instance.m_tempSnapPoints2.Count} {__instance.m_tempSnapPoints2}");
  //   return true;
  // }
  //
  // [HarmonyPatch(typeof(Player), "FindClosestSnapPoints")]
  // [HarmonyPostfix]
  // private static void FindClosestSnapPointsPostfix(Player __instance)
  // {
  //   Logger.LogDebug(
  //     $"LogPoints after: {__instance.m_tempSnapPoints1.Count} {__instance.m_tempSnapPoints1}");
  //   Logger.LogDebug(
  //     $"LogPoints after: {__instance.m_tempSnapPoints2.Count} {__instance.m_tempSnapPoints2}");
  // }

  [HarmonyPatch(typeof(ZNetView), "OnDestroy")]
  [HarmonyPrefix]
  private static bool ZNetView_OnDestroy(ZNetView __instance)
  {
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      mbr.RemovePiece(__instance);
      if (ValheimRaftPlugin.Instance.DisplacedRaftAutoFix.Value &&
          (bool)Player.m_localPlayer && Player.m_localPlayer.transform.IsChildOf(mbr.transform))
      {
        Logger.LogWarning(
          "DisplacedRaftAutoFix enabled: automatically regenerating broken since the player was attached it the raft");
        MoveRaftConsoleCommand.MoveRaft(Player.m_localPlayer, mbr.m_ship, new Vector3(0, 0, 0));
        // only regenerate if the raft glitches out
        return false;
      }
    }

    var bv = __instance.GetComponentInParent<BaseVehicle>();
    if ((bool)bv)
    {
      bv.RemovePiece(__instance);
      if (ValheimRaftPlugin.Instance.DisplacedRaftAutoFix.Value &&
          (bool)Player.m_localPlayer && Player.m_localPlayer.transform.IsChildOf(mbr.transform))
      {
        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "Destroy")]
  [HarmonyPrefix]
  private static bool WearNTear_Destroy(WearNTear __instance)
  {
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    var bv = __instance.GetComponentInParent<BaseVehicle>();

    if ((bool)mbr) mbr.DestroyPiece(__instance);
    if ((bool)bv) bv.DestroyPiece(__instance);
    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
  [HarmonyPrefix]
  private static bool WearNTear_ApplyDamage(WearNTear __instance, float damage)
  {
    return !__instance.GetComponent<MoveableBaseShipComponent>();
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPatch(typeof(WearNTear), "SetupColliders")]
  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> WearNTear_AttachShip(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.PropertyGetter(typeof(Collider), "attachedRigidbody")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(ValheimRAFT_Patch), "AttachRigidbodyMovableBase"));
        break;
      }

    return list;
  }

  private static Rigidbody AttachRigidbodyMovableBase(Collider collider)
  {
    var rb = collider.attachedRigidbody;
    if (!rb) return null;
    var mbr = rb.GetComponent<MoveableBaseRootComponent>();
    var bv = rb.GetComponent<BaseVehicle>();
    if ((bool)mbr || bv) return null;
    return rb;
  }

  [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> UpdatePlacementGhost(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.Method(typeof(Quaternion), "Euler", new Type[3]
          {
            typeof(float),
            typeof(float),
            typeof(float)
          })))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(ValheimRAFT_Patch), "RelativeEuler"));
    return list;
  }

  private static Quaternion RelativeEuler(float x, float y, float z)
  {
    var rot = Quaternion.Euler(x, y, z);
    if (!m_lastRayPiece) return rot;
    var mbr = m_lastRayPiece.GetComponentInParent<MoveableBaseRootComponent>();
    if (!mbr) return rot;
    return mbr.transform.rotation * rot;
  }

  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance, ref Ship __result)
  {
    if (!__instance.IsOnGround()) return false;
    if ((bool)__instance.m_lastGroundBody)
    {
      __result = __instance.m_lastGroundBody.GetComponent<Ship>();
      if (!__result)
      {
        var mb = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
        if ((bool)mb && (bool)mb.shipController)
          __result = mb.shipController.GetComponent<Ship>();
      }

      return false;
    }

    return false;
  }


  [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
  [HarmonyPostfix]
  private static void UpdateGroundContact(Character __instance)
  {
    if (__instance is Player { m_debugFly: not false })
    {
      if (__instance.transform.parent != null) __instance.transform.SetParent(null);
      return;
    }

    MoveableBaseRootComponent mbr = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      mbr = __instance.m_lastGroundBody.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr && __instance.transform.parent != mbr.transform)
        __instance.transform.SetParent(mbr.transform);
    }

    if (!mbr && __instance.transform.parent != null) __instance.transform.SetParent(null);
  }

  [HarmonyPatch(typeof(Player), "PlacePiece")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> PlacePiece(IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].operand != null && list[i].operand.ToString() ==
          "UnityEngine.GameObject Instantiate[GameObject](UnityEngine.GameObject, UnityEngine.Vector3, UnityEngine.Quaternion)")
      {
        list.InsertRange(i + 2, new CodeInstruction[3]
        {
          new(OpCodes.Ldarg_0),
          new(OpCodes.Ldloc_3),
          new(OpCodes.Call, AccessTools.Method(typeof(ValheimRAFT_Patch), "PlacedPiece"))
        });
        break;
      }

    return list;
  }

  private static void PlacedPiece(Player player, GameObject gameObject)
  {
    var piece = gameObject.GetComponent<Piece>();
    if (!piece) return;
    var rb = piece.GetComponentInChildren<Rigidbody>();
    if (((bool)rb && !rb.isKinematic) || !m_lastRayPiece) return;
    var netView = piece.GetComponent<ZNetView>();
    if ((bool)netView)
    {
      var cul = m_lastRayPiece.GetComponent<CultivatableComponent>();
      if ((bool)cul) cul.AddNewChild(netView);
    }

    var mb = m_lastRayPiece.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mb)
    {
      if ((bool)netView)
      {
        Logger.LogDebug($"adding new piece {piece.name} {gameObject.name}");
        mb.AddNewPiece(netView);
      }
      else
      {
        Logger.LogDebug("adding temp piece");
        mb.AddTemporaryPiece(piece);
      }
    }
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPrefix]
  private static bool PieceRayTest(Player __instance, ref bool __result, ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface,
    bool water)
  {
    var layerMask = __instance.m_placeRayMask;
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      var localPos = mbr.transform.InverseTransformPoint(__instance.transform.position);
      var start = localPos + Vector3.up * 2f;
      start = mbr.transform.TransformPoint(start);
      var localDir = ((Character)__instance).m_lookYaw * Quaternion.Euler(__instance.m_lookPitch,
        0f - mbr.transform.rotation.eulerAngles.y + yawOffset, 0f);
      var end = mbr.transform.rotation * localDir * Vector3.forward;
      if (Physics.Raycast(start, end, out var hitInfo, 10f, layerMask) && (bool)hitInfo.collider)
      {
        var mbrTarget = hitInfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
        if ((bool)mbrTarget)
        {
          point = hitInfo.point;
          normal = hitInfo.normal;
          piece = hitInfo.collider.GetComponentInParent<Piece>();
          heightmap = null;
          waterSurface = null;
          __result = true;
          return false;
        }
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "Save")]
  [HarmonyPrefix]
  private static void Player_Save(Player __instance, ZPackage pkg)
  {
    if ((bool)((Character)__instance).m_lastGroundCollider &&
        ((Character)__instance).m_lastGroundTouch < 0.3f)
      MoveableBaseRootComponent.AddDynamicParent(((Character)__instance).m_nview,
        ((Character)__instance).m_lastGroundCollider.gameObject);
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPostfix]
  private static void PieceRayTestPostfix(Player __instance, ref bool __result, ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface,
    bool water)
  {
    m_lastRayPiece = piece;
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPrefix]
  private static bool UpdateSupport(WearNTear __instance)
  {
    if (!__instance.isActiveAndEnabled) return false;
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if (!mbr) return true;
    if (__instance.transform.localPosition.y < 1f)
    {
      __instance.m_nview.GetZDO().Set("support", 1500f);
      return false;
    }

    return false;
  }

  [HarmonyPatch(typeof(Player), "FindHoverObject")]
  [HarmonyPrefix]
  private static bool FindHoverObject(Player __instance, ref GameObject hover,
    ref Character hoverCreature)
  {
    hover = null;
    hoverCreature = null;
    var array = Physics.RaycastAll(GameCamera.instance.transform.position,
      GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
    Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    var array2 = array;
    for (var i = 0; i < array2.Length; i++)
    {
      var raycastHit = array2[i];
      if ((bool)raycastHit.collider.attachedRigidbody &&
          raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject) continue;
      if (hoverCreature == null)
      {
        var character = raycastHit.collider.attachedRigidbody
          ? raycastHit.collider.attachedRigidbody.GetComponent<Character>()
          : raycastHit.collider.GetComponent<Character>();
        if (character != null) hoverCreature = character;
      }

      if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) <
          __instance.m_maxInteractDistance)
      {
        if (raycastHit.collider.GetComponent<Hoverable>() != null)
          hover = raycastHit.collider.gameObject;
        else if ((bool)raycastHit.collider.attachedRigidbody && !raycastHit.collider
                   .attachedRigidbody.GetComponent<MoveableBaseRootComponent>())
          hover = raycastHit.collider.attachedRigidbody.gameObject;
        else
          hover = raycastHit.collider.gameObject;
      }

      break;
    }

    RopeAnchorComponent.m_draggingRopeTo = null;
    if ((bool)hover && (bool)RopeAnchorComponent.m_draggingRopeFrom)
    {
      RopeAnchorComponent.m_draggingRopeTo = hover;
      hover = RopeAnchorComponent.m_draggingRopeFrom.gameObject;
    }

    return false;
  }

  [HarmonyPatch(typeof(CharacterAnimEvent), "OnAnimatorIK")]
  [HarmonyPrefix]
  private static bool OnAnimatorIK(CharacterAnimEvent __instance, int layerIndex)
  {
    if (__instance.m_character is Player player && player.IsAttached() &&
        (bool)player.m_attachPoint && (bool)player.m_attachPoint.parent)
    {
      var rudder = player.m_attachPoint.parent.GetComponent<RudderComponent>();
      if ((bool)rudder) rudder.UpdateIK(((Character)player).m_animator);
      var ladder = player.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        ladder.UpdateIK(((Character)player).m_animator);
        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "AttachStop")]
  [HarmonyPrefix]
  private static void AttachStop(Player __instance)
  {
    if (__instance.IsAttached() && (bool)__instance.m_attachPoint &&
        (bool)__instance.m_attachPoint.parent)
    {
      var ladder = __instance.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
      if ((bool)ladder) ladder.StepOffLadder(__instance);
      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }
  }

  // Logic for anchor needs to be moved to the Update method instead of fixed update which SetControls is called in
  [HarmonyPatch(typeof(Player), "SetControls")]
  [HarmonyPrefix]
  private static bool SetControls(Player __instance, Vector3 movedir, bool attack, bool attackHold,
    bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run,
    bool autoRun)
  {
    if (__instance.IsAttached() && (bool)__instance.m_attachPoint &&
        (bool)__instance.m_attachPoint.parent)
    {
      if (movedir.x == 0f && movedir.y == 0f && !jump && !crouch && !attack && !attackHold &&
          !secondaryAttack && !block)
      {
        var ladder = __instance.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
        if ((bool)ladder)
        {
          ladder.MoveOnLadder(__instance, movedir.z);
          return false;
        }
      }

      var rudder = __instance.m_attachPoint.parent.GetComponent<RudderComponent>();
      if ((bool)rudder && __instance.m_doodadController != null)
      {
        __instance.SetDoodadControlls(ref movedir, ref ((Character)__instance).m_lookDir, ref run,
          ref autoRun, blockHold);
        if (__instance.m_doodadController is ShipControlls shipControlls &&
            (bool)shipControlls.m_ship)
        {
          var mb = shipControlls.m_ship.GetComponent<MoveableBaseShipComponent>();
          if ((bool)mb)
          {
            var anchorKey =
              (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "False" &&
               ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set")
                ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown()
                : ZInput
                  .GetButtonDown("Run");
            if (anchorKey || ZInput.GetButtonDown("JoyRun"))
            {
              Logger.LogDebug("Anchor button is down setting anchor");
              mb.SetAnchor(!mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored));
            }
            else if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
            {
              mb.Ascend();
            }
            else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
            {
              mb.Descent();
            }
          }
        }

        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
  [HarmonyPrefix]
  private static bool CreateDestroyObjects()
  {
    return !m_disableCreateDestroy;
  }

  [HarmonyPatch(typeof(ZNetScene), "Shutdown")]
  [HarmonyPostfix]
  private static void ZNetScene_Shutdown()
  {
    ZDOPersistantID.Instance.Reset();
  }

  [HarmonyPatch(typeof(Character), "OnCollisionStay")]
  [HarmonyPrefix]
  private static bool OnCollisionStay(Character __instance, Collision collision)
  {
    if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner() ||
        __instance.m_jumpTimer < 0.1f) return false;
    var contacts = collision.contacts;
    for (var i = 0; i < contacts.Length; i++)
    {
      var contactPoint = contacts[i];
      var hitnormal = contactPoint.normal;
      var hitpoint = contactPoint.point;
      var hitDistance = Mathf.Abs(hitpoint.y - __instance.transform.position.y);
      if (!__instance.m_groundContact && hitnormal.y < 0f && hitDistance < 0.1f)
      {
        hitnormal *= -1f;
        hitpoint = __instance.transform.position;
      }

      if (!(hitnormal.y > 0.1f) || !(hitDistance < __instance.m_collider.radius)) continue;
      if (hitnormal.y > __instance.m_groundContactNormal.y || !__instance.m_groundContact)
      {
        __instance.m_groundContact = true;
        __instance.m_groundContactNormal = hitnormal;
        __instance.m_groundContactPoint = hitpoint;
        __instance.m_lowestContactCollider = collision.collider;
        continue;
      }

      var groundContactNormal = Vector3.Normalize(__instance.m_groundContactNormal + hitnormal);
      if (groundContactNormal.y > __instance.m_groundContactNormal.y)
      {
        __instance.m_groundContactNormal = groundContactNormal;
        __instance.m_groundContactPoint = (__instance.m_groundContactPoint + hitpoint) * 0.5f;
      }
    }

    return false;
  }
}