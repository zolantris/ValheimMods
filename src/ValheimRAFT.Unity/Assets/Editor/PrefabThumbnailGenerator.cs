#region

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

#endregion

/// <summary>
/// Add this Class to the Assets/Editor folder in Unity project
/// Find the component within Window > PrefabThumbnailGenerator
/// </summary>
public class PrefabThumbnailGenerator : EditorWindow
{

  private const int GuiWidth = 150;
  private static string outputDirPath = "Assets/ValheimVehicles/GeneratedIcons/"; // output dir

  private static GameObject sceneLight;

  [FormerlySerializedAs("excludedContainsPrefabNames")] public List<string> excludeContainsPrefabNames = new()
    { "shared_", "steering_wheel", "rope_ladder", "dirt_floor", "dirtfloor_icon", "rope_anchor", "keel", "rudder_basic", "custom_sail", "mechanism_swivel", "_old", "_test_variant", "tank_tread_icon", "vehicle_hammer", "_backup", "_deprecated" };
  public List<string> excludeExactPrefabNames = new()
    { "shared_", "steering_wheel", "rope_ladder", "dirt_floor", "dirtfloor_icon", "rope_anchor", "keel", "rudder_basic", "custom_sail", "mechanism_swivel", "_old", "_test_variant", "tank_tread_icon", "vehicle_hammer" };

  public Object searchDirectory;
  public Object targetSpriteAtlas;
  public string searchDirectoryPath = "Assets/ValheimVehicles/Prefabs/";
  public string targetSpriteAtlasPath = "Assets/ValheimVehicles/vehicle_icons.spriteatlasv2";
  private readonly List<GameObject> objList = new();

  private readonly List<string> spritePaths = new();
  private int height = 100; // image height

  private bool isRunning;

  private Vector3 lastPosition = Vector3.zero;
  private Object outputDirObj;

  private Camera previewCamera;
  private int width = 100; // image width

  private GUIContent CaptureRunButtonText => new(isRunning ? "Generating icons...please wait" : "Generate New Sprite Icons");
  private void OnGUI()
  {
    var runCaptureGuiContent = CaptureRunButtonText;
    if (GUILayout.Button(runCaptureGuiContent))
    {
      isRunning = true;
      try
      {
        CaptureTexturesForPrefabs();
      }
      catch
      {
        isRunning = false;
      }

      isRunning = false;
    }
    
    GUILayout.BeginHorizontal();
    var dynamicStatus = isRunning ? "Running" : "Idle";
    var status =$"Status: {dynamicStatus}";
    GUILayout.Label(status, GUILayout.Width(GuiWidth));
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();
    
    GUILayout.BeginHorizontal();
    GUILayout.Label("Sprite Atlas To Pack", GUILayout.Width(GuiWidth));
    targetSpriteAtlas = EditorGUILayout.ObjectField(targetSpriteAtlas, typeof(SpriteAtlas), false);
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();

    GUILayout.BeginHorizontal();
    GUILayout.Label("Search Directory : ", GUILayout.Width(GuiWidth));
    searchDirectory = EditorGUILayout.ObjectField(searchDirectory, typeof(Object), true);
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();

    GUILayout.BeginHorizontal();
    GUILayout.Label("Save directory : ", GUILayout.Width(GuiWidth));
    outputDirPath = EditorGUILayout.TextField(outputDirPath);
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();

    GUILayout.BeginHorizontal();
    GUILayout.Label("Width : ", GUILayout.Width(GuiWidth));
    width = EditorGUILayout.IntField(width);
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();

    GUILayout.BeginHorizontal();
    GUILayout.Label("Height : ", GUILayout.Width(GuiWidth));
    height = EditorGUILayout.IntField(height);
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();
    
    
    GUILayout.BeginHorizontal();
    GUILayout.Label("ExcludeContains PrefabNames", GUILayout.Width(GuiWidth));
    GUILayout.TextArea(string.Join( ",\n", excludeContainsPrefabNames));
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();
    
    GUILayout.BeginHorizontal();
    GUILayout.Label("ExcludeExact PrefabNames", GUILayout.Width(GuiWidth));
    GUILayout.TextArea(string.Join( ",\n", excludeExactPrefabNames));
    GUILayout.EndHorizontal();
    EditorGUILayout.Space();
  }

  [MenuItem("Window/PrefabThumbnailGenerator")]
  private static void ShowWindow()
  {
    GetWindow(typeof(PrefabThumbnailGenerator));
  }

  private List<GameObject> GetFilesFromSearchPath()
  {
    objList.Clear();
    var replaceDirectoryPath = searchDirectory ? AssetDatabase.GetAssetPath(searchDirectory) : searchDirectoryPath;
    var filePaths = Directory.GetFiles(replaceDirectoryPath, "*.*");
    List<GameObject> localList = new();
    foreach (var filePath in filePaths)
    {
      var obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject)) as GameObject;
      var isTruthy = obj != null;
      Debug.Log($"filePath {filePath}, obj {obj} isTruthy{isTruthy}");
      if (obj != null)
      {
        objList.Add(obj);
        localList.Add(obj);
      }
    }

    Debug.Log($"localList count {localList.Count}, objList count {objList.Count}");
    return localList;
  }

  private void CaptureTexturesForPrefabs()
  {
    AssetDatabase.DisallowAutoRefresh();
    spritePaths.Clear();

    if (!Directory.Exists(outputDirPath))
    {
      Directory.CreateDirectory(outputDirPath);
    }

    var tempObjList = GetFilesFromSearchPath();
    DeleteAllFilesInOutputFolder();
    // DeleteCurrentSpritesInTargetAtlas();

    // for (int i = 0; i < tempObjList.Count; i++)
    // {
    //     var tmpObj = tempObjList[i];
    //     Debug.Log($"TEMP OBJ ITEM, {tmpObj}");
    //     // Capture(tmpObj);
    // }

    lastPosition = Vector3.zero;

    foreach (var obj in tempObjList.ToArray())
    {
      Debug.Log("OBJ :  " + obj.name);

      // todo this all could be a regexp.
      
      var shouldExit = false;
      foreach (var excludedName in excludeContainsPrefabNames)
      {
        if (obj.name.Contains(excludedName))
        {
          shouldExit = true;
          break;
        }
      }
      
      foreach (var excludedName in excludeExactPrefabNames)
      {
        if (obj.name == excludedName)
        {
          shouldExit = true;
          break;
        }
      }
      
      if (shouldExit) continue;
      try
      {
        Capture(obj);
      }
      catch (Exception)
      {
        Debug.LogWarning($"Issue occurred while snapshotting {obj.name}");
      }
    }
    AssetDatabase.AllowAutoRefresh();

    // AddAllIconsToSpriteAtlas();
    // UpdateSpriteAtlas();
    if (sceneLight != null)
    {

      DestroyImmediate(sceneLight);
      sceneLight = null;
    }
  }

  private void DeleteCurrentSpritesInTargetAtlas()
  {
    var spriteAtlasPath = GetSpriteAtlasPath();
    var spriteAtlas = SpriteAtlasAsset.Load(spriteAtlasPath);
    var spriteAtlasObj = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteAtlasPath);
    if (!spriteAtlasObj) return;

    var currentSprites = spriteAtlasObj.GetPackables();
    spriteAtlas.Remove(currentSprites);
    SpriteAtlasAsset.Save(spriteAtlas, spriteAtlasPath);
  }

  private string GetSpriteAtlasPath()
  {
    return targetSpriteAtlas ? AssetDatabase.GetAssetPath(targetSpriteAtlas) : targetSpriteAtlasPath;
  }

  // must use the AssetDatabase get the current sprites and nuke them
  private void UpdateSpriteAtlas()
  {
    AssetDatabase.Refresh();
    Debug.Log($"AssetPath: {spritePaths.Count}");
    var spriteAtlasPath = GetSpriteAtlasPath();
    var spriteAtlas = SpriteAtlasAsset.Load(spriteAtlasPath);

    if (targetSpriteAtlasPath == null) return;

    var spritesList = new List<Object>();

    if (!spriteAtlas)
    {
      Debug.LogWarning("No sprite atlas");
      return;
    }

    foreach (var spritePath in spritePaths)
    {
      var obj = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
      if (obj == null) continue;
      spritesList.Add(obj);
    }

    Debug.Log($"Called UpdateSpriteAtlas {spritesList.Count}");

    spriteAtlas.Add(spritesList.ToArray());
    SpriteAtlasAsset.Save(spriteAtlas, GetSpriteAtlasPath());
    AssetDatabase.Refresh();
  }

  private void AddAllIconsToSpriteAtlas()
  {
    var iconAssets = AssetDatabase.FindAssets("", new[] { "Assets/ValheimVehicles/Icons" });
    foreach (var assetGuid in iconAssets)
    {
      var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
      spritePaths.Add(assetPath);
    }
  }

  private static void DeleteAllFilesInOutputFolder()
  {
    string[] trashFolders = { outputDirPath };
    foreach (var asset in AssetDatabase.FindAssets("", trashFolders))
    {
      var path = AssetDatabase.GUIDToAssetPath(asset);
      if (path.Contains(".png"))
      {
        AssetDatabase.DeleteAsset(path);
      }
    }
  }

  private static void WriteTextureToFile(Texture textureToRead, string texturePath)
  {
    var readableTexture = new Texture2D(textureToRead.width, textureToRead.height);
    var renderTexture = RenderTexture.GetTemporary(textureToRead.width, textureToRead.height);

    Graphics.Blit(textureToRead, renderTexture);

    var previous = RenderTexture.active;
    RenderTexture.active = renderTexture;

    readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
    readableTexture.Apply();

    RenderTexture.active = previous;
    RenderTexture.ReleaseTemporary(renderTexture);

    // Encode to PNG
    var bytes = readableTexture.EncodeToPNG();
    File.WriteAllBytes(texturePath, bytes);
    // DestroyImmediate(readableTexture);
    // DestroyImmediate(renderTexture);
  }


  /// <summary>
  /// Uses RuntimePreviewGenerator to generate assets, does not require injecting in game. Can be run in editor mode
  /// </summary>
  ///
  /// Previously used AssetPreview.GetAssetPreview but this did not allow for transparency so ShippedIcons that were using the default gray background
  /// <param name="obj"></param>
  private void Capture(GameObject obj)
  {
    Debug.Log($"called capture for obj {obj.name}");

    // Create a new directional light in the scene
    if (sceneLight == null)
    {
      sceneLight = new GameObject("PreviewLight");
    }
    var light = sceneLight.GetComponent<Light>();
    if (light == null)
      light = sceneLight.AddComponent<Light>();

    lastPosition += Vector3.right * 10;

    light.type = LightType.Directional;
    light.intensity = 1.0f; // You can tweak the intensity
    light.color = Color.white; // White light
    light.transform.rotation = Quaternion.Euler(50, -30, 0); // Set the light angle
    light.shadows = LightShadows.None; // Optional, disable shadows for the preview

    // Generate the preview
    RuntimePreviewGenerator.BackgroundColor = new Color(1, 1, 1, 0); // White background with transparency
    // RuntimePreviewGenerator.BackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0); // Light gray

    var objClone = Instantiate(obj, lastPosition, Quaternion.identity);
    var pg = RuntimePreviewGenerator.GenerateModelPreview(objClone.transform, width, height);
    var texturePath = $"{outputDirPath}{obj.name}.png";
    WriteTextureToFile(pg, texturePath);

    AssetDatabase.ImportAsset(texturePath);
    var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
    if (importer != null)
    {
      importer.textureType = TextureImporterType.Sprite;
      importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
    else
    {
      Debug.LogWarning("No importer, this will result in the GeneratedIcons not being written as Sprites");
    }

    AssetDatabase.WriteImportSettingsIfDirty(texturePath);
    DestroyImmediate(pg);
    DestroyImmediate(objClone);
  }

  private void SetupPreviewCamera()
  {
    // Create a new camera for preview generation
    var cameraGO = new GameObject("PreviewCamera");
    previewCamera = cameraGO.AddComponent<Camera>();
    previewCamera.orthographic = true; // Optional: Use orthographic projection for a flat view
    previewCamera.clearFlags = CameraClearFlags.SolidColor;
    previewCamera.backgroundColor = Color.white; // Background color (optional, can be transparent)
    previewCamera.transform.position = new Vector3(0, 1, -5); // Position the camera
    previewCamera.transform.LookAt(Vector3.zero); // Ensure it looks at the object

    // Set up the camera's exposure settings (if you're using post-processing)
    // previewCamera.exposure = 0.5f; // Adjust as necessary (if you're using post-processing exposure settings)
  }

  [UsedImplicitly]
  private void CaptureWithCustomCamera(GameObject obj)
  {
    // Setup the preview camera
    SetupPreviewCamera();

    // Render the object with the custom camera settings
    var renderTexture = new RenderTexture(width, height, 24);
    previewCamera.targetTexture = renderTexture;
    previewCamera.Render();

    // Capture the texture from the render texture
    var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
    RenderTexture.active = renderTexture;
    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    texture.Apply();

    // Save the texture as a PNG
    var texturePath = $"{outputDirPath}{obj.name}.png";
    var bytes = texture.EncodeToPNG();
    File.WriteAllBytes(texturePath, bytes);

    // Clean up
    RenderTexture.active = null;
    DestroyImmediate(previewCamera.gameObject);
    DestroyImmediate(renderTexture);
    DestroyImmediate(texture);

    if (sceneLight != null)
    {

      DestroyImmediate(sceneLight);
      sceneLight = null;
    }
  }
}