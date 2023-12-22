using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT;

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
		m_terrainLayer = LayerMask.GetMask("terrain");
		if (ZNetView.m_forceDisableInit)
		{
			WearNTear wnt = GetComponent<WearNTear>();
			if ((bool)wnt && (bool)wnt.m_new)
			{
				wnt.m_new.SetActive(value: false);
			}
			m_segmentObject.SetActive(value: false);
			Collider[] colliders = m_segmentObject.GetComponentsInChildren<Collider>(includeInactive: true);
			for (int j = 0; j < colliders.Length; j++)
			{
				colliders[j].enabled = false;
			}
			m_segmentObject.layer = LayerMask.NameToLayer("ghost");
			Transform[] transforms = m_segmentObject.GetComponents<Transform>();
			for (int i = 0; i < transforms.Length; i++)
			{
				transforms[i].gameObject.layer = m_segmentObject.layer;
			}
			InvokeRepeating("UpdateSegments", 0.5f, 0.1f);
		}
		UpdateSegments();
	}

	private void UpdateSegments()
	{
		base.transform.position = new Vector3(base.transform.position.x, ZoneSystem.instance.m_waterLevel + m_baseOffset, base.transform.position.z);
		RaycastHit[] hits = Physics.RaycastAll(base.transform.position, Vector3.down, 100f, m_terrainLayer);
		float furthestDistance = 0f;
		for (int k = 0; k < hits.Length; k++)
		{
			if (furthestDistance < hits[k].distance)
			{
				furthestDistance = hits[k].distance;
			}
		}
		int segments = Mathf.CeilToInt(furthestDistance / m_segmentHeight);
		for (int j = m_segmentObjects.Count; j < segments; j++)
		{
			GameObject segmentObj = Object.Instantiate(m_segmentObject, base.transform.position, Quaternion.identity, base.transform);
			m_segmentObjects.Add(segmentObj);
		}
		while (m_segmentObjects.Count > segments)
		{
			GameObject segmentObject = m_segmentObjects[m_segmentObjects.Count - 1];
			Object.Destroy(segmentObject);
			m_segmentObjects.RemoveAt(m_segmentObjects.Count - 1);
		}
		for (int i = 0; i < m_segmentObjects.Count; i++)
		{
			GameObject obj = m_segmentObjects[i];
			obj.SetActive(value: true);
			obj.transform.localPosition = new Vector3(0f, (0f - m_segmentHeight) * (float)(i + 1), 0f);
			obj.transform.localRotation = Quaternion.identity;
		}
	}
}
