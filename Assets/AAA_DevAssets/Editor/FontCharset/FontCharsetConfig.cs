using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Lokas.Editor.FontCharset
{
    [Serializable]
    public sealed class FontCharsetEntry
    {
        public bool selected = true;
        public string language;
        public Font sourceFont;
        public TMP_FontAsset targetFont;
        public TextAsset charset;
        public List<TextAsset> localizationFiles = new List<TextAsset>();
        [TextArea(2, 6)] public string extraText = "0123456789%+-.,:/ ()";
        public bool autoSize = true;
        public int pointSize;
        public int atlasWidth = 1024;
        public int atlasHeight = 1024;
        public int padding = 7;
        public GlyphRenderMode renderMode = GlyphRenderMode.SDF;
        [HideInInspector] public string generatedSignature;
    }

    // Editor-only authoring configuration; LanguageEntry remains the runtime authority.
    public sealed class FontCharsetConfig : ScriptableObject
    {
        public TMPLanguageFontConfig languageConfig;
        public List<FontCharsetEntry> entries = new List<FontCharsetEntry>();
        public const string DefaultPath = "Assets/AAA_DevAssets/Fonts/Editor/FontCharsetConfig.asset";

        public static FontCharsetConfig Load() => AssetDatabase.LoadAssetAtPath<FontCharsetConfig>(DefaultPath);

        public static FontCharsetConfig CreateDefault()
        {
            var existing = Load();
            if (existing != null) return existing;
            var config = CreateInstance<FontCharsetConfig>();
            config.languageConfig = AssetDatabase.LoadAssetAtPath<TMPLanguageFontConfig>("Assets/GameMain/ScriptableObjects/TMPLanguageFontConfig.asset");
            string[] languages = { "ChineseSimplified", "ChineseTraditional", "Japanese", "Korean" };
            string[] suffixes = { "CNS", "CNT", "JP", "KR" };
            for (int i = 0; i < languages.Length; i++)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameMain/Fonts/MFont_" + suffixes[i] + ".asset");
                if (font == null) throw new InvalidOperationException("找不到字体 MFont_" + suffixes[i]);
                var sourcePath = AssetDatabase.GUIDToAssetPath(font.creationSettings.sourceFontFileGUID);
                config.entries.Add(new FontCharsetEntry
                {
                    language = languages[i], targetFont = font,
                    sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath),
                    charset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AAA_DevAssets/Fonts/Charset/unity_sdf_charset_" + suffixes[i] + ".txt"),
                    localizationFiles = new List<TextAsset> { AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameMain/Localization/" + languages[i] + "/" + languages[i] + ".xml") },
                    pointSize = Mathf.RoundToInt(font.faceInfo.pointSize), atlasWidth = font.atlasWidth,
                    atlasHeight = font.atlasHeight, padding = font.atlasPadding, renderMode = font.atlasRenderMode
                });
            }
            if (!AssetDatabase.IsValidFolder("Assets/AAA_DevAssets/Fonts/Editor"))
                AssetDatabase.CreateFolder("Assets/AAA_DevAssets/Fonts", "Editor");
            AssetDatabase.CreateAsset(config, DefaultPath);
            AssetDatabase.SaveAssetIfDirty(config);
            return config;
        }
    }
}
