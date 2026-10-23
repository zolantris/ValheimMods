using System;
using UnityEngine;
namespace ValheimVehicles.ValheimVehicles.Components;

public class SailDestroyer : MonoBehaviour
{
  private ZNetView m_nview;
  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }
  public void Start()
  {
    if (m_nview == null || !m_nview.IsValid()) return;

    Destroy(this);
  }

  public void
    FindAndDestroySailsInArea()
  {

  }
}