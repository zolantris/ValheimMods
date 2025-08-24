// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

#endregion

/// <summary>
/// Window: Window > PrefabThumbnailGenerator
/// Full batch + single-runner; writes PNGs first, then does ONE import at the end.
/// Importer settings are applied by an AssetPostprocessor for the output folder.
/// Sprite Packer is disabled during that import, then we pack ONCE at the end,
/// without touching atlas packables (non-invasive).
/// </summary>
public class PrefabThumbnailGenerator : EditorWindow
{
  private const int GuiWidth = 150;

  private const string PrefabGenScenePath = "Assets/ValheimVehicles/Scene/GeneratePrefabIcons.unity";

  // Public so the postprocessor can access it
  public static string OutputDirPath => outputDirPath.TrimEnd('/', '\\') + "/";

  // You can change this from the UI
  private static string outputDirPath = "Assets/ValheimVehicles/GeneratedIcons/"; // output dir

  private static GameObject sceneLight;

  public static string lastScenePath = "";

  [SerializeField] private List<string> excludeContainsPrefabNames = new()
  {
    "shared_", "steering_wheel", "rope_ladder", "dirt_floor", "dirtfloor_icon",
    "cannon_shoot_part", "chain_link", "rope_anchor", "rudder_basic",
    "custom_sail", "mechanism_swivel", "_old", "_test_variant", "tank_tread_icon",
    "vehicle_hammer", "_backup", "_deprecated"
  };

  [SerializeField] private List<string> excludeExactPrefabNames = new()
  {
    "shared_", "steering_wheel", "rope_ladder", "dirt_floor", "dirtfloor_icon",
    "rope_anchor", "rudder_basic", "custom_sail", "mechanism_swivel",
    "_old", "_test_variant", "tank_tread_icon", "vehicle_hammer"
  };

  public Object searchDirectory;
  public Object targetSpriteAtlas;
  public List<string> searchDirectoryPaths = new() { "Assets/ValheimVehicles/Prefabs/", "Assets/ValheimVehicles/Prefabs/hulls-v4" };
  public string targetSpriteAtlasPath = "Assets/ValheimVehicles/vehicle_icons.spriteatlasv2";

  private readonly List<GameObject> objList = new();

  // Collected during capture pass
  private readonly List<string> spritePaths = new(); // generated png paths
  private readonly List<string> pendingImportPaths = new(); // imported once at end

  private int height = 100;
  private int width = 100;
  private bool isRunning;

  private Camera previewCamera;

  // Camera config: Pitch (X), Yaw (Y). Roll locked to 0.
  [SerializeField] private float cameraPitchDegrees = 25f; // look downward
  [SerializeField] private float cameraYawDegrees = 15f; // look slightly from right

  // Ensure tiny assets fill the frame (0.5..0.98). Used by OBB-fit distance calc.
  [SerializeField] private float minFrameFill = 0.85f;

  // ===== NEW: Bounds controls to avoid AOE / ghost geometry =====
  [Header("Bounds Filtering")]
  [SerializeField] private bool useCollidersForBounds = false; // if true, use colliders instead of renderers
  [SerializeField] private bool ignoreTriggerColliders = true; // skip isTrigger colliders (AOE / ranges)
  [SerializeField] private bool rejectFarOutliers = true; // remove far-away children from bounds
  [SerializeField] private float outlierFactor = 3.0f; // how aggressively to drop far nodes (2–5)
  [SerializeField] private List<string> boundsExcludeContains = new() // names to ignore when computing bounds
  {
    "aoe", "explosion", "range", "gizmo"
  };

  // --- Serialized binding for lists ---
  private SerializedObject _so;
  private SerializedProperty _spExcludeContains;
  private SerializedProperty _spExcludeExact;
  private ReorderableList _rlExcludeContains;
  private ReorderableList _rlExcludeExact;
  private static GameObject _previewRoot;

  // Global sprite packer suppression (Unity 2022)
  private SpritePackerMode _prevPackerMode;
  private bool _packerToggled;

  private Vector2 _scroll;

  // Optional: repack atlas at the end (non-invasive)
  [SerializeField] private bool repackAtlasAtEnd = true;

  private GUIContent CaptureRunButtonText => new(isRunning ? "Generating icons...please wait" : "Generate New Sprite Icons");

  private void OnEnable()
  {
    _so = new SerializedObject(this);
    _spExcludeContains = _so.FindProperty("excludeContainsPrefabNames");
    _spExcludeExact = _so.FindProperty("excludeExactPrefabNames");

    _rlExcludeContains = BuildStringReorderableList(
      _spExcludeContains,
      "ExcludeContains PrefabNames",
      "Names that, if contained in a prefab name, will be skipped."
    );

    _rlExcludeExact = BuildStringReorderableList(
      _spExcludeExact,
      "ExcludeExact PrefabNames",
      "Names that, if exactly matching a prefab name, will be skipped."
    );
  }

  // Add this helper anywhere in the class
  private void EnsureExclusionUIBindings()
  {
    if (_so == null) _so = new SerializedObject(this);

    if (_spExcludeContains == null)
      _spExcludeContains = _so.FindProperty("excludeContainsPrefabNames");

    if (_spExcludeExact == null)
      _spExcludeExact = _so.FindProperty("excludeExactPrefabNames");

    if (_rlExcludeContains == null)
      _rlExcludeContains = BuildStringReorderableList(
        _spExcludeContains,
        "ExcludeContains PrefabNames",
        "Names that, if contained in a prefab name, will be skipped."
      );

    if (_rlExcludeExact == null)
      _rlExcludeExact = BuildStringReorderableList(
        _spExcludeExact,
        "ExcludeExact PrefabNames",
        "Names that, if exactly matching a prefab name, will be skipped."
      );
  }

  private static ReorderableList BuildStringReorderableList(SerializedProperty prop, string header, string tooltip)
  {
    var rl = new ReorderableList(prop.serializedObject, prop, true, true, true, true)
    {
      drawHeaderCallback = rect =>
      {
        EditorGUI.LabelField(rect, new GUIContent(header, tooltip));
      },
      elementHeight = EditorGUIUtility.singleLineHeight + 6
    };

    rl.drawElementCallback = (rect, index, active, focused) =>
    {
      var element = prop.GetArrayElementAtIndex(index);
      rect.y += 2;
      rect.height = EditorGUIUtility.singleLineHeight;
      element.stringValue = EditorGUI.TextField(rect, element.stringValue);
    };

    rl.onAddCallback = list =>
    {
      prop.arraySize++;
      var el = prop.GetArrayElementAtIndex(prop.arraySize - 1);
      el.stringValue = string.Empty;
    };

    return rl;
  }

  private Vector2 _scrollPos;
  [SerializeField] private GameObject testPrefab; // single-runner target
  [SerializeField] private bool deleteOutputOnFullRun = true;
  [SerializeField] private bool deleteOutputOnTestRun = false;

  private void OnGUI()
  {
    EnsureExclusionUIBindings(); // <— ensures lists are always bound
    _so.Update();
    var oldLabelWidth = EditorGUIUtility.labelWidth;
    EditorGUIUtility.labelWidth = GuiWidth;

    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

    using (new EditorGUILayout.HorizontalScope())
    {
      if (GUILayout.Button("Generate New Sprite Icons (Full Run)"))
      {
        isRunning = true;
        if (TrySwitchToPrefabGenerationScene())
          EditorApplication.delayCall += RunGenerationAfterSceneLoad;
        else
          isRunning = false;
      }

      if (GUILayout.Button("Test Run (1 Prefab)"))
      {
        isRunning = true;
        if (TrySwitchToPrefabGenerationScene())
          EditorApplication.delayCall += RunSingleAfterSceneLoad;
        else
          isRunning = false;
      }
    }

    using (new EditorGUILayout.HorizontalScope())
    {
      EditorGUILayout.LabelField($"Status: {(isRunning ? "Running" : "Idle")}");
    }
    EditorGUILayout.Space();

    using (new EditorGUILayout.VerticalScope("box"))
    {
      testPrefab = (GameObject)EditorGUILayout.ObjectField("Test Prefab (optional):", testPrefab, typeof(GameObject), false);
      deleteOutputOnFullRun = EditorGUILayout.Toggle("Delete Output (Full Run):", deleteOutputOnFullRun);
      deleteOutputOnTestRun = EditorGUILayout.Toggle("Delete Output (Test Run):", deleteOutputOnTestRun);
    }
    EditorGUILayout.Space();

    using (new EditorGUILayout.VerticalScope("box"))
    {
      cameraPitchDegrees = EditorGUILayout.Slider("Camera Pitch X (deg):", cameraPitchDegrees, -60f, 60f);
      cameraYawDegrees = EditorGUILayout.Slider("Camera Yaw Y (deg):", cameraYawDegrees, -180f, 180f);
      minFrameFill = EditorGUILayout.Slider("Min Frame Fill:", minFrameFill, 0.50f, 0.98f);
      EditorGUILayout.HelpBox("Pitch looks down/up. Yaw rotates around the prefab. Roll is locked to 0° (upright sprites). Min Frame Fill ensures small prefabs are not tiny dots.", MessageType.None);
    }
    EditorGUILayout.Space();

    using (new EditorGUILayout.VerticalScope("box"))
    {
      targetSpriteAtlas = (SpriteAtlas)EditorGUILayout.ObjectField("Sprite Atlas (pack only)", targetSpriteAtlas, typeof(SpriteAtlas), false);
      repackAtlasAtEnd = EditorGUILayout.Toggle("Repack Atlas At End (non-invasive)", repackAtlasAtEnd);
      searchDirectory = EditorGUILayout.ObjectField("Search Directory :", searchDirectory, typeof(Object), true);
      outputDirPath = EditorGUILayout.TextField("Save directory :", outputDirPath);
    }
    EditorGUILayout.Space();

    using (new EditorGUILayout.HorizontalScope())
    {
      width = EditorGUILayout.IntField("Width :", width);
      height = EditorGUILayout.IntField("Height :", height);
    }
    EditorGUILayout.Space();

    // ===== NEW: Bounds filtering UI =====
    using (new EditorGUILayout.VerticalScope("box"))
    {
      EditorGUILayout.LabelField("Bounds Filtering", EditorStyles.boldLabel);
      useCollidersForBounds = EditorGUILayout.Toggle("Use Colliders For Bounds", useCollidersForBounds);
      ignoreTriggerColliders = EditorGUILayout.Toggle("Ignore Trigger Colliders", ignoreTriggerColliders);
      rejectFarOutliers = EditorGUILayout.Toggle("Reject Far Outliers", rejectFarOutliers);
      outlierFactor = EditorGUILayout.Slider("Outlier Factor", outlierFactor, 1.5f, 6f);
      EditorGUILayout.HelpBox("Use colliders (non-trigger) to avoid huge AOEs/ranges; or keep renderer mode to use visible meshes. 'Reject Far Outliers' drops children that are far from the cluster.", MessageType.None);

      // simple list editor for boundsExcludeContains
      EditorGUILayout.LabelField("Bounds Exclude Contains (names)", EditorStyles.miniBoldLabel);
      for (var i = 0; i < boundsExcludeContains.Count; i++)
      {
        using (new EditorGUILayout.HorizontalScope())
        {
          boundsExcludeContains[i] = EditorGUILayout.TextField(boundsExcludeContains[i]);
          if (GUILayout.Button("-", GUILayout.Width(22)))
          {
            boundsExcludeContains.RemoveAt(i);
            i--;
          }
        }
      }
      if (GUILayout.Button("+ Add Term", GUILayout.Width(100)))
      {
        boundsExcludeContains.Add(string.Empty);
      }
    }
    EditorGUILayout.Space();

    _rlExcludeContains.DoLayoutList();
    EditorGUILayout.Space(6);
    _rlExcludeExact.DoLayoutList();
    EditorGUILayout.Space();

    EditorGUILayout.EndScrollView();
    EditorGUIUtility.labelWidth = oldLabelWidth;

    _so.ApplyModifiedProperties();
  }

  [MenuItem("Window/PrefabThumbnailGenerator")]
  private static void ShowWindow()
  {
    GetWindow(typeof(PrefabThumbnailGenerator));
  }

  private bool ShouldSkip(GameObject obj)
  {
    for (var i = 0; i < excludeContainsPrefabNames.Count; i++)
    {
      var s = excludeContainsPrefabNames[i];
      if (!string.IsNullOrEmpty(s) && obj.name.Contains(s)) return true;
    }
    for (var i = 0; i < excludeExactPrefabNames.Count; i++)
    {
      var s = excludeExactPrefabNames[i];
      if (!string.IsNullOrEmpty(s) && obj.name == s) return true;
    }
    return false;
  }

  private void CaptureSpecificPrefabs(IEnumerable<GameObject> prefabs, bool deleteOutputFirst)
  {
    AssetDatabase.DisallowAutoRefresh();
    try
    {
      spritePaths.Clear();
      pendingImportPaths.Clear();

      if (!Directory.Exists(OutputDirPath))
        Directory.CreateDirectory(OutputDirPath);

      if (deleteOutputFirst)
        DeleteAllPngsInOutputFolder();

      foreach (var obj in prefabs)
      {
        if (obj == null) continue;
        if (ShouldSkip(obj)) continue;

        try
        {
          Capture(obj); // write PNG + enqueue
        }
        catch (Exception e)
        {
          Debug.LogWarning($"Issue occurred while snapshotting {obj.name}: {e.Message}");
        }
      }

      // One single import at the end
      FinalizeCapturedSprites();
    }
    finally
    {
      AssetDatabase.AllowAutoRefresh();
      EditorUtility.UnloadUnusedAssetsImmediate();
      GC.Collect();

      if (sceneLight != null)
      {
        SafeDestroy(sceneLight);
        sceneLight = null;
      }
    }
  }

  /// <summary>
  /// Imports all pending PNGs in one batch.
  /// Disables the global Sprite Packer to avoid per-import packing,
  /// then restores it. Importer settings applied by AssetPostprocessor.
  /// </summary>
  private void FinalizeCapturedSprites()
  {
    if (pendingImportPaths.Count == 0) return;

    DisableSpritePackerForBatch();

    try
    {
      AssetDatabase.StartAssetEditing();
      AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

      // Sanity: ensure sprites exist
      foreach (var path in pendingImportPaths)
      {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
          Debug.LogWarning($"FinalizeCapturedSprites: Sprite missing after import for {path}");
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"FinalizeCapturedSprites error: {e.Message}");
    }
    finally
    {
      AssetDatabase.StopAssetEditing();
      RestoreSpritePacker();
      AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
  }

  private void DisableSpritePackerForBatch()
  {
    try
    {
      _prevPackerMode = EditorSettings.spritePackerMode;
      if (_prevPackerMode != SpritePackerMode.Disabled)
      {
        EditorSettings.spritePackerMode = SpritePackerMode.Disabled;
        _packerToggled = true;
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Could not disable SpritePacker: {e.Message}");
    }
  }

  private void RestoreSpritePacker()
  {
    try
    {
      if (_packerToggled)
      {
        EditorSettings.spritePackerMode = _prevPackerMode;
        _packerToggled = false;
      }
    }
    catch (Exception e)
    {
      Debug.LogWarning($"Could not restore SpritePacker: {e.Message}");
    }
  }

  private void CaptureTexturesForPrefabs()
  {
    var all = GetFilesFromSearchPath();
    CaptureSpecificPrefabs(all, deleteOutputOnFullRun);
  }

  private void RunSingleAfterSceneLoad()
  {
    EditorApplication.delayCall -= RunSingleAfterSceneLoad;

    var activeScene = SceneManager.GetActiveScene();
    if (!activeScene.path.EndsWith("GeneratePrefabIcons.unity"))
    {
      Debug.LogError("Scene not loaded as expected!");
      isRunning = false;
      return;
    }

    try
    {
      GameObject target = null;

      if (testPrefab != null)
      {
        target = testPrefab;
      }
      else
      {
        var list = GetFilesFromSearchPath();
        target = list.FirstOrDefault(go => go != null && !ShouldSkip(go));
        if (target == null)
        {
          Debug.LogWarning("Test Run: No suitable prefab found (all excluded or none discovered).");
        }
      }

      if (target != null)
      {
        CaptureSpecificPrefabs(new[] { target }, deleteOutputOnTestRun);
        RequestAtlasRepackNonInvasive(); // pack-only
      }
    }
    finally
    {
      isRunning = false;
    }

    try
    {
      if (!string.IsNullOrEmpty(lastScenePath))
        EditorSceneManager.OpenScene(lastScenePath, OpenSceneMode.Single);
    }
    catch
    {
      Debug.LogError($"Failed to open scene at {PrefabGenScenePath}");
    }
  }

  private void RunGenerationAfterSceneLoad()
  {
    EditorApplication.delayCall -= RunGenerationAfterSceneLoad;

    var activeScene = SceneManager.GetActiveScene();
    if (!activeScene.path.EndsWith("GeneratePrefabIcons.unity"))
    {
      Debug.LogError("Scene not loaded as expected!");
      isRunning = false;
      return;
    }

    try
    {
      CaptureTexturesForPrefabs();
      RequestAtlasRepackNonInvasive(); // pack-only
    }
    finally
    {
      isRunning = false;
    }

    try
    {
      if (lastScenePath != string.Empty)
      {
        EditorSceneManager.OpenScene(lastScenePath, OpenSceneMode.Single);
      }
    }
    catch (Exception)
    {
      Debug.LogError($"Failed to open scene at {PrefabGenScenePath}");
    }
  }

  private static bool TrySwitchToPrefabGenerationScene()
  {
    var activeScene = SceneManager.GetActiveScene();
    var isGenerationScene = activeScene.path.EndsWith("GeneratePrefabIcons.unity");
    lastScenePath = isGenerationScene ? "" : activeScene.path;

    if (!isGenerationScene && activeScene.isDirty)
    {
      if (!EditorSceneManager.SaveScene(activeScene))
      {
        Debug.LogError($"Failed to save active scene: {activeScene.path}");
        return false;
      }
    }

    var scene = EditorSceneManager.OpenScene(PrefabGenScenePath, OpenSceneMode.Single);
    if (!scene.IsValid())
    {
      Debug.LogError($"Failed to open scene at {PrefabGenScenePath}");
      return false;
    }
    return true;
  }

  private List<GameObject> GetFilesFromSearchPath()
  {
    objList.Clear();
    var replaceDirectoryPath = searchDirectory ? new List<string> { AssetDatabase.GetAssetPath(searchDirectory) } : searchDirectoryPaths;
    var filePaths = replaceDirectoryPath.SelectMany(x => Directory.GetFiles(x, "*.prefab"));

    List<GameObject> localList = new();
    foreach (var filePath in filePaths)
    {
      var obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject)) as GameObject;
      if (obj != null)
      {
        objList.Add(obj);
        localList.Add(obj);
      }
    }
    return localList;
  }

  // ===== Atlas: non-invasive repack only =====

  private void RequestAtlasRepackNonInvasive()
  {
    if (!repackAtlasAtEnd) return; // optional toggle

    var atlasPath = GetSpriteAtlasPath();
    var atlasObj = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
    if (!atlasObj)
    {
      Debug.LogWarning($"SpriteAtlas not found at {atlasPath}");
      return;
    }

    try
    {
      SpriteAtlasUtility.PackAtlases(new[] { atlasObj }, EditorUserBuildSettings.activeBuildTarget);
      Debug.Log("SpriteAtlas repacked (non-invasive).");
    }
    catch (Exception e)
    {
      Debug.LogWarning($"PackAtlases failed: {e.Message}");
    }
  }

  private string GetSpriteAtlasPath()
  {
    return targetSpriteAtlas ? AssetDatabase.GetAssetPath(targetSpriteAtlas) : targetSpriteAtlasPath;
  }

  private static void DeleteAllPngsInOutputFolder()
  {
    string[] folders = { OutputDirPath };
    foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", folders))
    {
      var path = AssetDatabase.GUIDToAssetPath(guid);
      if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
      {
        AssetDatabase.DeleteAsset(path);
      }
    }
  }

  // ===== Camera / capture =====

  private RenderTexture _rt;
  private const int RtDepth = 24;
  private const float CamFov = 35f;

  private static GameObject CreateOrGetPreviewRoot()
  {
    var active = SceneManager.GetActiveScene();

    if (_previewRoot != null)
    {
      if (_previewRoot.scene != active)
        SceneManager.MoveGameObjectToScene(_previewRoot, active);

      _previewRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
      return _previewRoot;
    }

    _previewRoot = new GameObject("__PreviewRoot");
    _previewRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

    SceneManager.MoveGameObjectToScene(_previewRoot, active);
    return _previewRoot;
  }

  private void Capture(GameObject prefab)
  {
    EnsurePreviewCamera(width, height);

    GameObject instance = null;
    Texture2D tex = null;
    var prevActive = RenderTexture.active;
    var prevTarget = previewCamera.targetTexture;

    try
    {
      var root = CreateOrGetPreviewRoot();

      // 1) Clone
      instance = Instantiate(prefab);
      instance.name = prefab.name + "_PreviewClone";
      instance.transform.SetParent(root.transform, false);
      instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
      instance.transform.position = Vector3.zero;
      instance.transform.rotation = Quaternion.identity;
      instance.transform.localScale = Vector3.one;

      // 2) Bounds & framing (NEW: robust bounds)
      var b = GetIconBounds(instance, useCollidersForBounds, ignoreTriggerColliders, rejectFarOutliers, outlierFactor, boundsExcludeContains);
      if (b.size == Vector3.zero)
      {
        // Fall back to renderer bounds if custom filtering found nothing
        b = GetRenderableBounds(instance);
        if (b.size == Vector3.zero)
        {
          Debug.LogWarning($"No visible bounds found on {prefab.name}, skipping.");
          return;
        }
      }

      var aspect = (float)width / Mathf.Max(1, height);

      // Camera orientation: pitch/yaw, roll locked to 0
      var camRot = Quaternion.Euler(cameraPitchDegrees, cameraYawDegrees, 0f);
      var forward = camRot * Vector3.forward;

      // Tight OBB-fit distance so the object fills at least 'minFrameFill' of the frame
      var distance = ComputeDistanceOBBFit(b, camRot, previewCamera.fieldOfView, aspect, minFrameFill) * 1.02f; // tiny guard band

      var camPos = b.center - forward * distance;
      previewCamera.transform.SetPositionAndRotation(camPos, camRot);

      // Near/Far clip safety
      var extMag = b.extents.magnitude;
      previewCamera.nearClipPlane = Mathf.Max(0.001f, distance - extMag * 2f);
      previewCamera.farClipPlane = Mathf.Max(previewCamera.farClipPlane, distance + extMag * 4f);

      // 3) Shared light
      if (sceneLight == null)
      {
        sceneLight = new GameObject("PreviewLight");
        sceneLight.transform.SetParent(root.transform, false);
        sceneLight.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        var light = sceneLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        light.color = Color.white;
        light.shadows = LightShadows.None;
        sceneLight.transform.rotation = Quaternion.Euler(50, -30, 0);
      }

      // 4) Render
      previewCamera.targetTexture = _rt;
      previewCamera.Render();

      // 5) Save PNG
      tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
      RenderTexture.active = _rt;
      tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
      tex.Apply();

      var texturePath = $"{OutputDirPath}{prefab.name}.png";
      File.WriteAllBytes(texturePath, tex.EncodeToPNG());

      spritePaths.Add(texturePath);
      pendingImportPaths.Add(texturePath);
    }
    finally
    {
      RenderTexture.active = prevActive;
      previewCamera.targetTexture = prevTarget;

      SafeDestroy(tex);
      SafeDestroy(instance);
    }
  }

  /// <summary>
  /// Compute camera distance so the camera-space AABB of the prefab fills the frame
  /// by at least 'minFill' in both axes (perspective camera).
  /// </summary>
  private static float ComputeDistanceOBBFit(Bounds worldBounds, Quaternion camRot, float fovDeg, float aspect, float minFill)
  {
    // World-space half-sizes of the AABB
    var e = worldBounds.extents;

    // Camera basis in world space
    var r = camRot * Vector3.right;
    var u = camRot * Vector3.up;
    var f = camRot * Vector3.forward;

    // Per-axis absolute dot products to project the AABB onto camera axes
    var ex = new Vector3(Mathf.Abs(Vector3.Dot(Vector3.right, r)),
      Mathf.Abs(Vector3.Dot(Vector3.up, r)),
      Mathf.Abs(Vector3.Dot(Vector3.forward, r)));
    var ey = new Vector3(Mathf.Abs(Vector3.Dot(Vector3.right, u)),
      Mathf.Abs(Vector3.Dot(Vector3.up, u)),
      Mathf.Abs(Vector3.Dot(Vector3.forward, u)));
    var ez = new Vector3(Mathf.Abs(Vector3.Dot(Vector3.right, f)),
      Mathf.Abs(Vector3.Dot(Vector3.up, f)),
      Mathf.Abs(Vector3.Dot(Vector3.forward, f)));

    // Camera-space half-sizes
    var halfX = ex.x * e.x + ex.y * e.y + ex.z * e.z; // horizontal
    var halfY = ey.x * e.x + ey.y * e.y + ey.z * e.z; // vertical
    var halfZ = ez.x * e.x + ez.y * e.y + ez.z * e.z; // depth (for near safety)

    // FOVs
    var fovV = fovDeg * Mathf.Deg2Rad;
    var tanV = Mathf.Tan(0.5f * fovV);
    var tanH = tanV * aspect;

    // Ensure the object occupies at least 'minFill' of the view
    minFill = Mathf.Clamp(minFill, 0.01f, 0.98f);
    var distX = halfX / (tanH * minFill);
    var distY = halfY / (tanV * minFill);

    // Choose the limiting axis and ensure we're in front of the geometry by a hair
    var dist = Mathf.Max(distX, distY);
    return Mathf.Max(dist, halfZ * 1.05f);
  }

  /// <summary>
  /// Robust bounds for icon: prefers colliders (optionally skipping triggers) or visible mesh renderers,
  /// ignores particle/VFX renderers, and drops far-out children by cluster distance.
  /// </summary>
  private static Bounds GetIconBounds(GameObject root, bool useColliders, bool skipTriggers, bool dropOutliers, float outlierMul, List<string> nameExcludes)
  {
    nameExcludes ??= new List<string>();
    var excludes = nameExcludes.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.ToLowerInvariant()).ToList();

    var boundsList = new List<Bounds>();
    var centers = new List<Vector3>();

    if (useColliders)
    {
      var cols = root.GetComponentsInChildren<Collider>(includeInactive: false);
      foreach (var c in cols)
      {
        if (skipTriggers && c.isTrigger) continue;
        if (!c.enabled) continue;
        if (IsNameExcluded(c.gameObject.name, excludes)) continue;

        // Only colliders with positive size
        var b = c.bounds;
        if (b.size == Vector3.zero) continue;

        boundsList.Add(b);
        centers.Add(b.center);
      }
    }
    else
    {
      // Mesh + Skinned renderers only; ignore particle/VFX, trail, line, etc.
      var mrs = root.GetComponentsInChildren<Renderer>(includeInactive: false);
      foreach (var r in mrs)
      {
        if (!r.enabled) continue;
        if (r is ParticleSystemRenderer) continue; // ignore VFX shells
        if (IsNameExcluded(r.gameObject.name, excludes)) continue;

        // Skip "editor only" layers if you use them; example (optional):
        // if (r.gameObject.layer == LayerMask.NameToLayer("EditorOnly")) continue;

        var b = r.bounds;
        if (b.size == Vector3.zero) continue;

        boundsList.Add(b);
        centers.Add(b.center);
      }
    }

    if (boundsList.Count == 0)
      return new Bounds(root.transform.position, Vector3.zero);

    // Optional outlier rejection (robust to one child far away)
    if (dropOutliers && boundsList.Count > 1)
    {
      var centroid = Vector3.zero;
      for (var i = 0; i < centers.Count; i++) centroid += centers[i];
      centroid /= centers.Count;

      // distances of centers from centroid
      var dists = centers.Select(c => (c - centroid).magnitude).ToArray();
      var median = QuickMedian(dists);
      var mad = QuickMedian(dists.Select(d => Mathf.Abs(d - median)).ToArray());
      var thresh = median + outlierMul * (mad <= 1e-5f ? 1f : mad);

      var kept = new List<Bounds>();
      for (var i = 0; i < boundsList.Count; i++)
      {
        if (dists[i] <= thresh) kept.Add(boundsList[i]);
      }
      if (kept.Count > 0) boundsList = kept;
    }

    // Merge remaining bounds
    var merged = boundsList[0];
    for (var i = 1; i < boundsList.Count; i++) merged.Encapsulate(boundsList[i]);
    return merged;
  }

  private static bool IsNameExcluded(string name, List<string> excludes)
  {
    if (excludes == null || excludes.Count == 0) return false;
    var lower = name.ToLowerInvariant();
    for (var i = 0; i < excludes.Count; i++)
    {
      var t = excludes[i];
      if (string.IsNullOrEmpty(t)) continue;
      if (lower.Contains(t)) return true;
    }
    return false;
  }

  private static float QuickMedian(IList<float> arr)
  {
    if (arr == null || arr.Count == 0) return 0f;
    var tmp = new List<float>(arr);
    tmp.Sort();
    var n = tmp.Count;
    if (n % 2 == 1) return tmp[n / 2];
    return 0.5f * (tmp[n / 2 - 1] + tmp[n / 2]);
  }

  /// <summary>
  /// Legacy simple renderer-bounds (kept as fallback).
  /// </summary>
  private static Bounds GetRenderableBounds(GameObject root)
  {
    var renderers = root.GetComponentsInChildren<Renderer>();
    if (renderers == null || renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);

    var b = renderers[0].bounds;
    for (var i = 1; i < renderers.Length; i++)
      b.Encapsulate(renderers[i].bounds);
    return b;
  }

  private void OnDisable()
  {
    if (_rt != null)
    {
      _rt.Release();
      _rt = null;
    }

    if (_previewRoot != null)
    {
      SafeDestroy(_previewRoot);
      _previewRoot = null;
    }
    sceneLight = null;
  }

  private void EnsurePreviewCamera(int w, int h)
  {
    var root = CreateOrGetPreviewRoot();

    if (previewCamera == null)
    {
      var go = new GameObject("PreviewCamera");
      go.transform.SetParent(root.transform, false);
      go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

      previewCamera = go.AddComponent<Camera>();
      previewCamera.clearFlags = CameraClearFlags.SolidColor;
      previewCamera.backgroundColor = new Color(0, 0, 0, 0);
      previewCamera.orthographic = false;
      previewCamera.fieldOfView = CamFov;
      previewCamera.nearClipPlane = 0.01f;
      previewCamera.farClipPlane = 1000f;
      previewCamera.enabled = false; // manual renders only
    }

    // (Re)make RT if size changed
    if (_rt == null || _rt.width != w || _rt.height != h)
    {
      if (_rt != null) _rt.Release();
      _rt = new RenderTexture(w, h, RtDepth, RenderTextureFormat.ARGB32)
      {
        antiAliasing = 4
      };
      _rt.Create();
    }
  }

  private static void SafeDestroy(Object obj)
  {
    if (obj == null) return;
    DestroyImmediate(obj);
  }

  [UsedImplicitly]
  private void CaptureWithCustomCamera(GameObject obj)
  {
    // Legacy helper; unused in the new pipeline
  }
}

/// <summary>
/// Applies sprite importer settings automatically for icons generated under OutputDirPath.
/// This guarantees correct settings without per-file SaveAndReimport spam.
/// </summary>
public class PrefabIconImportPostprocessor : AssetPostprocessor
{
  private void OnPreprocessTexture()
  {
    if (!(assetImporter is TextureImporter ti)) return;

    var outDir = PrefabThumbnailGenerator.OutputDirPath;
    if (string.IsNullOrEmpty(outDir)) return;

    // Normalize separators and compare prefix
    var path = assetPath.Replace('\\', '/');
    var dir = outDir.Replace('\\', '/');

    if (!path.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
      return;

    ti.textureType = TextureImporterType.Sprite;
    ti.spriteImportMode = SpriteImportMode.Single;
    ti.alphaIsTransparency = true;
    ti.textureCompression = TextureImporterCompression.Uncompressed;
    ti.mipmapEnabled = false;
    ti.sRGBTexture = true;
    ti.npotScale = TextureImporterNPOTScale.None;
  }
}