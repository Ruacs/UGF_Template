using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.FontCharset
{
    [CustomEditor(typeof(FontStylePreset))]
    public sealed class FontStylePresetEditor : UnityEditor.Editor
    {
        private FontStylePreviewPanel preview;
        private void OnEnable()
        {
            preview = new FontStylePreviewPanel();
            Undo.undoRedoPerformed += Refresh;
            EditorApplication.projectChanged += Refresh;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Refresh;
            EditorApplication.projectChanged -= Refresh;
            preview?.Dispose();
        }
        private void Refresh() { preview?.Invalidate(); Repaint(); }
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            var preset = (FontStylePreset)target;
            EditorGUILayout.LabelField("样式预览", EditorStyles.boldLabel);
            preview.DrawFontPicker();
            preview.DrawControls();
            preview.DrawPreview(preset);
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("复制为新预设")) Duplicate(preset);
                using (new EditorGUI.DisabledScope(!EditorUtility.IsPersistent(preset)))
                    if (GUILayout.Button("保存此预设")) AssetDatabase.SaveAssetIfDirty(preset);
            }
            EditorGUILayout.HelpBox("下方参数直接编辑当前预设，支持撤销；共享预设会影响其引用方。需要独立效果时先复制。", MessageType.Info);
            DrawDefaultInspector();
        }

        private static void Duplicate(FontStylePreset source)
        {
            string originalPath = AssetDatabase.GetAssetPath(source);
            string directory = string.IsNullOrEmpty(originalPath) ? "Assets" : Path.GetDirectoryName(originalPath).Replace('\\', '/');
            string path = EditorUtility.SaveFilePanelInProject("复制为新预设", source.name + "_Copy", "asset", "选择新预设的位置", directory);
            if (string.IsNullOrEmpty(path)) return;
            // Never overwrite the source or an existing preset through the duplication action.
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var copy = Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);
            AssetDatabase.SaveAssetIfDirty(copy);
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
            GUIUtility.ExitGUI();
        }
    }
}
