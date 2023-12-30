using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class SailCreatorComponent : MonoBehaviour
{
  private static List<SailCreatorComponent> m_sailCreators = new List<SailCreatorComponent>();

  public static GameObject m_sailPrefab;

  public int m_sailSize;

  public void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      return;
    }

    if (m_sailCreators.Count > 0 &&
        (m_sailCreators[0].transform.position - base.transform.position).sqrMagnitude >
        SailComponent.m_maxDistanceSqr)
    {
      Logger.LogDebug("Sail creator corner distance too far.");
      m_sailCreators.Clear();
    }

    m_sailCreators.Add(this);
    if (m_sailCreators.Count >= m_sailSize)
    {
      Logger.LogDebug($"Creating new sail {m_sailCreators.Count}/{m_sailSize}");
      Vector3 center =
        (m_sailCreators[0].transform.position + m_sailCreators[1].transform.position) / 2f;
      SailComponent.m_sailInit = false;
      GameObject newSail = Object.Instantiate(m_sailPrefab, center, Quaternion.identity);
      SailComponent.m_sailInit = true;
      SailComponent sailcomp = newSail.GetComponent<SailComponent>();
      sailcomp.m_sailCorners = new List<Vector3>();
      for (int j = 0; j < m_sailSize; j++)
      {
        sailcomp.m_sailCorners.Add(m_sailCreators[j].transform.position - center);
      }

      sailcomp.LoadFromMaterial();
      sailcomp.CreateSailMesh();
      sailcomp.SaveZDO();
      Piece piece = newSail.GetComponent<Piece>();
      piece.SetCreator(m_sailCreators[0].GetComponent<Piece>().GetCreator());
      ZNetView netview = newSail.GetComponent<ZNetView>();
      MoveableBaseRootComponent mbroot =
        m_sailCreators[0].GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbroot)
      {
        mbroot.AddNewPiece(netview);
      }

      for (int i = 0; i < m_sailCreators.Count; i++)
      {
        Object.Destroy(m_sailCreators[i].gameObject);
      }

      m_sailCreators.Clear();
    }
  }
}