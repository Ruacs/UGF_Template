using UnityEditor;

namespace Lokas.Editor
{
    [CustomEditor(typeof(global::Lokas.ShopItemStyleConfigSO))]
    public class ShopItemStyleConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                if (iterator.propertyPath == "contentTextStyleKey")
                {
                    global::TMPStyleApplierEditor.DrawStyleKeyDropdown(iterator, "Content Text Style Key", Repaint);
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

