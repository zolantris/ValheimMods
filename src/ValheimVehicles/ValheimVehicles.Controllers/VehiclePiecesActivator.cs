using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Controllers;

public class VehiclePieceActivator : BasePieceActivatorComponent
{
  [SerializeField] private VehiclePiecesController _host;

  public override IPieceActivatorHost Host => _host;

  public void Init(VehiclePiecesController host)
  {
    _host = host;
  }

  protected override void TrySetPieceToParent(ZNetView netView)
  {
    _host.TrySetPieceToParent(netView);
  }

  protected override void AddPiece(ZNetView netView, bool isNewPiece = false)
  {
    _host.AddPiece(netView, isNewPiece);
  }
}