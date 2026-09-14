using GameFramework;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using Unity.CodeEditor;
namespace Lokas.Editor
{
    [UnityEditor.InitializeOnLoad]
    public static class CustomToolBars
    {
        private static GUIContent switchSceneBtContent;
        private static GUIContent toolsDropBtContent;
        private static GUIContent openCsProjectBtContent;

        //Toolbar栏工具箱下拉列表
        private static List<string> sceneAssetList;
        private static float lastPixelsPerPoint;
        static CustomToolBars()
        {
            sceneAssetList = new List<string>();
            var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");
            toolsDropBtContent = EditorGUIUtility.TrTextContentWithIcon("Tools", "工具箱", "CustomTool");
            openCsProjectBtContent = EditorGUIUtility.TrTextContentWithIcon("Open C# Project", "打开C#工程", "dll Script Icon");

            EditorSceneManager.sceneOpened += OnSceneOpened;

            ToolbarCallback.OnToolbarGUILeft += OnToolbarGUILeft;
            ToolbarCallback.OnToolbarGUIRight += OnToolbarGUIRight;

        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
        }
        private static void OnToolbarGUILeft()
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            rect.x = (rect.width - 150); // 居中位置
            rect.y = 0;
            rect.width = 150;

            if (GUI.Button(rect, switchSceneBtContent, EditorStyles.toolbarPopup))
            {
                DrawSwithSceneDropdownMenus();
            }

        }
        private static void OnToolbarGUIRight()
        {
            GUILayout.BeginHorizontal();


            if (GUILayout.Button("多语言编辑器", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GFTools.LocalizationEditor.LocalizationEditor.ShowWindow();
            }
            GUILayout.Space(10);
            if (GUILayout.Button(toolsDropBtContent, EditorStyles.toolbarPopup, GUILayout.Width(100)))
            {
                ShowToolsMenu();
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button(openCsProjectBtContent, EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                OpenCSharpProject();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void ShowToolsMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(
                new GUIContent("数据表/数据表常量生成器"),
                false,
                () => TableConstCodeGeneratorEditor.ShowWindow()
            );

            menu.AddItem(
                new GUIContent("数据表/生成数据表"),
                false,
                () => Lokas.Editor.DataTableTools.DataTableGeneratorMenu.GenerateDataTables()
            );
            menu.AddItem(
                      new GUIContent("数据表/数据表编辑器"),
                      false,
                      () => DataTableTextEditorWindow.ShowWindow()
                  );

            menu.AddItem(
                new GUIContent("子游戏/资源注册表配置"),
                false,
                () => Lokas.Editor.SubGameAssetRegistryConfigWindow.Open()
            );

            menu.AddItem(
                new GUIContent("子游戏/示例包导出、移除与导入"),
                false,
                () => Lokas.Editor.SampleSubGamesWindow.Open()
            );

            menu.AddItem(
                new GUIContent("资源/资源ID生成器"),
                false,
                () => IdGeneratorWindow.ShowWindow()
            );
            menu.AddItem(
                new GUIContent("资源/资源后缀设置"),
                false,
                () => ResourceExtensionSettingsWindow.Open()
            );
            menu.AddItem(
                new GUIContent("资源/DisplaySO批量生成器"),
                false,
                () => CreateAvatarSOFromSprites.Open()
            );
            menu.AddItem(
                new GUIContent("Json加密工具"),
                false,
                () => JsonEncryptTool.Open()
            );

       
            menu.AddItem(
                new GUIContent("截图工具"),
                false,
                () => Screenshot.ShowWindow()
            );

            menu.ShowAsContext();
        }

        static void DrawSwithSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            popMenu.allowDuplicateNames = true;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { ConstEditor.ScenePath });
            sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                sceneAssetList.Add(scenePath);
                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                bool isInRootDir = Utility.Path.GetRegularPath(ConstEditor.ScenePath).TrimEnd('/') == Utility.Path.GetRegularPath(fileDir).TrimEnd('/');
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;
                if (!isInRootDir)
                {
                    var sceneDir = System.IO.Path.GetRelativePath(ConstEditor.ScenePath, fileDir);
                    displayName = $"{sceneDir}/{sceneName}";
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
            }
            popMenu.ShowAsContext();
        }
        private static void SwitchScene(int menuIdx)
        {
            if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[menuIdx];
                var curScene = EditorSceneManager.GetActiveScene();
                if (curScene != null && curScene.isDirty)
                {
                    int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                    switch (opIndex)
                    {
                        case 0:
                            if (!EditorSceneManager.SaveOpenScenes())
                            {
                                return;
                            }
                            break;
                        case 1:
                            return;
                    }
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }


        static void OpenCSharpProject()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();

            CodeEditor.Editor.CurrentCodeEditor.OpenProject();
        }


    }

}
