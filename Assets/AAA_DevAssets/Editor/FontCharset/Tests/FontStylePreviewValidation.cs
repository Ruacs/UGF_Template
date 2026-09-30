using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.FontCharset.Tests
{
    // Runs on a Repaint event because PreviewRenderUtility needs an IMGUI context.
    public sealed class FontStylePreviewValidation : EditorWindow
    {
        private bool executed;
        private string result = "正在验证预览渲染……";
        private const string Output = "Library/FontCharset/StylePreviewValidation";

        [MenuItem("Game Framework/字体与字符集/运行样式预览验证", priority = 161)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑态运行。");
            var window = GetWindow<FontStylePreviewValidation>(true, "样式预览验证");
            window.executed = false;
            window.minSize = new Vector2(660, 260);
            window.Show();
            window.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(result, MessageType.Info);
            if (Event.current.type != EventType.Repaint || executed) return;
            executed = true;
            Directory.CreateDirectory(Output);
            try
            {
                Validate();
                result = "通过：预设渲染、实时修改、默认材质恢复、字号、字体切换、缺字处理及资产/场景隔离。";
                File.WriteAllText(Output + "/result.txt", "PASS\n" + result);
                Debug.Log(result + " 报告: " + Output);
            }
            catch (Exception ex)
            {
                result = ex.ToString();
                File.WriteAllText(Output + "/result.txt", "FAIL\n" + result);
                Debug.LogException(ex);
            }
            Repaint();
        }

        private static void Validate()
        {
            var config = FontCharsetConfig.Load();
            var fonts = config != null ? config.entries.Where(e => e.targetFont != null).Select(e => e.targetFont).Distinct().ToArray() :
                AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }).Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).Where(f => f != null).ToArray();
            Assert(fonts.Length > 0, "需要至少一个现有 TMP 字体。");
            var font = fonts[0];
            string fontJson = EditorJsonUtility.ToJson(font);
            string materialJson = EditorJsonUtility.ToJson(font.material);
            bool fontDirty = EditorUtility.IsDirty(font), materialDirty = EditorUtility.IsDirty(font.material);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            bool sceneDirty = scene.isDirty;
            var roots = scene.GetRootGameObjects().Select(o => o.GetInstanceID()).OrderBy(id => id).ToArray();
            int objectCount = PreviewObjectCount();
            var preset = CreateInstance<FontStylePreset>();
            preset.hideFlags = HideFlags.HideAndDontSave;
            preset.faceColor = Color.red;
            preset.outlineWidth = 0.12f;
            preset.enableUnderlay = true;
            preset.underlayOffset = new Vector2(1, -1);
            string presetJson = EditorJsonUtility.ToJson(preset);
            try
            {
                using (var preview = new FontStylePreview())
                {
                    var baseline = Capture(preview, font, null, 36, "default");
                    var styled = Capture(preview, font, preset, 36, "preset-red");
                    Assert(Difference(baseline, styled) > 100, "预设必须改变实际渲染像素。");
                    Assert(EditorJsonUtility.ToJson(preset) == presetJson, "预览不得修改预设参数。");
                    preset.faceColor = Color.green;
                    var changed = Capture(preview, font, preset, 36, "preset-green");
                    Assert(Difference(styled, changed) > 100, "修改预设必须立即反映到渲染中。");
                    var restored = Capture(preview, font, null, 36, "restored-default");
                    Assert(Difference(baseline, restored) == 0, "切回默认材质不得残留描边/阴影/颜色。");
                    var large = Capture(preview, font, null, 64, "size-64");
                    Assert(Foreground(large) > Foreground(baseline) * 1.4f, "增大字号必须改变实际显示尺寸。");
                    preset.faceColor = Color.white;
                    preset.outlineColor = Color.blue;
                    preset.outlineWidth = 0;
                    preset.enableUnderlay = false;
                    var plain = Capture(preview, font, preset, 48, "effects-off");
                    preset.outlineWidth = 0.25f;
                    var outline = Capture(preview, font, preset, 48, "outline-blue");
                    Assert(Difference(plain, outline) > 10, "描边参数必须改变实际渲染。");
                    preset.enableUnderlay = true;
                    preset.underlayColor = Color.green;
                    var shadow = Capture(preview, font, preset, 48, "shadow-green");
                    Assert(Difference(outline, shadow) > 10, "阴影参数必须改变实际渲染。");
                    foreach (var other in fonts.Skip(1)) Capture(preview, other, preset, 36, "font-" + other.name);
                    preview.Render(new Rect(0, 30, 640, 210), font, preset, "123\U0010FFFF", 36, Color.black);
                    Assert(preview.MissingCharacters.Contains("10FFFF"), "缺字必须明确报告。");
                }
                Assert(EditorJsonUtility.ToJson(font) == fontJson && EditorJsonUtility.ToJson(font.material) == materialJson, "源字体/材质不得改变。");
                Assert(EditorUtility.IsDirty(font) == fontDirty && EditorUtility.IsDirty(font.material) == materialDirty, "不得改变资产脏状态。");
                Assert(scene.isDirty == sceneDirty, "预览不得弄脏活动场景。");
                Assert(scene.GetRootGameObjects().Select(o => o.GetInstanceID()).OrderBy(id => id).SequenceEqual(roots), "预览不得在活动场景遗留对象。");
                Assert(PreviewObjectCount() == objectCount, "释放后不得遗留预览对象。");
            }
            finally { DestroyImmediate(preset); }
        }

        private static Color32[] Capture(FontStylePreview preview, TMP_FontAsset font, FontStylePreset preset, float size, string name)
        {
            Texture rendered = preview.Render(new Rect(0, 30, 640, 210), font, preset, "0123456789", size, Color.black);
            Assert(rendered != null, "预览需生成纹理。");
            RenderTexture previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(640, 210, 0, RenderTextureFormat.ARGB32);
            var image = new Texture2D(640, 210, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(rendered, rt);
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 640, 210), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); DestroyImmediate(image); }
        }
        private static int Foreground(Color32[] image) => image.Count(c => c.r > 20 || c.g > 20 || c.b > 20);
        private static int Difference(Color32[] a, Color32[] b) => a.Where((c, i) => Math.Abs(c.r - b[i].r) + Math.Abs(c.g - b[i].g) + Math.Abs(c.b - b[i].b) > 6).Count();
        private static int PreviewObjectCount() => Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Count(o => o != null && o.name.StartsWith("TMP style preview"));
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
