using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Zolantris.Shared
{

    public static class JointPoseDumpUtility
    {
        /// <summary>
        /// Dumps a pose dictionary to a file as C# code.
        /// </summary>
        /// <param name="poseDict">Dictionary of bone names to JointPose.</param>
        /// <param name="modelName">Base name for file and variable.</param>
        /// <param name="outputDir">Optional output directory. Defaults to Desktop.</param>
        public static void DumpPoseToFile(Dictionary<string, JointPose> poseDict, string modelName, string outputDir = null)
        {
            // Clean up model name for safe variable names
            string safeModelName = modelName.Replace(" ", "_").Replace("-", "_");
            string dateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string varName = $"ModelJointPoseSnapshot_{safeModelName}_{dateTime}";

            // File name is just modelName
            string fileName = $"{safeModelName}.cs";
            if (string.IsNullOrEmpty(outputDir))
                outputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(outputDir, fileName);

            // Write C# output
            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated model pose snapshot");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("public static class CustomAnimationPoses");
            sb.AppendLine("{");
            sb.AppendLine($"    public static readonly System.Collections.Generic.Dictionary<string, JointPose> {varName} = new() {{");
            foreach (var kvp in poseDict)
            {
                var p = kvp.Value.Position;
                var q = kvp.Value.Rotation;
                sb.AppendLine($"        [\"{kvp.Key}\"] = new JointPose(new Vector3({p.x}f, {p.y}f, {p.z}f), new Quaternion({q.x}f, {q.y}f, {q.z}f, {q.w}f)),");
            }
            sb.AppendLine("    };");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[JointPoseDumpUtility] Pose snapshot written to: {filePath}\nVariable: {varName}");
#endif
        }
    }
}