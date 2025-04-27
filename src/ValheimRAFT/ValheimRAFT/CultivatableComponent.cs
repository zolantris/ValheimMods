using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT.Util;

using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class CultivatableComponent : MonoBehaviour
{
  private ZNetView m_nview;

  private static Dictionary<int, List<int>> m_childObjects = new();

  private float textureScale = 8f;

  public bool isCultivatable { get; set; } = true;

  public Heightmap m_heightmap;

  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    var wnt = GetComponent<WearNTear>();
    if ((bool)wnt)
      wnt.m_onDestroyed =
        (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));

    m_heightmap = gameObject.AddComponent<Heightmap>();
  }

  public void Start()
  {
    UpdateMaterial();
  }

  public void UpdateMaterial()
  {
    var uvoffset = transform.parent
      ? new Vector2(0f - transform.localPosition.x,
        0f - transform.localPosition.z)
      : new Vector2(0f - transform.position.x, 0f - transform.position.z);
    uvoffset /= textureScale;
    var uvrotation = transform.parent
      ? 0f - transform.localEulerAngles.y
      : 0f - transform.eulerAngles.y;
    uvrotation /= 360f;
    var uvscale = new Vector2(transform.localScale.x, transform.localScale.z);
    uvscale /= textureScale;
    MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
    for (var i = 0; i < renderers.Length; i++)
    {
      var mat = renderers[i].material;
      mat.SetTextureOffset("_MainTex", uvoffset);
      mat.SetTextureScale("_MainTex", uvscale);
      mat.SetTextureOffset("_MainNormal", uvoffset);
      mat.SetTextureScale("_MainNormal", uvscale);
      mat.SetFloat("_MainRotation", uvrotation);
    }
  }

  private void OnDestroyed()
  {
    var myid =
      ZdoWatchController.Instance.GetOrCreatePersistentID(m_nview.m_zdo);
    if (!m_childObjects.TryGetValue(myid, out var list)) return;

    for (var i = 0; i < list.Count; i++)
    {
      var zdo = ZdoWatchController.Instance.GetZdo(list[i]);
      if (zdo == null) continue;

      var obj = ZNetScene.instance.FindInstance(zdo);
      if ((bool)obj)
      {
        if (gameObject == obj || transform.IsChildOf(obj.transform))
        {
          Logger.LogWarning(
            $" gameObject == obj || transform.IsChildOf(obj.transform) {gameObject} == {obj} || {transform.IsChildOf(obj.transform)}");
          continue;
        }

        var wnt = obj.GetComponent<WearNTear>();
        if ((bool)wnt)
        {
          wnt.Destroy();
          continue;
        }

        var netview = obj.GetComponent<ZNetView>();
        if ((bool)netview) netview.Destroy();
      }
      else
      {
        ZDOMan.instance.DestroyZDO(zdo);
      }
    }

    m_childObjects.Remove(myid);
  }

  public string GetHoverName()
  {
    return "";
  }

  public string GetHoverText()
  {
    return "";
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void AddNewChild(ZNetView child)
  {
    AddNewChild(
      ZdoWatchController.Instance.GetOrCreatePersistentID(m_nview.m_zdo),
      child);
  }

  public static void InitPiece(ZNetView netview)
  {
    var id = GetParentID(netview);
    if (id != 0) AddChild(id, netview);
  }

  public static int GetParentID(ZNetView netview)
  {
    var id = netview.m_zdo.GetInt(VehicleZdoVars.MBCultivatableParentIdHash);
    if (id == 0)
    {
      var zdoid = netview.m_zdo.GetZDOID(VehicleZdoVars.MBCultivatableParentHash);
      if (zdoid != ZDOID.None && ZDOMan.instance != null)
      {
        var zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = zdoparent == null
          ? ZdoWatchController.ZdoIdToId(zdoid)
          : ZdoWatchController.Instance.GetOrCreatePersistentID(zdoparent);
        netview.m_zdo.Set(VehicleZdoVars.MBCultivatableParentIdHash, id);
      }
    }

    return id;
  }

  public static void AddNewChild(int parent, ZNetView child)
  {
    child.m_zdo.Set(VehicleZdoVars.MBCultivatableParentIdHash, parent);
    AddChild(parent, child);
  }

  public static void AddChild(int parent, ZNetView child)
  {
    var sp = child.GetComponent<StaticPhysics>();
    if ((bool)sp) Destroy(sp);

    AddChild(parent,
      ZdoWatchController.Instance.GetOrCreatePersistentID(child.m_zdo));
  }

  private static void AddChild(int parent, int child)
  {
    if (parent != 0)
    {
      if (!m_childObjects.TryGetValue(parent, out var list))
      {
        list = new List<int>();
        m_childObjects.Add(parent, list);
      }

      list.Add(child);
    }
  }
}