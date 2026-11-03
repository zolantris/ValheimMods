using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

// Private host instrumentation. Install before any world objects are loaded.
public static class AnchorWriteObserver
{
    public sealed class Entry
    {
        public string marker, key, value;
        public float time;
    }
    public static readonly List<Entry> Writes = new List<Entry>();
    private static readonly Dictionary<int, string> Keys = new[] {
        "VehicleAnchorState", "L4zerShark.RaftAnchorFix.v1.valid",
        "L4zerShark.RaftAnchorFix.v1.position", "L4zerShark.RaftAnchorFix.v1.rotation",
        "L4zerShark.RaftAnchorFix.v2.lockedAxes"
    }.ToDictionary(x => x.GetStableHashCode(), x => x);

    public static void Install(Harmony harmony)
    {
        foreach (var type in new[] { typeof(int), typeof(bool), typeof(Vector3), typeof(Quaternion) })
        {
            var method = AccessTools.Method(typeof(ZDO), "Set", new[] { typeof(int), type });
            if (method == null) throw new MissingMethodException("ZDO.Set(int," + type.Name + ")");
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AnchorWriteObserver), nameof(BeforeWrite)));
        }
    }
    private static void BeforeWrite(ZDO __instance, int __0, object __1)
    {
        if (!Keys.TryGetValue(__0, out var key)) return;
        string marker = __instance.GetString("AnchorIntegrationCase");
        if (string.IsNullOrEmpty(marker)) return;
        Writes.Add(new Entry { marker = marker, key = key, value = __1.ToString(), time = Time.realtimeSinceStartup });
    }
    public static int Count(string marker) => Writes.Count(x => x.marker == marker);
}
