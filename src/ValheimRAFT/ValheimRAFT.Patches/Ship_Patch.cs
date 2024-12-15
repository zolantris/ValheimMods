using System;
using Components;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Ship_Patch
{
  [HarmonyPatch(typeof(Ship), "Awake")]
  [HarmonyPostfix]
  private static void Ship_Awake(Ship __instance)
  {
    Logger.LogDebug("Ship_Awake, called");
    if ((bool)__instance.m_nview && __instance.m_nview.m_zdo != null &&
        __instance.name.StartsWith("MBRaft"))
    {
      var ladders = __instance.GetComponentsInChildren<Ladder>();
      foreach (var t in ladders)
        t.m_useDistance = 10f;

      var mbShip =
        __instance.gameObject.AddComponent<MoveableBaseShipComponent>();

      if (VehicleDebugConfig.VehicleDebugMenuEnabled.Value)
      {
        var debugHelpersInstance = mbShip.VehicleDebugHelpersInstance =
          __instance.gameObject.AddComponent<VehicleDebugHelpers>();
        debugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
        {
          collider = mbShip.m_baseRoot.m_floatcollider,
          lineColor = Color.green,
          parent = mbShip.m_baseRoot.gameObject
        });
        debugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
        {
          collider = mbShip.m_baseRoot.m_blockingcollider,
          lineColor = Color.magenta,
          parent = mbShip.m_baseRoot.gameObject
        });
        debugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
        {
          collider = mbShip.m_baseRoot.m_onboardcollider,
          lineColor = Color.yellow,
          parent = mbShip.m_baseRoot.gameObject
        });
      }
    }
  }

  [HarmonyPatch(typeof(Ship), "UpdateUpsideDmg")]
  [HarmonyPrefix]
  private static bool Ship_UpdateUpsideDmg(Ship __instance)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb && __instance.transform.up.y < 0f)
      __instance.m_body.rotation = Quaternion.Euler(new Vector3(
        __instance.m_body.rotation.eulerAngles.x,
        __instance.m_body.rotation.eulerAngles.y, 0f));
    return !mb;
  }

  [HarmonyPatch(typeof(Ship), "UpdateWaterForce")]
  [HarmonyPrefix]
  private static bool Ship_UpdateWaterForce(Ship __instance, float depth,
    float time)
  {
    var num1 = (double)depth - (double)__instance.m_lastDepth;
    var num2 = time - __instance.m_lastUpdateWaterForceTime;
    __instance.m_lastDepth = depth;
    __instance.m_lastUpdateWaterForceTime = time;
    var num3 = (double)num2;
    var f = (float)(num1 / num3);
    if ((double)f > 0.0 || (double)Utils.Abs(f) <=
        (double)__instance.m_minWaterImpactForce ||
        (double)time - (double)__instance.m_lastWaterImpactTime <=
        (double)__instance.m_minWaterImpactInterval)
      return false;
    __instance.m_lastWaterImpactTime = time;
    __instance.m_waterImpactEffect.Create(__instance.transform.position,
      __instance.transform.rotation);
    if (__instance.m_players.Count <= 0)
      return false;
    __instance.m_destructible.Damage(new HitData()
    {
      m_damage =
      {
        m_blunt = __instance.m_waterImpactDamage
      },
      m_point = __instance.transform.position,
      m_dir = Vector3.up
    });
    return false;
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


  /**
   * todo this may not work well with two postfixes, there are two for UpdateSail
   */
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
        if (mast.m_sailObject.transform.localScale !=
            __instance.m_sailObject.transform.localScale)
          mast.m_sailCloth.enabled = false;
        mast.m_sailObject.transform.localScale =
          __instance.m_sailObject.transform.localScale;
        mast.m_sailCloth.enabled = __instance.m_sailCloth.enabled;
      }
      else
      {
        mast.m_sailObject.transform.localScale = Vector3.one;
        mast.m_sailCloth.enabled = !mast.m_disableCloth;
      }
    }

    for (var i = 0; i < mb.m_baseRoot.m_wheelPieces.Count; i++)
    {
      var rudder = mb.m_baseRoot.m_wheelPieces[i];
      if (!rudder)
      {
        mb.m_baseRoot.m_wheelPieces.RemoveAt(i);
        i--;
      }
      else if ((bool)rudder.wheelTransform)
      {
        if (rudder.wheelTransform != null)
          rudder.wheelTransform.localRotation = Quaternion.Slerp(
            rudder.wheelTransform.localRotation,
            Quaternion.Euler(
              __instance.m_rudderRotationMax * (0f - __instance.m_rudderValue) *
              rudder.m_wheelRotationFactor, 0f, 0f), 0.5f);
      }
    }
  }

  private static Vector3 CalculateAnchorStopVelocity(Vector3 currentVelocity)
  {
    var zeroVelocity = Vector3.zero;
    return Vector3.SmoothDamp(currentVelocity * 0.5f, Vector3.zero,
      ref zeroVelocity, 5f);
  }

  /**
   * only required for older ships.
   */
  [HarmonyPatch(typeof(Ship), "CustomFixedUpdate")]
  [HarmonyPrefix]
  private static bool Ship_FixedUpdate(Ship __instance)
  {
    if (!__instance.m_nview || __instance.m_nview.m_zdo == null ||
        !ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value) return true;

    var mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb) return true;

    /*
     * creative mode should not allows movement and applying force on a object will cause errors when the object is kinematic
     */
    if (mb.isCreative) return false;

    // This could be the spot that causes the raft to fly at spawn
    mb.m_targetHeight =
      __instance.m_nview.m_zdo.GetFloat("MBTargetHeight", mb.m_targetHeight);
    mb.m_flags =
      (MoveableBaseShipComponent.MBFlags)__instance.m_nview.m_zdo.GetInt(
        VehicleZdoVars.VehicleFlags,
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
        __instance.m_nview.m_zdo.Set(VehicleZdoVars.VehicleFlags,
          (int)mb.m_flags);
      }
    }

    if ((bool)__instance.m_nview && !__instance.m_nview.IsOwner()) return false;
    __instance.UpdateUpsideDmg(Time.fixedDeltaTime);
    if (!flag && (__instance.m_speed == Ship.Speed.Slow ||
                  __instance.m_speed == Ship.Speed.Back))
      __instance.m_speed = Ship.Speed.Stop;
    var worldCenterOfMass = __instance.m_body.worldCenterOfMass;
    var vector = __instance.m_floatCollider.transform.position +
                 __instance.m_floatCollider.transform.forward *
                 __instance.m_floatCollider.size.z /
                 2f;
    var vector2 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.forward *
                  __instance.m_floatCollider.size.z /
                  2f;
    var vector3 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.right *
                  __instance.m_floatCollider.size.x /
                  2f;
    var vector4 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.right *
                  __instance.m_floatCollider.size.x /
                  2f;
    var waterLevel = Floating.GetWaterLevel(worldCenterOfMass,
      ref __instance.m_previousCenter);
    var waterLevel2 =
      Floating.GetWaterLevel(vector3, ref __instance.m_previousLeft);
    var waterLevel3 =
      Floating.GetWaterLevel(vector4, ref __instance.m_previousRight);
    var waterLevel4 =
      Floating.GetWaterLevel(vector, ref __instance.m_previousForward);
    var waterLevel5 =
      Floating.GetWaterLevel(vector2, ref __instance.m_previousBack);
    var averageWaterHeight =
      (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
    var currentDepth = worldCenterOfMass.y - averageWaterHeight -
                       __instance.m_waterLevelOffset;
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
      var num4 =
        Mathf.Clamp01(Mathf.Abs(currentDepth) / __instance.m_forceDistance);
      var vector9 = Vector3.up * __instance.m_force * num4;
      __instance.m_body.AddForceAtPosition(vector9 * num3, worldCenterOfMass,
        ForceMode.VelocityChange);
      var num5 = Vector3.Dot(__instance.m_body.velocity,
        __instance.transform.forward);
      var num6 = Vector3.Dot(__instance.m_body.velocity,
        __instance.transform.right);
      var velocity = __instance.m_body.velocity;
      var value = velocity.y * velocity.y * Mathf.Sign(velocity.y) *
                  __instance.m_damping * num4;
      var value2 = num5 * num5 * Mathf.Sign(num5) *
                   __instance.m_dampingForward * num4;
      var value3 = num6 * num6 * Mathf.Sign(num6) *
                   __instance.m_dampingSideway * num4;
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
      __instance.m_body.AddForceAtPosition(Vector3.up * f * num3, vector,
        ForceMode.VelocityChange);
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
        __instance.m_body.AddForceAtPosition(Vector3.up * centerforce,
          centerpos,
          ForceMode.VelocityChange);
      }
    }
    else if (mb.m_targetHeight > 0f)
    {
      if (__instance.m_players.Count == 0 ||
          mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
      {
        var anchoredVelocity =
          CalculateAnchorStopVelocity(__instance.m_body.velocity);
        __instance.m_body.velocity = anchoredVelocity;
      }

      mb.UpdateStats(true);
      var side1 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.forward *
                  __instance.m_floatCollider.size.z /
                  2f;
      var side2 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.forward *
                  __instance.m_floatCollider.size.z /
                  2f;
      var side3 = __instance.m_floatCollider.transform.position -
                  __instance.m_floatCollider.transform.right *
                  __instance.m_floatCollider.size.x /
                  2f;
      var side4 = __instance.m_floatCollider.transform.position +
                  __instance.m_floatCollider.transform.right *
                  __instance.m_floatCollider.size.x /
                  2f;
      var centerpos2 = __instance.m_floatCollider.transform.position;
      var corner1curforce = __instance.m_body.GetPointVelocity(side1);
      var corner2curforce = __instance.m_body.GetPointVelocity(side2);
      var corner3curforce = __instance.m_body.GetPointVelocity(side3);
      var corner4curforce = __instance.m_body.GetPointVelocity(side4);
      var side1force =
        GetUpwardsForce(mb.m_targetHeight, side1.y + corner1curforce.y,
          mb.m_balanceForce);
      var side2force =
        GetUpwardsForce(mb.m_targetHeight, side2.y + corner2curforce.y,
          mb.m_balanceForce);
      var side3force =
        GetUpwardsForce(mb.m_targetHeight, side3.y + corner3curforce.y,
          mb.m_balanceForce);
      var side4force =
        GetUpwardsForce(mb.m_targetHeight, side4.y + corner4curforce.y,
          mb.m_balanceForce);
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
      __instance.m_body.AddForceAtPosition(Vector3.up * centerforce2,
        centerpos2,
        ForceMode.VelocityChange);
      var dir = Vector3.Dot(__instance.m_body.velocity,
        __instance.transform.forward);
      ApplySailForce(__instance, dir);
    }

    return false;
  }

  private static void ApplySailForce(Ship __instance, float num5)
  {
    var mb = __instance.GetComponent<MoveableBaseShipComponent>();

    var sailArea = 0f;

    if (mb.m_baseRoot) sailArea = mb.m_baseRoot.GetSailingForce();

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
      sailArea = 0f;

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
      __instance.transform.right * num7 * (0f - __instance.m_rudderValue) *
      Time.fixedDeltaTime,
      stearoffset, ForceMode.VelocityChange);
    var stearforce = Vector3.zero;
    switch (__instance.m_speed)
    {
      case Ship.Speed.Slow:
        stearforce += __instance.transform.forward *
                      __instance.m_backwardForce *
                      (1f - Mathf.Abs(__instance.m_rudderValue));
        break;
      case Ship.Speed.Back:
        stearforce += -__instance.transform.forward *
                      __instance.m_backwardForce *
                      (1f - Mathf.Abs(__instance.m_rudderValue));
        break;
    }

    if (__instance.m_speed == Ship.Speed.Back ||
        __instance.m_speed == Ship.Speed.Slow)
    {
      float num6 = __instance.m_speed != Ship.Speed.Back ? 1 : -1;
      stearforce += __instance.transform.right * __instance.m_stearForce *
                    (0f - __instance.m_rudderValue) * num6;
    }

    __instance.m_body.AddForceAtPosition(stearforce * Time.fixedDeltaTime,
      stearoffset,
      ForceMode.VelocityChange);
  }

  private static float GetUpwardsForce(float targetY, float currentY,
    float maxForce)
  {
    var dist = targetY - currentY;
    if (dist == 0f) return 0f;
    var force = 1f / (25f / (dist * dist));
    force *= dist > 0f ? maxForce : 0f - maxForce;
    return Mathf.Clamp(force, 0f - maxForce, maxForce);
  }
}