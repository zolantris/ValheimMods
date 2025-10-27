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
  public static class ComponentSwapUtil
  {
    private static readonly BindingFlags FieldFlags =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Copy all compatible fields from source to target.
    /// Skips Unity's hidden m_Script and fields with mismatched types.
    /// </summary>
    public static void CopyFields(Component source, Component target)
    {
      if (!source || !target) return;

      var sType = source.GetType();
      var tType = target.GetType();

      foreach (var sField in sType.GetFields(FieldFlags))
      {
        if (sField.Name == "m_Script") continue;

        var tField = tType.GetField(sField.Name, FieldFlags);
        if (tField == null) continue;
        if (tField.FieldType != sField.FieldType) continue;

        try
        {
          var value = sField.GetValue(source);
          tField.SetValue(target, value);
        }
        catch
        {
          /* swallow and continue */
        }
      }
    }

    /// <summary>
    /// Scan all components on root and its children and replace references to 'fromObj' with 'toObj'.
    /// Works for fields, arrays, and List&lt;&gt;. Only replaces when the field element type is assignable from 'toObj'.
    /// </summary>
    public static int RemapObjectReferences(GameObject root, UnityEngine.Object fromObj, UnityEngine.Object toObj)
    {
      if (!root || !fromObj || !toObj) return 0;

      var changes = 0;
      var comps = root.GetComponentsInChildren<Component>(true);

      foreach (var comp in comps)
      {
        if (!comp) continue;
        var cType = comp.GetType();

        foreach (var field in cType.GetFields(FieldFlags))
        {
          if (field.IsInitOnly || field.IsLiteral) continue;

          var fType = field.FieldType;

          // Direct object field
          if (typeof(UnityEngine.Object).IsAssignableFrom(fType))
          {
            var val = field.GetValue(comp) as UnityEngine.Object;
            if (val == fromObj && fType.IsAssignableFrom(toObj.GetType()))
            {
              field.SetValue(comp, toObj);
              changes++;
            }
            continue;
          }

          // Array
          if (fType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(fType.GetElementType()))
          {
            var arr = field.GetValue(comp) as Array;
            if (arr == null) continue;

            var modified = false;
            var elemType = fType.GetElementType();
            var newArr = Array.CreateInstance(elemType, arr.Length);

            for (var i = 0; i < arr.Length; i++)
            {
              var elem = arr.GetValue(i) as UnityEngine.Object;
              if (elem == fromObj && elemType.IsAssignableFrom(toObj.GetType()))
              {
                newArr.SetValue(toObj, i);
                modified = true;
                changes++;
              }
              else
              {
                newArr.SetValue(arr.GetValue(i), i);
              }
            }

            if (modified) field.SetValue(comp, newArr);
            continue;
          }

          // List<T>
          if (fType.IsGenericType && fType.GetGenericTypeDefinition() == typeof(List<>))
          {
            var elemType = fType.GetGenericArguments()[0];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(elemType)) continue;

            var list = field.GetValue(comp) as IList;
            if (list == null) continue;

            var modified = false;
            for (var i = 0; i < list.Count; i++)
            {
              var elem = list[i] as UnityEngine.Object;
              if (elem == fromObj && elemType.IsAssignableFrom(toObj.GetType()))
              {
                list[i] = toObj;
                modified = true;
                changes++;
              }
            }
            if (modified) field.SetValue(comp, list);
          }
        }
      }

      return changes;
    }

    /// <summary>
    /// Destroy an existing "Visual" child (if present) and instantiate a new visual prefab under that name.
    /// Returns the Animator found/created under Visual.
    /// </summary>
    public static Animator ReplaceVisual(GameObject root, GameObject visualPrefab)
    {
      if (!root || !visualPrefab) return null;

      var old = root.transform.Find("Visual");
      if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

      var instance = UnityEngine.Object.Instantiate(visualPrefab, root.transform, false);
      instance.name = visualPrefab.name;

      var animator = instance.GetComponentInChildren<Animator>(true)
                     ?? instance.AddComponent<Animator>();

      return animator;
    }

    public static void SafeAnimatorRebind(Animator animator)
    {
      if (!animator) return;
      animator.Rebind();
      animator.Update(0f);
    }
  }
}