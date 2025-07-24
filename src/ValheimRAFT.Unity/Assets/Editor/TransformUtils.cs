#region

using UnityEditor;
using UnityEngine;
// Place this in an Editor folder

#endregion

namespace ValheimVehicles.SharedScripts
{

    public class TransformPathUtil
    {
        [MenuItem("GameObject/Copy Full Transform Path", false, 0)]
        public static void CopySelectedTransformPath()
        {
            if (Selection.activeTransform == null)
            {
                Debug.LogWarning("No transform selected.");
                return;
            }

            string path = GetFullPath(Selection.activeTransform);
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log($"Transform path copied: {path}");
        }

        static string GetFullPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }

}
