using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditor.U2D;
using UnityEngine.U2D;

/// <summary>
/// Add this Class to the Assets/Editor folder in Unity project
/// Find the component within Window > PrefabThumbnailGenerator
/// </summary>
public class PrefabThumbnailGenerator : EditorWindow
{
    public Object searchDirectory;
    public Object targetSpriteAtlas;
    List<GameObject> objList = new List<GameObject>();
    private Object dirPathObj;
    static string dirPath = "Assets/ValheimVehicles/GeneratedIcons/"; // output dir
    int width = 100; // image width
    int height = 100; // image height

    private List<string> spritePaths = new();
 
    [MenuItem("Window/PrefabThumbnailGenerator")]
    static void ShowWindow()
    {
        GetWindow(typeof(PrefabThumbnailGenerator));
    }
 
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Sprite Atlas To Pack", GUILayout.Width(110));
        targetSpriteAtlas = EditorGUILayout.ObjectField(targetSpriteAtlas, typeof(SpriteAtlas), false);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search Directory : ", GUILayout.Width(110));
        searchDirectory = EditorGUILayout.ObjectField(searchDirectory, typeof(UnityEngine.Object), true);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
    
        GUILayout.BeginHorizontal();
        GUILayout.Label("Save directory : ", GUILayout.Width(110));
        dirPath = EditorGUILayout.TextField(dirPath);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
    
        GUILayout.BeginHorizontal();
        GUILayout.Label("Width : ", GUILayout.Width(110));
        width = EditorGUILayout.IntField(width);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
    
        GUILayout.BeginHorizontal();
        GUILayout.Label("Height : ", GUILayout.Width(110));
        height = EditorGUILayout.IntField(height);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
    
        if (GUILayout.Button(new GUIContent("Generated Sprite Icons")))
        {
            if (!targetSpriteAtlas) return;
            if (!searchDirectory) return;
            CaptureTexturesForPrefabs();
        }
    }

    private void CaptureTexturesForPrefabs()
    {
        spritePaths.Clear();
        if (searchDirectory == null) return;
    
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    
        objList.Clear();
    
        var replaceDirectoryPath = AssetDatabase.GetAssetPath(searchDirectory);
        var filePaths = Directory.GetFiles(replaceDirectoryPath, "*.*");
        foreach (var filePath in filePaths)
        {
            var obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject)) as GameObject;
            if (obj != null)
            {
                objList.Add(obj);
            }
        }
    
        foreach (var obj in objList)
        {
            Debug.Log("OBJ :  " + obj.name);
            
            // skips shared assets that are not meant for exporting
            if (obj.name.StartsWith("shared_")) continue;
            Capture(obj);
        }
        
        UpdateSpriteAtlas();
    }
    
    private void UpdateSpriteAtlas()
    {
        Debug.Log($"AssetPath: {spritePaths.Count}");

        // var spriteAssets = new List<Object>();
        var targetSpriteAtlasPath = AssetDatabase.GetAssetPath(targetSpriteAtlas);

        if (targetSpriteAtlasPath == null) return;

        var spriteAtlas = SpriteAtlasAsset.Load(targetSpriteAtlasPath);
        var spritesList = new List<Object>();

        var packables = spriteAtlas.GetMasterAtlas()?.GetPackables() ?? new Object[]{};
       
        spriteAtlas.Remove(packables);
        
        foreach (var spritePath in spritePaths)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (obj == null) continue;
            spritesList.Add(obj);
        }
        Debug.Log($"Called UpdateSpriteAtlas {spritesList.Count}");

        spriteAtlas.Add(spritesList.ToArray());
        SpriteAtlasAsset.Save(spriteAtlas, targetSpriteAtlasPath);
        AssetDatabase.Refresh();
    }
    
    static void DeleteAllFilesInFolder()
    {
        string[] trashFolders = {dirPath};
        foreach (var asset in AssetDatabase.FindAssets("", trashFolders))
        {
            var path = AssetDatabase.GUIDToAssetPath(asset);
            if (path.Contains(".png"))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private void Capture(GameObject obj)
    {
        var thumbnail = AssetPreview.GetAssetPreview(obj);
        if (thumbnail == null) return;
        // Create a new readable texture and set its pixels from the existing texture
        var readableTexture = new Texture2D(thumbnail.width, thumbnail.height);
        var rt = RenderTexture.GetTemporary(thumbnail.width, thumbnail.height);
       
        Graphics.Blit(thumbnail, rt);
        
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
 
        // Encode to PNG
        var bytes = readableTexture.EncodeToPNG();

        var fileName = $"{dirPath}{obj.name}";
        var texturePath = $"{fileName}.png";
        
        // sprite.texture.
        File.WriteAllBytes(texturePath, bytes);
        
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(texturePath);
        
        var importer = AssetImporter.GetAtPath(texturePath)as TextureImporter;
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

        spritePaths.Add(texturePath);
        // Optionally, destroy the copy to free memory
        DestroyImmediate(readableTexture);
    }
}
