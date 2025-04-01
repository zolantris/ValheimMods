// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
// ReSharper disable UseNullableReferenceTypesAnnotationSyntax
// required for unity 2022 (local project)

#nullable enable

using UnityEngine;
namespace ValheimVehicles.SharedScripts
{
  public static class ValheimExtensions
  {
    public static IWearNTearStub? GetWearNTear(this GameObject obj)
    {
      var provider = WearNTearProviderBase.GetWearNTearComponent(obj);
      return provider;
    }
  }
}