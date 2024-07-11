using System;
using UnityEngine;

namespace ValheimVehicles.Vehicles.Components;

public class VehicleMeshMaskCreator : MonoBehaviour
{
  private ZNetView m_nview;
  private VehicleMeshMaskManager? _meshMaskManager;

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    _meshMaskManager = GetComponentInParent<VehicleMeshMaskManager>();
  }

  private void Start()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!_meshMaskManager)
    {
      _meshMaskManager = GetComponentInParent<VehicleMeshMaskManager>();
    }

    _meshMaskManager?.AddCoordinateItem(gameObject);
  }
}