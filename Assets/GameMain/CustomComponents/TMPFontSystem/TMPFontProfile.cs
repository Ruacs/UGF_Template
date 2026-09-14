using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 一套字体样式配置。字体资源由 TMPLanguageFontConfig 按语言名称异步加载。
/// 不直接持有 Material，材质由 TMPFontManager 按需创建并缓存。
/// </summary>
[CreateAssetMenu(menuName = "TMP/Font Profile", fileName = "TMPFontProfile")]
public class TMPFontProfile : ScriptableObject
{
    [Serializable]
    public class StyleEntry
    {
        [Tooltip("与 TMPStyleApplier 上的 styleKey 对应，区分大小写")]
        public string key;
        [Tooltip("留空则使用 fontAsset 的默认材质")]
        public FontStylePreset preset;
    }

    [Tooltip("各样式 key → FontStylePreset 的映射")]
    public List<StyleEntry> styles = new List<StyleEntry>();

    /// <summary>
    /// 返回 key 对应的预设，找不到时返回 null（调用方降级使用 fontAsset 默认材质）。
    /// </summary>
    public FontStylePreset GetPreset(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (var entry in styles)
        {
            if (entry.key == key)
                return entry.preset;
        }
        return null;
    }
}
