using GameFramework.Localization;
using UnityEngine;

[CreateAssetMenu(menuName = "TMP/Language Entry", fileName = "LanguageEntry")]
public sealed class LanguageEntry : ScriptableObject
{
    [Tooltip("与游戏本地化系统的语言 key 保持一致，例如 ChineseSimplified、English")]
    public Language languageKey;
    public string displayName;
    public Sprite icon;
    [Tooltip("语言选择列表中显示的语言名图片。配置后优先使用图片，避免为了语言列表加载多套字体。")]
    public Sprite displayNameSprite;
    [Tooltip("TMP 字体资源名称，不填写扩展名，例如 Main、MiSans_ChineseSimplified")]
    public string fontAssetName;
    public TMPFontProfile fontProfile;

    private void OnValidate()
    {
        displayName = TMPLanguageFontConfig.GetDisplayName(languageKey);
    }
}
