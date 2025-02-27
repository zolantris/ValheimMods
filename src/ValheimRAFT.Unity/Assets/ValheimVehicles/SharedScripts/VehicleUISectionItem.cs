#region

using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#endregion

// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class VehicleUIMenuSectionItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
  {
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI DescriptionText;
    public TextMeshProUGUI SavedValueText;
    public GameObject DescriptionRow;
    public Button SaveButton;
    public Button ResetButton;
    public Button InfoButton;

    // todo may add support for this.
    public GameObject Tooltip;
    public TextMeshProUGUI TooltipText;

    public TMP_InputField InputField;
    public Toggle Toggle;

    public Slider Slider;
    public TextMeshProUGUI SliderMaxValueText;
    public TextMeshProUGUI SliderCurrentValueText;
    public TextMeshProUGUI SliderMinValueText;

    public Transform ContentParent;
    public Transform EditRow;
    public Transform MainRow;

    public float currentValue;
    private float _defaultValue;
    private Action<float> _onUpdate;
    private SectionItemType _sectionData;

    private VehicleUiMenuComponent menuComponent;
    private void Start()
    {
      if (!menuComponent)
      {
        menuComponent = GetComponentInParent<VehicleUiMenuComponent>();
      }
      if (!ContentParent)
      {
        if (menuComponent.Content)
        {
          ContentParent = menuComponent.Content;
        }
        else
        {

          ContentParent = transform.parent ? transform.parent : transform;
        }
      }

      if (!EditRow)
      {
        EditRow = transform.Find("edit_row");
      }

      if (!MainRow)
      {
        MainRow = transform.Find("main_row");
      }

      if (!DescriptionRow)
      {
        DescriptionRow = transform.Find("description_row").gameObject;
      }

      if (!DescriptionText)
      {
        DescriptionText = DescriptionRow.transform.Find("text").GetComponent<TextMeshProUGUI>();
      }

      if (!TitleText)
      {
        TitleText = MainRow.Find("title").GetComponent<TextMeshProUGUI>();
      }

      if (!Toggle)
      {
        Toggle = EditRow.Find("toggle").GetComponent<Toggle>();
      }

      if (!SaveButton)
      {
        SaveButton = EditRow.Find("action_buttons/save_button").GetComponent<Button>();
      }

      if (!ResetButton)
      {
        ResetButton = EditRow.Find("action_buttons/reset_button").GetComponent<Button>();
      }
      if (!InfoButton)
      {
        InfoButton = MainRow.Find("info_section/info_button").GetComponent<Button>();
      }

      if (!SavedValueText)
      {
        SavedValueText = MainRow.Find("info_section/value_field/value").GetComponent<TextMeshProUGUI>();
      }

      if (!Slider)
      {
        Slider = EditRow.Find("slider").GetComponent<Slider>();
      }

      if (!SliderMinValueText)
      {
        SliderMinValueText = Slider.transform.Find("minValueText").GetComponent<TextMeshProUGUI>();
      }

      if (!SliderMaxValueText)
      {
        SliderMaxValueText = Slider.transform.Find("maxValueText").GetComponent<TextMeshProUGUI>();
      }

      if (!SliderCurrentValueText)
      {
        SliderCurrentValueText = Slider.transform.Find("Handle Slide Area/Handle/currentValueText").GetComponent<TextMeshProUGUI>();
      }

      if (!InputField)
      {
        InputField = EditRow.Find("input").GetComponent<TMP_InputField>();
      }

      EditRow.gameObject.SetActive(false);
      DescriptionRow.gameObject.SetActive(false);
      InputField.gameObject.SetActive(false);
      Slider.gameObject.SetActive(false);
      Toggle.gameObject.SetActive(false);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
      Debug.Log($"Clicked on: {gameObject.name}");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
      Debug.Log($"PointerEnter {eventData.position}");
      // Tooltip.SetActive(true);
      // TooltipText.text = "Tooltip information here";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      // Tooltip.SetActive(false);
    }

    public void Initialize(SectionItemType section)
    {
      _sectionData = section;
      TitleText.text = section.Title;
      DescriptionText.text = section.Description;
      _defaultValue = section.DefaultValue;

      switch (section.Variant)
      {
        case SectionItemVariant.Input:
          InputField.gameObject.SetActive(true);
          InputField.text = section.DefaultValue.ToString();
          InputField.onEndEdit.AddListener(value => _onUpdate(float.Parse(value)));
          break;
        case SectionItemVariant.Checkbox:
          Toggle.gameObject.SetActive(true);
          Toggle.isOn = section.DefaultValue > 0;
          Toggle.onValueChanged.AddListener(value => _onUpdate(value ? 1 : 0));
          break;
        case SectionItemVariant.Slider:
          Slider.gameObject.SetActive(true);
          Slider.minValue = section.MinValue;
          Slider.maxValue = section.MaxValue;
          Slider.value = section.DefaultValue;
          Slider.onValueChanged.AddListener(value => OnSliderUpdate(value));
          Slider.interactable = Application.isEditor;
          break;
      }

      SaveButton.onClick.AddListener(() => Debug.Log($"Saved: {TitleText.text}"));
      ResetButton.onClick.AddListener(ResetValue);
      InfoButton.onClick.AddListener(ToggleDescription);

      DescriptionRow.SetActive(false);
      // the description panel always should exist side by side with the SectionItem
      // DescriptionPanel.transform.SetParent(ContentParent);
      // DescriptionPanel.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
    }

    private void OnSliderUpdate(float val)
    {
      currentValue = val;

      if (SliderCurrentValueText)
      {
        SliderCurrentValueText.text = val.ToString(CultureInfo.CurrentCulture);
      }
    }

    private void ResetValue()
    {
      switch (_sectionData.Variant)
      {
        case SectionItemVariant.Input:
          InputField.text = _defaultValue.ToString();
          break;
        case SectionItemVariant.Checkbox:
          Toggle.isOn = _defaultValue > 0;
          break;
        case SectionItemVariant.Slider:
          Slider.value = _defaultValue;
          break;
      }
    }

    private void ToggleDescription()
    {
      var nextState = !DescriptionRow.activeSelf;
      DescriptionRow.SetActive(nextState);
      LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
    }
  }
}