using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public class VehicleMeshMaskManager : MonoBehaviour
{
  private ZNetView m_nview;
  private VehicleShip vehicleShip;

  private List<Vector3> meshCoordinates = [new Vector3(0, 0, 0), new Vector3(0, 20, 0)];

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }

  public void Init(VehicleShip vehicle)
  {
    vehicleShip = vehicle;
  }

  public void AddPoint(Vector3 point)
  {
    meshCoordinates.AddItem(point);
  }

  public void OnTogglePanel(bool state)
  {
    var guiMan = Jotunn.Managers.GUIManager.Instance;
    var styledPanel =
      guiMan.CreateWoodpanel(null, Vector2.zero, Vector2.zero, new Vector2(50, 50), 600, 750);
    var scrollView =
      Jotunn.Managers.GUIManager.Instance.CreateScrollView(styledPanel.transform, false, true, 50,
        5, ColorBlock.defaultColorBlock, Color.gray, 500, 500);

    var guiButtonGo = guiMan.CreateButton($"Close", styledPanel.transform, Vector2.zero,
      Vector2.zero, new Vector2(0, 0));
    var guiButton = guiButtonGo.GetComponent<Button>();

    guiButton.onClick.AddListener(() =>
    {
      Logger.LogMessage("ClickedButton");
      scrollView.SetActive(!scrollView.activeInHierarchy);
    });

    var pos = 0;
    foreach (var meshCoordinate in meshCoordinates)
    {
      guiMan.CreateButton($"Coordinate: {meshCoordinate}", scrollView.transform, Vector2.zero,
        Vector2.zero, new Vector2(0, pos));
      pos += 50;
    }
  }

  public void UpdateMeshWithVehicleBounds()
  {
  }

  public static bool OnTriggerPanelFromLever(GameObject go, bool state)
  {
    var instance = go.GetComponentInParent<VehicleMeshMaskManager>();
    if (instance != null)
    {
      instance.OnTogglePanel(state);
      return true;
    }

    return false;
  }
}