#region

  using System.Collections.Generic;
  using UnityEngine;
  using ValheimVehicles.Interfaces;

#endregion

namespace ValheimVehicles.Helpers;

public static class PieceActivatorHelpers
{
  public static Dictionary<Material, Material> FixMaterialUniqueInstances = new();
  private static readonly int TriplanarLocalPos =
    Shader.PropertyToID("_TriplanarLocalPos");
  private static readonly int RippleDistance =
    Shader.PropertyToID("_RippleDistance");
  private static readonly int ValueNoise = Shader.PropertyToID("_ValueNoise");
  /// <summary>
  /// Gets the RaycastPieceActivator which is used for Swivels and VehiclePiecesController components. These components are responsible for activation and parenting of vehicle pieces and will always exist above the current piece in transform hierarchy.
  /// </summary>
  public static IRaycastPieceActivator? GetRaycastPieceActivator(
    Transform obj)
  {
    var pieceActivator = obj.GetComponentInParent<IRaycastPieceActivator>();
    return pieceActivator;
  }

  public static IRaycastPieceActivator? GetRaycastPieceActivator(
    GameObject obj)
  {
    var pieceActivator = obj.GetComponentInParent<IRaycastPieceActivator>();
    return pieceActivator;
  }

  /// <summary>
  /// Gets the activator host. If it exists.
  /// </summary>
  public static IPieceActivatorHost? GetPieceActivatorHost(
    GameObject obj)
  {
    var activatorHost = obj.GetComponentInParent<IPieceActivatorHost>();
    return activatorHost;
  }
  
  public static void FixPieceMeshes(ZNetView netView)
  {
    /*
     * It fixes shadow flicker on all of valheim's prefabs with boats
     * If this is removed, the raft is seizure inducing.
     */
    var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
    foreach (var meshRenderer in meshes)
    {
      // foreach (var meshRendererMaterial in meshRenderer.materials)
      //   FixMaterial(meshRendererMaterial);

      if (meshRenderer.sharedMaterials.Length > 0)
      {
        var sharedMaterials = meshRenderer.sharedMaterials;
        for (var j = 0; j < sharedMaterials.Length; j++)
          sharedMaterials[j] = FixMaterial(sharedMaterials[j]);

        meshRenderer.sharedMaterials = sharedMaterials;
      }
      else if (meshRenderer.materials.Length > 0)
      {
        var materials = meshRenderer.materials;

        for (var j = 0; j < materials.Length; j++)
          materials[j] = FixMaterial(materials[j]);
        meshRenderer.materials = materials;
      }
    }
  }
  
  /// <summary>
  /// Must return a new material
  /// </summary>
  /// <param name="material"></param>
  /// <returns></returns>
  public static Material FixMaterial(Material material)
  {
    if (!material) return null;

    // Check if material has any of the target properties
    if (!material.HasFloat(RippleDistance) && !material.HasFloat(ValueNoise) && !material.HasFloat(TriplanarLocalPos))
    {
      return material; // No need to fix
    }

    // If already fixed, return the cached instance
    if (FixMaterialUniqueInstances.TryGetValue(material, out var fixedMaterialInstance))
    {
      return fixedMaterialInstance;
    }

    // 🔹 Fix: Create a NEW material BEFORE modifying it
    var newMaterial = new Material(material);

    if (material.name.Contains("blackmarble"))
    {
      newMaterial.SetFloat(TriplanarLocalPos, 1f);
    }

    newMaterial.SetFloat(RippleDistance, 0f);
    newMaterial.SetFloat(ValueNoise, 0f);

    // Cache the fixed material
    FixMaterialUniqueInstances[material] = newMaterial;

    return newMaterial;
  }
}