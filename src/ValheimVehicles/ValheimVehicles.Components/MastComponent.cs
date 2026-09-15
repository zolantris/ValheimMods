using System;
using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace ValheimVehicles.Components;

public class MastComponent : MonoBehaviour
{
  public GameObject? m_sailObject;

  public Cloth? m_sailCloth;

  // Generated custom sails still use Unity Cloth. Vanilla 1.0 sails use a
  // skinned rig driven by these references instead of scaling a "Sail" child.
  public MagicaCloth? m_vanillaSailCloth;
  public Transform? m_sailBottomTransform;
  public Transform? m_sailFurledPosition;
  public Transform? m_sailMidfurledPosition;
  public Transform? m_sailUnfurledPosition;
  public AnimationCurve? m_sailBlendWeightCurve;

  public bool m_allowSailRotation = false;
  public Transform? m_rotationTransform = null;

  public bool m_allowSailShrinking = true;

  public bool m_disableCloth;


  // for custom masts. Other masts do not support this. We may need to add a selector to make this cleaner.
  public void Awake()
  {
    m_rotationTransform = transform.Find("rotational_yard");
  }

  public void ConfigureVanillaSail(Ship sourceShip)
  {
    var sourceRoot = sourceShip.m_mastObject.transform;
    m_vanillaSailCloth = RemapTransform(sourceRoot, sourceShip.m_sailCloth.transform)
      .GetComponent<MagicaCloth>();
    if (!m_vanillaSailCloth)
      throw new InvalidOperationException($"{name}: cloned vanilla sail cloth is missing.");

    // m_sailObject is null on the vanilla Raft and Karve in 1.0. Use the actual
    // cloth rig, not that obsolete field or the inactive legacy Drakkar sail.
    m_sailObject = m_vanillaSailCloth.gameObject;
    m_sailBottomTransform = RemapTransform(sourceRoot, sourceShip.m_sailBottomTransform);
    m_sailFurledPosition = RemapTransform(sourceRoot, sourceShip.m_sailFurledPosition);
    m_sailMidfurledPosition = RemapTransform(sourceRoot, sourceShip.m_sailMidfurledPosition);
    m_sailUnfurledPosition = RemapTransform(sourceRoot, sourceShip.m_sailUnfurledPosition);
    m_sailBlendWeightCurve = sourceShip.m_sailBlendWeightCurve;
  }

  private Transform RemapTransform(Transform sourceRoot, Transform source)
  {
    if (!source || !source.IsChildOf(sourceRoot))
      throw new InvalidOperationException($"{name}: vanilla sail reference is outside its mast.");

    // Drakkar has two children named "Sail". Replay sibling indices so cloned
    // references target the same rig, including inactive nodes, without pointing
    // back into the vanilla prefab.
    var indices = new Stack<int>();
    for (var current = source; current != sourceRoot; current = current.parent)
      indices.Push(current.GetSiblingIndex());
    var target = transform;
    while (indices.Count > 0) target = target.GetChild(indices.Pop());
    if (target.name != source.name && source != sourceRoot)
      throw new InvalidOperationException($"{name}: cloned sail hierarchy differs from the source.");
    return target;
  }

  public void UpdateSail(float sailPosition, Vector3 customScale)
  {
    if (m_vanillaSailCloth)
    {
      if (!m_sailBottomTransform || !m_sailFurledPosition ||
          !m_sailMidfurledPosition || !m_sailUnfurledPosition || m_sailBlendWeightCurve == null) return;

      var position = m_allowSailShrinking ? Mathf.Clamp01(sailPosition) : 1f;
      var from = position < 0.5f ? m_sailFurledPosition : m_sailMidfurledPosition;
      var to = position < 0.5f ? m_sailMidfurledPosition : m_sailUnfurledPosition;
      var amount = position < 0.5f ? position * 2f : (position - 0.5f) * 2f;
      m_sailBottomTransform.position = Vector3.Lerp(from.position, to.position, amount);

      // Match Ship.UpdateSailSize's rig/blend control. Toggling or rescaling
      // Magica each physics tick would reset its simulation.
      var weight = m_disableCloth ? 0f : m_sailBlendWeightCurve.Evaluate(position);
      if (!Mathf.Approximately(m_vanillaSailCloth.SerializeData.blendWeight, weight))
      {
        m_vanillaSailCloth.SerializeData.blendWeight = weight;
        m_vanillaSailCloth.SetParameterChange();
      }
      return;
    }

    if (!m_sailObject || !m_sailCloth) return; // Bare custom masts have no sail.
    var scale = m_allowSailShrinking ? customScale : Vector3.one;
    if (m_sailObject.transform.localScale != scale)
    {
      if (m_sailCloth.enabled) m_sailCloth.enabled = false;
      m_sailObject.transform.localScale = scale;
    }
    if (m_sailCloth.enabled != !m_disableCloth)
      m_sailCloth.enabled = !m_disableCloth;
  }
}
