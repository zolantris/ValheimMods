// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.SailCreatorComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{
  public class SailCreatorComponent : MonoBehaviour
  {
    private static List<SailCreatorComponent> m_sailCreators = new List<SailCreatorComponent>();
    public static GameObject m_sailPrefab;
    public int m_sailSize;

    public void Awake()
    {
      if (ZNetView.m_forceDisableInit)
        return;
      int num;
      if (SailCreatorComponent.m_sailCreators.Count > 0)
      {
        Vector3 vector3 = m_sailCreators[0].transform.position - transform.position;
        num = vector3.sqrMagnitude > (double)SailComponent.m_maxDistanceSqr
          ? 1
          : 0;
      }
      else
        num = 0;

      if (num != 0)
      {
        ZLog.Log((object)"Sail creator corner distance too far.");
        SailCreatorComponent.m_sailCreators.Clear();
      }

      SailCreatorComponent.m_sailCreators.Add(this);
      if (SailCreatorComponent.m_sailCreators.Count < this.m_sailSize)
        return;
      ZLog.Log((object)string.Format("Creating new sail {0}/{1}",
        (object)SailCreatorComponent.m_sailCreators.Count, (object)this.m_sailSize));
      Vector3 vector3_1 = (
        m_sailCreators[0].transform.position + m_sailCreators[1].transform.position) / 2f;
      SailComponent.m_sailInit = false;
      GameObject gameObject = Object.Instantiate<GameObject>(SailCreatorComponent.m_sailPrefab,
        vector3_1, Quaternion.identity);
      SailComponent.m_sailInit = true;
      SailComponent component1 = gameObject.GetComponent<SailComponent>();
      component1.m_sailCorners = new List<Vector3>();
      for (int index = 0; index < this.m_sailSize; ++index)
        component1.m_sailCorners.Add(m_sailCreators[index].transform.position - vector3_1);
      component1.LoadFromMaterial();
      component1.CreateSailMesh();
      component1.SaveZDO();
      gameObject.GetComponent<Piece>().SetCreator(
        ((Component)SailCreatorComponent.m_sailCreators[0]).GetComponent<Piece>().GetCreator());
      ZNetView component2 = gameObject.GetComponent<ZNetView>();
      MoveableBaseRootComponent componentInParent =
        ((Component)SailCreatorComponent.m_sailCreators[0])
        .GetComponentInParent<MoveableBaseRootComponent>();
      if (componentInParent)
        componentInParent.AddNewPiece(component2);
      for (int index = 0; index < SailCreatorComponent.m_sailCreators.Count; ++index)
        Object.Destroy((Object)((Component)SailCreatorComponent.m_sailCreators[index]).gameObject);
      SailCreatorComponent.m_sailCreators.Clear();
    }
  }
}