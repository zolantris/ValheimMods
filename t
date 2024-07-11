[1mdiff --git a/libs b/libs[m
[1m--- a/libs[m
[1m+++ b/libs[m
[36m@@ -1 +1 @@[m
[31m-Subproject commit b6ad4e3b849c677ee324fcdcea9e14687b3857e8[m
[32m+[m[32mSubproject commit b6ad4e3b849c677ee324fcdcea9e14687b3857e8-dirty[m
[1mdiff --git a/src/ValheimRAFT/Thunderstore/manifest.json b/src/ValheimRAFT/Thunderstore/manifest.json[m
[1mindex bff1d8b..08ddf47 100644[m
[1m--- a/src/ValheimRAFT/Thunderstore/manifest.json[m
[1m+++ b/src/ValheimRAFT/Thunderstore/manifest.json[m
[36m@@ -1,6 +1,6 @@[m
 ﻿{[m
   "name": "ValheimRAFT",[m
[31m-  "version_number": "2.2.1",[m
[32m+[m[32m  "version_number": "2.2.2",[m
   "website_url": "https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT",[m
   "description": "ValheimRAFT - a water vehicles mod for valheim. V2 has new hulls,walls,slabs.",[m
   "dependencies": [[m
[1mdiff --git a/src/ValheimRAFT/ValheimRAFT.csproj b/src/ValheimRAFT/ValheimRAFT.csproj[m
[1mindex c6c4769..5458c9b 100644[m
[1m--- a/src/ValheimRAFT/ValheimRAFT.csproj[m
[1m+++ b/src/ValheimRAFT/ValheimRAFT.csproj[m
[36m@@ -10,7 +10,7 @@[m
         <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>[m
         <Platforms>AnyCPU</Platforms>[m
         <PlatformTarget>AnyCPU</PlatformTarget>[m
[31m-        <ApplicationVersion>2.2.1</ApplicationVersion>[m
[32m+[m[32m        <ApplicationVersion>2.2.2</ApplicationVersion>[m
         <FileAlignment>512</FileAlignment>[m
         <RootNamespace>ValheimRAFT</RootNamespace>[m
         <LangVersion>latest</LangVersion>[m
[1mdiff --git a/src/ValheimRAFT/ValheimRAFT/ValheimRaftPlugin.cs b/src/ValheimRAFT/ValheimRAFT/ValheimRaftPlugin.cs[m
[1mindex d2e1218..b1da109 100644[m
[1m--- a/src/ValheimRAFT/ValheimRAFT/ValheimRaftPlugin.cs[m
[1m+++ b/src/ValheimRAFT/ValheimRAFT/ValheimRaftPlugin.cs[m
[36m@@ -38,7 +38,7 @@[m [mpublic class ValheimRaftPlugin : BaseUnityPlugin[m
 {[m
   // ReSharper disable MemberCanBePrivate.Global[m
   public const string Author = "zolantris";[m
[31m-  public const string Version = "2.2.1";[m
[32m+[m[32m  public const string Version = "2.2.2";[m
   public const string ModName = "ValheimRAFT";[m
   public const string ModGuid = $"{Author}.{ModName}";[m
   public const string HarmonyGuid = $"{Author}.{ModName}";[m
[1mdiff --git a/src/ValheimVehicles/ValheimVehicles.Prefabs/LoadValheimVehiclesAssets.cs b/src/ValheimVehicles/ValheimVehicles.Prefabs/LoadValheimVehiclesAssets.cs[m
[1mindex 64e3c5d..9e5613a 100644[m
[1m--- a/src/ValheimVehicles/ValheimVehicles.Prefabs/LoadValheimVehiclesAssets.cs[m
[1m+++ b/src/ValheimVehicles/ValheimVehicles.Prefabs/LoadValheimVehiclesAssets.cs[m
[36m@@ -130,7 +130,8 @@[m [mpublic class LoadValheimVehicleAssets : ILoadAssets[m
     ShipNautilus = assetBundle.LoadAsset<GameObject>("nautilus.prefab");[m
 [m
     SteeringWheel = assetBundle.LoadAsset<GameObject>("steering_wheel.prefab");[m
[31m-    PieceShader = assetBundle.LoadAsset<Shader>("Custom_Piece.shader");[m
[32m+[m
[32m+[m
     ShipKeelAsset = assetBundle.LoadAsset<GameObject>("keel");[m
     VehicleSwitchAsset = assetBundle.LoadAsset<GameObject>("mechanical_lever_switch");[m
     VehicleShipAsset =[m
[36m@@ -202,6 +203,9 @@[m [mpublic class LoadValheimVehicleAssets : ILoadAssets[m
     ;[m
     RamBladeLeft = assetBundle.LoadAsset<GameObject>([m
       "ram_blade_left.prefab");[m
[31m-    ;[m
[32m+[m
[32m+[m[32m    // comes from shared bundle[m
[32m+[m[32m    PieceShader =[m
[32m+[m[32m      PrefabRegistryController.vehicleSharedAssetBundle.LoadAsset<Shader>("Custom_Piece.shader");[m
   }[m
 }[m
\ No newline at end of file[m
[1mdiff --git a/src/ValheimVehicles/ValheimVehicles.Vehicles/Components/VehicleMeshMaskManager.cs b/src/ValheimVehicles/ValheimVehicles.Vehicles/Components/VehicleMeshMaskManager.cs[m
[1mindex b8be8c8..01f4a08 100644[m
[1m--- a/src/ValheimVehicles/ValheimVehicles.Vehicles/Components/VehicleMeshMaskManager.cs[m
[1m+++ b/src/ValheimVehicles/ValheimVehicles.Vehicles/Components/VehicleMeshMaskManager.cs[m
[36m@@ -6,6 +6,7 @@[m [musing UnityEngine;[m
 using UnityEngine.UI;[m
 using ValheimVehicles.Prefabs;[m
 using Logger = Jotunn.Logger;[m
[32m+[m[32musing Random = UnityEngine.Random;[m
 [m
 namespace ValheimVehicles.Vehicles.Components;[m
 [m
[36m@@ -21,8 +22,8 @@[m [mpublic class VehicleMeshMaskManager : MonoBehaviour[m
   private GameObject scrollViewObj;[m
   private GameObject? prevDemoMesh;[m
 [m
[31m-  public int numberOfVertices = 10000;[m
[31m-  public Vector3 bounds = new Vector3(200, 50, 200);[m
[32m+[m[32m  public int numberOfVertices = 10;[m
[32m+[m[32m  public Vector3 bounds = new Vector3(20, 5, 20);[m
   public Material meshMaterial;[m
 [m
   private void Awake()[m
[36m@@ -87,8 +88,9 @@[m [mpublic class VehicleMeshMaskManager : MonoBehaviour[m
       // scrollView.SetActive(!scrollView.activeInHierarchy);[m
     });[m
 [m
[31m-    var guiPreviewButtonGo = guiMan.CreateButton($"Close", currentPanel.transform, Vector2.zero,[m
[31m-      Vector2.zero, new Vector2(50, 50));[m
[32m+[m[32m    var guiPreviewButtonGo = guiMan.CreateButton($"Build Mesh", currentPanel.transform,[m
[32m+[m[32m      Vector2.zero,[m
[32m+[m[32m      Vector2.zero, new Vector2(200, 200));[m
     var onPreviewButton = guiPreviewButtonGo.GetComponent<Button>();[m
 [m
     onPreviewButton.onClick.AddListener(() =>[m
[36m@@ -120,30 +122,59 @@[m [mpublic class VehicleMeshMaskManager : MonoBehaviour[m
     var wmMaterial = LoadValheimAssets.waterMask.GetComponent<MeshRenderer>().material;[m
     var wm_material = LoadValheimAssets.waterMask.GetComponent<Material>();[m
 [m
[31m-    var generatedMesh = new Mesh();[m
[32m+[m[32m    // var generatedMesh = new Mesh();[m
 [m
[31m-    var vertices = new List<Vector3>();[m
[31m-    var uvs = new List<Vector2>();[m
[32m+[m[32m    // var vertices = new List<Vector3>();[m
[32m+[m[32m    // var uvs = new List<Vector2>();[m
 [m
     // 3 triangles per polygon...so a square of 2 triangles requires 2*3 = 6 triangles[m
     // triangles must be clockwise in order for them to display on camera...[m
     // https://www.youtube.com/watch?v=gmuHI_wsOgI[m
[31m-    var triangles = new List<int>();[m
[32m+[m[32m    // var triangles = new List<int>();[m
[32m+[m
[32m+[m[32m    // meshRenderer.material = wm_material;[m
[32m+[m[32m    // meshFilter.mesh = generatedMesh;[m
 [m
[31m-    meshRenderer.material = wm_material;[m
[31m-    meshFilter.mesh = generatedMesh;[m
[32m+[m[32m    var unlitColor = LoadValheimVehicleAssets.PieceShader;[m
[32m+[m[32m    // var twoSidedShader =[m
[32m+[m[32m    //   PrefabRegistryController.vehicleSharedAssetBundle[m
[32m+[m[32m    //     .LoadAsset<Shader>("Standard TwoSided.shader");[m
[32m+[m
[32m+[m[32m    var material = new Material(unlitColor)[m
[32m+[m[32m    {[m
[32m+[m[32m      color = Color.green[m
[32m+[m[32m    };[m
[32m+[m[32m    meshRenderer.sharedMaterial = material;[m
[32m+[m[32m    meshRenderer.material = material;[m
 [m
     var wmShader = wmMaterial?.shader;[m
[31m-    meshRenderer.material = wmMaterial;[m
[32m+[m[32m    // meshRenderer.material = wmMaterial;[m
[32m+[m[32m    // meshRenderer.sharedMaterial = wmMaterial;[m
[32m+[m[32m    List<Vector3> vertices = GenerateRandomVertices(numberOfVertices, bounds);[m
[32m+[m
[32m+[m[32m    var mesh = GenerateMeshesFromPoints(vertices);[m
[32m+[m[32m    // var mesh = GenerateMeshesFromPoints(meshCoordinates);[m
[32m+[m[32m    // mesh.SetVertices(vertices);[m
[32m+[m[32m    // mesh.SetTriangles(triangles, 0);[m
[32m+[m[32m    // mesh.SetUVs(0, uvs);[m
[32m+[m[32m    // mesh.Optimize();[m
[32m+[m[32m    // mesh.RecalculateNormals();[m
[32m+[m[32m    meshFilter.mesh = mesh;[m
[32m+[m[32m    // meshFilter.sharedMesh = mesh;[m
[32m+[m[32m  }[m
 [m
[31m-    var mesh = GenerateMeshesFromPoints(meshCoordinates);[m
[31m-    mesh.SetVertices(vertices);[m
[31m-    mesh.SetTriangles(triangles, 0);[m
[31m-    mesh.SetUVs(0, uvs);[m
[31m-    mesh.Optimize();[m
[31m-    mesh.RecalculateNormals();[m
[32m+[m[32m  List<Vector3> GenerateRandomVertices(int count, Vector3 bounds)[m
[32m+[m[32m  {[m
[32m+[m[32m    List<Vector3> vertices = new List<Vector3>();[m
[32m+[m[32m    for (int i = 0; i < count; i++)[m
[32m+[m[32m    {[m
[32m+[m[32m      float x = Random.Range(0, bounds.x);[m
[32m+[m[32m      float y = Random.Range(0, bounds.y);[m
[32m+[m[32m      float z = Random.Range(0, bounds.z);[m
[32m+[m[32m      vertices.Add(new Vector3(x, y, z));[m
[32m+[m[32m    }[m
 [m
[31m-    meshFilter.sharedMesh = mesh;[m
[32m+[m[32m    return vertices;[m
   }[m
 [m
   public Mesh GenerateMeshesFromPoints(List<Vector3> vertices)[m
[36m@@ -347,13 +378,16 @@[m [mpublic class VehicleMeshMaskManager : MonoBehaviour[m
 [m
   public void OnPreview()[m
   {[m
[32m+[m[32m    CreateWaterMaskMesh();[m
   }[m
 [m
   private void AddButtonsForMeshPoints()[m
   {[m
[32m+[m[32m    if (!scrollViewObj) return;[m
     var guiMan = Jotunn.Managers.GUIManager.Instance;[m
     var pos = 0;[m
     buttonItems = [];[m
[32m+[m
     foreach (var meshCoordinate in meshCoordinates)[m
     {[m
       buttonItems.Add(guiMan.CreateButton([m
[1mdiff --git a/src/ValheimVehicles/ValheimVehicles.Vehicles/DynamicMeshGeneratorOld.cs b/src/ValheimVehicles/ValheimVehicles.Vehicles/DynamicMeshGeneratorOld.cs[m
[1mindex 7f08fa8..ead9d8f 100644[m
[1m--- a/src/ValheimVehicles/ValheimVehicles.Vehicles/DynamicMeshGeneratorOld.cs[m
[1m+++ b/src/ValheimVehicles/ValheimVehicles.Vehicles/DynamicMeshGeneratorOld.cs[m
[36m@@ -34,8 +34,8 @@[m [mpublic class DynamicMeshGeneratorOld : MonoBehaviour[m
       Vector3 mousePosition = Input.mousePosition;[m
       mousePosition.z = 10f; // Distance from camera to the mesh (adjust as needed)[m
 [m
[31m-      Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);[m
[31m-      AddVertex(worldPosition);[m
[32m+[m[32m      // Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);[m
[32m+[m[32m      // AddVertex(worldPosition);[m
     }[m
 [m
     // Example: Rebuild mesh when vertices change[m
