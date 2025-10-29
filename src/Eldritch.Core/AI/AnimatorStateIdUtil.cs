// csharp

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eldritch.Core
{
  public static class AnimatorStateIdUtil
  {
    public readonly struct StateId
    {
      public readonly int Short; // hash of just the state name, e.g. "attack_tail"
      public readonly int Full; // hash of full path, e.g. "Base Layer.attack_tail"

      public StateId(int shortHash, int fullHash)
      {
        Short = shortHash;
        Full = fullHash;
      }

      public static StateId FromShort(string shortName)
      {
        return new StateId(SafeHash(shortName), 0);
      }

      public static StateId FromFull(string fullPath)
      {
        return new StateId(0, SafeHash(fullPath));
      }

      public static StateId FromBoth(string shortName, string fullPath)
      {
        return new StateId(SafeHash(shortName), SafeHash(fullPath));
      }

      public static StateId From(string nameOrPath)
      {
        var h = SafeHash(nameOrPath);
        return LooksLikePath(nameOrPath) ? new StateId(0, h) : new StateId(h, 0);
      }
    }

    public static StateId[] Build(params string[] namesOrPaths)
    {
      if (namesOrPaths == null || namesOrPaths.Length == 0) return Array.Empty<StateId>();
      var list = new List<StateId>(namesOrPaths.Length);
      for (var i = 0; i < namesOrPaths.Length; i++)
      {
        var s = namesOrPaths[i];
        if (!string.IsNullOrEmpty(s)) list.Add(StateId.From(s));
      }
      return list.ToArray();
    }

    public static bool IsPlaying(Animator a, int layer, in StateId id, bool includeNext = true)
    {
      if (!a || layer < 0 || layer >= a.layerCount) return false;

      var cur = a.GetCurrentAnimatorStateInfo(layer);
      if (Matches(cur, id)) return true;

      if (includeNext && a.IsInTransition(layer))
      {
        var nxt = a.GetNextAnimatorStateInfo(layer);
        if (Matches(nxt, id)) return true;
      }
      return false;
    }

    public static bool IsPlayingAny(Animator a, int layer, StateId[] ids, bool includeNext = true)
    {
      if (!a || ids == null || ids.Length == 0 || layer < 0 || layer >= a.layerCount) return false;

      var cur = a.GetCurrentAnimatorStateInfo(layer);
      for (var i = 0; i < ids.Length; i++)
        if (Matches(cur, ids[i]))
          return true;

      if (includeNext && a.IsInTransition(layer))
      {
        var nxt = a.GetNextAnimatorStateInfo(layer);
        for (var i = 0; i < ids.Length; i++)
          if (Matches(nxt, ids[i]))
            return true;
      }
      return false;
    }

    public static float NormalizedTime01(in AnimatorStateInfo info)
    {
      var n = info.normalizedTime;
      return float.IsFinite(n) ? n - Mathf.Floor(n) : 0f;
    }

    private static bool Matches(in AnimatorStateInfo s, in StateId id)
    {
      if (id.Short != 0 && s.shortNameHash == id.Short) return true;
      if (id.Full != 0 && s.fullPathHash == id.Full) return true;
      return false;
    }

    private static int SafeHash(string s)
    {
      return string.IsNullOrEmpty(s) ? 0 : Animator.StringToHash(s);
    }
    private static bool LooksLikePath(string s)
    {
      return !string.IsNullOrEmpty(s) && (s.IndexOf('.') >= 0 || s.IndexOf('/') >= 0);
    }
  }
}