using UnityEditor;
using UnityEngine;

// 指定只对 DisplayConfigSO 及其子类生效
[CustomEditor(typeof(DisplayConfigSO), true)]
[CanEditMultipleObjects]
public class DisplayConfigSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var spriteProp = serializedObject.FindProperty("sprite");
        var nameProp = serializedObject.FindProperty("displayName");
        var idProp = serializedObject.FindProperty("id");

        // Script（只读）
        GUI.enabled = false;
        EditorGUILayout.ObjectField(
            "Script",
            MonoScript.FromScriptableObject((ScriptableObject)target),
            typeof(ScriptableObject),
            false
        );
        GUI.enabled = true;

        // ID（只读）
        GUI.enabled = false;
        EditorGUILayout.PropertyField(idProp, new GUIContent("ID(?)", "统一生成分配"));
        GUI.enabled = true;

        // 名字
        EditorGUILayout.PropertyField(nameProp);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Icon");
        GUILayout.FlexibleSpace();

        Rect previewRect = GUILayoutUtility.GetRect(
            80, 80,
            GUILayout.Width(80),
            GUILayout.Height(80)
        );

        EditorGUI.BeginChangeCheck();
        Sprite newSprite = (Sprite)EditorGUI.ObjectField(
            previewRect,
            spriteProp.objectReferenceValue,
            typeof(Sprite),
            false
        );

        if (EditorGUI.EndChangeCheck())
        {
            spriteProp.objectReferenceValue = newSprite;
        }

        EditorGUILayout.EndHorizontal();

        DrawPropertiesExcluding(serializedObject, "m_Script", "id", "displayName", "sprite");

        serializedObject.ApplyModifiedProperties();
    }
}
