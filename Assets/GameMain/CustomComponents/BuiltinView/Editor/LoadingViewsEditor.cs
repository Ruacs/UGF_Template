using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadingViews))]
public class LoadingViewsEditor : Editor
{
    private SerializedProperty m_OpenSequence;
    private SerializedProperty m_CloseSequence;
    private SerializedProperty m_ProgressBar;
    private SerializedProperty m_TextProgress;
    private SerializedProperty m_ProgressRoot;
    private SerializedProperty m_progressSlider;
    private SerializedProperty m_useSlider;

    private void OnEnable()
    {
        m_OpenSequence = serializedObject.FindProperty("m_OpenSequence");
        m_CloseSequence = serializedObject.FindProperty("m_CloseSequence");
        m_ProgressBar = serializedObject.FindProperty("m_ProgressBar");
        m_TextProgress = serializedObject.FindProperty("m_TextProgress");
        m_ProgressRoot = serializedObject.FindProperty("m_ProgressRoot");
        m_progressSlider = serializedObject.FindProperty("m_progressSlider");
        m_useSlider = serializedObject.FindProperty("m_useSlider");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_OpenSequence);
        EditorGUILayout.PropertyField(m_CloseSequence);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_ProgressRoot);
        EditorGUILayout.PropertyField(m_useSlider);

        EditorGUILayout.Space(4);
        if (m_useSlider.boolValue)
        {
            EditorGUILayout.PropertyField(m_progressSlider);
        }
        else
        {
            EditorGUILayout.PropertyField(m_ProgressBar);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(m_TextProgress);

        serializedObject.ApplyModifiedProperties();
    }
}
