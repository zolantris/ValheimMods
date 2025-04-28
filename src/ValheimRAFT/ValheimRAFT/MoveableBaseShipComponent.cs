using System;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class MoveableBaseShipComponent : MonoBehaviour
{
  [Flags]
  public enum MBFlags
  {
    None = 0,
    IsAnchored = 1,
    HideMesh = 2
  }

  public VehicleDebugHelpers VehicleDebugHelpersInstance;

  public MoveableBaseRootComponent m_baseRoot;

  public bool isCreative = false;

  internal Rigidbody m_rigidbody;

  internal Ship m_ship;

  internal ShipStats m_shipStats = new();

  internal ZNetView m_nview;

  internal GameObject m_baseRootObject;

  internal ZSyncTransform m_zsync;

  public float m_targetHeight;

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  public MBFlags m_flags;
  public bool IsAnchored => m_flags.HasFlag(MBFlags.IsAnchored);

  public MoveableBaseRootComponent GetMbRoot()
  {
    return m_baseRoot;
  }

  public void Awake() {}
  public void UpdateVisual() {}

  public void OnDestroy() {}

  public ShipStats GetShipStats()
  {
    return m_shipStats;
  }

  /**
   * this creates the Raft 2x3 area
   */
  private void FirstTimeCreation() {}

  public void Ascend() {}

  public void Descent() {}


  public void UpdateStats(bool flight) {}

  public void SetAnchor(bool state) {}

  public void RPC_SetAnchor(long sender, bool state) {}

  internal void SetVisual(bool state) {}

  public void RPC_SetVisual(long sender, bool state) {}
}