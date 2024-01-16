using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class CultivatableComponent : MonoBehaviour
{
  private ZNetView m_nview;

  private static Dictionary<int, List<int>> m_childObjects = new Dictionary<int, List<int>>();

  private static readonly int MBCultivatableParentIdHash =
    "MBCultivatableParentId".GetStableHashCode();

  public static readonly KeyValuePair<int, int> MBCultivatableParentHash =
    ZDO.GetHashZDOID("MBCultivatableParent");

  private float textureScale = 8f;

  public bool isCultivatable { get; set; } = true;


  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    WearNTear wnt = GetComponent<WearNTear>();
    if ((bool)wnt)
    {
      wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
    }
  }

  public void Start()
  {
    UpdateMaterial();
  }

  public void UpdateMaterial()
  {
    Vector2 uvoffset = (base.transform.parent
      ? new Vector2(0f - base.transform.localPosition.x, 0f - base.transform.localPosition.z)
      : new Vector2(0f - base.transform.position.x, 0f - base.transform.position.z));
    uvoffset /= textureScale;
    float uvrotation = (base.transform.parent
      ? (0f - base.transform.localEulerAngles.y)
      : (0f - base.transform.eulerAngles.y));
    uvrotation /= 360f;
    Vector2 uvscale = new Vector2(base.transform.localScale.x, base.transform.localScale.z);
    uvscale /= textureScale;
    MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
    for (int i = 0; i < renderers.Length; i++)
    {
      Material mat = renderers[i].material;
      mat.SetTextureOffset("_MainTex", uvoffset);
      mat.SetTextureScale("_MainTex", uvscale);
      mat.SetTextureOffset("_MainNormal", uvoffset);
      mat.SetTextureScale("_MainNormal", uvscale);
      mat.SetFloat("_MainRotation", uvrotation);
    }
  }

  private void OnDestroyed()
  {
    int myid = ZDOPersistantID.Instance.GetOrCreatePersistentID(m_nview.m_zdo);
    if (!m_childObjects.TryGetValue(myid, out var list))
    {
      return;
    }

    for (int i = 0; i < list.Count; i++)
    {
      ZDO zdo = ZDOPersistantID.Instance.GetZDO(list[i]);
      if (zdo == null)
      {
        continue;
      }

      ZNetView obj = ZNetScene.instance.FindInstance(zdo);
      if ((bool)obj)
      {
        if (base.gameObject == obj || base.transform.IsChildOf(obj.transform))
        {
          Logger.LogWarning(
            $" gameObject == obj || transform.IsChildOf(obj.transform) {base.gameObject} == {obj} || {base.transform.IsChildOf(obj.transform)}");
          continue;
        }

        WearNTear wnt = obj.GetComponent<WearNTear>();
        if ((bool)wnt)
        {
          wnt.Destroy();
          continue;
        }

        ZNetView netview = obj.GetComponent<ZNetView>();
        if ((bool)netview)
        {
          netview.Destroy();
        }
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
    AddNewChild(ZDOPersistantID.Instance.GetOrCreatePersistentID(m_nview.m_zdo), child);
  }

  public static void InitPiece(ZNetView netview)
  {
    int id = GetParentID(netview);
    if (id != 0)
    {
      AddChild(id, netview);
    }
  }

  public static int GetParentID(ZNetView netview)
  {
    int id = netview.m_zdo.GetInt(MBCultivatableParentIdHash);
    if (id == 0)
    {
      ZDOID zdoid = netview.m_zdo.GetZDOID(MBCultivatableParentHash);
      if (zdoid != ZDOID.None)
      {
        ZDO zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = ((zdoparent == null)
          ? ZDOPersistantID.ZDOIDToId(zdoid)
          : ZDOPersistantID.Instance.GetOrCreatePersistentID(zdoparent));
        netview.m_zdo.Set(MBCultivatableParentIdHash, id);
      }
    }

    return id;
  }

  public static void AddNewChild(int parent, ZNetView child)
  {
    child.m_zdo.Set(MBCultivatableParentIdHash, parent);
    AddChild(parent, child);
  }

  public static void AddChild(int parent, ZNetView child)
  {
    StaticPhysics sp = child.GetComponent<StaticPhysics>();
    if ((bool)sp)
    {
      UnityEngine.Object.Destroy(sp);
    }

    AddChild(parent, ZDOPersistantID.Instance.GetOrCreatePersistentID(child.m_zdo));
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