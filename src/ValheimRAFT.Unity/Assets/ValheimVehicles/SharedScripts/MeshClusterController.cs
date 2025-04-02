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
    public static bool CanRefreshClusteringOnDestroy = true;
    public static int ClusterRenderingPieceThreshold = 500;
    public static Action<string> Logger = Debug.Log;


    /// <summary>
    /// enabling IsNonWearNTearMeshCombinationEnabled seems to cause a near infinite loop.
    /// todo We need to discover why this is happening
    /// </summary>
    public static bool IsNonWearNTearMeshCombinationEnabled = false;
    public static List<string> combinedMeshExcludePrefabNames =
      new()
      {
        "wheel", "portal",
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
    internal Dictionary<Material, GameObject> _currentCombinedMeshObjects = new(); // Store previous combined meshes
    internal List<MeshCollider> _currentCombinedMeshColliders = new(); // Store previous combined meshes
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

    public static bool CanUpdateHealthItem = false;

    public void OnPieceHealthUpdatedHandler(GameObject go)
    {
      if (!CanUpdateHealthItem) return;
      UpdatePieceAfterModification(go);
      CanUpdateHealthItem = false;
    }

    public void OnPieceDestroyHandler(GameObject go)
    {
      wntSubscribers.Remove(go);
      UpdatePieceAfterModification(go);
    }

    public void UpdatePieceAfterModification(GameObject go)
    {
      CleanupRelatedCombinedMeshes();
      if (!MBasePiecesController) return;
      if (!IsClusteringEnabled || MBasePiecesController.GetPieceCount() < ClusterRenderingPieceThreshold) return;

      if (!_relatedGameObjectToMaterialsMap.TryGetValue(go, out var items)) return;
      var relatedPrefabs = new List<GameObject>();

      foreach (var material in items)
      {
        if (_relatedMaterialToGameObjectsMap.TryGetValue(material, out var relatedGameObjects))
        {
          relatedPrefabs.AddRange(relatedGameObjects);
        }
        if (_currentCombinedMeshObjects.TryGetValue(material, out var previousCombinedMeshObject))
        {
          Destroy(previousCombinedMeshObject);
          _currentCombinedMeshObjects.Remove(material);
        }
      }

      if (relatedPrefabs.Count < 1) return;

      // TODO [PERFORMANCE] may want to debounce this. But it will be very laggy looking if we delay this step. 
      GenerateCombinedMeshes(relatedPrefabs.ToArray(), true);

      // do nothing if we have no colliders to ignore. This is super inefficient if we run it every time.
      if (_currentCombinedMeshObjects.Count > 0)
      {
        IgnoreAllVehicleCollidersCallback();
      }
    }

    /// <summary>
    /// Cleanup potentially null lists or hashsets
    ///
    /// TODO [PERFORMANCE] this likely a large amount of allocations that could cause performance problems. 
    /// </summary>
    private void CleanupRelatedCombinedMeshes()
    {
      // foreach (var o in prefabList)
      // {
      //   if (!CombinedMeshMaterialsObjMap.TryGetValue(o, out var materialList)) continue;
      //   foreach (var material in materialList)
      //   {
      //     if (material == null) continue;
      //     if (MaterialToMeshesMap.TryGetValue(material, out var mesh))
      //     {
      //       Destroy(mesh);
      //     }
      //   }
      // }
      
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

    public void EnableCollisionsForCombinedMeshColliders()
    {
      for (var index = 0; index < _currentCombinedMeshColliders.Count; index++)
      {
        var current = _currentCombinedMeshColliders[index];
        if (!current)
        {
          _currentCombinedMeshColliders.RemoveAt(index);
          index--;
          continue;
        }

        current.enabled = true;
      }
    }

    public bool ShouldInclude(string objName, Regex IncludeRegex, Regex ExcludeRegex)
    {
      if (ExcludeRegex.IsMatch(objName))
      {
        return false;
      }

      if (!IncludeRegex.IsMatch(objName))
      {
        return false;
      }

      return true;
    }

    public void InitCombinedMeshParentObj()
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
    }

    private List<MeshRenderer> _tempRenders = new();

    public void GenerateCombinedMeshes(GameObject[] prefabList, bool hasRunCleanup = false)
    {
      InitCombinedMeshParentObj();
      _tempRenders.Clear();

      if (!hasRunCleanup)
      {
        CleanupRelatedCombinedMeshes();
      }

      Dictionary<Material, List<CombineInstance>> MaterialToMeshesMap = new();
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

          // we do not want this enable always. This is slightly unstable.
          if (CanRefreshClusteringOnDestroy)
          {
            var hasSubscription = wntSubscribers.Contains(prefabItem);
            if (!hasSubscription)
            {
              wntSubscribers.Add(prefabItem);
              wnt.m_onHealthVisualChange += () => OnPieceHealthUpdatedHandler(prefabItem.gameObject);
              wnt.m_onDestroyed += () => OnPieceDestroyHandler(prefabItem.gameObject);
            }
          }
          // 🔹 WNT Exists: Get the active component & its MeshRenderer
          var activeWNTObject = GetWNTActiveComponent(wnt);
          if (activeWNTObject)
          {
            List<MeshRenderer> selectedRenderersCombinedMesh = new();
            activeWNTObject.GetComponentsInChildren<MeshRenderer>(true, _tempRenders);

            if (_tempRenders.Count > 0)
            {
              // if A combined mesh is found we skip all other mesh renderers without that name.
              foreach (var tempRenderer in _tempRenders)
              {
                if (ShouldInclude(tempRenderer.gameObject.name, meshFilterIncludeRegex, meshFilterExclusionRegex))
                {
                  selectedRenderersCombinedMesh.Add(tempRenderer);
                }
              }
            }

            selectedRenderers.AddRange(selectedRenderersCombinedMesh.Count > 0 ? selectedRenderersCombinedMesh : _tempRenders);
          }
        }

        
        // This code should be considered experimental and unstable as it grab literally all children nodes of MeshRenderer and adds them to the iterator.
        if (IsNonWearNTearMeshCombinationEnabled)
        {
          var nonWntRenderers = GetValidNonNestedMeshRenderers(selectedRenderers, prefabItem.transform, wnt);
          var tempNonWntCombinedRenderers = new List<MeshRenderer>();

          // 🔹 Include non-WNT MeshRenderers that are not nested inside other WNT objects
          // if A combined mesh is found we skip all other mesh renderers inside that object.
          // todo move name skipping into the GetValidNonNestedMeshRenderers
          foreach (var nonWntRenderer in nonWntRenderers)
          {
            if (ShouldInclude(nonWntRenderer.gameObject.name, meshFilterIncludeRegex, meshFilterExclusionRegex))
            {
              tempNonWntCombinedRenderers.Add(nonWntRenderer);
            }
          }
          selectedRenderers.AddRange(tempNonWntCombinedRenderers.Count > 0 ? tempNonWntCombinedRenderers : nonWntRenderers);
          tempNonWntCombinedRenderers.Clear();
        }

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
          if (!MaterialToMeshesMap.ContainsKey(fixedMaterial))
          {
            MaterialToMeshesMap[fixedMaterial] = new List<CombineInstance>();
          }

          // deletes previous mesh.
          if (_currentCombinedMeshObjects.TryGetValue(fixedMaterial, out var obj))
          {
            Destroy(obj);
            _currentCombinedMeshObjects.Remove(fixedMaterial);
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

          MaterialToMeshesMap[fixedMaterial].Add(new CombineInstance
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
        selectedRenderers.Clear();
      }

      foreach (var entry in MaterialToMeshesMap)
      {
        var material = entry.Key;
        var combineInstances = entry.Value;

        var combinedMesh = new Mesh
        {
          name = "ValheimVehicles_CombinedMesh_" + material.name,
          indexFormat = IndexFormat.UInt32 // combined meshes surpass the 16Bit default. This is expected.
        };

        combinedMesh.CombineMeshes(combineInstances.ToArray(), true);

        var meshObject = new GameObject($"CombinedMesh_{material.name}");
        meshObject.gameObject.layer = LayerHelpers.CustomRaftLayer;
        meshObject.transform.SetParent(combinedMeshParent.transform);

        var meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = combinedMesh;

        var meshRenderer = meshObject.AddComponent<MeshRenderer>();

        // 🔹 Apply fixed material instance
        meshRenderer.sharedMaterial = material;

#if DEBUG
        // Mesh colliders are not needed unless we start removing the smaller colliders due to physics optimizations. This is not recommended though because then hit damage would have to be applied differently.
        // AddMeshCollider(meshObject, combinedMesh);
#endif
        _currentCombinedMeshObjects.Add(material, meshObject);
      }

      MaterialToMeshesMap.Clear();
    }

#if DEBUG
    /// <summary>
    /// We may want this in the future.
    /// </summary>
    /// <param name="meshObject"></param>
    /// <param name="combinedMesh"></param>
    public void AddMeshColliderToCombinedMesh(GameObject meshObject, Mesh combinedMesh)
    {
      var meshCollider = meshObject.AddComponent<MeshCollider>();
      meshCollider.sharedMesh = combinedMesh;
      meshCollider.convex = false;
      meshCollider.enabled = false; // do not enable until we ignore collisions for this.
    }
#endif
  }
}