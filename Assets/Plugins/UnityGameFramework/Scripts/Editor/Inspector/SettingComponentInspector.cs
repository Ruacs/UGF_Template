//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(SettingComponent))]
    internal sealed class SettingComponentInspector : GameFrameworkInspector
    {
        private const float SettingRowSpacing = 2f;
        private const float ViewButtonWidth = 32f;
        private const float RemoveButtonWidth = 22f;

        private HelperInfo<SettingHelperBase> m_SettingHelperInfo = new HelperInfo<SettingHelperBase>("Setting");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SettingComponent t = (SettingComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_SettingHelperInfo.Draw();
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Setting Count", t.Count >= 0 ? t.Count.ToString() : "<Unknown>");
                if (t.Count > 0)
                {
                    string[] settingNames = t.GetAllSettingNames();
                    foreach (string settingName in settingNames)
                    {
                        if (DrawSetting(t, settingName))
                        {
                            break;
                        }
                    }
                }
            }

            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Save Settings"))
                {
                    t.Save();
                }
                if (GUILayout.Button("Remove All Settings"))
                {
                    t.RemoveAllSettings();
                }
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private bool DrawSetting(SettingComponent settingComponent, string settingName)
        {
            string settingValue = settingComponent.GetString(settingName) ?? "<Null>";
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float contentWidth = rowRect.width - ViewButtonWidth - RemoveButtonWidth - SettingRowSpacing * 3f;
            float settingNameWidth = Mathf.Min(260f, Mathf.Max(40f, contentWidth * 0.4f));
            float settingValueWidth = contentWidth - settingNameWidth;
            if (settingValueWidth < 60f)
            {
                settingValueWidth = 60f;
                settingNameWidth = Mathf.Max(40f, contentWidth - settingValueWidth);
            }

            Rect settingNameRect = new Rect(rowRect.x, rowRect.y, settingNameWidth, rowRect.height);
            Rect settingValueRect = new Rect(settingNameRect.xMax + SettingRowSpacing, rowRect.y, settingValueWidth, rowRect.height);
            Rect viewButtonRect = new Rect(settingValueRect.xMax + SettingRowSpacing, rowRect.y, ViewButtonWidth, rowRect.height);
            Rect removeButtonRect = new Rect(viewButtonRect.xMax + SettingRowSpacing, rowRect.y, RemoveButtonWidth, rowRect.height);

            EditorGUI.LabelField(settingNameRect, new GUIContent(settingName, settingName));
            EditorGUI.SelectableLabel(settingValueRect, settingValue, EditorStyles.textField);
            if (GUI.Button(viewButtonRect, new GUIContent("...", "View full value"), EditorStyles.miniButton))
            {
                PopupWindow.Show(viewButtonRect, new SettingValuePopup(settingName, settingValue));
            }

            if (GUI.Button(removeButtonRect, new GUIContent("X", string.Format("Remove '{0}'", settingName)), EditorStyles.miniButton))
            {
                settingComponent.RemoveSetting(settingName);
                return true;
            }

            return false;
        }

        private sealed class SettingValuePopup : PopupWindowContent
        {
            private const float WindowWidth = 520f;
            private const float WindowHeight = 260f;

            private readonly string m_SettingName;
            private readonly string m_SettingValue;
            private Vector2 m_ScrollPosition = Vector2.zero;
            private GUIStyle m_ValueStyle = null;

            public SettingValuePopup(string settingName, string settingValue)
            {
                m_SettingName = settingName;
                m_SettingValue = settingValue;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(WindowWidth, WindowHeight);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(m_SettingName, m_SettingName), EditorStyles.boldLabel);
                if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    EditorGUIUtility.systemCopyBuffer = m_SettingValue;
                }

                EditorGUILayout.EndHorizontal();

                if (m_ValueStyle == null)
                {
                    m_ValueStyle = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true
                    };
                }

                float valueWidth = Mathf.Max(100f, rect.width - 32f);
                float valueHeight = Mathf.Max(rect.height - 38f, m_ValueStyle.CalcHeight(new GUIContent(m_SettingValue), valueWidth));
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
                EditorGUILayout.SelectableLabel(m_SettingValue, m_ValueStyle, GUILayout.Height(valueHeight), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndScrollView();
            }
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_SettingHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_SettingHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
