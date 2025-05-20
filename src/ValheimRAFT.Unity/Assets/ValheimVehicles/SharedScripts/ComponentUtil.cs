// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;

namespace ValheimVehicles.SharedScripts.Helpers
{
  public static class ComponentUtil
  {
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
      var component = go.GetComponent<T>();
      if (!component)
      {
        component = go.AddComponent<T>();
      }

      return component;
    }

    public static T GetOrCache<T>(
      this MonoBehaviour caller,
      ref T field,
      ref bool hasInit
    ) where T : Component
    {
      if (hasInit)
      {
        return field;
      }

      field = caller.GetComponent<T>();
      if (!field)
      {
        field = caller.gameObject.AddComponent<T>();
      }

      hasInit = true;
      return field;
    }
  }
}