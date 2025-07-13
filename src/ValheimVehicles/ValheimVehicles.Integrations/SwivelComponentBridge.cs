#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.RPC;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Helpers;
  using ValheimVehicles.SharedScripts.PowerSystem.Compute;

#endregion

  namespace ValheimVehicles.Integrations;

  /// <summary>
  /// Integration component for SwivelComponent which allow it to work in Valheim.
  /// - Handles Data Syncing.
  /// - Handles config menu opening
  /// - [TODO] Handles lever system GUI. Allowing connections to a lever so a Swivel can be triggered remotely.
  /// - [TODO] Add a wire prefab which is a simple Tag prefab that allows connecting a Swivel to a lever to be legit.
  /// </summary>
  /// <logic>
  /// - OnDestroy the Swivel must remove all references of itself. Alternatively, we could remove unfound swivels.
  /// - Swivels are components that must have a persistentID
  /// - Swivels can function outside a vehicle.
  /// - Swivels can function inside the hierarchy of a vehicle. This requires setting the children of swivels and escaping out of any logic that sets the parent to the VehiclePiecesController container.
  /// </logic>
  ///
  /// Notes
  /// IRaycastPieceActivator is used for simplicity. It will easily match any component extending this in unity.
  public sealed class SwivelComponentBridge : SwivelComponent, IPieceActivatorHost, IPieceController, IRaycastPieceActivator, INetView, IPrefabConfig<SwivelCustomConfig>
  {
    public VehiclePiecesController? m_vehiclePiecesController;
    public VehicleManager? m_vehicle => m_vehiclePiecesController == null ? null : m_vehiclePiecesController.Manager;

    private int _persistentZdoId;
    public static readonly Dictionary<int, SwivelComponentBridge> ActiveInstances = [];
    public List<ZNetView> m_pieces = [];
    public List<ZNetView> m_tempPieces = [];

    private SwivelPieceActivator _pieceActivator = null!;
    public static float turnTime = 2f;

    private HoverFadeText m_hoverFadeText;
    public SwivelConfigSync prefabConfigSync;
    public SwivelCustomConfig Config => this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync).Config;
    public ChildZSyncTransform childZsyncTransform;
    public readonly SwivelMotionStateTracker motionTracker = new();

    private bool _hasInitPrefabSync = false;
    private int swivelId = 0;
    private PowerConsumerBridge powerConsumerIntegration;
    public static Dictionary<ZDO, SwivelComponentBridge> ZdoToComponent = new();
    public ZDO? _currentZdo;

    public override MotionState MotionState
    {
      get
      {
        currentMotionState = Config.MotionState;
        return Config.MotionState;
      }
      set
      {
        Config.MotionState = value;
        currentMotionState = value;
        SetMotionState(value);
      }
    }

    public override int SwivelPersistentId
    {
      get
      {
        if (swivelId == 0)
        {
          swivelId = GetPersistentId();
        }
        return swivelId;
      }
    }

    public override void Awake()
    {
      base.Awake();

      if (!prefabConfigSync)
      {
        this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync);
      }

      _IsReady = false;

      m_nview = GetComponent<ZNetView>();
      // required for syncing the animated component across clients.
      // childZsyncTransform = animatedTransform.gameObject.AddComponent<ChildZSyncTransform>();
      // childZsyncTransform.m_syncPosition = true;
      // childZsyncTransform.m_syncRotation = true;
      // childZsyncTransform.m_syncBodyVelocity = true;

      SetupHoverFadeText();

      SetupPieceActivator();
    }

    public override void SetMotionState(MotionState state)
    {
      if (!prefabConfigSync) return;
      if (prefabConfigSync.IsBroadcastSuppressed)
      {
        base.SetMotionState(state);
        return;
      }

      if (!this.IsNetViewValid(out var netView))
        return;

      var currentState = MotionState;
      if (state == currentState)
      {
        LoggerProvider.LogDebug("[Swivel] SetMotionState called with same state. Ignoring.");
        return;
      }

      if (!netView.IsOwner())
      {
        // this is likely unstable
        if (ShouldSyncClientOnlyUpdate)
        {
          prefabConfigSync.Load();
          base.SetMotionState(prefabConfigSync.Config.MotionState);
        }
        return;
      }

      base.SetMotionState(state);

      if (state is MotionState.ToTarget or MotionState.ToStart)
      {
        motionTracker.UpdateMotion(
          animatedTransform.localPosition,
          GetCurrentTargetPosition(),
          animatedTransform.localRotation,
          GetCurrentTargetRotation(),
          computedInterpolationSpeed
        );
      }
    }

    public override void Request_NextMotionState()
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (prefabConfigSync.Config.MotionState != MotionState)
      {
        LoggerProvider.LogWarning("SwivelComponentBridge.Request_NextMotionState called but the motion state is not the same as the current state. This is likely a bug.");
      }
      prefabConfigSync.Load([SwivelCustomConfig.Key_MotionState]);
      SwivelPrefabConfigRPC.Request_NextMotion(netView.GetZDO(), MotionState);
    }

    public Coroutine? _guardedMotionCheck;

    public void Request_SetMotionState(MotionState state)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      SwivelPrefabConfigRPC.Request_SetMotionState(netView.GetZDO(), state);
    }

    public void SetupHoverFadeText()
    {
      m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);
      m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;
      m_hoverFadeText.Hide();
    }

    public void OnEnable()
    {
      this.WaitForZNetView((nv) =>
      {
        _currentZdo = nv.GetZDO();
        var persistentId = GetPersistentId();
        ZdoToComponent.Add(_currentZdo, this);

        if (persistentId != 0 && !ActiveInstances.ContainsKey(persistentId))
        {
          ActiveInstances.Add(persistentId, this);
        }
      });

      if (_guardedMotionCheck != null)
      {
        StopCoroutine(_guardedMotionCheck);
      }
      _guardedMotionCheck = StartCoroutine(GuardedMotionCheck());
    }

    public void SetupPieceActivator()
    {
      _pieceActivator = gameObject.AddComponent<SwivelPieceActivator>();
      _pieceActivator.Init(this);
      _pieceActivator.OnActivationComplete = OnActivationComplete;
      _pieceActivator.OnInitComplete = OnInitComplete;
    }

    public void OnDisable()
    {
      if (_currentZdo != null)
      {
        ZdoToComponent.Remove(_currentZdo);
      }

      var persistentId = GetPersistentId();
      if (persistentId != 0 && ActiveInstances.TryGetValue(persistentId, out _))
      {
        ActiveInstances.Remove(persistentId);
      }

      if (_guardedMotionCheck != null)
      {
        StopCoroutine(_guardedMotionCheck);
        _guardedMotionCheck = null;
      }
    }

    public override void Update()
    {
      if (!CanRunBaseUpdate()) return;
      base.Update();
    }

    public IEnumerator GuardedMotionCheck()
    {
      while (isActiveAndEnabled)
      {
        yield return new WaitForFixedUpdate();
        if (!powerConsumerIntegration)
        {
          yield return null;
          continue;
        }

        // if we are mid motion. Wait for the motion to complete.
        while (MotionState is MotionState.ToStart or MotionState.ToTarget)
        {
          yield return new WaitForFixedUpdate();
        }

        // wait for next fixed update so motion values can be set.
        yield return new WaitForFixedUpdate();

        // guarded update on DemandState which can desync.
        if (MotionState is MotionState.AtStart or MotionState.AtTarget && powerConsumerIntegration && powerConsumerIntegration.IsDemanding && powerConsumerIntegration.Data.zdo != null && powerConsumerIntegration.Data.zdo.IsValid())
        {
          powerConsumerIntegration.Data.SetDemandState(false);
          PowerSystemRPC.Request_UpdatePowerConsumer(powerConsumerIntegration.Data.zdo.m_uid, powerConsumerIntegration.Data);
        }

        // todo see if we need this.
        // GuardSwivelValues();

        yield return new WaitForSeconds(30f);
      }
    }

    public bool CanRunBaseUpdate()
    {
      if (!CanUpdate()) return false;
      // no server only running of this update.
      if (!ZNet.instance || ZNet.instance.IsDedicated()) return false;

      if (!CanAllClientsSync)
      {
        if (!this.IsNetViewValid(out var netView))
        {
          return false;
        }
        // non-owners do not update. All logic is done via RPC Child sync.
        if (!netView.IsOwner())
        {
          return false;
        }
      }

      return true;
    }


    public static bool TryAddPieceToSwivelContainer(int persistentId, ZNetView netViewPrefab)
    {
      if (!ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
      {
        LoggerProvider.LogDev("No instance of SwivelComponentIntegration found for persistentId: " + persistentId + "This could mean the swivel is not yet loaded or the associated items did not get removed when the Swivel was destroyed.");
        return false;
      }

      return true;
    }

    public void AddPieceToParent(Transform pieceTransform)
    {
      pieceTransform.SetParent(piecesContainer);
    }

    public void StartActivatePendingSwivelPieces()
    {
      if (!_pieceActivator) return;
      _pieceActivator.StartActivatePendingPieces();
      GuardSwivelValues();
    }

    public void OnActivationComplete()
    {
      _IsReady = true;
    }

    public void Register() {}

    protected override void OnDestroy()
    {
      base.OnDestroy();
      StopAllCoroutines();
      Cleanup();
    }

    public void Cleanup()
    {
      if (!isActiveAndEnabled) return;
      if (ZNetScene.instance == null || Game.instance == null) return;
      foreach (var nvPiece in m_pieces)
      {
        if (nvPiece == null || nvPiece.GetZDO() == null) continue;
        nvPiece.transform.SetParent(null);
      }
    }


    /// <summary>
    /// Returns true if the item is part of a SwivelContainer even if it does not parent the item to the swivel container if it does not exist yet. 
    /// </summary>
    /// <param name="netView"></param>
    /// <param name="zdo"></param>
    /// <returns></returns>
    public static bool TryAddPieceToSwivelContainer(ZNetView netView, ZDO zdo)
    {
      if (!TryGetSwivelParentId(zdo, out var swivelParentId))
      {
        return false;
      }
      TryAddPieceToSwivelContainer(swivelParentId, netView);
      return true;
    }

    public bool TryBailOnSameObject(GameObject obj)
    {
      if (obj == gameObject) return true;
      return false;
    }

    public void AddNewPiece(GameObject obj)
    {
      if (TryBailOnSameObject(obj)) return;
      var nv = obj.GetComponent<ZNetView>();
      if (nv == null) return;
      AddNewPiece(nv);
    }

    // This will also add the piece to the vehicleParent if there is one. 
    public void AddNewPiece(ZNetView netView)
    {
      if (netView == null || netView.GetZDO() == null) return;
      var persistentId = GetPersistentId();
      if (persistentId == 0) return;
      var zdo = netView.GetZDO();
      zdo.Set(VehicleZdoVars.SwivelParentId, persistentId);

      m_hoverFadeText.Show();
      m_hoverFadeText.transform.position = transform.position + Vector3.up * 2f;
      m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;

      // must call this otherwise everything is in world position. 
      AddPieceToParent(netView.transform);

      netView.m_zdo.Set(VehicleZdoVars.MBRotationVecHash,
        netView.transform.localRotation.eulerAngles);
      netView.m_zdo.Set(VehicleZdoVars.MBPositionHash,
        netView.transform.localPosition);

      AddPiece(netView);
    }

    public static bool TryGetSwivelParentId(ZDO? zdo, out int swivelParentId)
    {
      swivelParentId = 0;
      if (zdo == null) return false;
      swivelParentId = zdo.GetInt(VehicleZdoVars.SwivelParentId);
      return swivelParentId != 0;
    }

    public static bool IsSwivelParent(ZDO? zdo)
    {
      if (zdo == null) return false;
      return zdo.GetInt(VehicleZdoVars.SwivelParentId) != 0;
    }

    public override void Start()
    {
      base.Start();
      m_vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
      _pieceActivator.StartInitPersistentId();
      prefabConfigSync.Load();
    }

    public override void OnTransformParentChanged()
    {
      if (transform.root != transform)
      {
        m_vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
        if (m_vehiclePiecesController && m_vehicle != null && !m_vehicle.IsLandVehicle && m_vehicle.MovementController != null && m_vehicle.MovementController.m_mastObject != null)
        {
          windDirectionTransform = m_vehicle.MovementController.m_mastObject.transform;
        }
      }
      base.OnTransformParentChanged();
    }

    /// <summary>
    /// Inefficient no-cache way to update the debugger arrow. This is not meant to be called a bunch.
    /// </summary>
    private void AdjustDebuggerArrowLocation()
    {
      var colliders = piecesContainer.GetComponentsInChildren<Collider>();
      Bounds? bounds = null;
      foreach (var collider in colliders)
      {
        var colliderBounds = collider.bounds;
        bounds ??= new Bounds(colliderBounds.center, Vector3.zero);
        bounds.Value.Encapsulate(colliderBounds);
      }

      if (bounds.HasValue)
      {
        directionDebuggerArrow.position = bounds.Value.center;
      }
      else
      {
        directionDebuggerArrow.position = Player.m_localPlayer.m_body.ClosestPointOnBounds(Player.m_localPlayer.transform.up * 3f);
      }
    }

    public override void InitPowerConsumer()
    {
      powerConsumerIntegration = gameObject.AddComponent<PowerConsumerBridge>();
      swivelPowerConsumer = powerConsumerIntegration.Logic;
      UpdatePowerConsumer();
    }

    public override void UpdatePowerConsumer()
    {
      if (!swivelPowerConsumer) return;
      if (!ZNet.instance) return;
      if (!this.IsNetViewValid(out var netView)) return;
      if (ZNet.instance.IsServer() || ZNet.IsSinglePlayer)
      {
        base.UpdatePowerConsumer();
        UpdateBasePowerConsumption();
        PowerSystemRPC.Request_UpdatePowerConsumer(netView.GetZDO().m_uid, swivelPowerConsumer.Data);
      }
      else
      {
        swivelPowerConsumer.Data.Load();
      }
    }

    /// <summary>
    /// For interpolated eventing.
    /// </summary>
    public void SetAuthoritativeMotion(SwivelMotionUpdateData motionUpdateData, bool isAuthoritative)
    {
      prefabConfigSync.Load();

      _isAuthoritativeMotionActive = isAuthoritative;
      Config.MotionState = motionUpdateData.MotionState;
      _motionStartTime = motionUpdateData.StartTime;

      // use remaining duration as this includes the lerped duration.
      _motionDuration = motionUpdateData.RemainingDuration;

      // Always interpolate from the CURRENT transform state!
      _motionFromLocalPos = animatedTransform.localPosition;
      _motionFromLocalRot = animatedTransform.localRotation;

      if (mode == SwivelMode.Move)
        _motionToLocalPos = Config.MotionState is MotionState.ToTarget or MotionState.AtTarget
          ? startLocalPosition + movementOffset
          : startLocalPosition;
      else if (mode == SwivelMode.Rotate)
        _motionToLocalRot = Config.MotionState is MotionState.ToTarget or MotionState.AtTarget
          ? CalculateRotationTarget(1f)
          : CalculateRotationTarget(0f);

      // Reset completion guard
      _hasArrivedAtDestination = false;


      if (!isAuthoritative)
      {
        prefabConfigSync.Load();
      }
    }

    public override double GetSyncedTime()
    {
      var now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;
      return now;
    }

    public void OnInitComplete()
    {
      // load config late in case something is misaligned.
      prefabConfigSync.Load();

      StartActivatePendingSwivelPieces();
    }

    public override Quaternion CalculateTargetWindDirectionRotation()
    {
      if (!m_vehicle || !m_vehicle.MovementController)
      {
        var windDir = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : transform.forward;

        // these calcs are probably all wrong.
        // var dir = Utils.YawFromDirection(transform.InverseTransformDirection(windDir));
        var dir = QuaternionExtensions.LookRotationSafe(
          -Vector3.Lerp(windDir,
            Vector3.Normalize(windDir - piecesContainer.forward), turnTime),
          piecesContainer.up);

        return Quaternion.RotateTowards(piecesContainer.transform.rotation, dir, 30f * Time.fixedDeltaTime);
      }
      // Use maxRotationEuler to get the rotation limits
      // If the value is too small, clamp it to a minimum of 15 degrees
      var maxRotationLimit = Mathf.Max(maxRotationEuler.y, 15f);

      // Get the original rotation in Euler angles
      var startEulerAngles = startRotation.eulerAngles;

      // Normalize the angle to -180 to 180 range for easier calculations
      var startYAngle = startEulerAngles.y;
      if (startYAngle > 180f)
        startYAngle -= 360f;

      // Get the mast/wind rotation angle
      var mastRotationY = m_vehicle.MovementController!.m_mastObject.transform.localRotation.eulerAngles.y;
      if (mastRotationY > 180f)
        mastRotationY -= 360f;

      // Calculate the difference between mast rotation and start rotation
      var rotationDifference = Mathf.DeltaAngle(startYAngle, mastRotationY);

      // Calculate the interpolation factor (0 to 1) based on how far the wind has turned
      var interpolationFactor = Mathf.Clamp01(Mathf.Abs(rotationDifference) / maxRotationLimit);

      // Apply the sign of the rotation difference to determine direction
      var targetYAngle = startYAngle + (rotationDifference > 0 ? interpolationFactor * maxRotationLimit : -interpolationFactor * maxRotationLimit);

      // Create a target rotation quaternion using the interpolated angle
      // Preserve the original x and z rotation values
      var targetRotation = Quaternion.Euler(
        startEulerAngles.x,
        targetYAngle,
        startEulerAngles.z);

      // Smooth interpolation to prevent sudden snapping
      return Quaternion.Slerp(
        animatedTransform.localRotation,
        targetRotation,
        Time.fixedDeltaTime * computedInterpolationSpeed * 0.05f);


    }

  #region IBasePieceActivator

    public int GetPersistentId()
    {
      if (!this.IsNetViewValid(out var netView))
      {
        return 0;
      }
      return PersistentIdHelper.GetPersistentIdFrom(netView, ref _persistentZdoId);
    }

    public ZNetView? GetNetView()
    {
      return m_nview;
    }
    public Transform GetPieceContainer()
    {
      return piecesContainer;
    }

  #endregion

  #region IPieceController

    public bool CanDestroy()
    {
      return GetPieceCount() == 0;
    }

    private static string _ComponentName => PrefabNames.SwivelPrefabName;
    public string ComponentName => _ComponentName;

    public int GetPieceCount()
    {
      if (m_nview == null || m_nview.GetZDO() == null) return m_pieces.Count;
      var pieceCount = m_nview.GetZDO().GetInt(VehicleZdoVars.MBPieceCount, m_pieces.Count);
      return pieceCount;
    }

    // all swivels allow hitting regardless of piece count.
    public bool CanRaycastHitPiece()
    {
      return true;
    }

    public static bool CanDestroySwivel(ZNetView? netView)
    {
      try
      {
        if (netView == null) return false;
        var swivelController = netView.GetComponent<SwivelComponentBridge>();
        if (swivelController == null)
        {
          LoggerProvider.LogDebug("Bailing vehicle deletion attempt: Valheim Attempted to delete a vehicle that matched the ValheimVehicle prefab instance but there was no VehicleBaseController or VehiclePiecesController found. This could mean there is a mod breaking the vehicle registration of ValheimRAFT.");
          return false;
        }
        var hasPendingPieces =
          BasePieceActivatorComponent.m_pendingPieces.TryGetValue(swivelController.GetPersistentId(),
            out var pendingPieces);
        var hasPieces = swivelController.GetPieceCount() != 0;

        // if there are pending pieces, do not let vehicle be destroyed
        if (pendingPieces != null && hasPendingPieces && pendingPieces.Count > 0)
          return false;

        return !hasPieces;
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning("A error was thrown in CanDestroyVehicle. This likely means the vehicle is corrupt. This check will protect the vehicle from being deleted.");
        return false;
      }
    }

    public void AddPiece(ZNetView nv, bool isNew = false)
    {
      if (nv == null)
      {
        BasePieceActivatorComponent.AddPendingPiece(GetPersistentId(), nv);
        return;
      }
      AddPieceToParent(nv.transform);

      if (m_vehiclePiecesController != null)
      {
        m_vehiclePiecesController.AddPiece(nv);
      }

      PieceActivatorHelpers.FixPieceMeshes(nv);
      m_pieces.Add(nv);

      TogglePlacementContainer(m_pieces.Count == 0);
    }

    public void AddCustomPiece(ZNetView nv, bool isNew = false)
    {
      LoggerProvider.LogWarning("CustomPieces not supported for SwivelComponentIntegration. This is likely a bug. Please report this to the mod author.");
      return;
    }

    public void AddCustomPiece(GameObject prefab, bool isNew = false)
    {
      LoggerProvider.LogWarning("CustomPieces not supported for SwivelComponentIntegration. This is likely a bug. Please report this to the mod author.");
      return;
    }


    public void TogglePlacementContainer(bool isZeroPieces)
    {
      connectorContainer.gameObject.SetActive(isZeroPieces);
    }

    public void RemovePiece(ZNetView nv)
    {
      if (nv == null) return;
      m_pieces.Remove(nv);

      if (m_vehiclePiecesController != null)
      {
        m_vehiclePiecesController.RemovePiece(nv);
      }

      var currentPieceCount = GetPieceCount();
      var hasZeroPieces = currentPieceCount == 0;
      if (hasZeroPieces)
      {
        m_hoverFadeText.Hide();
      }
      TogglePlacementContainer(hasZeroPieces);
    }
    public void TrySetPieceToParent(ZNetView netView)
    {
      if (netView == null) return;
      AddPieceToParent(netView.transform);
    }

    /// <summary>
    /// Allows directly adding pieces but is by default guarded so only valheim vehicles prefixes are allowed. This is provided the piece has no valid netview otherwise TrySetPieceToParent can be used with a netview and Zdo.
    /// </summary>
    /// <param name="prefab"></param>
    public void TrySetPieceToParent(GameObject prefab, bool isForced = false)
    {
      if (prefab.name.Contains(PrefabNames.ValheimVehiclesPrefix) || isForced)
      {
        AddPieceToParent(prefab.transform);
      }
    }

    ///
    /// <summary>
    ///   prevents ship destruction on m_nview null
    ///   - if null it would prevent getting the ZDO information for the ship pieces
    /// </summary>
    /// 
    public void DestroyPiece(WearNTear wnt)
    {
      if (wnt == null) return;
      var nv = wnt.GetComponent<ZNetView>();
      RemovePiece(nv);
    }

  #endregion

    public ZNetView? m_nview
    {
      get;
      set;
    }
  }