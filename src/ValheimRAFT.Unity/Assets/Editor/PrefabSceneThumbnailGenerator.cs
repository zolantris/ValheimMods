#region

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#endregion

namespace ValheimVehicles.scripts
{
    public class PrefabSceneThumbnailGenerator : MonoBehaviour
    {
        static readonly string outputDirPath = "Assets/ValheimVehicles/GeneratedIcons/"; // output dir

        private static readonly List<string> excludedNames = new()
            { "shared_", "steering_wheel", "rope_ladder", "dirt_floor", "dirtfloor_icon", "rope_anchor", "keel", "rudder_basic", "custom_sail" };
        public Object searchDirectory;
        public Object targetSpriteAtlas;
        public string searchDirectoryPath = "Assets/ValheimVehicles/Prefabs/";
        public string targetSpriteAtlasPath = "Assets/ValheimVehicles/vehicle_icons.spriteatlasv2";
        readonly int height = 100; // image height

        private bool isRunning;
        private readonly List<GameObject> objList = new();
        private Object outputDirObj;

        private readonly List<string> spritePaths = new();
        readonly int width = 100; // image width

        private GUIContent CaptureRunButtonText =>
            new GUIContent(isRunning ? "Generating...please wait" : "Generated Sprite Icons");
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
        }

        private void GetFilesFromSearchPath()
        {
            objList.Clear();
            var replaceDirectoryPath = searchDirectory ? AssetDatabase.GetAssetPath(searchDirectory) : searchDirectoryPath;
            var filePaths = Directory.GetFiles(replaceDirectoryPath, "*.*");
            foreach (var filePath in filePaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject)) as GameObject;
                if (obj != null)
                {
                    objList.Add(obj);
                }
            }
        }

        private void CaptureTexturesForPrefabs()
        {
            spritePaths.Clear();
            
            if (!Directory.Exists(outputDirPath))
            {
                Directory.CreateDirectory(outputDirPath);
            }

            GetFilesFromSearchPath();
            DeleteAllFilesInOutputFolder();
            // DeleteCurrentSpritesInTargetAtlas();
            
            foreach (var obj in objList)
            {
                var shouldExit = false;
                foreach (var excludedName in excludedNames)
                {
                    if (obj.name.Contains(excludedName))
                    {
                        shouldExit = true;
                        break;
                    }
                }
                if (shouldExit) continue;
                Capture(obj);
            }
            
            // AddAllIconsToSpriteAtlas();
            // UpdateSpriteAtlas();
        }

        private void DeleteCurrentSpritesInTargetAtlas()
        {
            // var spriteAtlasPath = GetSpriteAtlasPath();
            // var spriteAtlas = SpriteAtlasAsset.Load(spriteAtlasPath);
            // var spriteAtlasObj = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteAtlasPath);
            // if (!spriteAtlasObj) return;
            //
            // var currentSprites = spriteAtlasObj.GetPackables();
            // spriteAtlas.Remove(currentSprites);
            // SpriteAtlasAsset.Save(spriteAtlas,spriteAtlasPath);
        }

        private string GetSpriteAtlasPath()
        {
            return targetSpriteAtlas ? AssetDatabase.GetAssetPath(targetSpriteAtlas) : targetSpriteAtlasPath;
        }

        // must use the AssetDatabase get the current sprites and nuke them
        private void UpdateSpriteAtlas()
        {
            // AssetDatabase.Refresh();
            // Debug.Log($"AssetPath: {spritePaths.Count}");
            // var spriteAtlasPath = GetSpriteAtlasPath();
            // var spriteAtlas = SpriteAtlasAsset.Load(spriteAtlasPath);
            //
            // if (targetSpriteAtlasPath == null) return;
            //
            // var spritesList = new List<Object>();
            //
            // if (!spriteAtlas)
            // {
            //     Debug.LogWarning("No sprite atlas");
            //     return;
            // }
            //
            // foreach (var spritePath in spritePaths)
            // {
            //     var obj = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            //     if (obj == null) continue;
            //     spritesList.Add(obj);
            // }
            //
            // Debug.Log($"Called UpdateSpriteAtlas {spritesList.Count}");
            //
            // spriteAtlas.Add(spritesList.ToArray());
            // SpriteAtlasAsset.Save(spriteAtlas, GetSpriteAtlasPath());
            // AssetDatabase.Refresh();
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
            string[] trashFolders = {outputDirPath};
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
            // Uses alpha to make transparent, black is good for edges of object that are not perfectly cut
            RuntimePreviewGenerator.BackgroundColor = new Color(0,0,0,0);
            var pg = RuntimePreviewGenerator.GenerateModelPreview(obj.transform, width, height);
            var texturePath = $"{outputDirPath}{obj.name}.png";
            WriteTextureToFile(pg, texturePath);
            
            // AssetDatabase.Refresh();
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
            
            spritePaths.Add(texturePath);
        }
    }
}
