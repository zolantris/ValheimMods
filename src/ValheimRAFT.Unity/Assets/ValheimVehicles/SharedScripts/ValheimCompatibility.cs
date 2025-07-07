// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
// ReSharper disable UseNullableReferenceTypesAnnotationSyntax
// required for unity 2022 (local project)

#nullable enable

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class ValheimCompatibility
  {
    /// <summary>
    /// Quick getter for WearNTear api
    /// </summary>
    /// Todo: May want to use a hashMap of HashMap GameObject, IWearNTearStub and identify if object has been fetched before to return the hashmap value.
    /// 
    /// <param name="obj"></param>
    /// <returns></returns>
    public static IWearNTearStub? GetWearNTear(this GameObject obj)
    {
      var provider = WearNTearProviderBase.GetWearNTearComponent(obj);
      return provider;
    }

    public static Transform GetPrefabRoot(Transform t)
    {
#if !UNITY_EDITOR && !UNITY_2022
      var netView = t.GetComponentInParent<ZNetView>();
      if (netView != null)
      {
        return netView.transform;
      }
      return t.root;
#else
      return t.root;
#endif
    }
  }
}