#region

using Unity.Collections;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public ref struct FlattenedPointEnumerator
  {
    private readonly PrefabPieceData[] _pieces;
    private int _pieceIndex;
    private int _pointIndex;
    private NativeArray<Vector3> _currentPoints;

    public FlattenedPointEnumerator(PrefabPieceData[] pieces)
    {
      _pieces = pieces;
      _pieceIndex = -1;
      _pointIndex = -1;
      _currentPoints = default;
    }

    public bool MoveNext(out Vector3 point)
    {
      point = default;

      while (true)
      {
        // If current points array is uninitialized or finished, move to next piece
        if (!_currentPoints.IsCreated || ++_pointIndex >= _currentPoints.Length)
        {
          _pieceIndex++;
          _pointIndex = 0;

          if (_pieceIndex >= _pieces.Length)
            return false; // End of all pieces

          _currentPoints = _pieces[_pieceIndex].PrefabColliderPointData.Points;

          // If current points are empty, continue to next piece
          if (_currentPoints.Length == 0)
            continue;
        }

        point = _currentPoints[_pointIndex];
        return true;
      }
    }
  }
}