// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.PierComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{
  public class PierComponent : MonoBehaviour
  {
    public ZNetView m_nview;
    public GameObject m_segmentObject;
    public float m_segmentHeight;
    public float m_baseOffset;
    private int m_terrainLayer;
    private List<GameObject> m_segmentObjects = new List<GameObject>();

    public void Awake()
    {
      this.m_terrainLayer = LayerMask.GetMask(new string[1]
      {
        "terrain"
      });
      if (ZNetView.m_forceDisableInit)
      {
        WearNTear component1 = ((Component)this).GetComponent<WearNTear>();
        if (component1 && component1.m_new)
          component1.m_new.SetActive(false);
        this.m_segmentObject.SetActive(false);
        foreach (Collider componentsInChild in this.m_segmentObject
                   .GetComponentsInChildren<Collider>(true))
          componentsInChild.enabled = false;
        this.m_segmentObject.layer = LayerMask.NameToLayer("ghost");
        foreach (Component component2 in this.m_segmentObject.GetComponents<Transform>())
          component2.gameObject.layer = this.m_segmentObject.layer;
        this.InvokeRepeating("UpdateSegments", 0.5f, 0.1f);
      }

      this.UpdateSegments();
    }

    private void UpdateSegments()
    {
      ((Component)this).transform.position = new Vector3(((Component)this).transform.position.x,
        ZoneSystem.instance.m_waterLevel + this.m_baseOffset,
        ((Component)this).transform.position.z);
      RaycastHit[] raycastHitArray = Physics.RaycastAll(((Component)this).transform.position,
        Vector3.down, 100f, this.m_terrainLayer);
      float num1 = 0.0f;
      for (int index = 0; index < raycastHitArray.Length; ++index)
      {
        if ((double)num1 < raycastHitArray[index].distance)
          num1 = raycastHitArray[index].distance;
      }

      int num2 = Mathf.CeilToInt(num1 / this.m_segmentHeight);
      for (int count = this.m_segmentObjects.Count; count < num2; ++count)
        this.m_segmentObjects.Add(Object.Instantiate<GameObject>(this.m_segmentObject,
          ((Component)this).transform.position, Quaternion.identity, ((Component)this).transform));
      while (this.m_segmentObjects.Count > num2)
      {
        Object.Destroy((Object)this.m_segmentObjects[this.m_segmentObjects.Count - 1]);
        this.m_segmentObjects.RemoveAt(this.m_segmentObjects.Count - 1);
      }

      for (int index = 0; index < this.m_segmentObjects.Count; ++index)
      {
        GameObject segmentObject = this.m_segmentObjects[index];
        segmentObject.SetActive(true);
        segmentObject.transform.localPosition =
          new Vector3(0.0f, -this.m_segmentHeight * (float)(index + 1), 0.0f);
        segmentObject.transform.localRotation = Quaternion.identity;
      }
    }
  }
}