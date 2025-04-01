#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// Huge performance boost for larger ships using prefabs that share the same material resources.
  ///
  /// Ships go from 30 FPS with 1000+ pieces to over 80FPS again.
  /// </summary>
  public class MeshClusterController : MonoBehaviour
  {
    public static bool IsClusteringEnabled = true;
    public static int ClusterRenderingPieceThreshold = 500;
    public static Action<string> Logger = Debug.Log;
    public static List<string> combinedMeshExcludePrefabNames =
      new()
      {
        "wheel", "portal",
        // PrefabNames.MBRopeLadder,
        // PrefabNames.MBRopeAnchor,
        // PrefabNames.ShipSteeringWheel, 
        "door", "chest", "cart"
      };
    public static List<string> combineMeshMeshFilterExcludeNames = new()
    {
      "Destruction", "Portal_destruction", "Destruction_Cube"
    };
    public static List<string> PrefabExcludeNames =
      new()
      {
        "wheel", "portal",
        "door", "chest", "cart"
      };
    public static List<string> MeshFilterExcludeNames = new()
    {
      "Destruction", "high", "large_lod", "vehicle_water_mesh", "largelod", "Portal_destruction", "Destruction_Cube"
    };
    public static List<string> MeshFilterIncludesNames = new()
    {
      "Combined", "Combined_Mesh"
    };


    internal BasePiecesController MBasePiecesController;
    internal GameObject combinedMeshParent;
    internal Dictionary<Material, GameObject> _previousCombinedMeshObjects = new(); // Store previous combined meshes
    public Dictionary<GameObject, List<Material>> _relatedGameObjectToMaterialsMap = new();
    internal Dictionary<Material, List<GameObject>> _relatedMaterialToGameObjectsMap = new(); // Store previous combined meshes
    public Dictionary<GameObject, List<Material>> CombinedMeshMaterialsObjMap = new();
    private List<GameObject> currentMeshObjects = new();
    public Dictionary<GameObject, List<MeshRenderer>> hiddenMeshRenderersObjMap = new();

    public Action IgnoreAllVehicleCollidersCallback = () => {};
    private HashSet<GameObject> wntSubscribers = new();

    public void Awake()
    {
      MBasePiecesController = GetComponent<BasePiecesController>();
    }

    public void OnPieceDestroyHandler(GameObject go)
    {
      wntSubscribers.Remove(go);
      if (!MBasePiecesController) return;
      if (IsClusteringEnabled || MBasePiecesController.GetPieceCount() < ClusterRenderingPieceThreshold) return;
      CleanupRelatedCombinedMeshes();

      if (_relatedGameObjectToMaterialsMap.TryGetValue(go, out var items))
      {
        var relatedPrefabs = new List<GameObject>();
        foreach (var material in items)
        {
          if (_relatedMaterialToGameObjectsMap.TryGetValue(material, out var relatedGameObjects))
          {
            relatedPrefabs.AddRange(relatedGameObjects);
          }
          if (_previousCombinedMeshObjects.TryGetValue(material, out var previousCombinedMeshObject))
          {
            Destroy(previousCombinedMeshObject);
            _previousCombinedMeshObjects.Remove(material);
          }
        }
        if (relatedPrefabs.Count < 1) return;

        // TODO [PERFORMANCE] may want to debounce this. But it will be very laggy looking if we delay this step. 
        GenerateCombinedMeshes(relatedPrefabs.ToArray(), true);
        IgnoreAllVehicleCollidersCallback();
      }
    }

    /// <summary>
    /// Cleanup potentially null lists or hashsets
    /// </summary>
    private void CleanupRelatedCombinedMeshes()
    {
      _relatedGameObjectToMaterialsMap = _relatedGameObjectToMaterialsMap.Where(x => x.Value != null && x.Key != null).ToDictionary(x => x.Key, y => y.Value);
      _relatedMaterialToGameObjectsMap = _relatedMaterialToGameObjectsMap.Where(x => x.Value != null && x.Key != null).ToDictionary(x => x.Key, y => y.Value.Where(x => x != null).ToList());
    }

    public static Regex GenerateRegexFromList(List<string> prefixes)
    {
      if (prefixes == null || prefixes.Count == 0)
      {
        return new Regex("$^"); // 🔹 Matches NOTHING if list is empty
      }
      // Escape special characters in the strings and join them with a pipe (|) for OR condition
      var escapedPrefixes = new List<string>();
      foreach (var prefix in prefixes)
      {
        escapedPrefixes.Add(Regex.Escape(prefix));
      }

      // Create a regex pattern that matches the start of the string (^)
      // It will match any of the provided prefixes at the start of the string
      var pattern = "^(" + string.Join("|", escapedPrefixes) + ")";
      return new Regex(pattern);
    }
    public GameObject GetWNTActiveComponent(IWearNTearStub wnt)
    {
      if (wnt.m_wet && wnt.m_wet.activeSelf) return wnt.m_wet;
      if (wnt.m_worn && wnt.m_worn.activeSelf) return wnt.m_worn;
      if (wnt.m_broken && wnt.m_broken.activeSelf) return wnt.m_broken;
      return wnt.m_new;
    }
    private List<MeshRenderer> GetValidNonNestedMeshRenderers(List<MeshRenderer> selectedRenders, Transform root, [CanBeNull] IWearNTearStub wnt)
    {
      List<MeshRenderer> validRenderers = new();
      HashSet<Transform> excludedLODs = new(); // 🔹 Track excluded lower LOD objects
      
      foreach (Transform child in root)
      {
        if (wnt != null && IsChildOfWNT(child, wnt)) continue; // 🔹 Skip WNT sub-objects

        var lodGroup = child.GetComponent<LODGroup>();
        if (lodGroup)
        {
          // 🔹 Get LOD0 (Highest Quality LOD)
          var lods = lodGroup.GetLODs();
          if (lods.Length > 0)
          {
            var maxLODRenderers = lods[0].renderers.OfType<MeshRenderer>().Where(x => !selectedRenders.Contains(x));
            validRenderers.AddRange(maxLODRenderers);

            // 🔹 Mark all other LOD objects for exclusion
            for (var i = 1; i < lods.Length; i++)
            {
              foreach (var lowerLodRenderer in lods[i].renderers)
              {
                if (lowerLodRenderer)
                  excludedLODs.Add(lowerLodRenderer.transform);
              }
            }

            continue; // ✅ Skip further processing for this LODGroup
          }
        }

        // 🔹 If no LODGroup, only add if it's NOT part of an excluded LOD
        if (!excludedLODs.Contains(child))
        {
          validRenderers.AddRange(child.GetComponentsInChildren<MeshRenderer>(true));
        }
      }

      return validRenderers;
    }
    private bool IsChildOfWNT(Transform child, [CanBeNull] IWearNTearStub wnt)
    {
      // Ensure WNT exists before checking
      if (child == null || wnt == null) return false;

      // 🔹 Only call IsChildOf() if the transform is NOT null
      return wnt.m_wet && child.IsChildOf(wnt.m_wet.transform) ||
             wnt.m_worn && child.IsChildOf(wnt.m_worn.transform) ||
             wnt.m_broken && child.IsChildOf(wnt.m_broken.transform) ||
             wnt.m_new && child.IsChildOf(wnt.m_new.transform);
    }
    public void RestoreGeneratedMeshes(GameObject obj)
    {
      if (hiddenMeshRenderersObjMap.TryGetValue(obj, out var renderers))
      {
        renderers.ForEach(x =>
        {
          x.enabled = true;
        });
        renderers.Clear();
        hiddenMeshRenderersObjMap.Remove(obj);
      }
    }

    public void GenerateCombinedMeshes(GameObject[] prefabList, bool hasRunCleanup = false)
    {
      if (!combinedMeshParent)
      {
        combinedMeshParent = new GameObject
        {
          name = "ValheimVehicles_VehicleCombinedMesh",
          gameObject = { layer = LayerHelpers.CustomRaftLayer },
          transform = { parent = transform }
        };
      }

      if (!hasRunCleanup)
      {
        CleanupRelatedCombinedMeshes();
      }

      Dictionary<Material, List<CombineInstance>> materialToMeshes = new();
      var prefabExclusionRegex = GenerateRegexFromList(PrefabExcludeNames); // Compile Regex once
      var meshFilterExclusionRegex = GenerateRegexFromList(MeshFilterExcludeNames); // Compile Regex once
      var meshFilterIncludeRegex = GenerateRegexFromList(MeshFilterIncludesNames); // Compile Regex once

      foreach (var prefabItem in prefabList)
      {
        if (!prefabItem) continue; // Skip inactive
        if (prefabExclusionRegex.IsMatch(prefabItem.gameObject.name)) continue; // 🔹 Skip excluded objects

        var wnt = ValheimExtensions.GetWearNTear(prefabItem);
        List<MeshRenderer> selectedRenderers = new();

        if (wnt != null)
        {

          if (!wntSubscribers.Contains(prefabItem))
          {
            wntSubscribers.Add(prefabItem);
            wnt.m_onDestroyed += () => OnPieceDestroyHandler(prefabItem.gameObject);
          }
          // 🔹 WNT Exists: Get the active component & its MeshRenderer
          var activeWNTObject = GetWNTActiveComponent(wnt);
          if (activeWNTObject)
          {
            List<MeshRenderer> selectedRenderersCombinedMesh = new();
            var tempRenderers = activeWNTObject.GetComponentsInChildren<MeshRenderer>(true);

            // if A combined mesh is found we skip all other mesh renderers without that name.
            foreach (var tempRenderer in tempRenderers)
            {
              if (meshFilterIncludeRegex.IsMatch(tempRenderer.gameObject.name))
              {
                selectedRenderersCombinedMesh.Add(tempRenderer);
              }
            }

            selectedRenderers.AddRange(selectedRenderersCombinedMesh.Count > 0 ? selectedRenderersCombinedMesh : tempRenderers);
          }
        }

        var nonWntRenderers = GetValidNonNestedMeshRenderers(selectedRenderers, prefabItem.transform, wnt);
        var tempNonWntCombinedRenderers = new List<MeshRenderer>();

        // 🔹 Include non-WNT MeshRenderers that are not nested inside other WNT objects
        // if A combined mesh is found we skip all other mesh renderers without that name.
        foreach (var nonWntRenderer in nonWntRenderers)
        {
          if (meshFilterIncludeRegex.IsMatch(nonWntRenderer.gameObject.name))
          {
            tempNonWntCombinedRenderers.Add(nonWntRenderer);
          }
        }

        selectedRenderers.AddRange(tempNonWntCombinedRenderers.Count > 0 ? tempNonWntCombinedRenderers : nonWntRenderers);


        if (!hiddenMeshRenderersObjMap.TryGetValue(prefabItem.gameObject, out var currentDeactivatedMeshes))
        {
          currentDeactivatedMeshes = new List<MeshRenderer>();
        }

        foreach (var renderer in selectedRenderers)
        {
          if (!renderer || renderer.sharedMaterial == null) continue;
          var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
          if (mesh == null) return;
          if (!mesh.isReadable)
          {
#if DEBUG
            Logger($"Skipping unreadable mesh {mesh.name}");
#endif
            continue; // Skip unreadable meshes
          }
          if (meshFilterExclusionRegex.IsMatch(renderer.gameObject.name)) continue;

          var fixedMaterial = renderer.sharedMaterial;
          if (!materialToMeshes.ContainsKey(fixedMaterial))
          {
            materialToMeshes[fixedMaterial] = new List<CombineInstance>();
          }

          // deletes previous mesh.
          if (_previousCombinedMeshObjects.TryGetValue(fixedMaterial, out var obj))
          {
            Destroy(obj);
            _previousCombinedMeshObjects.Remove(fixedMaterial);
          }

          if (_relatedMaterialToGameObjectsMap.TryGetValue(fixedMaterial, out var relatedGameObjectsToMaterial))
          {
            relatedGameObjectsToMaterial.Add(prefabItem);
          }
          else
          {
            _relatedMaterialToGameObjectsMap[fixedMaterial] = new List<GameObject>
            {
              prefabItem
            };
          }

          var meshTransform = renderer.transform;

          // 🔹 Prevent floating-point errors in transform updates
          // todo need to confirm that this is a local position mesh and using world transform, not world point mesh vectors using relative transform.
          var position = meshTransform.position;
          var fixedPosition = new Vector3(
            Mathf.Round(position.x * 1000f) / 1000f,
            Mathf.Round(position.y * 1000f) / 1000f,
            Mathf.Round(position.z * 1000f) / 1000f
          );

          materialToMeshes[fixedMaterial].Add(new CombineInstance
          {
            mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
            transform = Matrix4x4.TRS(fixedPosition, meshTransform.rotation, meshTransform.lossyScale)
          });

          if (renderer.gameObject.activeSelf && renderer.enabled && !currentDeactivatedMeshes.Contains(renderer))
          {
            renderer.enabled = false;
            currentDeactivatedMeshes.Add(renderer);
          }

          if (_relatedGameObjectToMaterialsMap.TryGetValue(prefabItem, out var relatedMaterials))
          {
            if (!relatedMaterials.Contains(fixedMaterial))
            {
              relatedMaterials.Add(fixedMaterial);
            }
          }
          else
          {
            _relatedGameObjectToMaterialsMap.Add(prefabItem, new List<Material>
            {
              fixedMaterial
            });
          }
        }
      }

      foreach (var entry in materialToMeshes)
      {
        var material = entry.Key;
        var combineInstances = entry.Value;

        var combinedMesh = new Mesh
        {
          name = "ValheimVehicles_CombinedMesh_" + material.name,
          indexFormat = IndexFormat.UInt32 // we need this (we easily surpass the default value)
        };

        combinedMesh.CombineMeshes(combineInstances.ToArray(), true);

        var meshObject = new GameObject($"CombinedMesh_{material.name}");
        meshObject.gameObject.layer = LayerHelpers.CustomRaftLayer;
        meshObject.transform.SetParent(combinedMeshParent.transform);

        var meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = combinedMesh;

        var meshRenderer = meshObject.AddComponent<MeshRenderer>();

#if DEBUG
        // this causes problems with the low LOD items, we could hide those instead but for now not doing this..
        // material.renderQueue = material.renderQueue == 2000 ? 1999 : material.renderQueue;
#endif

        // 🔹 Apply fixed material instance
        meshRenderer.sharedMaterial = material;

        var meshCollider = meshObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = combinedMesh;
        meshCollider.convex = false;

        _previousCombinedMeshObjects.Add(material, meshObject);
      }
    }
  }
}