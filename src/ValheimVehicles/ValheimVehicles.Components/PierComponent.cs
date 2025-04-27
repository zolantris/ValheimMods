using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.Components;

public class PierComponent : MonoBehaviour
{
  public ZNetView m_nview;

  public GameObject m_segmentObject;

  public float m_segmentHeight;

  public float m_baseOffset;

  private int m_terrainLayer;

  private List<GameObject> m_segmentObjects = new();

  public void Awake()
  {
    m_terrainLayer = LayerMask.GetMask("terrain");
    if (ZNetView.m_forceDisableInit)
    {
      var wnt = GetComponent<WearNTear>();
      if ((bool)wnt && (bool)wnt.m_new) wnt.m_new.SetActive(false);
      m_segmentObject.SetActive(false);
      Collider[] colliders =
        m_segmentObject.GetComponentsInChildren<Collider>(true);
      for (var j = 0; j < colliders.Length; j++) colliders[j].enabled = false;
      m_segmentObject.layer = LayerMask.NameToLayer("ghost");
      Transform[] transforms = m_segmentObject.GetComponents<Transform>();
      for (var i = 0; i < transforms.Length; i++)
        transforms[i].gameObject.layer = m_segmentObject.layer;
      InvokeRepeating("UpdateSegments", 0.5f, 0.1f);
    }

    UpdateSegments();
  }

  private void UpdateSegments()
  {
    transform.position = new Vector3(transform.position.x,
      ZoneSystem.instance.m_waterLevel + m_baseOffset, transform.position.z);
    var hits = Physics.RaycastAll(transform.position, Vector3.down, 100f,
      m_terrainLayer);
    var furthestDistance = 0f;
    for (var k = 0; k < hits.Length; k++)
      if (furthestDistance < hits[k].distance)
        furthestDistance = hits[k].distance;
    var segments = Mathf.CeilToInt(furthestDistance / m_segmentHeight);
    for (var j = m_segmentObjects.Count; j < segments; j++)
    {
      var segmentObj = Instantiate(m_segmentObject, transform.position,
        Quaternion.identity, transform);
      m_segmentObjects.Add(segmentObj);
    }

    while (m_segmentObjects.Count > segments)
    {
      var segmentObject = m_segmentObjects[m_segmentObjects.Count - 1];
      Destroy(segmentObject);
      m_segmentObjects.RemoveAt(m_segmentObjects.Count - 1);
    }

    for (var i = 0; i < m_segmentObjects.Count; i++)
    {
      var obj = m_segmentObjects[i];
      obj.SetActive(true);
      obj.transform.localPosition =
        new Vector3(0f, (0f - m_segmentHeight) * (float)(i + 1), 0f);
      obj.transform.localRotation = Quaternion.identity;
    }
  }
}