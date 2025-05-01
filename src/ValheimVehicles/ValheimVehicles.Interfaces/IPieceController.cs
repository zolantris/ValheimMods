#region

  #endregion

  namespace ValheimVehicles.Interfaces;

/// <summary>
/// Shared PieceController interface for components like SwivelController or VehiclePiecesController
/// </summary>
///
/// Todo consider adding a few booleans and maybe a getter with an override to get the exact component. Like VehiclePiecesController and SwivelIntegrationComponent
public interface IPieceController
{
  public void AddPiece(ZNetView nv, bool isNew = false);
  public void DestroyPiece(WearNTear wnt);
  public void RemovePiece(ZNetView nv);
  
}