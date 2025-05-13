// PowerNetworkController.Visual
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    [SerializeField] private bool enableVisualWires = true;
    private readonly List<LineRenderer> _activeLines = new();

    protected void ClearVisualWires()
    {
      if (!enableVisualWires) return;
      foreach (var line in _activeLines)
      {
        if (line != null)
        {
          Destroy(line.gameObject);
        }
      }
      _activeLines.Clear();
    }

    protected void AddChainLine(Vector3[] points)
    {
      if (!enableVisualWires) return;
      var obj = new GameObject("PylonVisualLine");
      obj.transform.SetParent(transform, false);
      var line = obj.AddComponent<LineRenderer>();

      line.material = WireMaterial;
      line.widthMultiplier = 0.02f;
      line.positionCount = points.Length;
      line.SetPositions(points);

      _activeLines.Add(line);
    }
  }
}