#region

  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
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
  public List<ZNetView> m_pieces = [];
  public List<ZNetView> m_tempPieces = [];

  private SwivelPieceActivator _pieceActivator = null!;
  public static float turnTime = 50f;

  private HoverFadeText m_hoverFadeText;
  
  public override void Awake()
  {
    base.Awake();
    
    m_nview = GetComponent<ZNetView>();

    SetupHoverFadeText();
    
    SetupPieceActivator();
  }

  public void SetupHoverFadeText()
  {
    m_hoverFadeText = HoverFadeText.CreateHoverFadeText();
    m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;
    m_hoverFadeText.Hide();
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

  public void SetupPieceActivator()
  {
    _pieceActivator = gameObject.AddComponent<SwivelPieceActivator>();
    _pieceActivator.Init(this);
    _pieceActivator.OnActivationComplete = OnActivationComplete;
    _pieceActivator.OnInitComplete = OnInitComplete;
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
    m_hoverFadeText.FixedUpdate_UpdateText();
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

    return true;
  }

  public void AddPieceToParent(Transform pieceTransform)
  {
    pieceTransform.SetParent(pieceContainer);
  }

  public void StartActivatePendingSwivelPieces()
  {
    _pieceActivator.StartActivatePendingPieces();
  }

  public void ActivatePiece(ZNetView netView)
  {
    if (netView == null) return;
    var zdo = netView.GetZDO();
    if (netView.m_zdo == null) return;

    AddPieceToParent(netView.transform);

    // This should work just like finalize transform...so not needed technically. Need to see where the break in the logic is.
    netView.transform.localPosition =
      netView.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    netView.transform.localRotation =
      Quaternion.Euler(netView.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash,
        Vector3.zero));

    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = true;

    AddPiece(netView);
  }

  public void OnActivationComplete()
  {
    CanUpdate = true;
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
    foreach (var nvPiece in m_pieces)
    {
      if (nvPiece == null || nvPiece.GetZDO() == null) continue;
      nvPiece.GetZDO().RemoveInt(VehicleZdoVars.SwivelParentId);
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
    m_hoverFadeText.ResetHoverTimer();
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

  public void Start()
  {
    m_piecesController = GetComponentInParent<VehiclePiecesController>();
    UpdateRotation();
    _pieceActivator.StartInitPersistentId();
  }

  public void OnInitComplete()
  {
    StartActivatePendingSwivelPieces();
  }

  public void UpdateRotation()
  {
    if (m_nview != null && m_nview.GetZDO() != null)
    {
      var zdoRotation = m_nview.GetZDO().GetRotation();
      m_startPieceRotation = zdoRotation;
    }
    else
    {
      m_startPieceRotation = transform.localRotation;
    }
  }

  protected override Quaternion CalculateTargetWindDirectionRotation()
  {
    if (m_vehicle == null || m_vehicle.MovementController == null)
    {
      var windDir = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : transform.forward;
      
      // these calcs are probably all wrong.
      // var dir = Utils.YawFromDirection(transform.InverseTransformDirection(windDir));
      var dir = Quaternion.LookRotation(
        -Vector3.Lerp(windDir,
          Vector3.Normalize(windDir - pieceContainer.forward), turnTime),
        pieceContainer.up);
        
      return Quaternion.RotateTowards(pieceContainer.transform.rotation, dir, 30f * Time.fixedDeltaTime) ;
    }
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
    return ModTranslations.Swivel_HoverText;
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
    PieceActivatorHelpers.FixPieceMeshes(nv);
    m_pieces.Add(nv);

    if (connectorContainer != null)
    {
      connectorContainer.gameObject.SetActive(m_pieces.Count > 0);
    }
  }
  
  public void RemovePiece(ZNetView nv)
  {
    m_pieces.Remove(nv);
    if (connectorContainer != null)
    {
      connectorContainer.gameObject.SetActive(m_pieces.Count > 0);
    }
  }

  /**
  * prevent ship destruction on m_nview null
  * - if null it would prevent getting the ZDO information for the ship pieces
  */
  public void DestroyPiece(WearNTear wnt)
  {
    if (wnt != null)
    {
      var nv = wnt.GetComponent<ZNetView>();
      RemovePiece(nv);
    }
  }

#endregion

}