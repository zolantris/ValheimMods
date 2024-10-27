using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace ValheimVehicles.Scene
{
  [ExecuteInEditMode]
  [RequireComponent(typeof(Transform))]
  public class CustomCube : MonoBehaviour
  {
    public float
      cubeSize = 1f; // This should match the local scale of a Unity cube

    public Material InnerSelectiveMask;
    public Material SurfaceWaterMaskMaterial;
    
    private static Color greenish = new Color(0.15f, 0.5f, 0.3f, 0.2f);
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    
    public Color color = greenish;
    private List<GameObject> cubeObjs = new List<GameObject>();
    private List<Renderer> cubeRenders = new List<Renderer>();
    private GameObject Cube;
    public bool CanRenderTopOfCube = false;
    private void Awake()
    {
      transform.rotation = Quaternion.Euler(180, 0, 0);
      var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = transform.position;
      cube.transform.SetParent(transform);
      var meshRender = cube.GetComponent<MeshRenderer>();
        meshRender.sharedMaterial = InnerSelectiveMask;
        meshRender.sharedMaterial.renderQueue = 1000;
      CreateCubeFaces();
    }
    
    private void Update()
    {
      foreach (var cubeRender in cubeRenders)
      {
        // cubeRender.material.SetFloat("_MaxHeight", 0);
      }
      // if (material != null)
      // {
      //   // Update the _MaxHeight based on the object's local Y position
      //   float localY = transform.localPosition.y + maxHeightOffset;
      //   material.SetFloat("_MaxHeight", localY);
      // }
    }

    // private void OnUpdate()
    // {
    //   foreach (var o in cubeFace)
    //   {
    //     
    //   }
    // }
    private void SafeDestroy(GameObject obj)
    {
      if (!Application.isPlaying)
      {
        DestroyImmediate(obj);
      }
      else
      {
        Destroy(obj);
      }
    }

    public void Cleanup()
    {
      if (Cube != null) SafeDestroy(Cube);
      cubeRenders.Clear();
      foreach (var cubeObj in cubeObjs)
      {
        if (cubeObj == null) continue;
        SafeDestroy(cubeObj);
      }
      cubeObjs.Clear();
    }

    private void OnDestroy()
    {
      Cleanup();
    }

    private void OnDisable()
    {
      Cleanup();
    }

    private void CreateCubeFaces()
    {
      float halfSize = cubeSize / 2f;

      // Define face directions and positions
      Vector3[] directions =
      {
        Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left,
        Vector3.right
      };
      Vector3[] positions =
      {
        new Vector3(0, halfSize, 0), new Vector3(0, -halfSize, 0),
        new Vector3(0, 0, halfSize), new Vector3(0, 0, -halfSize),
        new Vector3(-halfSize, 0, 0), new Vector3(halfSize, 0, 0)
      };
      Vector3[] rotations =
      {
        new Vector3(90, 0, 0), new Vector3(-90, 0, 0),
        new Vector3(0, 0, 0), new Vector3(0, 180, 0),
        new Vector3(0, -90, 0), new Vector3(0, 90, 0)
      };

      // Create each face with two meshes for double-sided rendering
      for (int i = 0; i < 6; i++)
      {
        // Omit the top face based on the boolean
        if (i == 1 && !CanRenderTopOfCube) // i == 0 corresponds to the top face i==1 is bottom, but we flip the cube so shader worldY works better so it's top.
          continue;

        // Front side of the face
        CreateFaceMesh(positions[i], Quaternion.Euler(rotations[i]), directions[i]);

        // Back side of the face (flip normal)
        // CreateFaceMesh(positions[i], Quaternion.Euler(rotations[i] + new Vector3(0, 180, 0)), -directions[i]);
      }
    }

    private void CreateFaceMesh(Vector3 position, Quaternion rotation,
      Vector3 normal)
    {
      GameObject face = new GameObject("CubeFace");
      face.transform.SetParent(transform);
      face.transform.localPosition = position;
      face.transform.localRotation = rotation;
      face.transform.localScale = Vector3.one * cubeSize;

      // Mesh setup
      Mesh mesh = new Mesh();
      mesh.vertices = new Vector3[]
      {
        new Vector3(-0.5f, -0.5f, 0),
        new Vector3(0.5f, -0.5f, 0),
        new Vector3(-0.5f, 0.5f, 0),
        new Vector3(0.5f, 0.5f, 0)
      };
      mesh.triangles = new int[]
      {
        0, 2, 1, 2, 3, 1 // Single face with two triangles
      };
      mesh.normals = new Vector3[]
      {
        normal, normal, normal, normal
      };

      MeshRenderer renderer = face.AddComponent<MeshRenderer>();
      MeshFilter filter = face.AddComponent<MeshFilter>();
      filter.mesh = mesh;
      renderer.sharedMaterial = SurfaceWaterMaskMaterial;
      renderer.sharedMaterial.SetColor(Color1, color);
      renderer.sharedMaterial.renderQueue = 5;

      cubeRenders.Add(renderer);
      cubeObjs.Add(face);
    }
  }

}