using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Vehicles.Controllers;

public class MeshClusterController : MonoBehaviour
{
  public static List<string> combinedMeshExcludePrefabNames = ["wheel", "portal", PrefabNames.MBRopeLadder, PrefabNames.MBRopeAnchor, PrefabNames.ShipSteeringWheel, "door", "chest", "cart"];
  public static List<string> combineMeshMeshFilterExcludeNames = ["Destruction", "Portal_destruction", "Destruction_Cube"];
  public static List<string> PrefabExcludeNames = ["wheel", "portal", PrefabNames.MBRopeLadder, PrefabNames.MBRopeAnchor, PrefabNames.ShipSteeringWheel, "door", "chest", "cart"];
  public static List<string> MeshFilterExcludeNames = ["Destruction", "high", "large_lod", "vehicle_water_mesh", "largelod", "Portal_destruction", "Destruction_Cube"];
  public static List<string> MeshFilterIncludesNames = ["Combined", "Combined_Mesh"];

  public Dictionary<GameObject, List<Material>> CombinedMeshMaterialsObjMap = new();
  private HashSet<WearNTear> wntSubscribers = new();
  public GameObject combinedMeshParent;

  public Dictionary<GameObject, List<MeshRenderer>> hiddenMeshRenderersObjMap = new();
  private List<GameObject> currentMeshObjects = new();

  public void OnWearNTearPieceDestroy(GameObject gameObject)
  {
    var getMaterialsToRegenerate = CombinedMeshMaterialsObjMap.TryGetValue(gameObject, out var materialsToRegenerate);
    if (!getMaterialsToRegenerate)
    {
      var objects = transform.GetComponentsInChildren<GameObject>();
      GenerateCombinedMeshes(objects, PrefabExcludeNames, MeshFilterExcludeNames);
    }
    // IgnoreAllVehicleColliders();
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

  public GameObject GetWNTActiveComponent(WearNTear wnt)
  {
    if (wnt.m_wet && wnt.m_wet.activeSelf) return wnt.m_wet;
    if (wnt.m_worn && wnt.m_worn.activeSelf) return wnt.m_worn;
    if (wnt.m_broken && wnt.m_broken.activeSelf) return wnt.m_broken;
    return wnt.m_new;
  }


  private List<MeshRenderer> GetValidNonNestedMeshRenderers(List<MeshRenderer> selectedRenders, Transform root, WearNTear wnt)
  {
    List<MeshRenderer> validRenderers = new();
    HashSet<Transform> excludedLODs = new(); // 🔹 Track excluded lower LOD objects

    foreach (Transform child in root)
    {
      if (wnt && IsChildOfWNT(child, wnt)) continue; // 🔹 Skip WNT sub-objects

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


// 🔹 Helper: Detect if Transform belongs to a WNT component
  private bool IsChildOfWNT(Transform child, WearNTear wnt)
  {
    if (!wnt) return false; // 🔹 Ensure WNT exists before checking

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

  private void GenerateCombinedMeshes(GameObject[] prefabs, List<string> prefabNameExclusionList, List<string> meshFilterExclusionList)
  {
    if (combinedMeshParent)
    {
      Destroy(combinedMeshParent);
    }
    // CleanupPreviousCombinedMeshes();

    combinedMeshParent = new GameObject
    {
      name = "ValheimVehicles_VehicleCombinedMesh",
      gameObject = { layer = LayerHelpers.CustomRaftLayer },
      transform = { parent = transform }
    };

    Dictionary<Material, List<CombineInstance>> materialToMeshes = new();
    var prefabExclusionRegex = GenerateRegexFromList(prefabNameExclusionList); // Compile Regex once
    var meshFilterExclusionRegex = GenerateRegexFromList(meshFilterExclusionList); // Compile Regex once
    var meshFilterIncludeRegex = GenerateRegexFromList(MeshFilterIncludesNames); // Compile Regex once

    foreach (var x in prefabs)
    {
      if (!x) continue; // Skip inactive
      if (prefabExclusionRegex.IsMatch(x.gameObject.name)) continue; // 🔹 Skip excluded objects

      var wnt = x.GetComponent<WearNTear>();
      List<MeshRenderer> selectedRenderers = new();

      if (wnt != null)
      {

        if (!wntSubscribers.Contains(wnt))
        {
          wntSubscribers.Add(wnt);
          wnt.m_onDestroyed += () => OnWearNTearPieceDestroy(wnt.gameObject);
        }
        // 🔹 WNT Exists: Get the active component & its MeshRenderer
        var activeWNTObject = GetWNTActiveComponent(wnt);
        if (activeWNTObject)
        {
          selectedRenderers.AddRange(activeWNTObject.GetComponentsInChildren<MeshRenderer>(true));
        }
      }

      // 🔹 Include non-WNT MeshRenderers that are not nested inside other WNT objects
      selectedRenderers.AddRange(GetValidNonNestedMeshRenderers(selectedRenderers, x.transform, wnt));

      var shouldContinueAdding = true;
      if (!hiddenMeshRenderersObjMap.TryGetValue(x, out var currentDeactivatedMeshes))
      {
        currentDeactivatedMeshes = new List<MeshRenderer>();
      }

      foreach (var renderer in selectedRenderers)
      {
        if (!renderer || renderer.sharedMaterial == null) continue;
        if (meshFilterExclusionRegex.IsMatch(renderer.gameObject.name)) continue;
        if (meshFilterIncludeRegex.IsMatch(renderer.gameObject.name))
        {
          shouldContinueAdding = false;
        }

        var fixedMaterial = renderer.sharedMaterial;
        if (!materialToMeshes.ContainsKey(fixedMaterial))
        {
          materialToMeshes[fixedMaterial] = new List<CombineInstance>();
        }

        var meshTransform = renderer.transform;

        // 🔹 Prevent floating-point errors in transform updates
        var fixedPosition = new Vector3(
          Mathf.Round(meshTransform.position.x * 1000f) / 1000f,
          Mathf.Round(meshTransform.position.y * 1000f) / 1000f,
          Mathf.Round(meshTransform.position.z * 1000f) / 1000f
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

        if (!shouldContinueAdding)
        {
          break;
        }
      }

      hiddenMeshRenderersObjMap[x] = currentDeactivatedMeshes;
    }

    foreach (var entry in materialToMeshes)
    {
      var material = entry.Key;
      var combineInstances = entry.Value;

      var combinedMesh = new Mesh
      {
        name = "ValheimVehicles_CombinedMesh_" + material.name,
        indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
      };

      combinedMesh.CombineMeshes(combineInstances.ToArray(), true);

      // todo aggregate meshes based on layer. or alternatively make these meshes all passthrough/non-collision and only deactivate the meshrenderer section
      var layer = 0;
      var meshObject = new GameObject($"CombinedMesh_{material.name}_layer_{layer}")
      {
        gameObject =
        {
          layer = transform.root.gameObject.layer == LayerHelpers.CustomRaftLayer ? LayerHelpers.CustomRaftLayer : 0
        }
      };
      meshObject.transform.SetParent(combinedMeshParent.transform);

      var meshFilter = meshObject.AddComponent<MeshFilter>();
      meshFilter.mesh = combinedMesh;

      var meshRenderer = meshObject.AddComponent<MeshRenderer>();

      // 🔹 Apply fixed material instance
      meshRenderer.sharedMaterial = material;

      var meshCollider = meshObject.AddComponent<MeshCollider>();
      meshCollider.sharedMesh = combinedMesh;
      meshCollider.convex = false;

      currentMeshObjects.Add(meshObject);
    }
  }
}