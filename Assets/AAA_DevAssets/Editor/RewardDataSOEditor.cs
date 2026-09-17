using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    [CustomEditor(typeof(RewardDataSO), true)]
    public sealed class RewardDataSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Entries"), new GUIContent("奖励清单"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ChestStyle"), new GUIContent("宝箱品质 / 外观"));
            EditorGUILayout.HelpBox("1 项显示资源图标，2 项及以上显示指定宝箱。宝箱品质手动配置；单项忽略宝箱外观。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            try { ((RewardDataSO)target).ValidateEntries(); }
            catch (System.InvalidOperationException error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
        }
    }

    [CustomPropertyDrawer(typeof(RewardEntry))]
    public sealed class RewardEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (EditorGUIUtility.singleLineHeight + 2) * 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
            position.y += position.height + 2;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Definition"), new GUIContent("奖励资源"));
            position.y += position.height + 2;
            var mode = property.FindPropertyRelative("m_GrantMode");
            EditorGUI.PropertyField(position, mode, new GUIContent("发放方式"));
            position.y += position.height + 2;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Amount"),
                new GUIContent(mode.enumValueIndex == (int)RewardGrantMode.UnlimitedUse ? "持续时长（秒）" : "数量"));
            EditorGUI.EndProperty();
        }
    }
}
