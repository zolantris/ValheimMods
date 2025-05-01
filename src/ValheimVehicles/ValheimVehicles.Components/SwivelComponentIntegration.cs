#region

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.UI;
  using ValheimVehicles.Structs;

#endregion

namespace ValheimVehicles.Components;

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
public sealed class SwivelComponentIntegration : SwivelComponent, IPieceActivatorHost, IPieceController, IRaycastPieceActivator, Hoverable, Interactable
{
  public VehiclePiecesController? m_piecesController;
  public VehicleBaseController? m_vehicle => m_piecesController == null ? null : m_piecesController.BaseController;

  private ZNetView m_nview;
  private int _persistentZdoId;
  public static readonly Dictionary<int, SwivelComponentIntegration> ActiveInstances = [];
  public List<ZNetView> m_nviewPieces = [];
  public List<ZNetView> m_tempPieces = [];

  private SwivelPieceActivator _pieceActivator = null!;

  public override void Awake()
  {
    base.Awake();
    m_nview = GetComponent<ZNetView>();

    // TODO share activation logic with another component so we can reuse things.
    _pieceActivator = gameObject.AddComponent<SwivelPieceActivator>();
    _pieceActivator.Init(this);
    _pieceActivator.StartInitPersistentId();
  }

  public void OnEnable()
  {
    var persistentId = GetPersistentId();
    if (persistentId == 0) return;

    if (ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
    {
      return;
    }
    ActiveInstances.Add(persistentId, this);
  }

  public void OnDisable()
  {
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
    base.FixedUpdate();
  }

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

  public static bool TryAddPieceToSwivelContainer(int persistentId, ZNetView netViewPrefab)
  {
    if (!ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
    {
      LoggerProvider.LogDev("No instance of SwivelComponentIntegration found for persistentId: " + persistentId + "This could mean the swivel is not yet loaded or the associated items did not get removed when the Swivel was destroyed.");
      return false;
    }

    netViewPrefab.transform.SetParent(swivelComponentIntegration.pieceContainer);
    return true;
  }

  public void StartActivatePendingSwivelPieces()
  {
    _pieceActivator.StartActivatePendingPieces();
  }

  public void Register() {}

  public void OnDestroy()
  {
    StopAllCoroutines();
    Cleanup();
  }

  public void Cleanup()
  {
    if (!isActiveAndEnabled) return;
    if (ZNetScene.instance == null || Game.instance == null) return;
    foreach (var nvPiece in m_nviewPieces)
    {
      if (nvPiece == null || nvPiece.GetZDO() == null) continue;
      nvPiece.GetZDO().RemoveInt(VehicleZdoVars.SwivelParentId);
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

  public void Start()
  {
    m_piecesController = GetComponentInParent<VehiclePiecesController>();
  }
  

  protected override Quaternion CalculateTargetWindDirectionRotation()
  {
    if (m_vehicle == null || m_vehicle.MovementController == null) return base.CalculateTargetWindDirectionRotation();
    // use the sync mast
    return m_vehicle.MovementController.m_mastObject.transform.localRotation;
  }

#region IBasePieceActivator

  public int GetPersistentId()
  {
    return PersistentIdHelper.GetPersistentIdFrom(m_nview, ref _persistentZdoId);
  }

  public ZNetView? GetNetView()
  {
    return m_nview;
  }
  public Transform GetPieceContainer()
  {
    return pieceContainer;
  }

#endregion


  public string GetHoverText()
  {
    return "Hover text";
  }
  public string GetHoverName()
  {
    return "Hover name";
  }
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (SwivelUIPanelComponent.Instance == null) Game.instance.gameObject.AddComponent<SwivelUIPanelComponent>();
    if (SwivelUIPanelComponent.Instance != null)
    {
      if (SwivelUIPanelComponent.Instance.gameObject.activeInHierarchy)
      {
        SwivelUIPanelComponent.Instance.Hide();
      }
      else
      {
        SwivelUIPanelComponent.Instance.BindTo(this);
      }
      return true;
    }
    return false;
  }
  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

#region IPieceController

  public void AddPiece(ZNetView nv, bool isNew = false)
  {
    if (nv == null) return;
    nv.transform.SetParent(pieceContainer);
  }
  public void RemovePiece(ZNetView nv)
  {
    throw new NotImplementedException();
  }

#endregion

}