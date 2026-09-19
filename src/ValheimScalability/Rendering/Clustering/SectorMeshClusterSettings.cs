using UnityEngine;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Structural constants/helpers that are not user configuration.
/// </summary>
public static class SectorMeshClusterSettings
{
  public const int MaxInstancedMatricesPerDraw = 1023;

  private static int _pieceLayer = -2;

  public static int PieceLayer
  {
    get
    {
      if (_pieceLayer == -2)
      {
        _pieceLayer = LayerMask.NameToLayer("piece");
      }

      return _pieceLayer;
    }
  }

  public static void InvalidateLayerCache()
  {
    _pieceLayer = -2;
  }
}
