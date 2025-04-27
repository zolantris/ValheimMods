using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ValheimVehicles.ValheimVehicles.API;
namespace ValheimVehicles.ValheimVehicles.GUI;

public class DropdownRefreshOnHover : MonoBehaviour, IPointerEnterHandler
{
  private TMP_Dropdown dropdown = null!;
  public Action<TMP_Dropdown>? OnPointerEnterAction = null!;
  
  private void Awake()
  {
    dropdown = GetComponent<TMP_Dropdown>();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    OnPointerEnterAction?.Invoke(dropdown);
  }
}