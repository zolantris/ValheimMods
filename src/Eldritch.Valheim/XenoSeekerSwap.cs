// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using Eldritch.Core;
using Jotunn.Managers;
using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Valheim
{
  /// <summary>
  /// Swaps a Seeker-clone creature to your Xeno:
  /// - replaces Visual with xeno visual prefab
  /// - migrates MonsterAI -> Xeno_MonsterAI (field copy)
  /// - remaps references that pointed to the old MonsterAI
  /// - ensures CharacterAnimEvent is wired to ZNetView
  /// </summary>
  public static class XenoSeekerSwap
  {
    /// <param name="seekerClone">The cloned creature (from "Seeker").</param>
    /// <param name="xenoVisualPrefab">Your xeno visual prefab (contains the xeno rig/animator).</param>
    public static bool SwapToXeno(GameObject seekerClone, GameObject xenoVisualPrefab)
    {
      if (!seekerClone || !xenoVisualPrefab) return false;

      // 1) Visual swap + animator rebind
      var animator = ComponentSwapUtil.ReplaceVisual(seekerClone, xenoVisualPrefab);
      ComponentSwapUtil.SafeAnimatorRebind(animator);

      // 2) Ensure Character + ZNetView present
      var character = seekerClone.GetComponent<Character>() ?? seekerClone.AddComponent<Character>();
      var nview = seekerClone.GetComponent<ZNetView>() ?? seekerClone.AddComponent<ZNetView>();

      // 3) Ensure CharacterAnimEvent on animator object and hook to nview
      if (animator)
      {
        var animGO = animator.gameObject;
        var cae = animGO.GetComponent<CharacterAnimEvent>() ?? animGO.AddComponent<CharacterAnimEvent>();
        cae.m_nview = nview;
      }

      // 4) Migrate MonsterAI -> Xeno_MonsterAI
      var srcAI = seekerClone.GetComponent<MonsterAI>();
      if (!srcAI)
      {
        LoggerProvider.LogError("SwapToXeno: Source MonsterAI not found on clone.");
        return false;
      }

      // Add your derived AI
      var dstAI = seekerClone.GetComponent<Xeno_MonsterAI>();
      if (!dstAI) dstAI = seekerClone.AddComponent<Xeno_MonsterAI>();

      // Copy matching fields (ranges, timers, booleans, etc.)
      ComponentSwapUtil.CopyFields(srcAI, dstAI);

      // Make sure critical bindings are correct
      dstAI.m_character = character;
      // Path agent type stays same as Seeker unless you want to override:
      // dstAI.m_pathAgentType = Pathfinding.AgentType.Humanoid;

      // 5) Remap any references elsewhere that pointed to the old AI
      var replaced = ComponentSwapUtil.RemapObjectReferences(seekerClone, srcAI, dstAI);

      // 6) Remove the old AI
      Object.DestroyImmediate(srcAI);

      // 7) Collider sanity (optional)
      if (!seekerClone.GetComponent<CapsuleCollider>())
      {
        var cap = seekerClone.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1.0f, 0f);
        cap.radius = 0.45f;
        cap.height = 2.0f;
      }

      LoggerProvider.LogInfo($"SwapToXeno complete: migrated MonsterAI -> Xeno_MonsterAI, remapped {replaced} refs.");
      return true;
    }
  }

  /// <summary>
  /// Your derived AI – keeps MonsterAI contract so CopyFields can move tuning over.
  /// Implement your custom behaviors while honoring base fields (view/hear/aggro).
  /// </summary>
  public class Xeno_MonsterAI : MonsterAI
  {
    // Put your custom hooks/overrides here, e.g. tail attack, pounce, etc.
    // Keep base serialized fields so the Seeker tuning migrates cleanly.
  }
}