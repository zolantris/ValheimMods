#region

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    // ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

public static class FindPrefabsWithMissingScripts
    {
        [MenuItem("Tools/Diagnostics/Find Prefabs With Missing Scripts")]
        public static void FindAllPrefabsWithMissingScripts()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var prefabsWithMissing = new List<string>();

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);

                bool hasMissing = false;
                foreach (var go in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    var components = go.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            hasMissing = true;
                            break;
                        }
                    }

                    if (hasMissing)
                        break;
                }

                if (hasMissing)
                    prefabsWithMissing.Add(path);

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            if (prefabsWithMissing.Count == 0)
            {
                Debug.Log("✅ All prefabs are clean — no missing scripts found.");
            }
            else
            {
                Debug.LogWarning($"⚠️ Found {prefabsWithMissing.Count} prefab(s) with missing scripts:");
                foreach (var path in prefabsWithMissing)
                {
                    Debug.Log($"  • {path}");
                }
            }
        }
    }

}
