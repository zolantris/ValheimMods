#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public enum SectionItemVariant
  {
    Input,
    Checkbox,
    Slider
  }

  [Serializable]
  public struct SectionItemType
  {
    public string Title;
    public string Description;
    public SectionItemVariant Variant;
    public float MinValue;
    public float MaxValue;
    public float DefaultValue;
  }

  public class VehicleUiMenuComponent : MonoBehaviour
  {
    public Transform Content;
    public GameObject InputPrefab;
    public GameObject CheckboxPrefab;
    public GameObject SliderPrefab;
    public List<SectionItemType> Sections;

    private readonly List<VehicleUIMenuSectionItem> _sectionItems = new();

    private void Start()
    {
      GenerateSections();
    }

    private void Update()
    {
#if !VALHEIM
      HandleEditorInput();
#endif
    }

    private void GenerateSections()
    {
      foreach (var section in Sections)
      {
        var prefab = GetPrefab(section.Variant);
        if (prefab == null) continue;

        var sectionGO = Instantiate(prefab, Content);
        sectionGO.transform.SetParent(Content, false);
        var sectionItem = sectionGO.GetComponent<VehicleUIMenuSectionItem>();
        sectionItem.Initialize(section);
        _sectionItems.Add(sectionItem);
      }
    }

    private GameObject GetPrefab(SectionItemVariant variant)
    {
      return variant switch
      {
        SectionItemVariant.Input => InputPrefab,
        SectionItemVariant.Checkbox => CheckboxPrefab,
        SectionItemVariant.Slider => SliderPrefab,
        _ => null
      };
    }

#if !VALHEIM
    private void HandleEditorInput()
    {
      foreach (var sectionItem in _sectionItems)
      {
        if (sectionItem.Slider != null && sectionItem.Slider.gameObject.activeSelf)
        {
          sectionItem.Slider.interactable = true;
        }
      }
    }
#endif
  }
}