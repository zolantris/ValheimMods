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
/// Reflection is intentionally defensive because other mods can contain optional
/// compatibility types whose referenced assemblies are not installed.
/// One unloadable foreign type must never abort ValheimScalability startup.
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
    var destructibleInterface =
      typeof(IDestructible);

    var damageInterfaceMethod =
      AccessTools.Method(
        destructibleInterface,
        nameof(IDestructible.Damage),
        new[]
        {
          typeof(HitData)
        });

    if (damageInterfaceMethod == null)
    {
      yield break;
    }

    var seen =
      new HashSet<MethodBase>();

    foreach (var assembly in
             AppDomain.CurrentDomain.GetAssemblies())
    {
      foreach (var type in
               GetLoadableTypes(assembly))
      {
        var target =
          TryGetDamageTarget(
            type,
            destructibleInterface,
            damageInterfaceMethod);

        if (target != null &&
            seen.Add(target))
        {
          yield return target;
        }
      }
    }
  }

  /// <summary>
  /// All metadata inspection for a foreign type happens behind this exception boundary.
  ///
  /// A mod can successfully load an assembly while still containing individual types
  /// that cannot be fully resolved because an optional compatibility dependency is absent.
  /// Even operations such as Type.IsAssignableFrom can trigger that resolution.
  /// </summary>
  private static MethodBase? TryGetDamageTarget(
    Type? type,
    Type destructibleInterface,
    MethodInfo damageInterfaceMethod)
  {
    if (type == null)
    {
      return null;
    }

    try
    {
      // Compiler-generated closure/state-machine types can never be useful
      // IDestructible components and are a common place for optional-mod
      // compatibility references to surface.
      if (type.Name.StartsWith(
            "<",
            StringComparison.Ordinal))
      {
        return null;
      }

      if (type.IsInterface ||
          type.IsAbstract ||
          type.ContainsGenericParameters)
      {
        return null;
      }

      // IMPORTANT:
      // IsAssignableFrom itself can throw TypeLoadException when inspecting
      // a foreign type whose dependency is missing.
      if (!destructibleInterface.IsAssignableFrom(type))
      {
        return null;
      }

      var interfaceMap =
        type.GetInterfaceMap(
          destructibleInterface);

      for (var index = 0;
           index < interfaceMap.InterfaceMethods.Length;
           ++index)
      {
        if (!Equals(
              interfaceMap.InterfaceMethods[index],
              damageInterfaceMethod))
        {
          continue;
        }

        return interfaceMap.TargetMethods[index];
      }
    }
    catch
    {
      // Reflection over other mods must be best-effort.
      // Skipping one unloadable type is vastly safer than preventing the
      // entire plugin/Harmony patch set from loading.
    }

    return null;
  }

  private static IEnumerable<Type?> GetLoadableTypes(
    Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException exception)
    {
      // The successfully loaded entries are still useful. Individual entries
      // are guarded again by TryGetDamageTarget because even a returned Type
      // can fail later metadata inspection.
      return exception.Types;
    }
    catch
    {
      return Array.Empty<Type>();
    }
  }

  [HarmonyPrefix]
  private static void Prefix(
    object __instance)
  {
    if (Application.isBatchMode ||
        WearNTear.m_randomInitialDamage)
    {
      return;
    }

    if (__instance is not Component component ||
        !component)
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
      .NotifyDamage(
        component.gameObject);
  }
}
