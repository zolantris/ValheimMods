using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class SailCreatorComponent : MonoBehaviour
{
  private static List<SailCreatorComponent> m_sailCreators = [];

  public static GameObject sailPrefab;
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
      CreateSailFromCorners();
    }
  }

  public void CreateSailFromCorners()
  {
    Logger.LogDebug($"Creating new sail {m_sailCreators.Count}/{m_sailSize}");

    var center =
      (m_sailCreators[0].transform.position + m_sailCreators[1].transform.position) / 2f;
    if (!sailPrefab) return;

    // must switch initialization state to false otherwise LoadZDO will run and see the ZDO is erroring and delete itself
    var sailPrefabInstance = Instantiate(sailPrefab, center, Quaternion.identity);
    var sailComponent = sailPrefabInstance.GetComponent<SailComponent>();
    sailComponent.m_sailCorners = [];

    for (var j = 0; j < m_sailSize; j++)
    {
      sailComponent.m_sailCorners.Add(m_sailCreators[j].transform.position - center);
    }

    sailComponent.LoadFromMaterial();
    sailComponent.CreateSailMesh();
    sailComponent.SaveZdo();

    var piece = sailPrefabInstance.GetComponent<Piece>();
    piece.SetCreator(m_sailCreators[0].GetComponent<Piece>().GetCreator());

    var netView = sailPrefabInstance.GetComponent<ZNetView>();
    AddToVehicle(netView);

    foreach (var t in m_sailCreators)
    {
      Destroy(t.gameObject);
    }

    m_sailCreators.Clear();
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

  private bool AddToMoveableBaseRoot(ZNetView netView)
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

  private bool AddToBasicVehicle(ZNetView netView)
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