using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    /// <summary>原生 unitypackage + 显式注册清单；保留公共框架与玩家存档。</summary>
    public sealed class SampleSubGamesWindow : EditorWindow
    {
        [SerializeField] private string m_Report = "先保存场景。创建新项目时，逐个移除两个示例；回装请选择 unitypackage，并保留同目录的 .json 校验清单。";
        [SerializeField] private GameMode m_PrimaryAfterRemoval = GameMode.None;
        private Vector2 m_Scroll;
        private double m_NextRepaint;

        [MenuItem("Tools/Template/Sample SubGames")]
        public static void Open()
        {
            var window = GetWindow<SampleSubGamesWindow>("示例子游戏");
            window.minSize = new Vector2(520, 520);
        }
        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < m_NextRepaint) return;
            m_NextRepaint = EditorApplication.timeSinceStartup + 1;
            Repaint();
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("仅支持此 GF 模板版本的 HexaAway / RectMatch 示例包。公共音频、测试框架、7 个公共配置字段和玩家存档不会随包删除。不要在 Project 窗口直接删游戏目录。", MessageType.Info);
            var journal = SamplePackageService.LastOperation;
            bool pending = !string.IsNullOrEmpty(journal.phase) && journal.phase != "complete" && journal.phase != "restored";
            bool busy = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;
            if (busy) EditorGUILayout.HelpBox("请退出播放模式，等待编译与资源导入结束。", MessageType.Info);
            using (new EditorGUI.DisabledScope(busy || pending))
            {
                DrawSample("HexaAway");
                DrawSample("RectMatch");
                EditorGUILayout.Space(8);
                if (GUILayout.Button("打开示例包目录"))
                    Run(() =>
                    {
                        string directory = SamplePackagePaths.ExportDirectory();
                        Directory.CreateDirectory(directory);
                        EditorUtility.RevealInFinder(directory);
                        return "示例包目录：\n" + directory + "\n末尾 ~ 使 Unity 忽略此目录；请通过本窗口导入包。";
                    });
                if (GUILayout.Button("导入已有示例包…", GUILayout.Height(28)))
                {
                    string directory = SamplePackagePaths.Full(SamplePackagePaths.PackagesDirectory);
                    string path = EditorUtility.OpenFilePanel("选择可信示例 unitypackage（旁边须有 .json 清单）",
                        Directory.Exists(directory) ? directory : SamplePackagePaths.Workspace, "unitypackage");
                    if (!string.IsNullOrEmpty(path) && EditorUtility.DisplayDialog("导入示例", "包中的 C# 脚本将由 Unity 编译执行。仅导入自己导出或确认可信的包。\n\n主玩法设置保持不变，导入后按需手动设置。", "校验并导入", "取消"))
                        Run(() => { SamplePackageService.Import(path); return "已提交导入，请等待下方状态变为“完成”。"; });
                }
                if (GUILayout.Button("定位主玩法 / 已安装管理器"))
                {
                    var managers = UnityEngine.Object.FindObjectsOfType<GameManagerComponent>(true);
                    if (managers.Length == 1) { Selection.activeObject = managers[0]; EditorGUIUtility.PingObject(managers[0]); }
                    else m_Report = "先打开 Assets/GameMain/Scenes/GameLauncher.unity，再选择 GameEntry/GameFramework/GameManager。";
                }
                EditorGUILayout.Space(8);
                m_PrimaryAfterRemoval = (GameMode)EditorGUILayout.EnumPopup(new GUIContent("移除后主玩法", "若删除当前主玩法，必须显式选择仍安装的玩法或 None。不会自动让副玩法接管。"), m_PrimaryAfterRemoval);
                EditorGUILayout.HelpBox("下列操作先检查改动并备份，再撤销注册和移除所属目录。默认 None 表示移除后不自动启动任何玩法。", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    RemoveButton("HexaAway");
                    RemoveButton("RectMatch");
                }
            }
            EditorGUILayout.Space(8);
            if (!string.IsNullOrEmpty(journal.phase))
            {
                EditorGUILayout.LabelField("最近操作", journal.operation + " / " + journal.game + " / " + PhaseLabel(journal.phase));
                if (!string.IsNullOrEmpty(journal.backupDirectory))
                {
                    EditorGUILayout.SelectableLabel(journal.backupDirectory, EditorStyles.wordWrappedLabel, GUILayout.Height(36));
                    if (GUILayout.Button("打开备份目录")) EditorUtility.RevealInFinder(journal.backupDirectory);
                }
                if (!string.IsNullOrEmpty(journal.error)) EditorGUILayout.HelpBox(journal.error, MessageType.Error);
                using (new EditorGUI.DisabledScope(busy || journal.phase == "restored"))
                    if (GUILayout.Button("恢复最近一次操作…") && EditorUtility.DisplayDialog("恢复操作前状态", "将恢复该操作备份的公共配置及游戏资源。若操作后已有修改，将停止并要求手动合并。不会清理玩家存档。", "校验并恢复", "取消"))
                        Run(() => { SamplePackageService.RestoreLastOperation(); return "恢复已提交；请等待状态变为“已恢复”。"; });
            }
            if (GUILayout.Button("复制报告")) EditorGUIUtility.systemCopyBuffer = m_Report;
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            EditorGUILayout.TextArea(m_Report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        private void DrawSample(string game)
        {
            bool installed = AssetDatabase.IsValidFolder("Assets/GameMain/SubGame/" + game);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(game + (installed ? " · 已安装" : " · 未安装"), EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(!installed))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("检查依赖与本地改动")) Run(() => SamplePackageService.Inspect(game));
                    if (GUILayout.Button("导出 unitypackage"))
                        Run(() => { string path = SamplePackageService.Export(game); EditorUtility.RevealInFinder(path); return "导出完成：\n" + path + "\n请同时保存旁边的 .json 清单。"; });
                }
            }
        }
        private void RemoveButton(string game)
        {
            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder("Assets/GameMain/SubGame/" + game)))
                if (GUILayout.Button("移除 " + game + "…") && EditorUtility.DisplayDialog("确认移除 " + game,
                    "将移除：\nAssets/GameMain/SubGame/" + game + "\nAssets/GameMain/Scenes/SubGame/" + game +
                    "\n\n并同步 Launcher、流程、表、资源索引和 Build Settings。\n移除后主玩法：" + m_PrimaryAfterRemoval +
                    "\n备份写入 Backups/SamplePackages/；存档和公共资源保留。", "备份并移除", "取消"))
                    Run(() => { SamplePackageService.Remove(game, m_PrimaryAfterRemoval); return "移除已提交；请等待下方状态变为“完成”。"; });
        }
        private void Run(Func<string> action)
        {
            try { m_Report = action(); }
            catch (Exception exception) { m_Report = "操作未完成：\n" + exception.Message + "\n\n若已有备份，请先检查最近操作状态，再恢复或手动合并。"; }
        }
        private static string PhaseLabel(string phase) => phase switch
        {
            "prepared" => "已备份", "importing" => "导入资源中", "awaiting-compilation" => "编译 / 验证中",
            "complete" => "完成", "failed" => "失败，请恢复", "restoring-import" => "回装备份中",
            "awaiting-restore" => "验证恢复结果", "restored" => "已恢复", _ => phase
        };
    }
    public static class SampleSubGamePreflight
    {
        public static string Inspect(string game) => SamplePackageService.Inspect(game);
    }
}
