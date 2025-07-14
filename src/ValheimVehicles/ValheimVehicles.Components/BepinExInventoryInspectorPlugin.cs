using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Linq;

[BepInPlugin("dev.inventorypatchdetector", "Inventory Patch Detector", "1.0.0")]
public class PatchDetector : BaseUnityPlugin
{
  private void Start()
  {
    var inventoryType = typeof(Inventory);
    var methods = inventoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
      .Where(m => m.Name == "RemoveItem");

    foreach (var method in methods)
    {
      var info = Harmony.GetPatchInfo(method);
      if (info == null) continue;

      Logger.LogInfo($"RemoveItem PATCHED: {method}");
      foreach (var pre in info.Prefixes)
        Logger.LogInfo($"  Prefix: {pre.owner} {pre.PatchMethod.DeclaringType}.{pre.PatchMethod.Name}");
      foreach (var post in info.Postfixes)
        Logger.LogInfo($"  Postfix: {post.owner} {post.PatchMethod.DeclaringType}.{post.PatchMethod.Name}");
      foreach (var trans in info.Transpilers)
        Logger.LogInfo($"  Transpiler: {trans.owner} {trans.PatchMethod.DeclaringType}.{trans.PatchMethod.Name}");
    }
  }
}