using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class SailCreatorComponent : MonoBehaviour
{
  private static List<SailCreatorComponent> m_sailCreators = [];

  public static GameObject m_sailPrefab;

  public int m_sailSize;

  public void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      return;
    }

    if (m_sailCreators.Count > 4)
    {
      m_sailCreators.Clear();
      return;
    }

    foreach (var sailCreator in m_sailCreators.ToList())
    {
      if (sailCreator == null) m_sailCreators.Remove(sailCreator);
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
      var newSail = Instantiate(m_sailPrefab, center, Quaternion.identity);
      SailComponent.m_sailInit = true;
      var sailcomp = newSail.GetComponent<SailComponent>();
      sailcomp.m_sailCorners = new List<Vector3>();
      for (var j = 0; j < m_sailSize; j++)
      {
        sailcomp.m_sailCorners.Add(m_sailCreators[j].transform.position - center);
      }

      sailcomp.LoadFromMaterial();
      sailcomp.CreateSailMesh();
      sailcomp.SaveZDO();
      var piece = newSail.GetComponent<Piece>();
      piece.SetCreator(m_sailCreators[0].GetComponent<Piece>().GetCreator());
      var netview = newSail.GetComponent<ZNetView>();

      AddToVehicle(netview);

      foreach (var t in m_sailCreators)
      {
        Destroy(t.gameObject);
      }

      m_sailCreators.Clear();
    }
  }

  /**
   * <description/> Delegates to the VehicleController that it is placed within.
   * - This avoids the additional check if possible.
   */
  public void AddToVehicle(ZNetView netView)
  {
    if (AddToBasicVehicle(netView))
    {
      return;
    }

    AddToMoveableBaseRoot(netView);
  }

  public bool AddToMoveableBaseRoot(ZNetView netView)
  {
    var mbr =
      m_sailCreators[0].GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      mbr.AddNewPiece(netView);
      return true;
    }

    return false;
  }

  public bool AddToBasicVehicle(ZNetView netView)
  {
    var baseVehicle =
      m_sailCreators[0].GetComponentInParent<BaseVehicleController>();
    if ((bool)baseVehicle)
    {
      baseVehicle.AddNewPiece(netView);
      return true;
    }

    return false;
  }
}