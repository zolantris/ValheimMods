#region

#endregion

  using UnityEngine;
  namespace ValheimVehicles.Interfaces;

  /// <summary>
  /// Shared PieceController interface for components like SwivelController or VehiclePiecesController
  /// </summary>
  ///
  /// Todo consider adding a few booleans and maybe a getter with an override to get the exact component. Like VehiclePiecesController and SwivelIntegrationComponent
  public interface IPieceController
  {
    public Transform transform { get; }
    public string ComponentName { get; }
    public int GetPieceCount();
    public bool CanRaycastHitPiece(); // for raycast interactions with piece placement.
    public bool CanDestroy(); // To prevent destruction of prefab before pieces are unloaded.
    public void AddPiece(ZNetView nv, bool isNew = false);
    public void AddNewPiece(ZNetView nv);
    public void AddCustomPiece(ZNetView nv, bool isNew = false);
    public void AddCustomPiece(GameObject prefab, bool isNew = false);

    public void DestroyPiece(WearNTear wnt); // typically with wearntear but also in hammer deletion of non-wearnt pieces.
    public void RemovePiece(ZNetView nv);
    public void TrySetPieceToParent(ZNetView netView);
    public void TrySetPieceToParent(GameObject netView, bool isForced = false);

  }