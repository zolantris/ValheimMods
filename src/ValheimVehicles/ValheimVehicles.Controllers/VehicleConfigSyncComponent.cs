#region

  using System.Diagnostics.CodeAnalysis;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.Config;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.SharedScripts;

#endregion

  namespace ValheimVehicles.Controllers;

  public class VehicleConfigSyncComponent : PrefabConfigRPCSync<VehicleCustomConfig>, IVehicleSharedProperties
  {
    private VehicleManager _vehicle;
    private BoxCollider? FloatCollider => _vehicle.FloatCollider;
    private RetryGuard _rpcRegisterRetry;
    private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
    private float _cachedFloatationHeight = 0;

    public override void Awake()
    {
      base.Awake();
      _rpcRegisterRetry = new RetryGuard(this);
      _vehicle = GetComponent<VehicleManager>();
    }

    private void OnEnable()
    {
      RegisterRPCListeners();
    }

    private void OnDisable()
    {
      UnregisterRPCListeners();
    }

  #region IRPCSync

    public override void RegisterRPCListeners()
    {
      if (!IsValid(out var netView))
      {
        _rpcRegisterRetry.Retry(RegisterRPCListeners, 1);
        return;
      }

      base.RegisterRPCListeners();
      // retry guards. Sometimes things are not available on Awake().
      if (hasRegisteredRPCListeners) return;
      // ship piece bounds syncing
      netView.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

      // all config sync
      CustomConfig.Load(netView.GetZDO());

      hasRegisteredRPCListeners = true;
    }

    public override void UnregisterRPCListeners()
    {
      if (!hasRegisteredRPCListeners) return;
      if (!IsValid(out var netView))
      {
        hasRegisteredRPCListeners = false;
        return;
      }

      base.UnregisterRPCListeners();

      // ship piece bounds syncing
      netView.Unregister(nameof(RPC_SyncBounds));

      hasRegisteredRPCListeners = false;
    }

  #endregion

    public VehicleFloatationMode GetWaterFloatationHeightMode()
    {
      if (CustomConfig.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
      return PhysicsConfig.HullFloatationColliderLocation.Value;
    }

    public void SendSyncFloatationMode(bool isCustom, float relativeHeight)
    {
      // do nothing for land-vehicles.
      if (_vehicle.IsLandVehicle) return;

      // set config immediately, but it might not be updated if the ranges are out of bounds
      CustomConfig.HasCustomFloatationHeight = isCustom;
      CustomConfig.CustomFloatationHeight = relativeHeight;

      SendPrefabConfig();
    }

    // bounds sync

    /// <summary>
    /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
    ///
    /// Also should prevent desyncs if we can synchronize it.
    /// </summary>
    public void SendSyncBounds()
    {
      if (!IsValid(out var netView) || OnboardController == null) return;

      OnboardController.m_localPlayers.ForEach(x =>
      {
        netView.InvokeRPC(x.GetPlayerID(), nameof(RPC_SyncBounds));
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

  #region IVehicleSharedProperties

    public VehiclePiecesController? PiecesController
    {
      get;
      set;
    }
    public VehicleMovementController? MovementController
    {
      get;
      set;
    }
    public VehicleConfigSyncComponent? VehicleConfigSync
    {
      get;
      set;
    }
    public VehicleOnboardController? OnboardController
    {
      get;
      set;
    }
    public VehicleWheelController? WheelController
    {
      get;
      set;
    }
    public VehicleManager? Manager
    {
      get;
      set;
    }

  #endregion

  }