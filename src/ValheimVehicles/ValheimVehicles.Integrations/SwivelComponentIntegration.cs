#region

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.Config;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.UI;
  using ValheimVehicles.Structs;

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
  public sealed class SwivelComponentIntegration : SwivelComponent, IPieceActivatorHost, IPieceController, IRaycastPieceActivator, INetView, IPrefabConfig<SwivelCustomConfig>
  {
    public VehiclePiecesController? m_vehiclePiecesController;
    public VehicleManager? m_vehicle => m_vehiclePiecesController == null ? null : m_vehiclePiecesController.Manager;

    private int _persistentZdoId;
    public static readonly Dictionary<int, SwivelComponentIntegration> ActiveInstances = [];
    public List<ZNetView> m_pieces = [];
    public List<ZNetView> m_tempPieces = [];

    private SwivelPieceActivator _pieceActivator = null!;
    public static float turnTime = 50f;

    private HoverFadeText m_hoverFadeText;
    public SwivelConfigRPCSync prefabConfigSync;
    public SwivelCustomConfig Config => prefabConfigSync.Config;
    public ChildZSyncTransform childZsyncTransform;
    public readonly SwivelMotionStateTracker motionTracker = new();

    public static bool CanAllClientsSync = true;

    private int swivelId = 0;

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
        prefabConfigSync = gameObject.AddComponent<SwivelConfigRPCSync>();
      }

      CanUpdate = false;

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
      // Call base first to update state
      // base.SetMotionState(state);
      if (!this.IsNetViewValid(out var netView))
      {
        return;
      }
      var currentState = MotionState;

      if (state == currentState)
      {
        LoggerProvider.LogDebug("Bailing. Infinite loop detected in SetMotionState");
        return;
      }

      if (!netView.IsOwner())
      {
        Request_SetMotionState(state);
      }
      else
      {
        prefabConfigSync.Config.MotionState = state;
        if (state != currentState)
        {
          base.SetMotionState(state);
          Request_SetMotionState(state);
        }
      }

      // Only record transitions that initiate movement
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


    public MotionState GetNextMotionState()
    {
      var previousState = MotionState;
      prefabConfigSync.Load();
      LoggerProvider.LogDebug("MotionState -> Previous: " + previousState.ToString() + " | Current: " + MotionState.ToString() + " |");
      switch (MotionState)
      {
        case MotionState.ToTarget:
        case MotionState.AtTarget:
          return MotionState.ToStart;
        case MotionState.ToStart:
        case MotionState.AtStart:
          return MotionState.ToTarget;
        default:
          LoggerProvider.LogError("Somehow got unhandled motionState force setting user to ToStart");
          return MotionState.ToStart;
      }
    }

    public override void Request_NextMotionState()
    {
      prefabConfigSync.Request_NextMotion();
    }

    public void Request_SetMotionState(MotionState state)
    {
      prefabConfigSync.Request_SetMotionState(state);
    }

    public void SetupHoverFadeText()
    {
      m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);
      m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;
      m_hoverFadeText.Hide();
    }

    public void OnEnable()
    {
      onMovementReturned += OnMovementStateUpdate;

      var persistentId = GetPersistentId();
      if (persistentId == 0) return;

      if (ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
      {
        return;
      }
      ActiveInstances.Add(persistentId, this);
    }

    /// <summary>
    /// Should not call for non-owners
    /// </summary>
    private void OnMovementStateUpdate()
    {
      LoggerProvider.LogInfo("called OnMovementStateUpdate");
      // prefabConfigSync.Request_NextMotion();
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
      onMovementReturned -= OnMovementStateUpdate;

      var persistentId = GetPersistentId();
      if (persistentId == 0) return;
      if (!ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
      {
        return;
      }
      ActiveInstances.Remove(persistentId);
    }

    public override void FixedUpdate()
    {
      if (!CanAllClientsSync)
      {
        if (!this.IsNetViewValid(out var netView))
        {
          return;
        }
        // non-owners do not update. All logic is done via RPC Child sync.
        if (!netView.IsOwner())
        {
          return;
        }
      }
      else
      {
        base.FixedUpdate();
      }

      if (m_pieces.Count > 0)
      {
        m_hoverFadeText.FixedUpdate_UpdateText();
      }
    }

#if DEBUG
    public void AddNearestPiece()
    {
      var hits = Physics.SphereCastAll(transform.position, 30f, Vector3.up, 30f, LayerHelpers.PhysicalLayers);
      if (hits == null || hits.Length == 0) return;
      var listHits = hits.ToList().Select(x => x.transform.transform.GetComponentInParent<Piece>()).Where(x => x != null && x.transform.root != transform.root).ToList();
      listHits.Sort((x, y) =>
        Vector3.Distance(transform.position, x.transform.position)
          .CompareTo(Vector3.Distance(transform.position, y.transform.position)));

      var firstHit = listHits.First();
      TryAddPieceToSwivelContainer(GetPersistentId(), firstHit.transform.GetComponentInParent<ZNetView>());
    }
#endif

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
    }

    // public void ActivatePiece(ZNetView netView)
    // {
    //   if (netView == null) return;
    //   var zdo = netView.GetZDO();
    //   if (netView.m_zdo == null) return;
    //
    //   AddPieceToParent(netView.transform);
    //
    //   // This should work just like finalize transform...so not needed technically. Need to see where the break in the logic is.
    //   netView.transform.localPosition =
    //     netView.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    //   netView.transform.localRotation =
    //     Quaternion.Euler(netView.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash,
    //       Vector3.zero));
    //
    //   var wnt = netView.GetComponent<WearNTear>();
    //   if ((bool)wnt) wnt.enabled = true;
    //
    //   AddPiece(netView);
    // }

    public void OnActivationComplete()
    {
      // SetInitialLocalRotation();
      // prefabConfigSync.SyncPrefabConfig();
      CanUpdate = true;
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

    public void AddNewPiece(ZNetView netView)
    {

      // do not add a swivel within a swivel. This could cause some really weird behaviors so it's not supported.
      if (netView.name.StartsWith(PrefabNames.SwivelPrefabName)) return;
      if (netView == null || netView.GetZDO() == null) return;
      var persistentId = GetPersistentId();
      if (persistentId == 0) return;
      var zdo = netView.GetZDO();
      zdo.Set(VehicleZdoVars.SwivelParentId, persistentId);

      m_hoverFadeText.Show();
      m_hoverFadeText.transform.position = transform.position + Vector3.up;
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
      var powerConsumerIntegration = gameObject.AddComponent<PowerConsumerComponentIntegration>();
      swivelPowerConsumer = powerConsumerIntegration.Logic;
      UpdatePowerConsumer();
      UpdateBasePowerConsumption();
    }

    public void OnInitComplete()
    {
      StartActivatePendingSwivelPieces();
    }

    public override Quaternion CalculateTargetWindDirectionRotation()
    {
      if (!m_vehicle || !m_vehicle.MovementController)
      {
        var windDir = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : transform.forward;

        // these calcs are probably all wrong.
        // var dir = Utils.YawFromDirection(transform.InverseTransformDirection(windDir));
        var dir = Quaternion.LookRotation(
          -Vector3.Lerp(windDir,
            Vector3.Normalize(windDir - piecesContainer.forward), turnTime),
          piecesContainer.up);

        return Quaternion.RotateTowards(piecesContainer.transform.rotation, dir, 30f * Time.fixedDeltaTime);
      }
      // use the sync mast
      return m_vehicle.MovementController!.m_mastObject.transform.localRotation;
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

    public bool CanRaycastHitPiece()
    {
      return GetPieceCount() > 0;
    }

    public static bool CanDestroySwivel(ZNetView? netView)
    {
      try
      {
        if (netView == null) return false;
        var swivelController = netView.GetComponent<SwivelComponentIntegration>();
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