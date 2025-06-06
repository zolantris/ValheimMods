using System;
using UnityEngine;

namespace ValheimVehicles.Components;

public class MastComponent : MonoBehaviour
{
  public GameObject? m_sailObject;

  public Cloth? m_sailCloth;

  public bool m_allowSailRotation = false;
  public Transform? m_rotationTransform = null;

  public bool m_allowSailShrinking = true;

  public bool m_disableCloth;


  // for custom masts. Other masts do not support this. We may need to add a selector to make this cleaner.
  public void Awake()
  {
    m_rotationTransform = transform.Find("rotational_yard");
  }
}