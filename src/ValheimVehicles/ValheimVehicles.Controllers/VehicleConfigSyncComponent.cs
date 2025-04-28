using System;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Controllers;

public class VehicleConfigSyncComponent : MonoBehaviour
{
  private VehicleShip _vehicle;
  private ZNetView? m_nview => _vehicle.NetView;
  private VehiclePiecesController? PiecesController => _vehicle.PiecesController;
  private VehicleOnboardController? OnboardController => _vehicle.OnboardController;
  private BoxCollider? FloatCollider => _vehicle.FloatCollider;
  private bool _hasRegister;
  public void Awake()
  {
    _vehicle = GetComponent<VehicleShip>();
  }

  private void OnEnable()
  {
    RegisterRPCListeners();
  }

  private void OnDisable()
  {
    UnregisterRPCListeners();
  }

  private const int MAX_DELAYS = 50;
  private int rpcRegisterDelayCount = 0;

  private void RegisterRPCListeners()
  {
    // retry guards. Sometimes things are not available on Awake().
    if (m_nview == null)
    {
      if (rpcRegisterDelayCount > MAX_DELAYS) return;
      Invoke(nameof(RegisterRPCListeners), 1f);
      rpcRegisterDelayCount++;
      return;
    }
    rpcRegisterDelayCount = 0;
    if (_hasRegister) return;
    // ship piece bounds syncing
    m_nview.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

    // ship water floatation syncing
    m_nview.Register(nameof(RPC_SyncWaterFloatationHeight), RPC_SyncWaterFloatationHeight);

    _hasRegister = true;
  }

  private void UnregisterRPCListeners()
  {
    if (!_hasRegister || m_nview == null)
    {
      _hasRegister = false;
      return;
    }

    // ship piece bounds syncing
    m_nview.Unregister(nameof(RPC_SyncBounds));

    // ship water floatation height 
    m_nview.Unregister(nameof(RPC_SyncWaterFloatationHeight));

    _hasRegister = false;
  }

  public void SetWaterFloatHeight(float waterFloatHeight)
  {
    if (!isActiveAndEnabled || !m_nview || m_nview.GetZDO() == null) return;
    m_nview.GetZDO().Set(VehicleZdoVars.VehicleFloatationHeight, waterFloatHeight);
    m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncWaterFloatationHeight));
  }


  // floatation Sync

  public void RPC_SyncWaterFloatationHeight(long sender)
  {
    if (!PiecesController) return;
    SyncWaterFloatationHeight();
  }

  public void SyncWaterFloatationHeight()
  {
    if (!PiecesController || FloatCollider == null) return;
    var center = FloatCollider.center;
    center.y = GetWaterFloatationHeight(center.y);
    FloatCollider.center = center;
  }

  public float GetWaterFloatationHeight(float fallbackValue = 0)
  {
    if (m_nview == null || m_nview.GetZDO() == null) return fallbackValue;
    var floatationHeight = m_nview.GetZDO().GetFloat(VehicleZdoVars.VehicleFloatationHeight, fallbackValue);
    return floatationHeight;
  }

  // bounds sync

  /// <summary>
  /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
  ///
  /// Also should prevent desyncs if we can synchronize it.
  /// </summary>
  public void SendSyncBounds()
  {
    if (m_nview == null || OnboardController == null) return;

    OnboardController.m_localPlayers.ForEach(x =>
    {
      m_nview.InvokeRPC(x.GetPlayerID(), nameof(RPC_SyncBounds));
    });
  }

  /// <summary>
  ///   Forces a resync of bounds for all players on the ship, this may need to be
  ///   only from host but then would require syncing all collider data that updates
  ///   in the OnBoundsUpdate
  /// </summary>
  public void RPC_SyncBounds(long sender)
  {
    if (!PiecesController) return;
    SyncVehicleBounds();
  }

  public void SyncVehicleBounds()
  {
    if (PiecesController == null) return;
    PiecesController.RequestBoundsRebuild();
  }
}