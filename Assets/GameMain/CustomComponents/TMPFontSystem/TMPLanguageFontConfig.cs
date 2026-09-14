using System.Collections.Generic;
using UnityEngine;
using GameFramework.Localization;

/// <summary>
/// 多语言字体配置数据库：将语言 key 映射到独立的 LanguageEntry 和 TMPFontProfile。
/// </summary>
[CreateAssetMenu(menuName = "TMP/Language Font Config", fileName = "TMPLanguageFontConfig")]
public class TMPLanguageFontConfig : ScriptableObject
{
    public static readonly Dictionary<Language, string> LanguageDisplayNames = new()
    {
        { Language.Arabic, "العربية" },
        { Language.Bengali, "বাংলা" },
        { Language.ChineseSimplified, "中文" },
        { Language.ChineseTraditional, "繁體中文" },
        { Language.English, "English" },
        { Language.Filipino, "Filipino" },
        { Language.French, "Français" },
        { Language.German, "Deutsch" },
        { Language.Hindi, "हिन्दी" },
        { Language.Indonesian, "Indonesia" },
        { Language.Italian, "Italiano" },
        { Language.Burmese, "မြန်မာ" },
        { Language.Polish, "Polski" },
        { Language.Swedish, "Svenska" },
        { Language.Urdu, "اردو" },
        { Language.Japanese, "日本語" },
        { Language.Korean, "한국어" },
        { Language.Portuguese, "Português" },
        { Language.PortugueseBrazil, "Português" },
        { Language.PortuguesePortugal, "Português" },
        { Language.Russian, "Русский" },
        { Language.Spanish, "Español" },
        { Language.Thai, "ไทย" },
        { Language.Turkish, "Türkçe" },
        { Language.Vietnamese, "Tiếng Việt" },
    };

    [Tooltip("找不到匹配语言时使用的兜底配置")]
    public TMPFontProfile defaultProfile;

    public List<LanguageEntry> languageProfiles = new List<LanguageEntry>();

    public TMPFontProfile GetProfile(Language languageKey)
    {
        if (languageProfiles != null)
        {
            foreach (var entry in languageProfiles)
            {
                if (entry != null && entry.languageKey == languageKey && entry.fontProfile != null)
                    return entry.fontProfile;
            }
        }

        return defaultProfile;
    }

    public LanguageEntry GetEntry(Language languageKey)
    {
        if (languageProfiles != null)
        {
            foreach (var entry in languageProfiles)
            {
                if (entry != null && entry.languageKey == languageKey)
                    return entry;
            }
        }

        return null;
    }

    public static string GetDisplayName(Language languageKey)
    {
        return LanguageDisplayNames.TryGetValue(languageKey, out string displayName)
            ? displayName
            : languageKey.ToString();
    }

}
