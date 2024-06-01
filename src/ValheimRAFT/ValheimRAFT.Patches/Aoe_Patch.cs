using HarmonyLib;
using UnityEngine;

namespace ValheimRAFT.Patches;

public class Aoe_Patch
{
  [HarmonyPatch(typeof(Aoe), "CustomFixedUpdate")]
  [HarmonyPrefix]
  public static bool CustomFixedUpdate(Aoe __instance, float fixedDeltaTime)
  {
    if ((UnityEngine.Object)__instance.m_nview != (UnityEngine.Object)null &&
        !__instance.m_nview.IsOwner())
      return false;
    if (__instance.m_initRun && !__instance.m_useTriggers && !__instance.m_hitAfterTtl &&
        (double)__instance.m_activationTimer <= 0.0)
    {
      __instance.m_initRun = false;
      if ((double)__instance.m_hitInterval <= 0.0)
        __instance.Initiate();
    }

    if ((UnityEngine.Object)__instance.m_owner != (UnityEngine.Object)null &&
        __instance.m_attachToCaster)
    {
      __instance.transform.position =
        __instance.m_owner.transform.TransformPoint(__instance.m_offset);
      __instance.transform.rotation = __instance.m_owner.transform.rotation * __instance.m_localRot;
    }

    if ((double)__instance.m_activationTimer > 0.0)
      return false;
    if ((double)__instance.m_hitInterval > 0.0 && !__instance.m_useTriggers)
    {
      __instance.m_hitTimer -= fixedDeltaTime;
      if ((double)__instance.m_hitTimer <= 0.0)
      {
        __instance.m_hitTimer = __instance.m_hitInterval;
        __instance.Initiate();
      }
    }

    if ((double)__instance.m_chainStartChance > 0.0 && (double)__instance.m_chainDelay >= 0.0)
    {
      __instance.m_chainDelay -= fixedDeltaTime;
      if ((double)__instance.m_chainDelay <= 0.0 &&
          (double)UnityEngine.Random.value < (double)__instance.m_chainStartChance)
      {
        Vector3 position1 = __instance.transform.position;
        __instance.FindHits();
        __instance.SortHits();
        int num1 = UnityEngine.Random.Range(__instance.m_chainMinTargets,
          __instance.m_chainMaxTargets + 1);
        foreach (Collider hit in Aoe.s_hitList)
        {
          if ((double)UnityEngine.Random.value < (double)__instance.m_chainChancePerTarget)
          {
            Vector3 position2 = hit.gameObject.transform.position;
            bool flag = false;
            for (int index = 0; index < Aoe.s_chainObjs.Count; ++index)
            {
              if ((bool)(UnityEngine.Object)Aoe.s_chainObjs[index])
              {
                if ((double)Vector3.Distance(Aoe.s_chainObjs[index].transform.position, position2) <
                    0.10000000149011612)
                {
                  flag = true;
                  break;
                }
              }
              else
                Aoe.s_chainObjs.RemoveAt(index);
            }

            if (!flag)
            {
              GameObject gameObject1 =
                UnityEngine.Object.Instantiate<GameObject>(__instance.m_chainObj, position2,
                  hit.gameObject.transform.rotation);
              Aoe.s_chainObjs.Add(gameObject1);
              IProjectile componentInChildren = gameObject1.GetComponentInChildren<IProjectile>();
              if (componentInChildren != null)
              {
                componentInChildren.Setup(__instance.m_owner, position1.DirTo(position2), -1f,
                  __instance.m_hitData, __instance.m_itemData, __instance.m_ammo);
                if (componentInChildren is Aoe aoe)
                  aoe.m_chainChance =
                    __instance.m_chainChance * __instance.m_chainStartChanceFalloff;
              }

              --num1;
              float num2 = Vector3.Distance(position2, __instance.transform.position);
              foreach (GameObject gameObject2 in __instance.m_chainEffects.Create(
                         position1 + Vector3.up,
                         Quaternion.LookRotation(position1.DirTo(position2 + Vector3.up))))
                gameObject2.transform.localScale = Vector3.one * num2;
            }
          }

          if (num1 <= 0)
            break;
        }
      }
    }

    if ((double)__instance.m_ttl <= 0.0)
      return false;
    __instance.m_ttl -= fixedDeltaTime;
    if ((double)__instance.m_ttl > 0.0)
      return false;
    if (__instance.m_hitAfterTtl)
      __instance.Initiate();
    if (!(bool)(UnityEngine.Object)ZNetScene.instance)
      return false;
    ZNetScene.instance.Destroy(__instance.gameObject);
    return false;
  }
}