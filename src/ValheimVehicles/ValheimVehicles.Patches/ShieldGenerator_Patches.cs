using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Patches;
using Object = UnityEngine.Object;
namespace ValheimVehicles.ValheimVehicles.Patches;

public class ShieldGenerator_Patches
{
  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Start))]
  [HarmonyPrefix]
  public void Start(ShieldGenerator __instance)
  {
    if (Player.IsPlacementGhost(__instance.gameObject))
    {
      __instance.enabled = false;
      __instance.m_isPlacementGhost = true;
    }
    else
    {
      ShieldGenerator.m_instances.Add(this);
      ++ShieldGenerator.m_instanceChangeID;
      __instance.m_nview = __instance.GetComponent<ZNetView>();
      if ((Object)__instance.m_nview == (Object)null)
        __instance.m_nview = __instance.GetComponentInParent<ZNetView>();
      if ((Object)__instance.m_nview == (Object)null || __instance.m_nview.GetZDO() == null)
        return;
      if ((bool)(Object)__instance.m_addFuelSwitch)
      {
        __instance.m_addFuelSwitch.m_onUse += new Switch.Callback(__instance.OnAddFuel);
        __instance.m_addFuelSwitch.m_onHover = new Switch.TooltipCallback(__instance.OnHoverAddFuel);
      }
      __instance.m_nview.Register("RPC_AddFuel", new Action<long>(__instance.RPC_AddFuel));
      __instance.m_nview.Register<float>("RPC_SetFuel", new Action<long, float>(__instance.RPC_SetFuel));
      __instance.m_nview.Register("RPC_Attack", new Action<long>(__instance.RPC_Attack));
      __instance.m_nview.Register("RPC_HitNow", new Action<long>(__instance.RPC_HitNow));
      __instance.m_projectileMask = LayerMask.GetMask();
      if (!(bool)(Object)ShieldGenerator.m_shieldDomeEffect)
        ShieldGenerator.m_shieldDomeEffect = Object.FindFirstObjectByType<ShieldDomeImageEffect>();
      if (!__instance.m_enableAttack && __instance.m_fuelItems.Count == 0)
        __instance.m_addFuelSwitch.gameObject.SetActive(false);
      __instance.m_particleFlareGradient = new Gradient();
      __instance.m_particleFlareGradient.colorKeys = new GradientColorKey[1]
      {
        new(Color.white, 0.0f)
      };
      __instance.m_particleFlareGradient.alphaKeys = new GradientAlphaKey[1]
      {
        new(0.0f, 0.0f)
      };
      __instance.m_propertyBlock = new MaterialPropertyBlock();
      __instance.m_meshRenderers = __instance.m_enabledObject.GetComponentsInChildren<MeshRenderer>();
      __instance.InvokeRepeating("UpdateShield", 0.0f, 0.22f);
    }
  }


  [HarmonyPatch(typeof(ShieldGenerator), nameof(ShieldGenerator.Update))]
  [HarmonyPrefix]
  public void Update(ShieldGenerator __instance)
  {
    if ((bool)(Object)__instance.m_shieldDome)
    {
      var num = __instance.m_shieldDome.transform.localScale.x + (__instance.m_radius - __instance.m_shieldDome.transform.localScale.x) * __instance.m_decreaseInertia;
      __instance.m_shieldDome.transform.localScale = new Vector3(num, num, num);
    }
    if ((double)__instance.m_radiusTarget != (double)__instance.m_radius)
    {
      if (!__instance.m_firstCheck)
      {
        __instance.m_firstCheck = true;
        __instance.m_radius = __instance.m_radiusTarget;
      }
      var f = __instance.m_radiusTarget - __instance.m_radius;
      __instance.m_radius += Mathf.Min(__instance.m_startStopSpeed * Time.deltaTime, Mathf.Abs(f)) * ((double)f > 0.0 ? 1f : -1f);
    }
    if ((double)__instance.m_lastFuel == (double)__instance.m_lastFuelSent)
    {
      __instance.m_lastFuelSent = __instance.m_lastFuel;
      __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetFuel", (object)__instance.m_lastFuel);
    }
    if ((double)__instance.m_radius == (double)__instance.m_radiusSent)
    {
      __instance.m_radiusSent = __instance.m_radius;
      __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetRadius", (object)__instance.m_radius);
    }
    if ((double)__instance.m_lastHitTime != (double)__instance.m_lastHitTimeSent)
    {
      __instance.m_lastHitTimeSent = __instance.m_lastHitTime;
      __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetLastHitTime", (object)__instance.m_lastHitTime);
    }
  }
}