using System;
using UnityEngine;
namespace ValheimVehicles.Helpers;

public class DelayedSelfDeletingComponent : MonoBehaviour
{
  public float delayTimeInSeconds = 2f;
  private ZNetView m_nview;
  private void Start()
  {
    m_nview = GetComponent<ZNetView>();
    if (!m_nview.IsValid())
    {
      return;
    }

    if (m_nview.m_ghost) return;

    Invoke(nameof(DestroySelf), delayTimeInSeconds);
  }

  private void DestroySelf()
  {
    if (ZNetScene.instance == null)
    {
      Destroy(gameObject);
      return;
    }

    ZNetScene.instance.Destroy(gameObject);
  }
}