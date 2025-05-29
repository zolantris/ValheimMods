// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using ValheimVehicles.Structs;

public class PowerConduitPlateBridge :
  PowerNetworkDataEntity<PowerConduitPlateBridge, PowerConduitPlateComponent, PowerConduitData>,
  IPowerConduit
{
  public string NetworkId => Logic.NetworkId;
  public Vector3 Position => transform.position;
  public Vector3 ConnectorPoint => Position;

  public bool IsActive => Data.IsActive;
  public bool IsDemanding => Logic.IsDemanding;

  public static List<PowerConduitPlateBridge> Instances = new();
  public List<Player> m_localPlayers = new();
  public Coroutine? _triggerCoroutine;
  public float interval = 2f;

  public void OnEnable()
  {
    if (!Instances.Contains(this))
    {
      Instances.Add(this);
    }
  }

  public void OnDisable()
  {
    Instances.Remove(this);
    Instances.RemoveAll(x => !x);
  }

  public float RequestPower(float deltaTime)
  {
    LoggerProvider.LogWarning("Deprecated RequestPower");
    return 0f;
  }
  public float SupplyPower(float deltaTime)
  {
    LoggerProvider.LogWarning("Deprecated SupplyPower");
    return 0f;
  }

  public bool MustSync = false;
  public Rigidbody m_body;
  public FixedJoint m_joint;
  private Transform? _lastParent;

  private void HandlePlayerEnterActiveZone(Player player)
  {
    if (!player) return;
    Data.AddOrUpdate(player.GetPlayerID(), player.GetEitr(), player.GetMaxEitr());
    Logic.SetHasPlayerInRange(Data.HasPlayersWithEitr);
  }

  private void HandlePlayerExitActiveZone(Player player)
  {
    if (!player) return;
    Data.RemovePlayer(player);
    Logic.SetHasPlayerInRange(Data.HasPlayersWithEitr);
  }

#region Events

  private void OnTriggerEnter(Collider other)
  {
    var player = other.GetComponentInParent<Player>();
    if (!player)
    {
      Physics.IgnoreCollision(other, Logic.m_triggerCollider, true);
      return;
    }

    HandlePlayerEnterActiveZone(player);

    // start the coroutine if it has not been started yet.
    if (m_localPlayers.Count > 0 && _triggerCoroutine == null)
    {
      _triggerCoroutine = StartCoroutine(HandleTriggerRoutine());
    }
  }

  /// <summary>
  /// A polling system to update eitr across active conduits. Will only call from active client components.
  /// </summary>
  /// <param name="collider"></param>
  /// <returns></returns>
  private IEnumerator HandleTriggerRoutine()
  {
    if (!this.IsNetViewValid(out var netview))
    {
      _triggerCoroutine = null;
      yield break;
    }
    while (isActiveAndEnabled && m_localPlayers.Count > 0 && netview != null && netview.m_zdo != null)
    {
      PowerSystemRPC.Request_OfferAllPlayerEitr(netview.m_zdo, m_localPlayers);
      yield return new WaitForSeconds(interval);
    }
    _triggerCoroutine = null;
  }

  private void OnTriggerExit(Collider other)
  {
    var player = other.GetComponentInParent<Player>();
    if (!player) return;
    HandlePlayerExitActiveZone(player);

    if (m_localPlayers.Count <= 0 && _triggerCoroutine != null)
    {
      StopCoroutine(_triggerCoroutine);
      _triggerCoroutine = null;
    }

    if (this.IsNetViewValid(out var netView))
    {
      PowerSystemRPC.Request_PlayerExitedConduit(netView.GetZDO().m_uid, player.GetPlayerID());
    }
  }

#endregion

  protected override void Start()
  {
    this.WaitForPowerSystemNodeData<PowerConduitData>((data) =>
    {
      Data = data;
      Data.Load();
      Logic.GetPlayerEitr = () => PowerConduitData.GetAverageEitr(Data.PlayerDataById.Values.ToList(), Data.PlayerDataById);
      Logic.AddPlayerEitr = val => Data.AddEitrToPlayers(val);
      Logic.SubtractPlayerEitr = val => Data.TryRemoveEitrFromPlayers(val);
      _lastParent = transform.parent;
      AddRigidbodyIfParentIsRigidbody();
    });

    base.Start();
  }

  public void OnTransformParentChanged()
  {
    var newParent = transform.parent;
    if (newParent && newParent == _lastParent) return;
    LoggerProvider.LogDev($"Parent changed from {_lastParent?.name ?? "null"} to {newParent?.name ?? "null"}");
    _lastParent = newParent;
    AddRigidbodyIfParentIsRigidbody();
  }

  protected void AddRigidbodyIfParentIsRigidbody()
  {
    var parentRigidbody = GetComponentInParent<Rigidbody>();
    if (!m_body)
    {
      m_body = gameObject.AddComponent<Rigidbody>();
    }
    m_body.isKinematic = !parentRigidbody;
    if (!parentRigidbody)
    {
      if (m_joint) Destroy(m_joint);
      return;
    }

    var joint = gameObject.AddComponent<FixedJoint>();
    joint.connectedBody = parentRigidbody;
    joint.enableCollision = false;
  }

  protected override void OnDestroy()
  {
    base.OnDestroy();
  }
  protected override void RegisterDefaultRPCs() {}

  public void SetNetworkId(string id)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.PowerSystem_NetworkId);
    Logic.SetNetworkId(idFromZdo);
  }
}