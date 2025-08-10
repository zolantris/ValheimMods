using UnityEngine;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Zolantris.Shared
{
  [System.Serializable]
  public struct JointPose
  {
    public Vector3 Position;
    public Quaternion Rotation;

    public JointPose(Vector3 pos, Quaternion rot)
    {
      Position = pos;
      Rotation = rot;
    }

    public override string ToString() =>
      $"new JointPose(new Vector3({Position.x}f, {Position.y}f, {Position.z}f), new Quaternion({Rotation.x}f, {Rotation.y}f, {Rotation.z}f, {Rotation.w}f))";
  }
}