// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Eldritch.Valheim
{
  public static class XenoFromSeekerMinimalMerge
  {
    private const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // Hard excludes: anything that can mess visuals/rig/physics or will duplicate valheim renderers
    private static readonly HashSet<Type> DenyTypes = new()
    {
      typeof(Transform),
      typeof(Animator), typeof(Animation),
      typeof(SkinnedMeshRenderer), typeof(MeshRenderer), typeof(MeshFilter),
      typeof(Light), typeof(ReflectionProbe), typeof(LODGroup),
      typeof(ParticleSystem), typeof(ParticleSystemRenderer),
      typeof(TrailRenderer), typeof(LineRenderer), typeof(Cloth),
      typeof(Rigidbody), typeof(Collider), typeof(CharacterController)
    };

    // Fields we never overwrite when we copy (we want Xeno’s authored refs)
    private static readonly HashSet<string> DenyFieldNames = new(StringComparer.Ordinal)
    {
      "m_animator", "m_head", "m_eye",
      "m_visual", "m_visualRoot", "m_model", "m_root"
    };

    // Optional: only copy these gameplay components if present on Seeker.
    // (Leave empty to copy everything except DenyTypes.)
    private static readonly HashSet<string> WhitelistTypeNames = new(StringComparer.Ordinal)
    {
      "Character",
      "Humanoid",
      "BaseAI",
      "MonsterAI",
      "SEMan",
      "Noise",
      "FootStep",
      "Tameable",
      "ZNetView",
      "ZSyncTransform",
      "ZSyncAnimation" // will be rebound to the Xeno animator below
    };

    /// <summary>
    /// Merge Seeker ROOT components into an already-instantiated Xeno instance:
    /// - Only adds components that do NOT already exist on Xeno root (by exact type).
    /// - Never touches visuals/bones/animator.
    /// - Rebinds Humanoid.m_animator and ZSyncAnimation to the Xeno Animator.
    /// </summary>
    public static bool MergeIntoXeno(GameObject xenoInstance, GameObject seekerPrefab)
    {
      if (!xenoInstance || !seekerPrefab) return false;

      // 1) Find the Xeno animator under Visual (or anywhere under root)
      var xenoAnimator = FindXenoAnimator(xenoInstance);
      if (!xenoAnimator)
      {
        Debug.LogError("[XenoMerge] Xeno Animator not found; aborting merge.");
        return false;
      }

      // 2) Spin up a TEMP seeker (we’ll read fields from it, then destroy)
      var seekerTemp = UnityEngine.Object.Instantiate(seekerPrefab);
      try
      {
        var seekerComps = seekerTemp.GetComponents<Component>();
        foreach (var src in seekerComps)
        {
          if (!src) continue;
          var t = src.GetType();

          if (DenyTypes.Contains(t)) continue;
          if (WhitelistTypeNames.Count > 0 && !WhitelistTypeNames.Contains(t.Name)) continue;

          // Only add if Xeno root DOES NOT already have this component type
          var existing = xenoInstance.GetComponent(t);
          if (existing) continue;

          var dst = xenoInstance.AddComponent(t);
          CopySerializableFields(src, dst, DenyFieldNames);
          PostCopyFixups(dst, xenoAnimator);
        }

        // 3) Mandatory bindings for Valheim
        var humanoid = xenoInstance.GetComponent<Humanoid>();
        if (humanoid && humanoid.m_animator != xenoAnimator)
          humanoid.m_animator = xenoAnimator;

        EnsureZSyncAnimationLivesOnAnimator(xenoInstance, xenoAnimator);

        // 4) Refresh animator graph
        xenoAnimator.Rebind();
        xenoAnimator.Update(0f);
        xenoAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // (optional: for debug)

        return true;
      }
      finally
      {
        UnityEngine.Object.Destroy(seekerTemp);
      }
    }

    private static Animator FindXenoAnimator(GameObject xeno)
    {
      var visual = xeno.transform.Find("Visual");
      if (visual)
      {
        var a = visual.GetComponentInChildren<Animator>(true);
        if (a) return a;
      }
      return xeno.GetComponentInChildren<Animator>(true);
    }

    private static void CopySerializableFields(Component src, Component dst, HashSet<string> skipNames)
    {
      var sType = src.GetType();
      var dType = dst.GetType();

      // Match fields by name/type; ignore m_Script and excluded names
      foreach (var sf in sType.GetFields(F))
      {
        if (sf.IsInitOnly || sf.IsLiteral) continue;
        if (sf.Name == "m_Script") continue;
        if (skipNames.Contains(sf.Name)) continue;

        var df = dType.GetField(sf.Name, F);
        if (df == null) continue;
        if (df.FieldType != sf.FieldType) continue;

        var val = sf.GetValue(src);

        // Don’t copy transform/animator/renderers through arbitrary fields
        if (val is Transform || val is Animator || val is Renderer || val is MeshFilter) continue;

        try { df.SetValue(dst, val); }
        catch
        {
          /* ignore bad set */
        }
      }
    }

    /// <summary>
    /// After copying, rewire common animator fields to the Xeno animator.
    /// </summary>
    private static void PostCopyFixups(Component dst, Animator xenoAnimator)
    {
      if (!dst || !xenoAnimator) return;

      var t = dst.GetType();

      // Rebind m_animator if present
      var f = t.GetField("m_animator", F);
      if (f != null && typeof(Animator).IsAssignableFrom(f.FieldType))
      {
        f.SetValue(dst, xenoAnimator);
      }
    }

    /// <summary>
    /// Ensure exactly one ZSyncAnimation lives on the Animator GameObject,
    /// and remove/disable any strays from the root to avoid conflicts.
    /// </summary>
    private static void EnsureZSyncAnimationLivesOnAnimator(GameObject xenoRoot, Animator anim)
    {
      if (!xenoRoot || !anim) return;

      // Remove or disable ZSyncAnimation on root if present & not the animator GO
      var stray = xenoRoot.GetComponent<ZSyncAnimation>();
      if (stray && stray.gameObject != anim.gameObject)
      {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(stray);
        else UnityEngine.Object.Destroy(stray);
#else
        UnityEngine.Object.Destroy(stray);
#endif
      }

      // Ensure ZSyncAnimation on the animator object
      var zs = anim.GetComponent<ZSyncAnimation>() ?? anim.gameObject.AddComponent<ZSyncAnimation>();

      // If ZSyncAnimation has an explicit animator field (some forks do), point it
      var zt = zs.GetType();
      var f = zt.GetField("m_animator", F);
      if (f != null && typeof(Animator).IsAssignableFrom(f.FieldType))
      {
        f.SetValue(zs, anim);
      }
    }
  }
}