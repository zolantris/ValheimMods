// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Reflection;
using Eldritch.Core;
using Jotunn.Managers;
using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Valheim
{
  /// <summary>
  /// Builds a Xeno prefab from a Seeker clone by:
  /// - swapping Visual to Xeno model (animator rebound)
  /// - migrating MonsterAI -> XenoDrone_MonsterAI
  /// - grafting your top-level APIs from a template Xeno prefab onto the ROOT
  /// - remapping references pointing at old components to their new counterparts
  /// </summary>
  public static class XenoFromSeekerBuilder
  {
    // Add/adjust your top-level APIs here:
    private static readonly System.Type[] TopLevelApis =
    {
      typeof(XenoDroneAI), // your brain
      typeof(XenoAIMovementController), // locomotion
      typeof(AbilityManager), // abilities
      typeof(XenoAudioController) // audio controls
    };

    /// <param name="seekerCloneRoot">The already-cloned "Seeker" GameObject</param>
    /// <param name="xenoVisualPrefab">Your model/rig prefab (goes under Visual)</param>
    /// <param name="xenoTemplateRoot">
    /// A prefab/GO that has your full top-level Xeno components configured as you like.
    /// We copy serialized fields from here when adding those components to the Seeker clone root.
    /// </param>
    public static bool BuildXenoFromSeeker(GameObject seekerCloneRoot, GameObject xenoVisualPrefab, GameObject xenoTemplateRoot)
    {
      if (!seekerCloneRoot || !xenoVisualPrefab || !xenoTemplateRoot)
      {
        LoggerProvider.LogError("BuildXenoFromSeeker: missing required prefabs/roots.");
        return false;
      }

      // 1) Ensure required Valheim basics
      var nview = seekerCloneRoot.GetComponent<ZNetView>() ?? seekerCloneRoot.AddComponent<ZNetView>();
      var character = seekerCloneRoot.GetComponent<Character>() ?? seekerCloneRoot.AddComponent<Character>();
      var zSyncTransform = seekerCloneRoot.GetComponent<ZSyncTransform>() ?? seekerCloneRoot.AddComponent<ZSyncTransform>();
      var zSyncAnimation = seekerCloneRoot.GetComponent<ZSyncAnimation>() ?? seekerCloneRoot.AddComponent<ZSyncAnimation>();

      // capsule collider setup

      var seekerCapsule = seekerCloneRoot.GetComponent<CapsuleCollider>();
      var xenoTemplateCapsule = xenoTemplateRoot.GetComponent<CapsuleCollider>();

      if (xenoTemplateCapsule)
      {
        if (seekerCapsule)
        {
          LayerHelpers.GetActiveLayers(seekerCapsule.includeLayers, "seekerCapsule includeLayers");
          LayerHelpers.GetActiveLayers(seekerCapsule.includeLayers, "seekerCapsule excludeLayers");
          PrefabGraft.CopyFields(xenoTemplateCapsule, seekerCapsule);
        }
        else
        {
          var cap = seekerCloneRoot.AddComponent<CapsuleCollider>();
          PrefabGraft.CopyFields(xenoTemplateCapsule, cap);
        }
      }
      else if (!seekerCapsule)
      {
        LoggerProvider.LogDebug("No SeekerCapsule detected; adding default Xeno capsule.");
        var cap = seekerCloneRoot.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1.5f, 0f);
        cap.radius = 0.7f;
        cap.height = 3.0f;
      }

      // 2) Replace Visual → Xeno and bind anim events
      var animator = PrefabGraft.ReplaceVisual(seekerCloneRoot, xenoVisualPrefab);

      if (!animator)
      {
        LoggerProvider.LogError("BuildXenoFromSeeker: animator not found after Visual swap.");
        return false;
      }
      var cae = animator.gameObject.GetComponent<CharacterAnimEvent>() ?? animator.gameObject.AddComponent<CharacterAnimEvent>();
      cae.m_nview = nview;

      // 3) MonsterAI → XenoDrone_MonsterAI (migrate tuning)
      var srcAI = seekerCloneRoot.GetComponent<MonsterAI>();
      var xenoAI = seekerCloneRoot.GetComponent<XenoDrone_MonsterAI>() ?? seekerCloneRoot.AddComponent<XenoDrone_MonsterAI>();
      if (srcAI)
      {
        PrefabGraft.CopyFields(srcAI, xenoAI);
        xenoAI.m_character = character;
        var remapped = PrefabGraft.RemapObjectReferences(seekerCloneRoot, srcAI, xenoAI);
        Object.DestroyImmediate(srcAI);
        LoggerProvider.LogInfo($"MonsterAI→XenoDrone_MonsterAI migrated; remapped {remapped} refs.");
      }
      else
      {
        // No MonsterAI on source? just ensure character binding
        xenoAI.m_character = character;
      }

      // 4) Graft your top-level APIs ONTO ROOT from your template root
      foreach (var type in TopLevelApis)
      {
        var src = xenoTemplateRoot.GetComponent(type);
        var dst = seekerCloneRoot.GetComponent(type) ?? seekerCloneRoot.AddComponent(type);
        if (src) PrefabGraft.CopyFields(src, dst);

        // Make a best-effort binding pass for common fields
        TryBindCommonFields(dst, character, xenoAI, animator);
        // Remap references from template components to the newly added ones where possible
        PrefabGraft.RemapObjectReferences(seekerCloneRoot, src, dst);
      }

      // 5) Final animator sanity
      animator.Rebind();
      animator.Update(0f);

      return true;
    }

    // Best-effort convenience binder for typical fields you likely have
    private static void TryBindCommonFields(Component cmp, Character character, MonsterAI ai, Animator animator)
    {
      if (!cmp) return;
      var t = cmp.GetType();
      var f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      void SetIfExists(string name, object value)
      {
        var field = t.GetField(name, f);
        if (field != null && field.FieldType.IsInstanceOfType(value)) field.SetValue(cmp, value);
      }

      // common names—adjust/extend as needed
      SetIfExists("m_character", character);
      SetIfExists("character", character);
      SetIfExists("ownerCharacter", character);

      SetIfExists("m_ai", ai);
      SetIfExists("ai", ai);
      SetIfExists("brain", ai);

      SetIfExists("m_animator", animator);
      SetIfExists("animator", animator);
      SetIfExists("animationController", animator);
    }
  }
}