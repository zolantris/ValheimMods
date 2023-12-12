// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.CultivatableComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  public class CultivatableComponent : MonoBehaviour
  {
    private ZNetView m_nview;
    private static Dictionary<int, List<int>> m_childObjects = new Dictionary<int, List<int>>();
    private static readonly int MBCultivatableParentIdHash = StringExtensionMethods.GetStableHashCode("MBCultivatableParentId");
    public static readonly KeyValuePair<int, int> MBCultivatableParentHash = ZDO.GetHashZDOID("MBCultivatableParent");
    private float textureScale = 8f;

    public bool isCultivatable { get; set; } = true;

    public void Awake()
    {
      this.m_nview = ((Component) this).GetComponent<ZNetView>();
      WearNTear component = ((Component) this).GetComponent<WearNTear>();
      if (!Object.op_Implicit((Object) component))
        return;
      component.m_onDestroyed += new Action(this.OnDestroyed);
    }

    public void Start() => this.UpdateMaterial();

    public void UpdateMaterial()
    {
      Vector2 vector2_1 = Vector2.op_Division(Object.op_Implicit((Object) ((Component) this).transform.parent) ? new Vector2(-((Component) this).transform.localPosition.x, -((Component) this).transform.localPosition.z) : new Vector2(-((Component) this).transform.position.x, -((Component) this).transform.position.z), this.textureScale);
      float num = (Object.op_Implicit((Object) ((Component) this).transform.parent) ? -((Component) this).transform.localEulerAngles.y : -((Component) this).transform.eulerAngles.y) / 360f;
      Vector2 vector2_2;
      // ISSUE: explicit constructor call
      ((Vector2) ref vector2_2).\u002Ector(((Component) this).transform.localScale.x, ((Component) this).transform.localScale.z);
      vector2_2 = Vector2.op_Division(vector2_2, this.textureScale);
      foreach (Renderer componentsInChild in ((Component) this).GetComponentsInChildren<MeshRenderer>())
      {
        Material material = componentsInChild.material;
        material.SetTextureOffset("_MainTex", vector2_1);
        material.SetTextureScale("_MainTex", vector2_2);
        material.SetTextureOffset("_MainNormal", vector2_1);
        material.SetTextureScale("_MainNormal", vector2_2);
        material.SetFloat("_MainRotation", num);
      }
    }

    private void OnDestroyed()
    {
      int persistantId = ZDOPersistantID.Instance.GetOrCreatePersistantID(this.m_nview.m_zdo);
      List<int> intList;
      if (!CultivatableComponent.m_childObjects.TryGetValue(persistantId, out intList))
        return;
      for (int index = 0; index < intList.Count; ++index)
      {
        ZDO zdo = ZDOPersistantID.Instance.GetZDO(intList[index]);
        if (zdo != null)
        {
          ZNetView instance = ZNetScene.instance.FindInstance(zdo);
          if (Object.op_Implicit((Object) instance))
          {
            if (Object.op_Equality((Object) ((Component) this).gameObject, (Object) instance) || ((Component) this).transform.IsChildOf(((Component) instance).transform))
            {
              ZLog.LogWarning((object) string.Format(" gameObject == obj || transform.IsChildOf(obj.transform) {0} == {1} || {2}", (object) ((Component) this).gameObject, (object) instance, (object) ((Component) this).transform.IsChildOf(((Component) instance).transform)));
            }
            else
            {
              WearNTear component1 = ((Component) instance).GetComponent<WearNTear>();
              if (Object.op_Implicit((Object) component1))
              {
                component1.Destroy();
              }
              else
              {
                ZNetView component2 = ((Component) instance).GetComponent<ZNetView>();
                if (Object.op_Implicit((Object) component2))
                  component2.Destroy();
              }
            }
          }
          else
            ZDOMan.instance.DestroyZDO(zdo);
        }
      }
      CultivatableComponent.m_childObjects.Remove(persistantId);
    }

    public string GetHoverName() => "";

    public string GetHoverText() => "";

    public bool Interact(Humanoid user, bool hold, bool alt) => true;

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public void AddNewChild(ZNetView child) => CultivatableComponent.AddNewChild(ZDOPersistantID.Instance.GetOrCreatePersistantID(this.m_nview.m_zdo), child);

    public static void InitPiece(ZNetView netview)
    {
      int parentId = CultivatableComponent.GetParentID(netview);
      if (parentId == 0)
        return;
      CultivatableComponent.AddChild(parentId, netview);
    }

    public static int GetParentID(ZNetView netview)
    {
      int parentId = netview.m_zdo.GetInt(CultivatableComponent.MBCultivatableParentIdHash, 0);
      if (parentId == 0)
      {
        ZDOID zdoid = netview.m_zdo.GetZDOID(CultivatableComponent.MBCultivatableParentHash);
        if (ZDOID.op_Inequality(zdoid, ZDOID.None))
        {
          ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
          parentId = zdo == null ? ZDOPersistantID.ZDOIDToId(zdoid) : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdo);
          netview.m_zdo.Set(CultivatableComponent.MBCultivatableParentIdHash, parentId, false);
        }
      }
      return parentId;
    }

    public static void AddNewChild(int parent, ZNetView child)
    {
      child.m_zdo.Set(CultivatableComponent.MBCultivatableParentIdHash, parent, false);
      CultivatableComponent.AddChild(parent, child);
    }

    public static void AddChild(int parent, ZNetView child)
    {
      StaticPhysics component = ((Component) child).GetComponent<StaticPhysics>();
      if (Object.op_Implicit((Object) component))
        Object.Destroy((Object) component);
      CultivatableComponent.AddChild(parent, ZDOPersistantID.Instance.GetOrCreatePersistantID(child.m_zdo));
    }

    private static void AddChild(int parent, int child)
    {
      if (parent == 0)
        return;
      List<int> intList;
      if (!CultivatableComponent.m_childObjects.TryGetValue(parent, out intList))
      {
        intList = new List<int>();
        CultivatableComponent.m_childObjects.Add(parent, intList);
      }
      intList.Add(child);
    }
  }
}
