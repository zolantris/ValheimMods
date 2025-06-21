#region

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    // ReSharper disable ArrangeNamespaceBody
    // ReSharper disable NamespaceStyle

    public static class RemoveProBuilderComponents
    {
        [MenuItem("Tools/Cleanup/Remove ProBuilder Scripts From All Prefabs")]
        public static void RemoveProBuilderScripts()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var modifiedPrefabs = new List<string>();

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool hasChanges = false;
                var root = PrefabUtility.LoadPrefabContents(path);

                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;

                    var type = component.GetType();
                    string ns = type.Namespace ?? string.Empty;
                    string fullName = type.FullName ?? string.Empty;

                    if (
                        ns.Contains("UnityEngine.ProBuilder") ||
                        fullName.Contains("ProBuilderMesh") ||
                        fullName.Contains("ProBuilderMeshFilter") ||
                        fullName.Contains("ProBuilderMeshRenderer")
                    )
                    {
                        Object.DestroyImmediate(component, true);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    modifiedPrefabs.Add(path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            if (modifiedPrefabs.Count == 0)
            {
                Debug.Log("✅ No prefabs needed cleanup. All clean.");
            }
            else
            {
                Debug.Log($"✅ Removed ProBuilder scripts from {modifiedPrefabs.Count} prefab(s):");
                foreach (var path in modifiedPrefabs)
                {
                    Debug.Log($"  • {path}");
                }
            }
        }
    }
}