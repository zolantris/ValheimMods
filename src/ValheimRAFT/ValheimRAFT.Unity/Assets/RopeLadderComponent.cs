using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeLadderComponent : MonoBehaviour
{
    public GameObject m_stepObject;
    public LineRenderer m_ropeLine;

    public float m_stepDistance = 0.33333f;
    public float m_ladderHeight = 1f;

    private List<GameObject> m_steps = new List<GameObject>();

    // Start is called before the first frame update
    void Awake()
    {
        m_stepObject = transform.Find("step").gameObject;
        m_ropeLine = GetComponent<LineRenderer>();

        UpdateSteps();
    }

    private void UpdateSteps()
    {
        if (!m_stepObject)
            return;

        var steps = m_ladderHeight / m_stepDistance;

        while(m_steps.Count > steps)
        {
            Destroy(m_steps[0]);
            m_steps.RemoveAt(0);
        }

        while(m_steps.Count <= steps)
        {
            var go = GameObject.Instantiate(m_stepObject, transform.position + new Vector3(0f, -m_stepDistance * m_steps.Count, 0f), transform.rotation, transform);
            m_steps.Add(go);
        }

        m_ropeLine.SetPosition(0, transform.position + new Vector3(0.4f, 0f, 0f));
        m_ropeLine.SetPosition(1, transform.position + new Vector3(0.4f, -m_stepDistance * (m_steps.Count-1), 0f));
        m_ropeLine.SetPosition(2, transform.position + new Vector3(-0.4f, -m_stepDistance * (m_steps.Count-1), 0f));
        m_ropeLine.SetPosition(3, transform.position + new Vector3(-0.4f, 0f, 0f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
