using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class WearNTear_Patch
{
  [HarmonyPatch(typeof(WearNTear), "Highlight")]
  [HarmonyPrefix]
  private static bool WearNTear_Highlight(WearNTear __instance)
  {
    // 0.217.46 caused lots of issues with the new null check on m_oldMaterials
    // return __instance.m_oldMaterials != null;
    if (__instance.m_oldMaterials == null)
    {
      __instance.m_oldMaterials = new List<WearNTear.OldMeshData>();
      foreach (Renderer highlightRenderer in __instance.GetHighlightRenderers())
      {
        WearNTear.OldMeshData oldMeshData = new WearNTear.OldMeshData()
        {
          m_materials = highlightRenderer.sharedMaterials
        };
        oldMeshData.m_color = new Color[oldMeshData.m_materials.Length];
        oldMeshData.m_emissiveColor = new Color[oldMeshData.m_materials.Length];
        for (int index = 0; index < oldMeshData.m_materials.Length; ++index)
        {
          if (oldMeshData.m_materials[index] == null) continue;
          if (oldMeshData.m_materials[index].HasProperty("_Color"))
            oldMeshData.m_color[index] = oldMeshData.m_materials[index].GetColor("_Color");
          if (oldMeshData.m_materials[index].HasProperty("_EmissionColor"))
            oldMeshData.m_emissiveColor[index] =
              oldMeshData.m_materials[index].GetColor("_EmissionColor");
        }

        oldMeshData.m_renderer = highlightRenderer;
        __instance.m_oldMaterials.Add(oldMeshData);
      }
    }

    float supportColorValue = __instance.GetSupportColorValue();
    Color color = new Color(0.6f, 0.8f, 1f);
    if ((double)supportColorValue >= 0.0)
    {
      float H;
      float S;
      Color.RGBToHSV(
        Color.Lerp(new Color(1f, 0.0f, 0.0f), new Color(0.0f, 1f, 0.0f), supportColorValue), out H,
        out S, out float _);
      S = Mathf.Lerp(1f, 0.5f, supportColorValue);
      float V = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
      color = Color.HSVToRGB(H, S, V);
    }

    foreach (WearNTear.OldMeshData oldMaterial in __instance.m_oldMaterials)
    {
      if ((bool)(UnityEngine.Object)oldMaterial.m_renderer)
      {
        foreach (Material material in oldMaterial.m_renderer.materials)
        {
          material.SetColor("_EmissionColor", color * 0.4f);
          material.color = color;
        }
      }
    }

    __instance.CancelInvoke("ResetHighlight");
    __instance.Invoke("ResetHighlight", 0.2f);

    return false;
  }

  [HarmonyPatch(typeof(WearNTear), "Start")]
  [HarmonyPrefix]
  private static bool WearNTear_Start(WearNTear __instance)
  {
    // we could check to see if the object is within a Controller, but this is unnecessary. Start just needs a protector.
    // this is a patch for basegame to prevent WNT from calling on objects without heightmaps which will return a NRE
    var hInstance = Heightmap.FindHeightmap(__instance.transform.position);

    if (hInstance != null) return true;

    Logger.LogWarning(
      $"WearNTear heightmap not found, this could be a problem with a prefab layer type not being a piece, netview name: {__instance.m_nview.name}");

    __instance.m_connectedHeightMap = hInstance;
    return false;
  }

  [HarmonyPatch(typeof(WearNTear), "Destroy")]
  [HarmonyPrefix]
  private static bool WearNTear_Destroy(WearNTear __instance)
  {
    // todo to find a better way to omit hull damage on item creation, most likely it's a collider problem triggering extreme damage.
    // allows for skipping other checks since this is the ship that needs to be destroyed and not trigger a loop
    if (__instance.gameObject.name.Contains(PrefabNames.WaterVehicleShip))
    {
      return BaseVehicleController.CanDestroyVehicle(__instance.m_nview);
    }


    var bv = __instance.GetComponentInParent<BaseVehicleController>();
    if ((bool)bv)
    {
      bv.DestroyPiece(__instance);
      return true;
    }

    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr) mbr.DestroyPiece(__instance);

    /*
     * SAFETY FEATURE
     * - prevent destruction of ship attached pieces if the ship fails to initialize properly
     */
    if (__instance.m_nview && !(bool)mbr && !(bool)bv)
    {
      var hasVehicleParent =
        __instance.m_nview.m_zdo.GetInt(BaseVehicleController.MBParentIdHash, 0);
      if (hasVehicleParent != 0)
      {
        __instance.enabled = false;
        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
  [HarmonyPrefix]
  private static bool WearNTear_ApplyDamage(WearNTear __instance, float damage)
  {
    var mbr = __instance.GetComponent<MoveableBaseShipComponent>();
    var bv = __instance.GetComponent<BaseVehicleController>();

    // todo to find a better way to omit hull damage on item creation, most likely it's a collider problem triggering extreme damage.
    if (__instance.gameObject.name.Contains(PrefabNames.WaterVehicleContainer))
    {
      return false;
    }

    // vehicles ignore WNT for now...
    if ((bool)mbr || (bool)bv)
    {
      return false;
    }

    return true;
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
          AccessTools.Method(typeof(WearNTear_Patch),
            nameof(AttachRigidbodyMovableBase)));
        break;
      }

    return list;
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPrefix]
  private static bool UpdateSupport(WearNTear __instance)
  {
    if (!__instance.isActiveAndEnabled) return false;
    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    var baseVehicle = __instance.GetComponentInParent<BaseVehicleController>();
    if (!(bool)mbr && !(bool)baseVehicle) return true;
    // if (__instance.transform.localPosition.y > 1f) return true;

    // makes all support values below 1f very high
    __instance.m_nview.GetZDO().Set(ZDOVars.s_support, 1500f);
    __instance.m_support = 1500f;
    __instance.m_supports = true;
    __instance.m_noSupportWear = true;
    return false;
  }

  private static Rigidbody? AttachRigidbodyMovableBase(Collider collider)
  {
    var rb = collider.attachedRigidbody;
    if (!rb) return null;
    var mbr = rb.GetComponent<MoveableBaseRootComponent>();
    var bvc = rb.GetComponent<BaseVehicleController>();
    if ((bool)mbr || bvc) return null;
    return rb;
  }
}