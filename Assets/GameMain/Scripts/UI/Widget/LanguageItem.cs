using System;
using GameFramework.Localization;
using Lokas;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LanguageItem : UIItemBase
{
    public Toggle _selfToggle;
    public Image _icon;
    public TMPro.TMP_Text _languageName;
    public TMPro.TMP_Text _languageName_sel;
    [SerializeField] private Image _languageNameImage;
    [SerializeField] UnityEvent m_OnSelected;
    [SerializeField] UnityEvent m_OnUnselected;

    [SerializeField] Color m_DefaultColor;
    [SerializeField] Color m_SelectedColor;

    public Language language;


    Action m_Action;
    protected override void OnInit()
    {
        base.OnInit();
        if (_selfToggle == null)
        {
            return;
        }

        _selfToggle.onValueChanged.RemoveAllListeners();
        _selfToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }



    public void SetData(LanguageEntry entry, ToggleGroup toggleGroup, Action action)
    {
        if (entry == null || _selfToggle == null)
        {
            return;
        }

        _selfToggle.group = toggleGroup;
        language = entry.languageKey;
        m_Action = action;
        SetDisplayName(entry);
        var isOn = GameEntry.SaveData.Language == language;
        _selfToggle.SetIsOnWithoutNotify(isOn);
        RefreshToggleState(isOn);
    }

    private void SetDisplayName(LanguageEntry entry)
    {
        if (entry.displayNameSprite != null)
        {
            Image image = EnsureDisplayNameImage();
            if (image != null)
            {
                image.sprite = entry.displayNameSprite;
                image.SetNativeSize();
                image.enabled = true;
                image.gameObject.SetActive(true);
            }

            if (_languageName != null)
            {
                _languageName.gameObject.SetActive(false);
            }

            if (_languageName_sel != null)
            {
                _languageName_sel.gameObject.SetActive(false);
            }

            return;
        }

        if (_languageNameImage != null)
        {
            _languageNameImage.enabled = false;
            _languageNameImage.gameObject.SetActive(false);
        }

        if (_languageName != null)
        {
            _languageName.gameObject.SetActive(true);
            _languageName.text = entry.displayName;
        }

        if (_languageName_sel != null)
        {
            _languageName_sel.gameObject.SetActive(true);
            _languageName_sel.text = entry.displayName;
        }
    }

    private Image EnsureDisplayNameImage()
    {
        if (_languageNameImage != null)
        {
            return _languageNameImage;
        }

        if (_languageName == null)
        {
            return null;
        }

        RectTransform labelRect = _languageName.rectTransform;
        GameObject imageObject = new GameObject("LanguageNameImage", typeof(RectTransform), typeof(Image));
        imageObject.layer = _languageName.gameObject.layer;
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.SetParent(labelRect.parent, false);
        imageRect.anchorMin = labelRect.anchorMin;
        imageRect.anchorMax = labelRect.anchorMax;
        imageRect.anchoredPosition = labelRect.anchoredPosition;
        imageRect.sizeDelta = labelRect.sizeDelta;
        imageRect.pivot = labelRect.pivot;

        _languageNameImage = imageObject.GetComponent<Image>();
        _languageNameImage.raycastTarget = false;
        _languageNameImage.preserveAspect = true;
        return _languageNameImage;
    }

    public void SetIcon(Sprite icon)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
        }
    }

    public void OnToggleValueChanged(bool isOn)
    {
        RefreshToggleState(isOn);
        if (isOn && language != GameEntry.SaveData.Language)
        {
            GameEntry.Sound.PlaySound(SoundId.UI_Click);
            GameEntry.SaveData.Language = language;
            m_Action?.Invoke();
        }
    }

    void RefreshToggleState(bool isOn)
    {
        if (_selfToggle != null && _selfToggle.graphic != null)
        {
            _selfToggle.graphic.gameObject.SetActive(isOn);
        }

        if (isOn)
        {
            m_OnSelected?.Invoke();
            if (_languageName_sel != null)
            {
                SetLabelTextColor(m_SelectedColor);
            }
        }
        else
        {
            m_OnUnselected?.Invoke();
            if (_languageName_sel != null)
            {
                SetLabelTextColor(m_DefaultColor);
            }
        }
    }

    public void SetLabelTextColor(Color color)
    {
        if (_languageName != null)
        {
            _languageName.color = color;
        }

        if (_languageName_sel != null)
        {
            _languageName_sel.color = color;
        }
    }
   

}
