using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Components;

public class SwivelPieceActivator : BasePieceActivatorComponent
{
  [SerializeField] private SwivelComponentIntegration _host;
  public override IPieceActivatorHost Host => _host;

  protected IEnumerator InitPersistentId(VehicleShip vehicleShip)
  {
    while (!_host.GetNetView())
    {
      yield return null;
    }
  }

  protected override void TrySetPieceToParent(ZNetView netView)
  {
    // Classic vehicle-specific logic
    netView.transform.SetParent(_host.GetPieceContainer(), false);
  }

  public static void AddPendingSwivelPiece(int swivelId, ZNetView netView)
  {
    if (!m_pendingPieces.TryGetValue(swivelId, out var list))
    {
      list = new List<ZNetView>();
      m_pendingPieces[swivelId] = list;
    }

    list.Add(netView);
  }
}