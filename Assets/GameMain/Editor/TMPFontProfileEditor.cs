using TMPro;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TMPFontProfile))]
public class TMPFontProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var profile = (TMPFontProfile)target;
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            $"字体资源由语言配置按需加载；材质由 TMPFontComponent 在运行时按需创建。\n已配置样式数：{profile.styles.Count}",
            MessageType.Info);
    }
}
