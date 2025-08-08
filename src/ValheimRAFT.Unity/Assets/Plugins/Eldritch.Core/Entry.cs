using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zolantris.Shared;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{
  public static class Entry
  {
    public const string name = "EldritchCore";
    public const string version = "1.0.0";

    public static AssetBundle LoadAssembly()
    {
      var assetBundle = LoadAssetBundleFromResources("eldritch", Assembly.GetExecutingAssembly());
      return assetBundle;
    }

    private static AssetBundle LoadAssetBundleFromResources(string bundleName, Assembly resourceAssembly)
    {
      if (resourceAssembly == null)
      {
        throw new ArgumentNullException("Parameter resourceAssembly can not be null.");
      }

      string resourceName = null;
      try
      {
        resourceName = resourceAssembly.GetManifestResourceNames().Single(str => str.EndsWith(bundleName));
      }
      catch (Exception) {}

      if (resourceName == null)
      {
        LoggerProvider.LogError($"AssetBundle {bundleName} not found in assembly manifest");
        return null;
      }

      AssetBundle ret;
      using (var stream = resourceAssembly.GetManifestResourceStream(resourceName))
      {
        ret = AssetBundle.LoadFromStream(stream);
      }

      return ret;
    }
  }
}