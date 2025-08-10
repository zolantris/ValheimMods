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
    /// Computes a delta pose: only joints different from the base are included.
    /// </summary>
    public static Dictionary<string, JointPose> ComputeDeltaPose(
        Dictionary<string, JointPose> basePose,
        Dictionary<string, JointPose> otherPose,
        float positionEpsilon = 0.01f,
        float rotationEpsilon = 0.01f)
    {
        var delta = new Dictionary<string, JointPose>();
        foreach (var kvp in otherPose)
        {
            bool isDifferent = true;
            if (basePose.TryGetValue(kvp.Key, out var baseVal))
            {
                bool posDiff = (kvp.Value.Position - baseVal.Position).sqrMagnitude > positionEpsilon * positionEpsilon;
                bool rotDiff = Quaternion.Angle(kvp.Value.Rotation, baseVal.Rotation) > rotationEpsilon;
                isDifferent = posDiff || rotDiff;
            }
            // Not in basePose, or different enough
            if (isDifferent)
                delta[kvp.Key] = kvp.Value;
        }
        return delta;
    }

    /// <summary>
    /// Dumps a delta pose to a file as C# code (same format as your static pose dictionaries).
    /// </summary>
    public static void DumpDeltaPoseToFile(
        Dictionary<string, JointPose> basePose,
        Dictionary<string, JointPose> otherPose,
        string deltaName,
        string outputDir = null)
    {
        var delta = ComputeDeltaPose(basePose, otherPose);
        DumpPoseToFile(delta, deltaName, outputDir);
    }
        
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

            string outputFolderName = "unity-model-poses-export";

            // File name is just modelName
            string fileName = $"{safeModelName}.cs";
            if (string.IsNullOrEmpty(outputDir))
                outputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
           
            outputDir = Path.Combine(outputDir, outputFolderName);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
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