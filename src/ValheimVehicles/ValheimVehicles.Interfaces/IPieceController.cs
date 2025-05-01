namespace ValheimVehicles.Interfaces;

/// <summary>
/// Shared PieceController interface for components like SwivelController or VehiclePiecesController
/// </summary>
public interface IPieceController
{
  public void AddPiece(ZNetView nv, bool isNew = false);
  public void RemovePiece(ZNetView nv);
}