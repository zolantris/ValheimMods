#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.IO;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.RPC;
  using ValheimVehicles.Serialization;
  using ValheimVehicles.SharedScripts;
  using Zolantris.Shared;

#endregion

  namespace ValheimVehicles.Controllers;

  public class VehicleConfigSyncComponent : PrefabConfigSync<VehicleCustomConfig, IVehicleConfig>, IVehicleSharedProperties
  {
    private VehicleManager? _vehicle;
    private BoxCollider? FloatCollider => _vehicle != null ? _vehicle.FloatCollider : null;
    private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
    private float _cachedFloatationHeight = 0;

    public Action? OnLoadSubscriptions;

    public override void Awake()
    {
      _vehicle = GetComponent<VehicleManager>();
      base.Awake();
    }

    public override void OnLoad()
    {
      OnLoadSubscriptions?.Invoke();
    }

  #region IRPCSync

    /// <summary>
    /// This adds a retryGuard.
    /// </summary>
    public override void RegisterRPCListeners()
    {
      if (retryGuard == null) return;
      if (hasRegisteredRPCListeners) return;
      if (!this.IsNetViewValid(out var netView))
      {
        retryGuard.Retry(RegisterRPCListeners, 1);
        return;
      }

      base.RegisterRPCListeners();

      rpcHandler?.Register<ZPackage>(nameof(RPC_SyncBounds), RPC_SyncBounds);
      rpcHandler?.Register<ZPackage>(nameof(RPC_SyncFloatationMode), RPC_SyncFloatationMode);
      rpcHandler?.Register<ZPackage>(nameof(RPC_SyncBuildMode), RPC_SyncBuildMode);

      hasRegisteredRPCListeners = true;
    }

  #endregion

    public VehicleFloatationMode GetWaterFloatationHeightMode()
    {
      if (Config.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
      return PhysicsConfig.HullFloatationColliderLocation.Value;
    }

    public void Request_SyncFloatationMode(bool isCustom, float relativeHeight)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var pkg = new ZPackage();
      pkg.Write(isCustom);
      pkg.Write(relativeHeight);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SyncFloatationMode), pkg);
    }

    private void RPC_SyncFloatationMode(long sender, ZPackage pkg)
    {
      if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

      var isCustom = pkg.ReadBool();
      var relativeHeight = pkg.ReadSingle();

      var updated = new VehicleCustomConfig();
      updated.ApplyFrom(Config);

      updated.HasCustomFloatationHeight = isCustom;
      updated.CustomFloatationHeight = relativeHeight;

      CommitConfigChange(updated); // saves + broadcasts
    }

    public void Request_SyncBuildMode(VehicleBuildMode mode)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var pkg = new ZPackage();
      pkg.Write((int)mode);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SyncBuildMode), pkg);
    }

    private void RPC_SyncBuildMode(long sender, ZPackage pkg)
    {
      if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

      var buildMode = (VehicleBuildMode)pkg.ReadInt();

      var updated = new VehicleCustomConfig();
      updated.ApplyFrom(Config);

      updated.BuildMode = buildMode;

      CommitConfigChange(updated); // saves + broadcasts
    }

    /// <summary>
    /// bounds sync for all players on the boat.
    /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
    ///
    /// Also should prevent desyncs if we can synchronize it.
    /// </summary>
    public void SendSyncBounds(int hullRevision, byte[] payload)
    {
      if (!this.IsNetViewValid(out _)) return;
      if (!this || OnboardController == null) return;
      if (m_zdo == null || !m_zdo.IsValid()) return;

      var localPlayer = Player.m_localPlayer;
      if (localPlayer == null) return;


      // ensures that the latest data is sent to other clients.
      var package = new ZPackage();
      package.Write(hullRevision);
      package.Write(payload);

      // sends the update only to peers nearby. Other peers will not need the update unless in the area.
      RPCUtils.RunIfNearby(m_zdo, 250f, (peerId) =>
      {
        rpcHandler?.InvokeRPC(peerId, nameof(RPC_SyncBounds), package);
      });

      // Recalculate once on this machine.
      SyncVehicleBounds();
    }

    /// <summary>
    ///   Forces a resync of bounds for all players on the ship, this may need to be
    ///   only from host but then would require syncing all collider data that updates
    ///   in the OnBoundsUpdate
    /// </summary>
    public void RPC_SyncBounds(long sender, ZPackage pkg)
    {
      if (!PiecesController) return;
      pkg.SetPos(0);

      var hashId = pkg.ReadInt();
      var binaryStream = pkg.GetArray();

      try
      {
        var data = ConvexHullDataSerializer.Deserialize(binaryStream);
        if (data.Count == 0)
        {
          SyncVehicleBounds();
        }
        else
        {
          PiecesController.RebuildHullFromData(hashId, data);
        }
      }
      catch (InvalidDataException ex)
      {
        LoggerProvider.LogError(
          $"Failed to restore convex hull geometry: {ex.Message}");
      }
    }

    public void SyncVehicleBounds()
    {
      if (PiecesController == null) return;
      if (PiecesController.m_nview == null) return;

      // owner should not resync to avoid a loop
      if (PiecesController.m_nview.IsOwner()) return;

      // only update chunkbounds data. Rebuild will be requested after all updates are done.
      PiecesController.UpdateChunkBoundsData(false);
      PiecesController.RequestBoundsRebuild();
    }

  #region IVehicleSharedProperties

    public VehiclePiecesController? PiecesController
    {
      get;
      set;
    } = null!;
    public VehicleMovementController? MovementController
    {
      get;
      set;
    } = null!;
    public VehicleConfigSyncComponent? VehicleConfigSync
    {
      get;
      set;
    } = null!;
    public VehicleOnboardController? OnboardController
    {
      get;
      set;
    } = null!;
    public VehicleLandMovementController? LandMovementController
    {
      get;
      set;
    } = null!;
    public VehicleManager Manager
    {
      get;
      set;
    } = null!;

    public bool IsControllerValid => Manager.IsControllerValid;

    public bool IsInitialized => Manager.IsInitialized;

    public bool IsDestroying => Manager.IsDestroying;

  #endregion

  }