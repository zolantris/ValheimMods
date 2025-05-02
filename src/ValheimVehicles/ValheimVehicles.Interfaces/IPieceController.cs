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
    public int GetPieceCount();
    public bool CanRaycastHitPiece(); // for raycast interactions with piece placement.
    public bool CanDestroy(); // To prevent destruction of prefab before pieces are unloaded.
    public void AddPiece(ZNetView nv, bool isNew = false);
    public void DestroyPiece(WearNTear wnt); // typically with wearntear but also in hammer deletion of non-wearnt pieces.
    public void RemovePiece(ZNetView nv);

  }