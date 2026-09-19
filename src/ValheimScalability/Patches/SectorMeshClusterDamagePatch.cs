using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimScalability.Rendering.Clustering;

namespace ValheimScalability.Patches;

/// <summary>
/// Hooks concrete IDestructible.Damage(HitData) implementations without maintaining
/// a hard-coded list of WearNTear / tree / rock / mine-rock types.
///
/// The manager ignores damage for objects that were never registered as cluster candidates.
/// </summary>
[HarmonyPatch]
public static class SectorMeshClusterDamagePatch
{
  /// <summary>
  /// Harmony calls this once while PatchAll is being applied.
  /// Each yielded MethodBase becomes a target for the Prefix below.
  /// </summary>
  [HarmonyTargetMethods]
  private static IEnumerable<MethodBase> TargetMethods()
  {
    var destructibleInterface = typeof(IDestructible);
    var damageInterfaceMethod = AccessTools.Method(
      destructibleInterface,
      nameof(IDestructible.Damage),
      new[] { typeof(HitData) });

    if (damageInterfaceMethod == null)
    {
      yield break;
    }

    var seen = new HashSet<MethodBase>();

    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      foreach (var type in GetLoadableTypes(assembly))
      {
        if (type == null ||
            type.IsInterface ||
            type.IsAbstract ||
            type.ContainsGenericParameters ||
            !destructibleInterface.IsAssignableFrom(type))
        {
          continue;
        }

        InterfaceMapping interfaceMap;

        try
        {
          interfaceMap = type.GetInterfaceMap(destructibleInterface);
        }
        catch
        {
          continue;
        }

        for (var index = 0;
             index < interfaceMap.InterfaceMethods.Length;
             ++index)
        {
          if (!Equals(interfaceMap.InterfaceMethods[index], damageInterfaceMethod))
          {
            continue;
          }

          var target = interfaceMap.TargetMethods[index];

          if (target != null && seen.Add(target))
          {
            yield return target;
          }

          break;
        }
      }
    }
  }

  private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException exception)
    {
      return exception.Types;
    }
    catch
    {
      return Array.Empty<Type>();
    }
  }

  [HarmonyPrefix]
  private static void Prefix(object __instance)
  {
    if (Application.isBatchMode ||
        WearNTear.m_randomInitialDamage)
    {
      return;
    }

    if (__instance is not Component component || !component)
    {
      return;
    }

    // Characters also implement IDestructible, but they are never sector-cluster candidates.
    // Avoid even entering the manager lookup for ordinary creature/player combat.
    if (component.GetComponentInParent<Character>())
    {
      return;
    }

    SectorMeshClusterManager.Instance?
      .NotifyDamage(component.gameObject);
  }
}
