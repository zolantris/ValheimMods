// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Eldritch.Core
{
  public static class PrefabGraft
  {
    private static readonly BindingFlags F =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ---------- Field copier ----------
    public static void CopyFields(Component src, Component dst)
    {
      if (!src || !dst) return;
      var sType = src.GetType();
      var dType = dst.GetType();

      foreach (var sf in sType.GetFields(F))
      {
        if (sf.Name == "m_Script") continue;
        var df = dType.GetField(sf.Name, F);
        if (df == null) continue;
        if (df.FieldType != sf.FieldType) continue;

        try { df.SetValue(dst, sf.GetValue(src)); }
        catch
        {
          /* ignored */
        }
      }
    }

    // ---------- Reference remapper (fields, arrays, List<T>) ----------
    public static int RemapObjectReferences(GameObject root, UnityEngine.Object fromObj, UnityEngine.Object toObj)
    {
      if (!root || !fromObj || !toObj) return 0;
      var changes = 0;

      foreach (var c in root.GetComponentsInChildren<Component>(true))
      {
        if (!c) continue;
        var t = c.GetType();
        foreach (var f in t.GetFields(F))
        {
          if (f.IsInitOnly || f.IsLiteral) continue;
          var ft = f.FieldType;

          // direct ref
          if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
          {
            var v = f.GetValue(c) as UnityEngine.Object;
            if (v == fromObj && ft.IsAssignableFrom(toObj.GetType()))
            {
              f.SetValue(c, toObj);
              changes++;
            }
            continue;
          }

          // array
          if (ft.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(ft.GetElementType()))
          {
            var arr = f.GetValue(c) as Array;
            if (arr == null) continue;
            var et = ft.GetElementType();
            var mod = false;
            var newArr = Array.CreateInstance(et, arr.Length);
            for (var i = 0; i < arr.Length; i++)
            {
              var elem = arr.GetValue(i) as UnityEngine.Object;
              if (elem == fromObj && et.IsAssignableFrom(toObj.GetType()))
              {
                newArr.SetValue(toObj, i);
                mod = true;
                changes++;
              }
              else newArr.SetValue(arr.GetValue(i), i);
            }
            if (mod) f.SetValue(c, newArr);
            continue;
          }

          // List<T>
          if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>))
          {
            var et = ft.GetGenericArguments()[0];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(et)) continue;

            var list = f.GetValue(c) as IList;
            if (list == null) continue;
            var mod = false;
            for (var i = 0; i < list.Count; i++)
            {
              var elem = list[i] as UnityEngine.Object;
              if (elem == fromObj && et.IsAssignableFrom(toObj.GetType()))
              {
                list[i] = toObj;
                mod = true;
                changes++;
              }
            }
            if (mod) f.SetValue(c, list);
          }
        }
      }

      return changes;
    }

    private static bool IsVisualName(string s)
    {
      return !string.IsNullOrEmpty(s) && string.Equals(s, "Visual", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// If we ended up with Visual/Visual nesting, promote the inner children and remove redundant nodes.
    /// </summary>
    public static void FlattenVisuals(Transform visualRoot)
    {
      if (!visualRoot) return;

      // Repeatedly collapse Visual nodes that have a single child named Visual
      bool changed;
      var safety = 16;
      do
      {
        changed = false;
        if (--safety <= 0) break;

        // Look for immediate children that are also named Visual
        for (var i = 0; i < visualRoot.childCount; i++)
        {
          var child = visualRoot.GetChild(i);
          if (!IsVisualName(child.name)) continue;

          // Promote this child’s children into visualRoot, then destroy the child
          var promoted = 0;
          for (var j = 0; j < child.childCount; j++)
          {
            var grand = child.GetChild(j);
            grand.SetParent(visualRoot, false);
            j--;
            promoted++;
          }
          UnityEngine.Object.DestroyImmediate(child.gameObject);
          changed = true;
        }
      } while (changed);
    }

    // ---------- Visual replacement + animator ----------
    public static Animator ReplaceVisual(GameObject root, GameObject visualPrefab)
    {
      if (!root || !visualPrefab) return null;
      var old = root.transform.Find("Visual");
      if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

      var instance = UnityEngine.Object.Instantiate(visualPrefab, root.transform, false);
      instance.name = visualPrefab.name;

      var animator = instance.GetComponentInChildren<Animator>(true)
                     ?? instance.AddComponent<Animator>();
      animator.Rebind();
      animator.Update(0f);
      return animator;
    }

    // ---------- Generic "ensure + copy" on root ----------
    public static T EnsureOnRoot<T>(GameObject root, Component srcForFields = null) where T : Component
    {
      var dst = root.GetComponent<T>() ?? root.AddComponent<T>();
      if (srcForFields) CopyFields(srcForFields, dst);
      return dst;
    }

    // ---------- Graft a component type by name from a template root to a target root ----------
    public static Component GraftComponent(GameObject targetRoot, GameObject templateRoot, Type componentType)
    {
      var src = templateRoot.GetComponent(componentType);
      var dst = targetRoot.GetComponent(componentType) ?? targetRoot.AddComponent(componentType);
      if (src) CopyFields(src, dst);
      return dst;
    }

    public static T GraftComponent<T>(GameObject targetRoot, GameObject templateRoot) where T : Component
    {
      return (T)GraftComponent(targetRoot, templateRoot, typeof(T));
    }
  }
}