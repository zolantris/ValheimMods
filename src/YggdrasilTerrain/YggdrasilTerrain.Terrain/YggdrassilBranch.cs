using System;
using UnityEngine;
using YggdrasilTerrain.Config;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

public abstract class YggdrasilBranch
{
  // position: 6326.154 2370 864.6407
  // worlds end player position near branch 10018.89 1911.092 -9.4584
  private static int? _originalBranchLayer;

  private static GameObject? _branchInstance;

  public static GameObject? BranchInstance
  {
    get
    {
      if (_branchInstance != null)
      {
        return _branchInstance;
      }

      _branchInstance = GetBranchObject();
      return _branchInstance;
    }
  }

  /// <summary>
  /// Simple way to get or add components,
  /// </summary>
  /// <todo>
  /// Add a typed generic getter. It would have to extend component to work, not sure how to do this.
  /// </todo>
  /// <param name="gameObject"></param>
  /// <returns></returns>
  private static MeshCollider GetOrAddMeshCollider(GameObject gameObject)
  {
    var component = gameObject.GetComponent<MeshCollider>();
    return !component
      ? gameObject.AddComponent<MeshCollider>()
      : component;
  }

  private static void AddMeshCollider(GameObject gameObject)
  {
    var branchMesh = gameObject.GetComponent<MeshFilter>();
    if (!branchMesh) return;
    var meshCollider = GetOrAddMeshCollider(gameObject);

    if (!meshCollider)
    {
      // should never get here.
      return;
    }

    meshCollider.convex = false;
    meshCollider.sharedMesh = branchMesh.sharedMesh;
    meshCollider.includeLayers = YggdrasilConfig.BranchCollisionLayerMask.Value;
  }

  public static void RemoveMeshCollider(GameObject gameObject)
  {
    var meshCollider = gameObject.GetComponent<MeshCollider>();
    if (!meshCollider) return;
    Object.Destroy(meshCollider);
  }

  public static void OnSceneReady()
  {
    OnBranchCollisionChange(
      YggdrasilConfig.AllowCollisionsOnYggdrasilBranch.Value);
  }

  private static GameObject? GetBranchObject()
  {
    var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
    GameObject? branchGameObject = null;

    foreach (var gameObject in allObjects)
    {
      if (gameObject.transform.parent == null) continue;

      if (gameObject.name == "YggdrasilBranch")
      {
        var branchTransform = gameObject.transform.Find("branch");
        branchGameObject = branchTransform?.gameObject;
        break;
      }

      if (gameObject.transform.parent.name == "YggdrassilBranch" &&
          gameObject.name == "branch")
      {
        branchGameObject = gameObject;
        break;
      }
    }

    if (branchGameObject == null)
    {
      Logger.LogError("Could not find branchGameObject");
    }

    return branchGameObject;
  }

  private static void RemoveBranchCollision()
  {
    if (BranchInstance == null) return;

    if (_originalBranchLayer != null)
    {
      if (BranchInstance.layer != _originalBranchLayer)
      {
        BranchInstance.layer = _originalBranchLayer.Value;
      }
    }

    RemoveMeshCollider(BranchInstance!);
  }

  private static void AddBranchCollision()
  {
    if (BranchInstance == null) return;

    _originalBranchLayer ??= BranchInstance.layer;

    AddMeshCollider(BranchInstance);
    BranchInstance.layer = YggdrasilConfig.BranchLayer.Value;
  }

  private static void OnBranchCollisionChange(bool val)
  {
    if (val)
    {
      AddBranchCollision();
    }
    else
    {
      RemoveBranchCollision();
    }
  }

  public static void OnBranchCollisionChange(object sender, EventArgs e)
  {
    OnBranchCollisionChange(YggdrasilConfig.AllowCollisionsOnYggdrasilBranch
      .Value);
  }
}