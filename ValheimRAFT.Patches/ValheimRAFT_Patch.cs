// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.Patches.ValheimRAFT_Patch

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimRAFT.Util;

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
    if (character == Player.m_localPlayer)
    {
      m_lastUsedControls = __instance;
      __instance.m_ship.m_controlGuiPos.position = __instance.transform.position;
    }
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
      Ladder[] ladders = __instance.GetComponentsInChildren<Ladder>();
      for (int i = 0; i < ladders.Length; i++)
      {
        ladders[i].m_useDistance = 10f;
      }

      __instance.gameObject.AddComponent<MoveableBaseShipComponent>();
    }
  }

  [HarmonyPatch(typeof(Ship), "UpdateUpsideDmg")]
  [HarmonyPrefix]
  private static bool Ship_UpdateUpsideDmg(Ship __instance)
  {
    MoveableBaseShipComponent mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb && __instance.transform.up.y < 0f)
    {
      __instance.m_body.rotation = Quaternion.Euler(new Vector3(
        __instance.m_body.rotation.eulerAngles.x, __instance.m_body.rotation.eulerAngles.y, 0f));
    }

    return !mb;
  }

  [HarmonyPatch(typeof(Ship), "UpdateSail")]
  [HarmonyPostfix]
  private static void Ship_UpdateSail(Ship __instance)
  {
    MoveableBaseShipComponent mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !mb.m_baseRootDelegate)
    {
      return;
    }

    for (int i = 0; i < mb.m_baseRootDelegate.m_mastPieces.Count; i++)
    {
      MastComponent mast = mb.m_baseRootDelegate.m_mastPieces[i];
      if (!mast)
      {
        mb.m_baseRootDelegate.m_mastPieces.RemoveAt(i);
        i--;
      }
      else if (mast.m_allowSailRotation)
      {
        mast.transform.localRotation = __instance.m_mastObject.transform.localRotation;
      }
    }
  }

  [HarmonyPatch(typeof(Ship), "UpdateSail")]
  [HarmonyPostfix]
  private static void Ship_UpdateSailSize(Ship __instance)
  {
    MoveableBaseShipComponent mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !mb.m_baseRootDelegate)
    {
      return;
    }

    for (int j = 0; j < mb.m_baseRootDelegate.m_mastPieces.Count; j++)
    {
      MastComponent mast = mb.m_baseRootDelegate.m_mastPieces[j];
      if (!mast)
      {
        mb.m_baseRootDelegate.m_mastPieces.RemoveAt(j);
        j--;
      }
      else if (mast.m_allowSailShrinking)
      {
        if (mast.m_sailObject.transform.localScale != __instance.m_sailObject.transform.localScale)
        {
          mast.m_sailCloth.enabled = false;
        }

        mast.m_sailObject.transform.localScale = __instance.m_sailObject.transform.localScale;
        mast.m_sailCloth.enabled = __instance.m_sailCloth.enabled;
      }
      else
      {
        mast.m_sailObject.transform.localScale = Vector3.one;
        mast.m_sailCloth.enabled = !mast.m_disableCloth;
      }
    }

    for (int i = 0; i < mb.m_baseRootDelegate.m_rudderPieces.Count; i++)
    {
      RudderComponent rudder = mb.m_baseRootDelegate.m_rudderPieces[i];
      if (!rudder)
      {
        mb.m_baseRootDelegate.m_rudderPieces.RemoveAt(i);
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

  [HarmonyPatch(typeof(Ship), "CustomFixedUpdate")]
  [HarmonyPrefix]
  private static bool Ship_FixedUpdate(Ship __instance)
  {
    MoveableBaseShipComponent mb = __instance.GetComponent<MoveableBaseShipComponent>();
    if (!mb || !__instance.m_nview || __instance.m_nview.GetZDO() == null)
    {
      return true;
    }

    mb.m_targetHeight = __instance.m_nview.GetZDO().GetFloat("MBTargetHeight", mb.m_targetHeight);
    mb.m_flags =
      (MoveableBaseShipComponent.MBFlags)__instance.m_nview.GetZDO().GetInt("MBFlags",
        (int)mb.m_flags);
    mb.m_zsync.m_useGravity = mb.m_targetHeight == 0f;
    bool flag = __instance.HaveControllingPlayer();
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

    if ((bool)__instance.m_nview && !__instance.m_nview.IsOwner())
    {
      return false;
    }

    __instance.UpdateUpsideDmg(Time.fixedDeltaTime);
    if (!flag && (__instance.m_speed == Ship.Speed.Slow || __instance.m_speed == Ship.Speed.Back))
    {
      __instance.m_speed = Ship.Speed.Stop;
    }

    Vector3 worldCenterOfMass = __instance.m_body.worldCenterOfMass;
    Vector3 vector = __instance.m_floatCollider.transform.position +
                     __instance.m_floatCollider.transform.forward *
                     __instance.m_floatCollider.size.z / 2f;
    Vector3 vector2 = __instance.m_floatCollider.transform.position -
                      __instance.m_floatCollider.transform.forward *
                      __instance.m_floatCollider.size.z / 2f;
    Vector3 vector3 = __instance.m_floatCollider.transform.position -
                      __instance.m_floatCollider.transform.right *
                      __instance.m_floatCollider.size.x / 2f;
    Vector3 vector4 = __instance.m_floatCollider.transform.position +
                      __instance.m_floatCollider.transform.right *
                      __instance.m_floatCollider.size.x / 2f;
    float waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref __instance.m_previousCenter);
    float waterLevel2 = Floating.GetWaterLevel(vector3, ref __instance.m_previousLeft);
    float waterLevel3 = Floating.GetWaterLevel(vector4, ref __instance.m_previousRight);
    float waterLevel4 = Floating.GetWaterLevel(vector, ref __instance.m_previousForward);
    float waterLevel5 = Floating.GetWaterLevel(vector2, ref __instance.m_previousBack);
    float averageWaterHeight =
      (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
    float currentDepth = worldCenterOfMass.y - averageWaterHeight - __instance.m_waterLevelOffset;
    if (!(currentDepth > __instance.m_disableLevel))
    {
      mb.UpdateStats(flight: false);
      __instance.m_body.WakeUp();
      __instance.UpdateWaterForce(currentDepth, Time.fixedDeltaTime);
      Vector3 vector5 = new Vector3(vector3.x, waterLevel2, vector3.z);
      Vector3 vector6 = new Vector3(vector4.x, waterLevel3, vector4.z);
      Vector3 vector7 = new Vector3(vector.x, waterLevel4, vector.z);
      Vector3 vector8 = new Vector3(vector2.x, waterLevel5, vector2.z);
      float fixedDeltaTime = Time.fixedDeltaTime;
      float num3 = fixedDeltaTime * 50f;
      float num4 = Mathf.Clamp01(Mathf.Abs(currentDepth) / __instance.m_forceDistance);
      Vector3 vector9 = Vector3.up * __instance.m_force * num4;
      __instance.m_body.AddForceAtPosition(vector9 * num3, worldCenterOfMass,
        ForceMode.VelocityChange);
      float num5 = Vector3.Dot(__instance.m_body.velocity, __instance.transform.forward);
      float num6 = Vector3.Dot(__instance.m_body.velocity, __instance.transform.right);
      Vector3 velocity = __instance.m_body.velocity;
      float value = velocity.y * velocity.y * Mathf.Sign(velocity.y) * __instance.m_damping * num4;
      float value2 = num5 * num5 * Mathf.Sign(num5) * __instance.m_dampingForward * num4;
      float value3 = num6 * num6 * Mathf.Sign(num6) * __instance.m_dampingSideway * num4;
      velocity.y -= Mathf.Clamp(value, -1f, 1f);
      velocity -= __instance.transform.forward * Mathf.Clamp(value2, -1f, 1f);
      velocity -= __instance.transform.right * Mathf.Clamp(value3, -1f, 1f);
      if (velocity.magnitude > __instance.m_body.velocity.magnitude)
      {
        velocity = velocity.normalized * __instance.m_body.velocity.magnitude;
      }

      if (__instance.m_players.Count == 0 ||
          mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored))
      {
        velocity.x *= 0.1f;
        velocity.z *= 0.1f;
      }

      __instance.m_body.velocity = velocity;
      __instance.m_body.angularVelocity -=
        __instance.m_body.angularVelocity * __instance.m_angularDamping * num4;
      float num7 = 0.15f;
      float num8 = 0.5f;
      float f = Mathf.Clamp((vector7.y - vector.y) * num7, 0f - num8, num8);
      float f2 = Mathf.Clamp((vector8.y - vector2.y) * num7, 0f - num8, num8);
      float f3 = Mathf.Clamp((vector5.y - vector3.y) * num7, 0f - num8, num8);
      float f4 = Mathf.Clamp((vector6.y - vector4.y) * num7, 0f - num8, num8);
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
        Vector3 centerpos = __instance.m_floatCollider.transform.position;
        float centerforce = GetUpwardsForce(mb.m_targetHeight,
          centerpos.y + __instance.m_body.velocity.y, mb.m_liftForce);
        __instance.m_body.AddForceAtPosition(Vector3.up * centerforce, centerpos,
          ForceMode.VelocityChange);
      }
    }
    else if (mb.m_targetHeight > 0f)
    {
      mb.UpdateStats(flight: true);
      Vector3 side1 = __instance.m_floatCollider.transform.position +
                      __instance.m_floatCollider.transform.forward *
                      __instance.m_floatCollider.size.z / 2f;
      Vector3 side2 = __instance.m_floatCollider.transform.position -
                      __instance.m_floatCollider.transform.forward *
                      __instance.m_floatCollider.size.z / 2f;
      Vector3 side3 = __instance.m_floatCollider.transform.position -
                      __instance.m_floatCollider.transform.right *
                      __instance.m_floatCollider.size.x / 2f;
      Vector3 side4 = __instance.m_floatCollider.transform.position +
                      __instance.m_floatCollider.transform.right *
                      __instance.m_floatCollider.size.x / 2f;
      Vector3 centerpos2 = __instance.m_floatCollider.transform.position;
      Vector3 corner1curforce = __instance.m_body.GetPointVelocity(side1);
      Vector3 corner2curforce = __instance.m_body.GetPointVelocity(side2);
      Vector3 corner3curforce = __instance.m_body.GetPointVelocity(side3);
      Vector3 corner4curforce = __instance.m_body.GetPointVelocity(side4);
      float side1force =
        GetUpwardsForce(mb.m_targetHeight, side1.y + corner1curforce.y, mb.m_balanceForce);
      float side2force =
        GetUpwardsForce(mb.m_targetHeight, side2.y + corner2curforce.y, mb.m_balanceForce);
      float side3force =
        GetUpwardsForce(mb.m_targetHeight, side3.y + corner3curforce.y, mb.m_balanceForce);
      float side4force =
        GetUpwardsForce(mb.m_targetHeight, side4.y + corner4curforce.y, mb.m_balanceForce);
      float centerforce2 = GetUpwardsForce(mb.m_targetHeight,
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
      float dir = Vector3.Dot(__instance.m_body.velocity, __instance.transform.forward);
      ApplySailForce(__instance, dir);
    }

    return false;
  }

  private static void ApplySailForce(Ship __instance, float num5)
  {
    float sailSize = 0f;
    if (__instance.m_speed == Ship.Speed.Full)
    {
      sailSize = 1f;
    }
    else if (__instance.m_speed == Ship.Speed.Half)
    {
      sailSize = 0.5f;
    }

    Vector3 sailForce = __instance.GetSailForce(sailSize, Time.fixedDeltaTime);
    Vector3 position = __instance.m_body.worldCenterOfMass;
    __instance.m_body.AddForceAtPosition(sailForce, position, ForceMode.VelocityChange);
    Vector3 stearoffset = __instance.m_floatCollider.transform.position -
                          __instance.m_floatCollider.transform.forward *
                          __instance.m_floatCollider.size.z / 2f;
    float num7 = num5 * __instance.m_stearVelForceFactor;
    __instance.m_body.AddForceAtPosition(
      __instance.transform.right * num7 * (0f - __instance.m_rudderValue) * Time.fixedDeltaTime,
      stearoffset, ForceMode.VelocityChange);
    Vector3 stearforce = Vector3.zero;
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
      float num6 = ((__instance.m_speed != Ship.Speed.Back) ? 1 : (-1));
      stearforce += __instance.transform.right * __instance.m_stearForce *
                    (0f - __instance.m_rudderValue) * num6;
    }

    __instance.m_body.AddForceAtPosition(stearforce * Time.fixedDeltaTime, stearoffset,
      ForceMode.VelocityChange);
  }

  private static float GetUpwardsForce(float targetY, float currentY, float maxForce)
  {
    float dist = targetY - currentY;
    if (dist == 0f)
    {
      return 0f;
    }

    float force = 1f / (25f / (dist * dist));
    force *= ((dist > 0f) ? maxForce : (0f - maxForce));
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
    Server.InitZDO(zdo);
  }

  [HarmonyPatch(typeof(ZDO), "Reset")]
  [HarmonyPrefix]
  private static void ZDO_Reset(ZDO __instance)
  {
    ZDOUnload(__instance);
  }

  public static void ZDOUnload(ZDO zdo)
  {
    Server.RemoveZDO(zdo);
    ZDOPersistantID.Instance.Unregister(zdo);
  }

  [HarmonyPatch(typeof(ZNetView), "Awake")]
  [HarmonyPostfix]
  private static void ZNetView_Awake(ZNetView __instance)
  {
    if (__instance.GetZDO() != null)
    {
      Server.InitPiece(__instance);
      CultivatableComponent.InitPiece(__instance);
    }
  }

  [HarmonyPatch(typeof(ZNetView), "OnDestroy")]
  [HarmonyPrefix]
  private static void ZNetView_OnDestroy(ZNetView __instance)
  {
    Server mbr = __instance.GetComponentInParent<Server>();
    if ((bool)mbr)
    {
      mbr.RemovePiece(__instance);
    }
  }

  [HarmonyPatch(typeof(WearNTear), "Destroy")]
  [HarmonyPrefix]
  private static void WearNTear_Destroy(WearNTear __instance)
  {
    Server mbr = __instance.GetComponentInParent<Server>();
    if ((bool)mbr)
    {
      mbr.DestroyPiece(__instance);
    }
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
    //IL_004f: Unknown result type (might be due to invalid IL or missing references)
    //IL_0059: Expected O, but got Unknown
    List<CodeInstruction> list = instructions.ToList();
    for (int i = 0; i < list.Count; i++)
    {
      if (CodeInstructionExtensions.Calls(list[i],
            AccessTools.PropertyGetter(typeof(Collider), "attachedRigidbody")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          (object)AccessTools.Method(typeof(ValheimRAFT_Patch), "AttachRigidbodyMoveableBase",
            (Type[])null, (Type[])null));
        break;
      }
    }

    return list;
  }

  private static Rigidbody AttachRigidbodyMoveableBase(Collider collider)
  {
    Rigidbody rb = collider.attachedRigidbody;
    if (!rb)
    {
      return null;
    }

    Server mbr = rb.GetComponent<Server>();
    if ((bool)mbr)
    {
      return null;
    }

    return rb;
  }

  [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> UpdatePlacementGhost(
    IEnumerable<CodeInstruction> instructions)
  {
    //IL_0080: Unknown result type (might be due to invalid IL or missing references)
    //IL_008a: Expected O, but got Unknown
    List<CodeInstruction> list = instructions.ToList();
    for (int i = 0; i < list.Count; i++)
    {
      if (CodeInstructionExtensions.Calls(list[i], AccessTools.Method(typeof(Quaternion), "Euler",
            new Type[3]
            {
              typeof(float),
              typeof(float),
              typeof(float)
            }, (Type[])null)))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          (object)AccessTools.Method(typeof(ValheimRAFT_Patch), "RelativeEuler", (Type[])null,
            (Type[])null));
      }
    }

    return list;
  }

  private static Quaternion RelativeEuler(float x, float y, float z)
  {
    Quaternion rot = Quaternion.Euler(x, y, z);
    if (!m_lastRayPiece)
    {
      return rot;
    }

    Server
      mbr = m_lastRayPiece.GetComponentInParent<Server>();
    if (!mbr)
    {
      return rot;
    }

    return mbr.transform.rotation * rot;
  }

  [HarmonyPatch(typeof(Character), "GetStandingOnShip")]
  [HarmonyPrefix]
  private static bool Character_GetStandingOnShip(Character __instance, ref Ship __result)
  {
    if (!__instance.IsOnGround())
    {
      return false;
    }

    if ((bool)__instance.m_lastGroundBody)
    {
      __result = __instance.m_lastGroundBody.GetComponent<Ship>();
      if (!__result)
      {
        Server mb =
          __instance.m_lastGroundBody.GetComponentInParent<Server>();
        if ((bool)mb && (bool)mb.m_moveableBaseShip)
        {
          __result = mb.m_moveableBaseShip.GetComponent<Ship>();
        }
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
      if (__instance.transform.parent != null)
      {
        __instance.transform.SetParent(null);
      }

      return;
    }

    Server mbr = null;
    if ((bool)__instance.m_lastGroundBody)
    {
      mbr = __instance.m_lastGroundBody.GetComponentInParent<Server>();
      if ((bool)mbr && __instance.transform.parent != mbr.transform)
      {
        __instance.transform.SetParent(mbr.transform);
      }
    }

    if (!mbr && __instance.transform.parent != null)
    {
      __instance.transform.SetParent(null);
    }
  }

  [HarmonyPatch(typeof(Player), "PlacePiece")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> PlacePiece(IEnumerable<CodeInstruction> instructions)
  {
    //IL_0053: Unknown result type (might be due to invalid IL or missing references)
    //IL_0059: Expected O, but got Unknown
    //IL_0061: Unknown result type (might be due to invalid IL or missing references)
    //IL_0067: Expected O, but got Unknown
    //IL_0084: Unknown result type (might be due to invalid IL or missing references)
    //IL_008a: Expected O, but got Unknown
    List<CodeInstruction> list = instructions.ToList();
    for (int i = 0; i < list.Count; i++)
    {
      if (list[i].operand != null && list[i].operand.ToString() ==
          "UnityEngine.GameObject Instantiate[GameObject](UnityEngine.GameObject, UnityEngine.Vector3, UnityEngine.Quaternion)")
      {
        list.InsertRange(i + 2, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[3]
        {
          new(OpCodes.Ldarg_0, (object)null),
          new(OpCodes.Ldloc_3, (object)null),
          new(OpCodes.Call,
            AccessTools.Method(typeof(ValheimRAFT_Patch), "PlacedPiece", (Type[])null,
              (Type[])null))
        });
        break;
      }
    }

    return list;
  }

  private static void PlacedPiece(Player player, GameObject gameObject)
  {
    Piece piece = gameObject.GetComponent<Piece>();
    if (!piece)
    {
      return;
    }

    Rigidbody rb = piece.GetComponentInChildren<Rigidbody>();
    if (((bool)rb && !rb.isKinematic) || !m_lastRayPiece)
    {
      return;
    }

    ZNetView netview = piece.GetComponent<ZNetView>();
    if ((bool)netview)
    {
      CultivatableComponent cul = m_lastRayPiece.GetComponent<CultivatableComponent>();
      if ((bool)cul)
      {
        cul.AddNewChild(netview);
      }
    }

    Server mb = m_lastRayPiece.GetComponentInParent<Server>();
    if ((bool)mb)
    {
      if ((bool)netview)
      {
        mb.AddNewPiece(netview);
      }
      else
      {
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
    int layerMask = __instance.m_placeRayMask;
    Server mbr = __instance.GetComponentInParent<Server>();
    if ((bool)mbr)
    {
      Vector3 localPos = mbr.transform.InverseTransformPoint(__instance.transform.position);
      Vector3 start = localPos + Vector3.up * 2f;
      start = mbr.transform.TransformPoint(start);
      Quaternion localDir = ((Character)__instance).GetLookYaw() *
                            Quaternion.Euler(__instance.m_lookPitch,
                              0f - mbr.transform.rotation.eulerAngles.y + yawOffset, 0f);
      Vector3 end = mbr.transform.rotation * localDir * Vector3.forward;
      if (Physics.Raycast(start, end, out var hitInfo, 10f, layerMask) && (bool)hitInfo.collider)
      {
        Server mbrTarget =
          hitInfo.collider.GetComponentInParent<Server>();
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
    if ((bool)(__instance).GetLastGroundCollider() &&
        (__instance).m_lastGroundTouch < 0.3f)
    {
      Server.AddDynamicParent((__instance).m_nview,
        (__instance).GetLastGroundCollider().gameObject);
    }
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
    if (!__instance.isActiveAndEnabled)
    {
      return false;
    }

    Server mbr = __instance.GetComponentInParent<Server>();
    if (!mbr)
    {
      return true;
    }

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
    RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position,
      GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
    Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    RaycastHit[] array2 = array;
    for (int i = 0; i < array2.Length; i++)
    {
      RaycastHit raycastHit = array2[i];
      if ((bool)raycastHit.collider.attachedRigidbody &&
          raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject)
      {
        continue;
      }

      if (hoverCreature == null)
      {
        Character character = (raycastHit.collider.attachedRigidbody
          ? raycastHit.collider.attachedRigidbody.GetComponent<Character>()
          : raycastHit.collider.GetComponent<Character>());
        if (character != null)
        {
          hoverCreature = character;
        }
      }

      if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) <
          __instance.m_maxInteractDistance)
      {
        if (raycastHit.collider.GetComponent<Hoverable>() != null)
        {
          hover = raycastHit.collider.gameObject;
        }
        else if ((bool)raycastHit.collider.attachedRigidbody && !raycastHit.collider
                   .attachedRigidbody.GetComponent<Server>())
        {
          hover = raycastHit.collider.attachedRigidbody.gameObject;
        }
        else
        {
          hover = raycastHit.collider.gameObject;
        }
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
        (bool)player.GetAttachPoint() && (bool)player.GetAttachPoint().parent)
    {
      RudderComponent rudder = player.GetAttachPoint().parent.GetComponent<RudderComponent>();
      if ((bool)rudder)
      {
        rudder.UpdateIK(((Character)player).m_animator);
      }

      RopeLadderComponent ladder =
        player.GetAttachPoint().parent.GetComponent<RopeLadderComponent>();
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
    if (__instance.IsAttached() && (bool)__instance.GetAttachPoint() &&
        (bool)__instance.GetAttachPoint().parent)
    {
      RopeLadderComponent ladder =
        __instance.GetAttachPoint().parent.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        ladder.StepOffLadder(__instance);
      }

      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }
  }

  [HarmonyPatch(typeof(Player), "SetControls")]
  [HarmonyPrefix]
  private static bool SetControls(Player __instance, Vector3 movedir, bool attack, bool attackHold,
    bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run,
    bool autoRun)
  {
    if (__instance.IsAttached() && (bool)__instance.GetAttachPoint() &&
        (bool)__instance.GetAttachPoint().parent)
    {
      if (movedir.x == 0f && movedir.y == 0f && !jump && !crouch && !attack && !attackHold &&
          !secondaryAttack && !block)
      {
        RopeLadderComponent ladder =
          __instance.GetAttachPoint().parent.GetComponent<RopeLadderComponent>();
        if ((bool)ladder)
        {
          ladder.MoveOnLadder(__instance, movedir.z);
          return false;
        }
      }

      RudderComponent rudder = __instance.GetAttachPoint().parent.GetComponent<RudderComponent>();
      if ((bool)rudder && __instance.m_doodadController != null)
      {
        __instance.SetDoodadControlls(ref movedir, ref ((Character)__instance).m_lookDir, ref run,
          ref autoRun, blockHold);
        if (__instance.m_doodadController is ShipControlls shipControlls &&
            (bool)shipControlls.m_ship)
        {
          MoveableBaseShipComponent mb =
            shipControlls.m_ship.GetComponent<MoveableBaseShipComponent>();
          if ((bool)mb)
          {
            if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
            {
              mb.Accend();
            }
            else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
            {
              mb.Descent();
            }
            else if (ZInput.GetButtonDown("Run") || ZInput.GetButtonDown("JoyRun"))
            {
              mb.SetAnchor(!mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored));
            }
            else if (mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored) &&
                     movedir != Vector3.zero)
            {
              mb.SetAnchor(state: false);
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
        __instance.m_jumpTimer < 0.1f)
    {
      return false;
    }

    ContactPoint[] contacts = collision.contacts;
    for (int i = 0; i < contacts.Length; i++)
    {
      ContactPoint contactPoint = contacts[i];
      Vector3 hitnormal = contactPoint.normal;
      Vector3 hitpoint = contactPoint.point;
      float hitDistance = Mathf.Abs(hitpoint.y - __instance.transform.position.y);
      if (!__instance.m_groundContact && hitnormal.y < 0f && hitDistance < 0.1f)
      {
        hitnormal *= -1f;
        hitpoint = __instance.transform.position;
      }

      if (!(hitnormal.y > 0.1f) || !(hitDistance < __instance.m_collider.radius))
      {
        continue;
      }

      if (hitnormal.y > __instance.m_groundContactNormal.y || !__instance.m_groundContact)
      {
        __instance.m_groundContact = true;
        __instance.m_groundContactNormal = hitnormal;
        __instance.m_groundContactPoint = hitpoint;
        __instance.m_lowestContactCollider = collision.collider;
        continue;
      }

      Vector3 groundContactNormal = Vector3.Normalize(__instance.m_groundContactNormal + hitnormal);
      if (groundContactNormal.y > __instance.m_groundContactNormal.y)
      {
        __instance.m_groundContactNormal = groundContactNormal;
        __instance.m_groundContactPoint = (__instance.m_groundContactPoint + hitpoint) * 0.5f;
      }
    }

    return false;
  }
}