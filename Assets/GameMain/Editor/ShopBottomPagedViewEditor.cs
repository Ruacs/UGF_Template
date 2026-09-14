using Lokas;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    [CustomEditor(typeof(ShopBottomPagedView))]
    [CanEditMultipleObjects]
    public class ShopBottomPagedViewEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PagedRect;
        private SerializedProperty m_PageTemplate;
        private SerializedProperty m_ItemPrefab;
        private SerializedProperty m_CatalogOverride;
        private SerializedProperty m_RefreshOnEnable;
        private SerializedProperty m_HideWhenEmpty;
        private SerializedProperty m_ItemReferenceSize;
        private SerializedProperty m_ItemMaxSize;
        private SerializedProperty m_ItemCenterOffset;
        private SerializedProperty m_SpaceBetweenPages;
        private SerializedProperty m_CurrentPageScale;
        private SerializedProperty m_PreviewPageScale;
        private SerializedProperty m_LoopSeamlessly;
        private SerializedProperty m_AutomaticallyMoveToNextPage;
        private SerializedProperty m_DelayBetweenPages;
        private SerializedProperty m_LoopEndlessly;

        private void OnEnable()
        {
            m_PagedRect = serializedObject.FindProperty("m_PagedRect");
            m_PageTemplate = serializedObject.FindProperty("m_PageTemplate");
            m_ItemPrefab = serializedObject.FindProperty("m_ItemPrefab");
            m_CatalogOverride = serializedObject.FindProperty("m_CatalogOverride");
            m_RefreshOnEnable = serializedObject.FindProperty("m_RefreshOnEnable");
            m_HideWhenEmpty = serializedObject.FindProperty("m_HideWhenEmpty");
            m_ItemReferenceSize = serializedObject.FindProperty("m_ItemReferenceSize");
            m_ItemMaxSize = serializedObject.FindProperty("m_ItemMaxSize");
            m_ItemCenterOffset = serializedObject.FindProperty("m_ItemCenterOffset");
            m_SpaceBetweenPages = serializedObject.FindProperty("m_SpaceBetweenPages");
            m_CurrentPageScale = serializedObject.FindProperty("m_CurrentPageScale");
            m_PreviewPageScale = serializedObject.FindProperty("m_PreviewPageScale");
            m_LoopSeamlessly = serializedObject.FindProperty("m_LoopSeamlessly");
            m_AutomaticallyMoveToNextPage = serializedObject.FindProperty("m_AutomaticallyMoveToNextPage");
            m_DelayBetweenPages = serializedObject.FindProperty("m_DelayBetweenPages");
            m_LoopEndlessly = serializedObject.FindProperty("m_LoopEndlessly");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawReferences();
            EditorGUILayout.Space(8f);
            DrawRuntimeOptions();
            EditorGUILayout.Space(8f);
            DrawPagingPanel();
            EditorGUILayout.Space(8f);
            DrawLiveLayoutPanel();

            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed)
                ApplyLiveSettings();
        }

        private void DrawReferences()
        {
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PagedRect);
            EditorGUILayout.PropertyField(m_PageTemplate);
            EditorGUILayout.PropertyField(m_ItemPrefab);
            EditorGUILayout.PropertyField(m_CatalogOverride);
        }

        private void DrawRuntimeOptions()
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_RefreshOnEnable);
            EditorGUILayout.PropertyField(m_HideWhenEmpty);
        }

        private void DrawLiveLayoutPanel()
        {
            EditorGUILayout.LabelField("Live Layout", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ItemReferenceSize);
            EditorGUILayout.PropertyField(m_ItemMaxSize);
            EditorGUILayout.PropertyField(m_ItemCenterOffset);
            m_SpaceBetweenPages.floatValue = EditorGUILayout.Slider("Fixed Card Gap", m_SpaceBetweenPages.floatValue, 0f, 200f);
            m_CurrentPageScale.floatValue = EditorGUILayout.Slider("Current Page Scale", m_CurrentPageScale.floatValue, 0.5f, 1.5f);
            m_PreviewPageScale.floatValue = EditorGUILayout.Slider("Preview Page Scale", m_PreviewPageScale.floatValue, 0.1f, 1.2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10"))
                    m_SpaceBetweenPages.floatValue -= 10f;

                if (GUILayout.Button("-1"))
                    m_SpaceBetweenPages.floatValue -= 1f;

                if (GUILayout.Button("44"))
                    m_SpaceBetweenPages.floatValue = 44f;

                if (GUILayout.Button("+1"))
                    m_SpaceBetweenPages.floatValue += 1f;

                if (GUILayout.Button("+10"))
                    m_SpaceBetweenPages.floatValue += 10f;
            }

            m_SpaceBetweenPages.floatValue = Mathf.Clamp(m_SpaceBetweenPages.floatValue, 0f, 200f);

            if (EditorGUI.EndChangeCheck())
                ApplyModifiedPropertiesNow();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Layout"))
                    ApplyModifiedPropertiesNow();

                if (GUILayout.Button("Refresh Pages"))
                    RefreshPages();
            }
        }

        private void DrawPagingPanel()
        {
            EditorGUILayout.LabelField("Paging", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LoopSeamlessly);
            EditorGUILayout.PropertyField(m_AutomaticallyMoveToNextPage);
            m_DelayBetweenPages.floatValue = EditorGUILayout.FloatField("Delay Between Pages", m_DelayBetweenPages.floatValue);
            EditorGUILayout.PropertyField(m_LoopEndlessly);

            m_DelayBetweenPages.floatValue = Mathf.Max(0f, m_DelayBetweenPages.floatValue);

            if (EditorGUI.EndChangeCheck())
                ApplyModifiedPropertiesNow();
        }

        private void ApplyModifiedPropertiesNow()
        {
            serializedObject.ApplyModifiedProperties();
            ApplyLiveSettings();
            serializedObject.Update();
        }

        private void ApplyLiveSettings()
        {
            foreach (Object targetObject in targets)
            {
                ShopBottomPagedView view = targetObject as ShopBottomPagedView;
                if (view == null)
                    continue;

                view.ApplyEditorSettings();
                EditorUtility.SetDirty(view);
            }
        }

        private void RefreshPages()
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object targetObject in targets)
            {
                ShopBottomPagedView view = targetObject as ShopBottomPagedView;
                if (view == null)
                    continue;

                view.ApplyEditorSettings();
                if (Application.isPlaying)
                    view.RefreshPages();

                EditorUtility.SetDirty(view);
            }

            serializedObject.Update();
        }
    }
}

