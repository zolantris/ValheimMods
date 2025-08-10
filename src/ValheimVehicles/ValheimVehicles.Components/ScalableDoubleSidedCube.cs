using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

// [ExecuteInEditMode]
public class ScalableDoubleSidedCube : MonoBehaviour
{
  public Vector3
    rectangleSize =
      Vector3.one; // This should match the local scale of a Unity cube

  public float baseFaceSize = 1f;

  private static Material? _CubeMaskMaterial;
  private static Material? _VisibleSurfaceMaterial;

  public static Material CubeMaskMaterial => GetCubeMaskMaterial();

  public static Material CubeVisibleSurfaceMaterial =>
    GetVisibleSurfaceMaterial();

  private static readonly int ColorId = Shader.PropertyToID("_Color");
  private static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");

  private List<GameObject> cubeObjs = new();
  private List<Renderer> cubeRenders = new();

  /// <summary>
  /// Controller flags
  /// </summary>
  public bool CanRenderTopOfCube = false;

  public int CubeLayer = LayerHelpers.IgnoreRaycastLayer;
  public bool ShouldUpdateHeight = false;
  public float ForcedMaxHeight = 30f;
  public Color color = new(0.5f, 0.5f, 1f, 0.8f);
  public static bool EnabledFixedUpdateSync = false;

  public bool HasForcedHeight = true;

  // This should be only enabled for non-doublesided shaders
  public bool RenderDoubleSided = false;
  public bool RenderMaskOnSecondFace = false;

  private BlendMode SelectedDestinationBlend = BlendMode.OneMinusSrcAlpha;
  private GameObject? _cubeMaskObj = null;
  private BoxCollider? _cubeCollider = null;

  public static Material GetCubeMaskMaterial()
  {
    if (_CubeMaskMaterial == null)
      _CubeMaskMaterial =
        new Material(LoadValheimVehicleAssets.TransparentDepthMaskMaterial);

    return _CubeMaskMaterial;
  }

  public static Material GetVisibleSurfaceMaterial()
  {
    if (_VisibleSurfaceMaterial == null)
      _CubeMaskMaterial =
        new Material(LoadValheimVehicleAssets.WaterHeightMaterial);

    return _VisibleSurfaceMaterial;
  }

  private void Start()
  {
    Setup();
  }

  public void InitCubes()
  {
    if (CubeMaskMaterial == null || CubeVisibleSurfaceMaterial == null) return;

    CreateCube();
  }

  private void OnEnable()
  {
    Setup();
  }

  private void Setup()
  {
    if (ZNetView.m_forceDisableInit) return;
    var nv = GetComponent<ZNetView>();
    if (nv == null) return;
    if (nv.GetZDO() == null) return;

    var scale =
      nv.GetZDO().GetVec3(VehicleZdoVars.CustomMeshScale, Vector3.one);
    if (scale != Vector3.one) rectangleSize = scale;

    transform.localScale = rectangleSize;


    InitCubes();
  }

  public BoxCollider? GetCubeCollider()
  {
    if (!_cubeCollider)
      _cubeCollider = _cubeMaskObj?.GetComponent<BoxCollider>();

    return _cubeCollider;
  }

  private WaterVolume? _prevLiquidLevel = null;

  private void AlignTopOfCubeWithWater(float waterHeight)
  {
    if (!ShouldUpdateHeight) return;

    var cubeSizeOffset = transform.localScale.y / 2;
    var bottomOfCube = transform.position.y - cubeSizeOffset;
    var topOfCube = transform.position.y + cubeSizeOffset;
    if (_cubeMaskObj == null) return;
    // Moving the gameobject out of bounds should never happen.
    if (waterHeight < bottomOfCube || waterHeight > topOfCube) return;

    _cubeMaskObj.transform.position = new Vector3(transform.position.x,
      waterHeight, transform.position.z);
  }

  // private void FixedUpdate()
  // {
  //   if (EnabledFixedUpdateSync)
  //   {
  //     UpdateAllValues();
  //   }
  // }

  /// <summary>
  /// Todo may remove, this is meant for a more complicated component with additional faces
  /// </summary>
  private void UpdateAllValues()
  {
    if (ZNetView.m_forceDisableInit) return;
    var waterLevel =
      Floating.GetWaterLevel(transform.position,
        ref _prevLiquidLevel);
    AlignTopOfCubeWithWater(waterLevel);

    if (transform.gameObject.layer != CubeLayer)
      transform.gameObject.layer = CubeLayer;

    if (transform.localScale != rectangleSize)
      transform.localScale = rectangleSize;

    if (_cubeMaskObj != null && _cubeMaskObj.gameObject.layer != CubeLayer)
      _cubeMaskObj.gameObject.layer = CubeLayer;

    foreach (var cubeRender in cubeRenders)
    {
      if (ShouldUpdateHeight)
      {
        waterLevel = HasForcedHeight
          ? ForcedMaxHeight
          : Floating.GetWaterLevel(transform.position,
            ref _prevLiquidLevel);
        cubeRender.material.SetFloat(MaxHeight, waterLevel);
      }

      if (cubeRender.material.color != color)
        cubeRender.material.SetColor(ColorId, color);

      if (cubeRender.gameObject.layer != CubeLayer)
        cubeRender.gameObject.layer = CubeLayer;
    }
  }

  private void SafeDestroy(GameObject obj)
  {
    if (!Application.isPlaying)
      DestroyImmediate(obj);
    else
      Destroy(obj);
  }

  public void Cleanup()
  {
    if (_cubeMaskObj != null) SafeDestroy(_cubeMaskObj);
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

  /// <summary>
  /// Simplistic single cube to render the mask only
  /// </summary>
  private void CreateCube()
  {
    if (_cubeMaskObj) return;
    var halfSize = baseFaceSize / 2f;

    var topDirection = Vector3.up;
    var topPosition = new Vector3(0, halfSize, 0);
    var topRotation = new Vector3(90, 0, 0);

    CreateFaceMesh(topPosition, Quaternion.Euler(topRotation),
      topDirection, CubeFaceType.MaskFace);
    _cubeCollider = GetCubeCollider();
    _cubeCollider?.gameObject.AddComponent<WaterMaskZoneColliderComponent>();
  }

  /// <summary>
  /// Advanced variant, with individual faces created, not performant.
  /// </summary>
  private void CreateCubeFaces()
  {
    var halfSize = baseFaceSize / 2f;

    var topDirection = Vector3.up;
    var topPosition = new Vector3(0, halfSize, 0);
    var topRotation = new Vector3(90, 0, 0);

    // Define face directions and positions
    Vector3[] directions =
    {
      topDirection,
      Vector3.down, Vector3.forward, Vector3.back, Vector3.left,
      Vector3.right
    };

    Vector3[] positions =
    {
      topPosition,
      new(0, -halfSize, 0),
      new(0, 0, halfSize),
      new(0, 0, -halfSize),
      new(-halfSize, 0, 0),
      new(halfSize, 0, 0)
    };
    Vector3[] rotations =
    {
      topRotation,
      new(-90, 0, 0),
      new(0, 0, 0),
      new(0, 180, 0),
      new(0, -90, 0),
      new(0, 90, 0)
    };

    if (!CanRenderTopOfCube && !_cubeMaskObj)
      CreateFaceMesh(topPosition, Quaternion.Euler(topRotation),
        topDirection, CubeFaceType.MaskFace);

    // Create each face with two meshes for double-sided rendering
    for (var i = 0; i < positions.Length; i++)
    {
      // Omit the top face based on the boolean
      if (i == 1 &&
          !CanRenderTopOfCube) // i == 0 corresponds to the top face i==1 is bottom, but we flip the cube so shader worldY works better so it's top.
        continue;

      // Front side of the face
      CreateFaceMesh(positions[i], Quaternion.Euler(rotations[i]),
        directions[i], CubeFaceType.HeightFace);

      // Back side of the face (flip normal)
      if (RenderDoubleSided || RenderMaskOnSecondFace)
        CreateFaceMesh(positions[i],
          Quaternion.Euler(rotations[i] + new Vector3(0, 180, 0)),
          -directions[i], CubeFaceType.MaskFace);
    }
  }

  private enum CubeFaceType
  {
    MaskFace,
    HeightFace
  }

  private void CreateCubeMesh(Vector3 position, Quaternion rotation,
    Vector3 direction, CubeFaceType faceType)
  {
    //
    // // Mesh setup
    // var mesh = new Mesh
    // {
    //   vertices = new Vector3[]
    //   {
    //     new Vector3(-0.5f, -0.5f, 0),
    //     new Vector3(0.5f, -0.5f, 0),
    //     new Vector3(-0.5f, 0.5f, 0),
    //     new Vector3(0.5f, 0.5f, 0)
    //   },
    //   triangles = new int[]
    //   {
    //     0, 2, 1, 2, 3, 1 // Single face with two triangles
    //   },
    //   normals = new Vector3[]
    //   {
    //     normal, normal, normal, normal
    //   }
    // };
    //
    // MeshRenderer renderer = cubeFace.AddComponent<MeshRenderer>();
    // MeshFilter filter = cubeFace.AddComponent<MeshFilter>();
    // filter.mesh = mesh;
  }

  public void UpdateScale(Vector3 scale)
  {
    rectangleSize = scale;
    transform.localScale = rectangleSize;
  }

  private void CreateFaceMesh(Vector3 position, Quaternion rotation,
    Vector3 normal, CubeFaceType faceType)
  {
    if (transform.localScale != rectangleSize)
      transform.localScale = rectangleSize;

    var primitiveType = faceType == CubeFaceType.HeightFace
      ? PrimitiveType.Quad
      : PrimitiveType.Cube;
    var cubeFace = GameObject.CreatePrimitive(primitiveType);
    cubeFace.name = $"{Enum.GetName(typeof(CubeFaceType), (int)faceType)}";
    cubeFace.layer = CubeLayer;
    cubeFace.transform.SetParent(transform);
    cubeFace.transform.localPosition = position;
    cubeFace.transform.localRotation = rotation;

    if (faceType == CubeFaceType.MaskFace)
      cubeFace.transform.localScale =
        new Vector3(1, 1, 0.1f);
    else
      cubeFace.transform.localScale = Vector3.one;

    var componentRenderer = cubeFace.GetComponent<MeshRenderer>();
    if (faceType == CubeFaceType.HeightFace)
    {
      componentRenderer.sharedMaterial = CubeVisibleSurfaceMaterial;
      componentRenderer.material.SetColor(ColorId, color);
      cubeRenders.Add(componentRenderer);
      cubeObjs.Add(cubeFace);
    }
    else
    {
      componentRenderer.sharedMaterial = CubeMaskMaterial;
      _cubeMaskObj = cubeFace;
    }
  }
}