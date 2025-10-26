using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Zolantris.Shared
{
  public struct JointPose
  {
    [CanBeNull] public Transform transform;
    public Vector3 Position;
    public Quaternion Rotation;

    public JointPose(Vector3 pos, Quaternion rot, Transform tr = null)
    {
      transform = tr;
      Position = pos;
      Rotation = rot;
    }

    public override string ToString()
    {
      return $"new JointPose(new Vector3({Position.x}f, {Position.y}f, {Position.z}f), new Quaternion({Rotation.x}f, {Rotation.y}f, {Rotation.z}f, {Rotation.w}f))";
    }
  }
}