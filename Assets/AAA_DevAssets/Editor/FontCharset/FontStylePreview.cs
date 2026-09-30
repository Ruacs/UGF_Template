using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.FontCharset
{
    // Shared by the preset Inspector and charset window. Owns preview objects only.
    internal sealed class FontStylePreview : IDisposable
    {
        private PreviewRenderUtility renderer;
        private TextMeshPro text;
        private TMP_FontAsset sourceFont, fontCopy;
        private Material material;
        public string MissingCharacters { get; private set; } = "";

        internal Texture Render(Rect rect, TMP_FontAsset font, FontStylePreset preset, string sample, float size, Color background)
        {
            if (font == null || font.material == null || rect.width < 1 || rect.height < 1) return null;
            if (renderer == null)
            {
                renderer = new PreviewRenderUtility();
                var go = new GameObject("TMP style preview", typeof(TextMeshPro)) { hideFlags = HideFlags.HideAndDontSave };
                renderer.AddSingleGO(go);
                text = go.GetComponent<TextMeshPro>();
                text.isOrthographic = true;
                text.richText = false;
                text.enableAutoSizing = false;
                text.enableWordWrapping = true;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
            }
            if (sourceFont != font || fontCopy == null)
            {
                ReleaseFont();
                sourceFont = font;
                fontCopy = UnityEngine.Object.Instantiate(font);
                fontCopy.name = "TMP style preview font";
                fontCopy.hideFlags = HideFlags.HideAndDontSave;
                // Preview never populates an atlas or changes a source font's fallback list.
                fontCopy.atlasPopulationMode = AtlasPopulationMode.Static;
                fontCopy.fallbackFontAssetTable = new List<TMP_FontAsset>();
                fontCopy.ReadFontAssetDefinition();
                material = new Material(font.material) { name = "TMP style preview material", hideFlags = HideFlags.HideAndDontSave };
            }
            material.shader = font.material.shader;
            material.CopyPropertiesFromMaterial(font.material);
            if (preset != null) preset.ApplyTo(material);
            text.font = fontCopy;
            text.fontSharedMaterial = material;
            text.fontSize = size;
            text.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, rect.width - 32), Mathf.Max(1, rect.height - 32));
            text.text = FilterMissing(sample ?? "");
            text.UpdateMeshPadding();
            text.ForceMeshUpdate();
            renderer.camera.orthographic = true;
            // Fixed scale: changing the preview font size must visibly change its size.
            renderer.camera.orthographicSize = rect.height * 0.5f;
            renderer.camera.transform.position = new Vector3(0, 0, -10);
            renderer.camera.nearClipPlane = 0.1f;
            renderer.camera.farClipPlane = 100;
            renderer.camera.clearFlags = CameraClearFlags.SolidColor;
            renderer.camera.backgroundColor = background;
            renderer.BeginPreview(rect, GUIStyle.none);
            renderer.Render(true);
            return renderer.EndPreview();
        }

        private string FilterMissing(string sample)
        {
            var output = new StringBuilder();
            var missing = new HashSet<uint>();
            for (int i = 0; i < sample.Length; i++)
            {
                uint code = sample[i];
                string glyph = sample[i].ToString();
                if (char.IsHighSurrogate(sample[i]) && i + 1 < sample.Length && char.IsLowSurrogate(sample[i + 1]))
                {
                    code = (uint)char.ConvertToUtf32(sample[i], sample[i + 1]);
                    glyph = sample.Substring(i++, 2);
                }
                if (code == '\n' || code == '\r' || fontCopy.characterLookupTable.ContainsKey(code)) output.Append(glyph);
                else if (code == '\t') output.Append(' ');
                else
                {
                    missing.Add(code);
                    // Avoid TMP consulting project-wide dynamic fallbacks during a read-only preview.
                    if (fontCopy.characterLookupTable.ContainsKey('?')) output.Append('?');
                }
            }
            MissingCharacters = string.Join(" ", missing.Take(12).Select(c => "U+" + c.ToString("X4")));
            if (missing.Count > 12) MissingCharacters += " …";
            return output.ToString();
        }

        private void ReleaseFont()
        {
            if (text != null) { text.text = ""; text.font = null; }
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
            if (fontCopy != null) UnityEngine.Object.DestroyImmediate(fontCopy);
            material = null; fontCopy = null; sourceFont = null;
        }

        public void Dispose()
        {
            // Destroy text/submeshes before the font and material they refer to.
            renderer?.Cleanup(); renderer = null; text = null;
            ReleaseFont();
        }
    }

    internal sealed class FontStylePreviewPanel : IDisposable
    {
        internal TMP_FontAsset Font;
        internal string Sample = "领取奖励 0123456789\nReward +100%";
        private float size = 36;
        private Color background = new Color(0.12f, 0.12f, 0.12f);
        private int mode;
        private readonly FontStylePreview current = new FontStylePreview();
        private readonly FontStylePreview original = new FontStylePreview();
        private string[] fontPaths;
        private static readonly string[] Modes = { "当前预设", "默认材质", "左右对照" };

        internal void DrawFontPicker()
        {
            if (fontPaths == null) RefreshFonts();
            using (new EditorGUILayout.HorizontalScope())
            {
                Font = (TMP_FontAsset)EditorGUILayout.ObjectField("预览字体", Font, typeof(TMP_FontAsset), false);
                if (GUILayout.Button("切换", GUILayout.Width(46)))
                {
                    var menu = new GenericMenu();
                    foreach (string path in fontPaths)
                    {
                        string captured = path;
                        string label = System.IO.Path.GetFileNameWithoutExtension(path);
                        if (fontPaths.Count(p => System.IO.Path.GetFileNameWithoutExtension(p) == label) > 1)
                            label += " (" + path.Replace("/", " › ") + ")";
                        menu.AddItem(new GUIContent(label, path),
                            AssetDatabase.GetAssetPath(Font) == path, () => Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(captured));
                    }
                    if (fontPaths.Length == 0) menu.AddDisabledItem(new GUIContent("尚无 TMP 字体"));
                    menu.ShowAsContext();
                }
            }
        }

        private void RefreshFonts()
        {
            fontPaths = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            if (Font == null)
            {
                string first = fontPaths.FirstOrDefault(p => p.EndsWith("/MFont_CNS.asset")) ?? fontPaths.FirstOrDefault();
                if (first != null) Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(first);
            }
        }

        internal void DrawControls()
        {
            Sample = EditorGUILayout.TextArea(Sample, GUILayout.Height(48));
            size = EditorGUILayout.Slider("预览字号", size, 12, 96);
            using (new EditorGUILayout.HorizontalScope())
            {
                background = EditorGUILayout.ColorField("背景", background);
                if (GUILayout.Button("深", GUILayout.Width(28))) background = new Color(0.12f, 0.12f, 0.12f);
                if (GUILayout.Button("浅", GUILayout.Width(28))) background = new Color(0.9f, 0.9f, 0.9f);
            }
            mode = GUILayout.Toolbar(mode, Modes);
        }

        internal void DrawPreview(FontStylePreset preset)
        {
            if (Font == null || Font.material == null)
            {
                EditorGUILayout.HelpBox("请选择带有默认材质的 TMP 字体。", MessageType.Info);
                return;
            }
            Rect area = GUILayoutUtility.GetRect(80, 210, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                if (mode == 2)
                {
                    var left = new Rect(area.x, area.y, (area.width - 6) / 2, area.height);
                    var right = new Rect(left.xMax + 6, area.y, left.width, area.height);
                    DrawOne(original, left, null, "默认材质");
                    DrawOne(current, right, preset, preset != null ? "当前预设" : "未指定预设");
                }
                else DrawOne(current, area, mode == 1 ? null : preset, mode == 1 || preset == null ? "默认材质" : preset.name);
            }
            string missing = current.MissingCharacters;
            EditorGUILayout.HelpBox(string.IsNullOrEmpty(missing) ? "直接调整预设参数即可查看效果；左右对照使用相同字号。" :
                "当前字库缺字：" + missing + "。以 ? 替代或省略；请补齐字库后查看。",
                string.IsNullOrEmpty(missing) ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.LabelField("预览仅使用当前字库，不加载运行时 fallback；字号与背景仅影响此预览。", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawOne(FontStylePreview preview, Rect rect, FontStylePreset preset, string label)
        {
            Texture texture = preview.Render(rect, Font, preset, Sample, size, background);
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            GUI.Label(new Rect(rect.x + 6, rect.y + 4, rect.width - 12, 18), label, EditorStyles.whiteMiniLabel);
        }

        internal void Invalidate() { current.Dispose(); original.Dispose(); fontPaths = null; }
        public void Dispose() => Invalidate();
    }
}
