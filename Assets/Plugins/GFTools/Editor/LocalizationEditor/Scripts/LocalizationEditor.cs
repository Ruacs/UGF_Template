
using GameFramework.Localization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Language = GameFramework.Localization.Language;

namespace GFTools.LocalizationEditor
{
    [System.Serializable]
    public class LocalizationEntry
    {
        public string Key;
        public string Value;
        /// <summary>
        /// 本地化词条对象
        /// </summary>
        /// <param name="key"> 键 </param>
        /// <param name="value"> 值 </param>
        public LocalizationEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public LocalizationEntry()
        {

        }
    }

    public enum FileType
    {
        Xml,
        Json,
    }


    public class LocalizationEditor : EditorWindow
    {
        public const float WindowWidth = 700;
        public const float WindowHeight = 720;
        private const float MinWindowHeight = 420;
        private const int MaxEditUndoCount = 100;
        /// <summary>
        /// 文件夹地址
        /// </summary>
        private string folderPath = "Assets/GameMain/Localization/";

        private string m_FilePath = "English";

        private FileType m_FileType = FileType.Xml;
        /// <summary>
        /// 语言类型列表
        /// </summary>
        private List<string> m_LanguageTypeList;
        private ReorderableList reorderableLanguageList;
        private string m_CurLanguage;
        public string CurLanguage
        {
            get => m_CurLanguage;
            set
            {
                if (m_CurLanguage != value)
                {
                    m_CurLanguage = value;
                    // Debug.Log("Selected: " + value);
                }
            }
        }

        /// <summary>
        /// 允许拖拽排序
        /// </summary>
        public bool AllowDrag
        {
            get => EditorPrefs.GetBool("AllowDrag", false);
            set => EditorPrefs.SetBool("AllowDrag", value);

        }
        /// <summary>
        /// 关闭时保存
        /// </summary>
        public bool SaveWhenClosed
        {
            get => EditorPrefs.GetBool("SaveWhenClosed", true);
            set => EditorPrefs.SetBool("SaveWhenClosed", value);
        }
        public int SelectLanguageIndex
        {
            get
            {
                m_selectLanguageIndex = EditorPrefs.GetInt("SelectLanguageIndex", 0);
                return m_selectLanguageIndex;
            }

            set
            {
                m_selectLanguageIndex = value;
                EditorPrefs.SetInt("SelectLanguageIndex", m_selectLanguageIndex);
            }
        }
        /// <summary>
        /// 当前选择的语言索引
        /// </summary>
        private int m_selectLanguageIndex = 0;
        /// <summary>
        /// 键值对
        /// </summary>
        private Dictionary<string, string> m_KeyValuePairs;
        /// <summary>
        /// 键值对条目列表 便于修改
        /// </summary>
        private List<LocalizationEntry> m_CurEntryList = new List<LocalizationEntry>();
        private List<LocalizationEntry> m_FilteredEntryList = new List<LocalizationEntry>();
        private List<int> m_FilteredEntrySourceIndices = new List<int>();
        /// <summary>
        /// 存储已添加语言的本地化条目
        /// </summary>
        private Dictionary<string, List<LocalizationEntry>> m_AllLangguageLocalitionEntry = new Dictionary<string, List<LocalizationEntry>>();
        private ReorderableList m_ReorderableEntryList;

        private GUIStyle _evenRowStyle;  //偶数行
        private GUIStyle _oddRowStyle;   //奇数行

        private bool m_SaveWhenClosed = true;
        private bool m_AllowDrag = false;

        private Vector2 m_EntryScrollPos;
        private Vector2 m_LanguageListScrollPos;
        private Vector2 m_MainScrollPos;
        private string m_SearchText = string.Empty;
        private readonly Stack<EditUndoRecord> m_EditUndoRecords = new Stack<EditUndoRecord>();

        private class EditUndoRecord
        {
            public string Language;
            public int EntryIndex;
            public string Key;
            public string Value;
        }

        [MenuItem("Game Framework/多语言编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationEditor>("多语言编辑器");
            window.minSize = new Vector2(WindowWidth, MinWindowHeight); // 可选：设置最小尺寸
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            // 计算居中位置
            float posX = main.x + (main.width - WindowWidth) / 2;
            float posY = main.y + (main.height - WindowHeight) / 2;

            window.position = new Rect(posX, posY, WindowWidth, WindowHeight);
            string iconPath = LocalizationUtility.GetIconPath(window);

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            window.titleContent = new GUIContent(" 本地化编辑器", icon, "Localization Editor");
        }

        private void OnEnable()
        {
            LoadLanguageTypeList();
            // 先加载所有语言的本地化词条
            for (int i = 0; i < m_LanguageTypeList.Count; i++)
            {
                LoadLocalizationEntry(m_LanguageTypeList[i]);
            }
            // 加载完成后，根据SelectLanguageIndex设置当前词条列表
            if (m_LanguageTypeList.Count > 0 && SelectLanguageIndex < m_LanguageTypeList.Count)
            {
                string selectedLanguage = m_LanguageTypeList[SelectLanguageIndex];
                CurLanguage = selectedLanguage;
                if (m_AllLangguageLocalitionEntry.TryGetValue(selectedLanguage, out var localEntry) && localEntry != null)
                {
                    m_CurEntryList = localEntry;
                }
                else if (m_AllLangguageLocalitionEntry.Count > 0)
                {
                    // 如果当前选择的语言没有加载成功，使用第一个语言
                    var firstLanguage = m_AllLangguageLocalitionEntry.Keys.First();
                    m_CurEntryList = m_AllLangguageLocalitionEntry[firstLanguage];
                    SelectLanguageIndex = m_LanguageTypeList.IndexOf(firstLanguage);
                    CurLanguage = firstLanguage;
                }
            }
            InitReorderableEntryList();
        }

        private void OnDisable()
        {
            Debug.Log("关闭");
            if (SaveWhenClosed)
                SaveAll();
        }

        private void OnGUI()
        {
            HandleEditUndoShortcut();
            m_MainScrollPos = EditorGUILayout.BeginScrollView(m_MainScrollPos);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("多语言文件夹路径(?)", "eg:Assets/AAAGame/Localization/"), labelStyle, GUILayout.Width(200));
                folderPath = EditorGUILayout.TextField(folderPath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("文件存储路径(?)", "eg:ChineseSimplified"), labelStyle, GUILayout.Width(200));
                m_FilePath = EditorGUILayout.TextField(m_FilePath);
                m_FileType = (FileType)EditorGUILayout.EnumPopup(m_FileType, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            DrawLanguagePopup();
            EditorGUILayout.Space();
            DrawLocalizationEntry();
            EditorGUILayout.Space();
            DrawLanguageTypeList();

            DrawBottomButton();

            EditorGUILayout.EndScrollView();
        }

        private bool otherFoldout = false;
        /// <summary>
        /// 绘制底部按钮
        /// </summary>
        private void DrawBottomButton()
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();


            if (GUILayout.Button("保存当前", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveToFile();
            }
            if (GUILayout.Button("保存全部", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveAll();
            }
            EditorGUI.BeginDisabledGroup(m_EditUndoRecords.Count == 0);
            if (GUILayout.Button(new GUIContent("撤回编辑", "撤回上一次 Key/Value 输入框修改（Ctrl+Z）"), GUILayout.Width(100), GUILayout.Height(30)))
            {
                UndoLastTextEdit();
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(new GUIContent("同步所有关键词(?)", "将当前关键词列表应用到所有语言文件"), GUILayout.Width(120), GUILayout.Height(30)))
            {
                SyncAllKeycodes();
            }
            if (GUILayout.Button(new GUIContent("导出Key常量(?)", "将所有Key导出为CS常量文件"), GUILayout.Width(120), GUILayout.Height(30)))
            {
                ExportKeysToCS();
            }

            GUILayout.FlexibleSpace();


            EditorGUILayout.EndHorizontal();

            otherFoldout = EditorGUILayout.Foldout(otherFoldout, new GUIContent("其他设置"));

            EditorGUILayout.BeginHorizontal("box");
            if (otherFoldout)
            {
                SaveWhenClosed = EditorGUILayout.Toggle(new GUIContent("关闭时保存："), SaveWhenClosed);
                AllowDrag = EditorGUILayout.Toggle(new GUIContent("允许拖拽排序："), AllowDrag);
                m_ReorderableEntryList.draggable = AllowDrag && !IsSearchActive();
            }


            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// 绘制语言选择下拉框
        /// </summary>
        private void DrawLanguagePopup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("选择语言：", EditorStyles.boldLabel);
            int newIndex = EditorGUILayout.Popup(SelectLanguageIndex, m_LanguageTypeList.ToArray(), GUILayout.Width(300f));

            if (newIndex != SelectLanguageIndex)
            {
                SelectLanguageIndex = newIndex;
                OnPopupLanguageChange(SelectLanguageIndex);
            }

            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// 绘制词条列表
        /// </summary>
        private void DrawLocalizationEntry()
        {
            if (m_ReorderableEntryList != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("本地化词条列表", EditorStyles.boldLabel, GUILayout.Width(120f));
                GUILayout.FlexibleSpace();
                DrawSearchToolbar();
                EditorGUILayout.EndHorizontal();

                m_EntryScrollPos = EditorGUILayout.BeginScrollView(m_EntryScrollPos, GUILayout.Height(380f));
                m_ReorderableEntryList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSearchToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("搜索：", GUILayout.Width(45f));

            EditorGUI.BeginChangeCheck();
            string newSearchText = EditorGUILayout.TextField(m_SearchText);
            if (EditorGUI.EndChangeCheck())
            {
                m_SearchText = newSearchText;
                RebuildSearchResults();
            }

            if (GUILayout.Button("清空", GUILayout.Width(55f)))
            {
                m_SearchText = string.Empty;
                RebuildSearchResults();
                GUI.FocusControl(null);
            }

            int totalCount = m_CurEntryList != null ? m_CurEntryList.Count : 0;
            int displayCount = GetDisplayedEntryList().Count;
            EditorGUILayout.LabelField($"{displayCount}/{totalCount}", GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// 绘制语言类型列表
        /// </summary>
        private void DrawLanguageTypeList()
        {

            if (reorderableLanguageList != null)
            {
                m_LanguageListScrollPos = EditorGUILayout.BeginScrollView(m_LanguageListScrollPos, GUILayout.Height(140));
                reorderableLanguageList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 加载指定语言的本地化词条
        /// </summary>
        /// <param name="language"></param>
        private List<LocalizationEntry> LoadLocalizationEntry(string language)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            List<LocalizationEntry> localizationEntryList = new List<LocalizationEntry>();
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("路径为空，无法加载文件。");
                return null;
            }
            string fullPath = GetCurrentLanguageFilePath(language);
            if (!File.Exists(fullPath))
            {
                Debug.LogError(fullPath + " is not exists!");
            }

            localizationEntryList = ReadLocalizationEntryList(m_FileType, fullPath);

            // 检查语言是否已经在字典中，如果存在则更新，不存在则添加
            if (m_AllLangguageLocalitionEntry.ContainsKey(language))
            {
                m_AllLangguageLocalitionEntry[language] = localizationEntryList;
            }
            else
            {
                m_AllLangguageLocalitionEntry.Add(language, localizationEntryList);
            }

            return localizationEntryList;
        }

        /// <summary>
        /// 初始化可重排序的本地化词条列表
        /// </summary>
        private void InitReorderableEntryList()
        {
            RebuildSearchResults();
            m_ReorderableEntryList = new ReorderableList(GetDisplayedEntryList(), typeof(LocalizationEntry), true, true, true, true);
            m_ReorderableEntryList.drawHeaderCallback = (Rect rect) =>
            {
                float padding = 4f;
                float labelWidth = 40f;
                float deleteButtonWidth = 50f;
                float contentWidth = rect.width - labelWidth - deleteButtonWidth - padding * 5;
                float keyWidth = contentWidth * 0.4f;
                float valueWidth = contentWidth * 0.6f;

                float x = rect.x + padding;
                float y = rect.y;

                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(4, 4, 2, 2)
                };

                EditorGUI.LabelField(new Rect(x, y, labelWidth - padding, EditorGUIUtility.singleLineHeight), "序号", headerStyle);
                x += labelWidth;

                EditorGUI.LabelField(new Rect(x + padding, y, keyWidth, EditorGUIUtility.singleLineHeight), "Key", headerStyle);
                x += keyWidth + padding;

                EditorGUI.LabelField(new Rect(x + padding, y, valueWidth, EditorGUIUtility.singleLineHeight), "Value", headerStyle);
                x += valueWidth + padding;

                EditorGUI.LabelField(new Rect(x + padding, y, deleteButtonWidth, EditorGUIUtility.singleLineHeight), "操作", headerStyle);
            };


            m_ReorderableEntryList.draggable = AllowDrag && !IsSearchActive();
            m_ReorderableEntryList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var displayedEntryList = GetDisplayedEntryList();
                if (index < 0 || index >= displayedEntryList.Count)
                {
                    return;
                }

                var entry = displayedEntryList[index];
                int sourceIndex = GetEntrySourceIndex(index);

                float padding = 4f;
                float labelWidth = 40f;
                float deleteButtonWidth = 50f;
                float contentWidth = rect.width - labelWidth - deleteButtonWidth - padding * 5;
                float keyWidth = contentWidth * 0.4f;
                float valueWidth = contentWidth * 0.6f;

                float y = rect.y + 2;
                if (IsSearchActive())
                {
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight + 4), new Color(0.23f, 0.27f, 0.18f));
                }

                // 显示序号
                EditorGUI.LabelField(
                    new Rect(rect.x + padding, y, labelWidth - padding, EditorGUIUtility.singleLineHeight),
                    (sourceIndex + 1).ToString(),
                    EditorStyles.miniButtonLeft
                );

                Rect keyRect = new Rect(rect.x + labelWidth + padding, y, keyWidth, EditorGUIUtility.singleLineHeight);
                Rect valueRect = new Rect(rect.x + labelWidth + keyWidth + padding * 2, y, valueWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                string newKey = EditorGUI.TextField(
                    keyRect,
                    entry.Key
                );
                if (EditorGUI.EndChangeCheck())
                {
                    RecordTextEdit(sourceIndex, entry.Key, entry.Value);
                    entry.Key = newKey;
                    EditorUtility.SetDirty(this);
                }

                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUI.TextField(
                    valueRect,
                    entry.Value
                );
                if (EditorGUI.EndChangeCheck())
                {
                    RecordTextEdit(sourceIndex, entry.Key, entry.Value);
                    entry.Value = newValue;
                    EditorUtility.SetDirty(this);
                }

                if (GUI.Button(
                    new Rect(rect.x + labelWidth + keyWidth + valueWidth + padding * 3, y, deleteButtonWidth, EditorGUIUtility.singleLineHeight),
                    "删除"))
                {
                    RemoveEntryAtIndex(sourceIndex);
                    GUIUtility.ExitGUI();
                }
            };


            m_ReorderableEntryList.onAddCallback = OnAddEntryCallback;

            m_ReorderableEntryList.onRemoveCallback = OnRemoveCallBack;

        }

        private bool IsSearchActive()
        {
            return !string.IsNullOrWhiteSpace(m_SearchText);
        }

        private List<LocalizationEntry> GetDisplayedEntryList()
        {
            return IsSearchActive() ? m_FilteredEntryList : m_CurEntryList;
        }

        private int GetEntrySourceIndex(int displayIndex)
        {
            if (!IsSearchActive())
            {
                return displayIndex;
            }

            if (displayIndex >= 0 && displayIndex < m_FilteredEntrySourceIndices.Count)
            {
                return m_FilteredEntrySourceIndices[displayIndex];
            }

            return -1;
        }

        private void RebuildSearchResults()
        {
            if (m_FilteredEntryList == null)
            {
                m_FilteredEntryList = new List<LocalizationEntry>();
            }
            if (m_FilteredEntrySourceIndices == null)
            {
                m_FilteredEntrySourceIndices = new List<int>();
            }

            m_FilteredEntryList.Clear();
            m_FilteredEntrySourceIndices.Clear();

            if (!IsSearchActive() || m_CurEntryList == null)
            {
                if (m_ReorderableEntryList != null)
                {
                    m_ReorderableEntryList.list = m_CurEntryList;
                    m_ReorderableEntryList.draggable = AllowDrag;
                }
                return;
            }

            for (int i = 0; i < m_CurEntryList.Count; i++)
            {
                LocalizationEntry entry = m_CurEntryList[i];
                string key = entry.Key ?? string.Empty;
                string value = entry.Value ?? string.Empty;
                if (key.IndexOf(m_SearchText, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf(m_SearchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_FilteredEntryList.Add(entry);
                    m_FilteredEntrySourceIndices.Add(i);
                }
            }

            if (m_ReorderableEntryList != null)
            {
                m_ReorderableEntryList.list = m_FilteredEntryList;
                m_ReorderableEntryList.draggable = false;
                m_ReorderableEntryList.index = Mathf.Clamp(m_ReorderableEntryList.index, -1, m_FilteredEntryList.Count - 1);
            }
        }

        private void HandleEditUndoShortcut()
        {
            Event current = Event.current;
            if (current == null || m_EditUndoRecords.Count == 0)
            {
                return;
            }

            bool isUndoCommand = current.type == EventType.ExecuteCommand && current.commandName == "Undo";
            bool isUndoKey = current.type == EventType.KeyDown && current.keyCode == KeyCode.Z && (current.control || current.command);
            if (!isUndoCommand && !isUndoKey)
            {
                return;
            }

            UndoLastTextEdit();
            current.Use();
        }

        private void RecordTextEdit(int entryIndex, string key, string value)
        {
            string language = GetSelectedLanguage();
            if (string.IsNullOrEmpty(language) || entryIndex < 0)
            {
                return;
            }

            m_EditUndoRecords.Push(new EditUndoRecord
            {
                Language = language,
                EntryIndex = entryIndex,
                Key = key,
                Value = value
            });

            if (m_EditUndoRecords.Count > MaxEditUndoCount)
            {
                TrimEditUndoRecords();
            }
        }

        private void TrimEditUndoRecords()
        {
            if (m_EditUndoRecords.Count <= MaxEditUndoCount)
            {
                return;
            }

            var records = m_EditUndoRecords.ToArray();
            m_EditUndoRecords.Clear();
            int keepCount = Mathf.Min(MaxEditUndoCount, records.Length);
            for (int i = keepCount - 1; i >= 0; i--)
            {
                m_EditUndoRecords.Push(records[i]);
            }
        }

        private void UndoLastTextEdit()
        {
            while (m_EditUndoRecords.Count > 0)
            {
                EditUndoRecord record = m_EditUndoRecords.Pop();
                if (!m_AllLangguageLocalitionEntry.TryGetValue(record.Language, out var entryList) ||
                    record.EntryIndex < 0 ||
                    record.EntryIndex >= entryList.Count)
                {
                    continue;
                }

                LocalizationEntry entry = entryList[record.EntryIndex];
                entry.Key = record.Key;
                entry.Value = record.Value;
                EditorUtility.SetDirty(this);
                RebuildSearchResults();
                Repaint();
                return;
            }
        }

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitLanguageLabelStyles()
        {
            if (_evenRowStyle == null)
            {
                _evenRowStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 2, 2),
                };
                _evenRowStyle.normal.textColor = Color.white;
                _evenRowStyle.normal.background = LocalizationUtility.MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f));
            }

            if (_oddRowStyle == null)
            {
                _oddRowStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 2, 2),
                };
                _oddRowStyle.normal.textColor = Color.white;
                _oddRowStyle.normal.background = LocalizationUtility.MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));
            }
        }
        /// <summary>
        /// 添加本地化词条回调
        /// </summary>
        /// <param name="list"></param>
        private void OnAddEntryCallback(ReorderableList list)
        {
            for (int i = 0; i < m_AllLangguageLocalitionEntry.Count; i++)
            {
                string lang = m_LanguageTypeList[i];
                var entryList = m_AllLangguageLocalitionEntry[m_LanguageTypeList[i]];
                // 注册撤销点
                Undo.RecordObject(this, $"Add Entry to {lang}");
                entryList.Add(new LocalizationEntry("NewKey", "NewValue"));

                EditorUtility.SetDirty(this);
            }
            RebuildSearchResults();
        }
        /// <summary>
        /// 移除本地化词条回调
        /// </summary>
        /// <param name="list"></param>
        private void OnRemoveCallBack(ReorderableList list)
        {
            int index = m_CurEntryList.Count - 1;
            int sourceIndex = GetEntrySourceIndex(list.index);
            if (sourceIndex >= 0 && sourceIndex < m_CurEntryList.Count)
            {
                index = sourceIndex;
            }

            RemoveEntryAtIndex(index);
        }

        private void RemoveEntryAtIndex(int index)
        {
            for (int i = 0; i < m_AllLangguageLocalitionEntry.Count; i++)
            {
                string lang = m_LanguageTypeList[i];
                var entryList = m_AllLangguageLocalitionEntry[m_LanguageTypeList[i]];
                if (index < 0 || index >= entryList.Count)
                {
                    continue;
                }

                // 注册撤销点
                Undo.RecordObject(this, $"Remove Entry from {lang}");
                entryList.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }
            RebuildSearchResults();
        }

        /// <summary>
        /// 读取语言类型列表、并初始化可重排序列表
        /// </summary>
        private void LoadLanguageTypeList()
        {
            if (m_LanguageTypeList == null)
                m_LanguageTypeList = new List<string>();

            m_LanguageTypeList = LocalizationUtility.GetFileNames(folderPath);

            reorderableLanguageList = new ReorderableList(m_LanguageTypeList, typeof(string), true, true, true, true);
            reorderableLanguageList.draggable = false;
            reorderableLanguageList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "语言类型列表", EditorStyles.boldLabel);
            };


            reorderableLanguageList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                InitLanguageLabelStyles(); // 确保样式已初始化

                var style = index % 2 == 0 ? _evenRowStyle : _oddRowStyle; ;
                EditorGUI.LabelField(
                     new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
                     m_LanguageTypeList[index],
                     style
                 );
            };

            reorderableLanguageList.onAddCallback = (ReorderableList list) =>
            {
                GenericMenu menu = new GenericMenu();
                foreach (var lang in System.Enum.GetValues(typeof(Language)))
                {
                    string langStr = lang.ToString();
                    if (!m_LanguageTypeList.Contains(langStr))
                    {
                        menu.AddItem(new GUIContent(langStr), false, () =>
                        {
                            m_LanguageTypeList.Add(langStr);
                            m_AllLangguageLocalitionEntry.Add(langStr, AddNewLocalizationEntryList());

                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(langStr));
                    }
                }

                // 显示下拉菜单
                menu.ShowAsContext();

            };

            reorderableLanguageList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < m_LanguageTypeList.Count)
                {
                    m_LanguageTypeList.RemoveAt(list.index);
                }
            };

        }

        /// <summary>
        /// 下拉框值修改事件
        /// </summary>
        /// <param name="index"></param>
        private void OnPopupLanguageChange(int index)
        {
            if (index >= 0 && index < m_LanguageTypeList.Count)
            {
                CurLanguage = m_LanguageTypeList[index];
                if (m_AllLangguageLocalitionEntry.TryGetValue(CurLanguage, out var localEntry) && localEntry != null)
                {
                    m_CurEntryList = localEntry;
                }
                else
                {
                    // 如果当前语言没有加载成功，尝试重新加载
                    var reloadedEntry = LoadLocalizationEntry(CurLanguage);
                    if (reloadedEntry != null)
                    {
                        m_CurEntryList = reloadedEntry;
                    }
                    else if (m_AllLangguageLocalitionEntry.Count > 0)
                    {
                        // 如果重新加载失败，使用第一个可用的语言
                        var firstLanguage = m_AllLangguageLocalitionEntry.Keys.First();
                        m_CurEntryList = m_AllLangguageLocalitionEntry[firstLanguage];
                        SelectLanguageIndex = m_LanguageTypeList.IndexOf(firstLanguage);
                        CurLanguage = firstLanguage;
                    }
                }
                // 重新初始化可重排序列表，确保UI更新
                InitReorderableEntryList();
            }
        }

        /// <summary>
        /// 导出所有Key为CS常量文件
        /// </summary>
        private void ExportKeysToCS()
        {
            const string outputPath = "Assets/GameMain/Scripts/Localization/LocalizationKeys.cs";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// Auto-generated by LocalizationEditor. Do not edit manually.");
            sb.AppendLine("public static partial class LocalizationKeys");
            sb.AppendLine("{");
            foreach (var entry in m_CurEntryList)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;
                string fieldName = entry.Key.Replace('.', '_').Replace('-', '_').Replace(' ', '_');
                sb.AppendLine($"    public const string {fieldName} = \"{entry.Key}\";");
            }
            sb.AppendLine("}");

            string fullPath = Path.Combine(Application.dataPath, "../", outputPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, sb.ToString(), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"LocalizationKeys.cs 已导出至：{outputPath}");
        }

        /// <summary>
        /// 同步
        /// </summary>
        private void SyncAllKeycodes()
        {

            foreach (var lang in m_LanguageTypeList)
            {

                if (lang != m_CurLanguage)
                {
                    int fixCount = 0;
                    var entryList = m_AllLangguageLocalitionEntry[lang];
                    for (int i = 0; i < entryList.Count; i++)
                    {
                        if (entryList[i].Key != m_CurEntryList[i].Key)
                        {
                            entryList[i].Key = m_CurEntryList[i].Key;
                            fixCount++;
                        }

                    }
                    Debug.Log(lang + "：" + fixCount + "个已修改");
                }

            }

        }

        /// <summary>
        /// 保存当前本地化文件
        /// </summary>
        private void SaveToFile()
        {
            string language = GetSelectedLanguage();
            if (string.IsNullOrEmpty(language))
            {
                Debug.LogError("保存当前本地化文件失败：当前语言为空。");
                return;
            }

            m_KeyValuePairs = new Dictionary<string, string>();

            foreach (var entry in m_CurEntryList)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    if (!m_KeyValuePairs.ContainsKey(entry.Key))
                        m_KeyValuePairs.Add(entry.Key, entry.Value);
                    else
                        Debug.LogWarning($"重复的 Key：{entry.Key}，已跳过。");
                }
            }
            WriteLocalizationEntryList(language, m_CurEntryList);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 保存指定语言的本地化文件
        /// </summary>
        /// <param name="language"></param>
        private void SaveToFile(string language)
        {
            var localizationEntryList = m_AllLangguageLocalitionEntry[language];
            WriteLocalizationEntryList(language, localizationEntryList);
        }

        /// <summary>
        /// 全部保存
        /// </summary>
        private void SaveAll()
        {
            foreach (var lang in m_LanguageTypeList)
            {
                SaveToFile(lang);
            }

            AssetDatabase.Refresh();
        }

        private string GetSelectedLanguage()
        {
            if (!string.IsNullOrEmpty(m_CurLanguage))
            {
                return m_CurLanguage;
            }

            if (m_LanguageTypeList != null && m_selectLanguageIndex >= 0 && m_selectLanguageIndex < m_LanguageTypeList.Count)
            {
                CurLanguage = m_LanguageTypeList[m_selectLanguageIndex];
                return m_CurLanguage;
            }

            return string.Empty;
        }

        /// <summary>
        /// 添加新语言词条集
        /// </summary>
        /// <returns></returns>
        private List<LocalizationEntry> AddNewLocalizationEntryList()
        {
            var newList = new List<LocalizationEntry>();
            for (int i = 0; i < m_CurEntryList.Count; i++)
            {
                newList.Add(new LocalizationEntry(m_CurEntryList[i].Key, "New Key"));
            }
            return newList;
        }

        private string GetCurrentLanguageFilePath(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = m_LanguageTypeList[m_selectLanguageIndex];
            }
            return folderPath + language + "/" + language + LocalizationUtility.GetSuffixByFileType(m_FileType);
        }

        private List<LocalizationEntry> ReadLocalizationEntryList(FileType fileType, string filePath)
        {
            switch (fileType)
            {
                case FileType.Xml:
                    return LocalizationUtility.ReadXml(filePath);
                case FileType.Json:
                    return LocalizationUtility.ReadJson(filePath);
                default:
                    return new List<LocalizationEntry>();
            }
        }

        private void WriteLocalizationEntryList(string language, List<LocalizationEntry> localizationEntryList)
        {
            string fullPath = GetCurrentLanguageFilePath(language);

            switch (m_FileType)
            {
                case FileType.Xml:
                    LocalizationUtility.WriteXml(language, fullPath, localizationEntryList);
                    break;
                case FileType.Json:
                    LocalizationUtility.WriteJson(fullPath, localizationEntryList);
                    break;
                default:
                    break;
            }
        }
    }

}
