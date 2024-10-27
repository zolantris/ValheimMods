using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValheimVehicles.Scene
{
 // [ExecuteInEditMode]
[RequireComponent(typeof(Transform))]
public class CustomCube : MonoBehaviour
  {
    public float
      cubeSize = 1f; // This should match the local scale of a Unity cube

    // public Material InnerSelectiveMask;
    // public Material SurfaceWaterMaskMaterial;
    public Material CubeMaskMaterial;

    public Material CubeVisibleSurfaceMaterial;

    private static Color greenish = new Color(0.15f, 0.5f, 0.3f, 0.2f);
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

    public Color color = greenish;
    private List<GameObject> cubeObjs = new List<GameObject>();
    private List<Renderer> cubeRenders = new List<Renderer>();
    private GameObject Cube;
    public bool CanRenderTopOfCube = false;
    private LayerMask NonSolidLayer => LayerMask.NameToLayer("piece_nonsolid");

    private BlendMode SelectedDestinationBlend = BlendMode.OneMinusSrcAlpha;

    private void Start()
    {
      InitCubes();
    }

    public void InitCubes()
    {
      if (CubeMaskMaterial == null || CubeVisibleSurfaceMaterial == null)
      {
        return;
      }

      var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = transform.position;
      cube.transform.SetParent(transform);
      gameObject.layer = NonSolidLayer;
      var meshRender = cube.GetComponent<MeshRenderer>();
      meshRender.sharedMaterial = CubeMaskMaterial;
      meshRender.sharedMaterial.renderQueue = 1000;
      meshRender.gameObject.layer = NonSolidLayer;

      CreateCubeFaces();
    }


    // private WaterVolume prevLiquidLevel = new WaterVolume();

    /// <summary>
    ///  This is pretty heavy. Likely will need to not call as much by doing some optimistic checks instead of setting shader every time.
    /// </summary>
    private void FixedUpdate()
    {
      // foreach (var cubeRender in cubeRenders)
      // {
      //   WaterVolume previousAndOut = null;
      //   var waterLevel =
      //     Floating.GetWaterLevel(cubeRender.transform.position,
      //       ref prevLiquidLevel);
      //   cubeRender.material.SetFloat(MaxHeight, waterLevel);
      //   if (cubeRender.material.color != color)
      //   {
      //     cubeRender.material.SetColor(ColorId, color);
      //   }

        // if (!Mathf.Approximately(cubeRender.material.GetFloat(DstBlend),
        //       (float)SelectedDestinationBlend))
        // {
        //   cubeRender.material.SetFloat(DstBlend, (float)SelectedDestinationBlend);
        // }
      }
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
        if (i == 1 &&
            !CanRenderTopOfCube) // i == 0 corresponds to the top face i==1 is bottom, but we flip the cube so shader worldY works better so it's top.
          continue;

        // Front side of the face
        CreateFaceMesh(positions[i], Quaternion.Euler(rotations[i]),
          directions[i]);

        // Back side of the face (flip normal)
        // CreateFaceMesh(positions[i], Quaternion.Euler(rotations[i] + new Vector3(0, 180, 0)), -directions[i]);
      }
    }

    private void CreateFaceMesh(Vector3 position, Quaternion rotation,
      Vector3 normal)
    {
      var face = new GameObject("CubeFace");
      face.layer = NonSolidLayer;
      face.transform.SetParent(transform);
      face.transform.localPosition = position;
      face.transform.localRotation = rotation;
      face.transform.localScale = Vector3.one * cubeSize;

      // Mesh setup
      var mesh = new Mesh
      {
        vertices = new Vector3[]
        {
          new Vector3(-0.5f, -0.5f, 0),
          new Vector3(0.5f, -0.5f, 0),
          new Vector3(-0.5f, 0.5f, 0),
          new Vector3(0.5f, 0.5f, 0)
        },
        triangles = new int[]
        {
          0, 2, 1, 2, 3, 1 // Single face with two triangles
        },
        normals = new Vector3[]
        {
          normal, normal, normal, normal
        }
      };

      MeshRenderer renderer = face.AddComponent<MeshRenderer>();
      MeshFilter filter = face.AddComponent<MeshFilter>();
      filter.mesh = mesh;
      renderer.sharedMaterial = CubeVisibleSurfaceMaterial;
      renderer.material.SetColor(ColorId, color);
      renderer.material.renderQueue = 5;

      cubeRenders.Add(renderer);
      cubeObjs.Add(face);
    }
  }
}