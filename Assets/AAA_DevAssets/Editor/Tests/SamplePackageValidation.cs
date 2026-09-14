using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lokas.Editor.Tests
{
    /// <summary>仅在带显式 CLI 标志的隔离副本运行；不会在工作工程删除示例。</summary>
    [InitializeOnLoad]
    public static class SamplePackageValidation
    {
        [Serializable] private sealed class State
        {
            public int step;
            public string phase = "ready";
            public string playPhase;
            public int gameIndex;
            public int[] modes;
            public int primary;
            public long deadline;
            public long nextAction;
            public string failure;
            public List<string> passed = new();
            public List<string> errors = new();
            public List<string> editorStopDiagnostics = new();
        }
        private static readonly string StatePath = Path.Combine(SamplePackagePaths.Workspace, "Validation", "matrix.json");
        private static readonly List<TestModeModuleContext> OldContexts = new();
        private static double s_NextPoll;
        private static bool s_InUpdate;
        private static bool IsValidationCopy => Environment.GetCommandLineArgs().Contains("-samplePackageValidation") &&
            SamplePackagePaths.Workspace.Replace('\\', '/').Contains("/Backups/SamplePackageValidation-") &&
            SamplePackagePaths.Workspace.Replace('\\', '/').EndsWith("/Project", StringComparison.Ordinal);
        static SamplePackageValidation()
        {
            if (!IsValidationCopy) return;
            EditorApplication.update += Update;
            Application.logMessageReceived += CaptureLog;
        }
        public static void Start()
        {
            if (!IsValidationCopy) throw new InvalidOperationException("Refusing destructive validation outside the isolated validation copy.");
            if (File.Exists(StatePath)) throw new InvalidOperationException("Validation already started. Use a new copy for a fresh run.");
            PlayerSettings.companyName = "GFTemplateValidation";
            PlayerSettings.productName = "SamplePackages-" + Directory.GetParent(SamplePackagePaths.Workspace).Name;
            Application.runInBackground = true;
            var state = new State { deadline = DateTime.UtcNow.AddMinutes(10).Ticks };
            SamplePackagePaths.Write(StatePath, state);
        }
        public static void ReplayFinalMatrix()
        {
            if (!IsValidationCopy || !File.Exists(StatePath)) throw new InvalidOperationException("Not an initialized validation copy.");
            var state = SamplePackagePaths.Read<State>(StatePath);
            string phase = SamplePackageService.LastOperation.phase;
            if ((state.phase != "failed" && state.phase != "complete") ||
                (phase != "awaiting-restore" && phase != "restored" && phase != "complete"))
                throw new InvalidOperationException("Finish recovery before replaying the full matrix.");
            File.Copy(StatePath, StatePath + ".before-final-replay-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            SamplePackagePaths.Write(StatePath, new State { step = -1, phase = "operation", deadline = DateTime.UtcNow.AddMinutes(6).Ticks });
        }
        public static void VerifyAfterRestart()
        {
            if (!IsValidationCopy || !File.Exists(StatePath)) throw new InvalidOperationException("Not an initialized validation copy.");
            var state = SamplePackagePaths.Read<State>(StatePath);
            if (state.phase != "complete") throw new InvalidOperationException("Matrix is not complete.");
            SamplePackageService.ValidateInstalledProject();
            foreach (string game in new[] { "HexaAway", "RectMatch" })
            {
                if (!SamplePackageService.Inspect(game).Contains("Blocking findings: 0")) throw new InvalidOperationException("Final preflight failed: " + game);
                var catalog = SamplePackagePaths.Read<SamplePackageCatalog>(Package(game) + ".json");
                foreach (var file in catalog.files) SamplePackageService.CheckImportTarget(SamplePackagePaths.Asset(file.path), file);
            }
            state.passed.Add("after editor restart: installed registrations, persisted build scenes and both package baselines verified");
            Save(state); ExitValidationEditor(0);
        }
        private static void CaptureLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (!File.Exists(StatePath)) return;
            var state = SamplePackagePaths.Read<State>(StatePath);
            string normalized = message.Replace("\r", "").TrimEnd();
            const string tweenStop = "Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)\nThe following scene GameObjects were found:\n[DOTween]";
            if (state.phase == "stopping" && normalized == tweenStop)
            {
                state.editorStopDiagnostics.Add("Step " + state.step + ": " + message);
                SamplePackagePaths.Write(StatePath, state);
                return; // 真实 Editor 停止诊断单独保留，不能写成 Console 零错误；其他错误仍使测试失败。
            }
            state.errors.Add(message + "\n" + stack);
            SamplePackagePaths.Write(StatePath, state);
        }
        private static void Update()
        {
            if (!IsValidationCopy || s_InUpdate || EditorApplication.timeSinceStartup < s_NextPoll || !File.Exists(StatePath)) return;
            s_NextPoll = EditorApplication.timeSinceStartup + 0.5;
            var state = SamplePackagePaths.Read<State>(StatePath);
            if (state.phase == "complete" || state.phase == "failed") return;
            s_InUpdate = true;
            try
            {
                if (DateTime.UtcNow.Ticks > state.deadline) throw new TimeoutException("Matrix step timed out: " + state.step + " / " + state.phase + " / " + state.playPhase);
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Unity compilation failed.");
                if (state.errors.Count > 0) throw new InvalidOperationException("Unity Console error: " + state.errors[0]);
                if (DateTime.UtcNow.Ticks < state.nextAction) return;
                if (state.phase == "operation")
                {
                    SamplePackageService.FinishPending();
                    var journal = SamplePackageService.LastOperation;
                    if (journal.phase == "failed") throw new InvalidOperationException(journal.error);
                    if (journal.phase != "complete" && journal.phase != "restored") return;
                    state.passed.Add("operation " + state.step + ": " + journal.operation + " " + journal.game + " " + journal.phase);
                    Advance(state); return;
                }
                if (state.phase == "play") { PlayUpdate(state); return; }
                if (state.phase == "stopping")
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    state.passed.Add("play " + state.step + ": modes=" + string.Join(",", state.modes) + "; menu/settings/test pages/pause/resume/restart/game-module cleanup passed; editor-stop diagnostics tracked separately");
                    Advance(state); return;
                }
                switch (state.step)
                {
                    case 0: SetPrimary((GameMode)2); BeginPlay(state, new[] { 2, 3 }, 2); break;
                    case 1: BeginOperation(state, () => SamplePackageService.Remove("RectMatch", (GameMode)2)); break;
                    case 2: BeginPlay(state, new[] { 2 }, 2); break;
                    case 3: BeginOperation(state, () => SamplePackageService.Remove("HexaAway", GameMode.None)); break;
                    case 4: BeginPlay(state, Array.Empty<int>(), 0); break;
                    case 5: BeginOperation(state, () => SamplePackageService.Import(Package("RectMatch"))); break;
                    case 6: SetPrimary((GameMode)3); BeginPlay(state, new[] { 3 }, 3); break;
                    case 7: BeginOperation(state, () => SamplePackageService.Import(Package("HexaAway"))); break;
                    case 8: BeginPlay(state, new[] { 2, 3 }, 3); break;
                    case 9: BeginOperation(state, () => SamplePackageService.Import(Package("HexaAway"))); break;
                    case 10: BeginOperation(state, SamplePackageService.RestoreLastOperation); break;
                    case 11: BeginOperation(state, () => SamplePackageService.Remove("RectMatch", (GameMode)2)); break;
                    case 12: BeginOperation(state, SamplePackageService.RestoreLastOperation); break;
                    case 13:
                        SamplePackageService.ValidateInstalledProject();
                        state.passed.Add("final: two games restored with valid registrations; see editorStopDiagnostics for Editor shutdown findings");
                        state.phase = "complete"; Save(state); ExitValidationEditor(0); break;
                }
            }
            catch (Exception exception)
            {
                state.phase = "failed"; state.failure = exception.ToString(); Save(state);
                Debug.LogError("Sample package matrix failed: " + exception);
                ExitValidationEditor(2);
            }
            finally { s_InUpdate = false; }
        }
        private static string Package(string game) => Directory.GetFiles(SamplePackagePaths.Full(SamplePackagePaths.PackagesDirectory), game + "-*.unitypackage").OrderBy(value => value).Last();
        private static void ExitValidationEditor(int code)
        {
            // MCP 插件的交互编辑器退出清理会命中全局 localhost server；隔离副本不能关闭主工程连接。
            var cleanup = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("MCPForUnity.Editor.Services.McpEditorShutdownCleanup")).FirstOrDefault(type => type != null);
            var method = cleanup?.GetMethod("OnEditorQuitting", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method != null) EditorApplication.quitting -= (Action)Delegate.CreateDelegate(typeof(Action), method);
            EditorApplication.Exit(code);
        }
        private static void Save(State state)
        {
            if (File.Exists(StatePath))
            {
                var disk = SamplePackagePaths.Read<State>(StatePath);
                state.errors = state.errors.Concat(disk.errors).Distinct().ToList();
                state.editorStopDiagnostics = state.editorStopDiagnostics.Concat(disk.editorStopDiagnostics ?? new List<string>()).Distinct().ToList();
            }
            SamplePackagePaths.Write(StatePath, state);
        }
        private static void Advance(State state)
        {
            state.step++; state.phase = "ready"; state.deadline = DateTime.UtcNow.AddMinutes(6).Ticks;
            state.nextAction = DateTime.UtcNow.AddSeconds(3).Ticks; Save(state);
        }
        private static void BeginOperation(State state, Action action)
        {
            state.phase = "operation"; Save(state); action();
        }
        private static void SetPrimary(GameMode mode)
        {
            var scene = EditorSceneManager.OpenScene(SamplePackagePaths.Launcher);
            var owner = UnityEngine.Object.FindObjectOfType<GameManagerComponent>(true);
            var serialized = new SerializedObject(owner);
            serialized.FindProperty("m_PrimaryGameMode").intValue = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        private static void BeginPlay(State state, int[] modes, int primary)
        {
            SamplePackageService.ValidateInstalledProject();
            EditorSceneManager.OpenScene(SamplePackagePaths.Launcher);
            state.modes = modes; state.primary = primary; state.gameIndex = 0; state.phase = "play"; state.playPhase = "menu";
            state.deadline = DateTime.UtcNow.AddMinutes(4).Ticks; Save(state);
            EditorApplication.isPlaying = true;
        }
        private static void PlayUpdate(State state)
        {
            if (!EditorApplication.isPlaying || GameEntry.Procedure == null || GameEntry.Procedure.CurrentProcedure == null) return;
            if (state.playPhase == "menu")
            {
                if (!(GameEntry.Procedure.CurrentProcedure is ProcedureMenu)) return;
                if (UnityEngine.Object.FindObjectOfType<MainUIPanel>() == null) return;
                if (!GameEntry.SubGames.Installed.Select(value => (int)value.GameMode).OrderBy(value => value).SequenceEqual(state.modes.OrderBy(value => value)))
                    throw new InvalidOperationException("Runtime installed set differs.");
                if ((int)GameEntry.GameManager.PrimaryGameMode != state.primary) throw new InvalidOperationException("Primary game unexpectedly changed.");
                // 隔离 QA 存档跳过首次引导，专测正常玩法进出；不修改工作工程存档。
                foreach (var installed in GameEntry.SubGames.Installed)
                {
                    var field = installed.GetType().GetField("m_ModuleData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    object data = field?.GetValue(installed);
                    var tutorial = data?.GetType().GetProperty("TutorialCompleted");
                    if (tutorial != null && tutorial.CanWrite) tutorial.SetValue(data, true);
                }
                GameEntry.TestMode.SetEnabled(true); GameEntry.TestMode.Show();
                if (GameEntry.TestMode.Modules.Modules.Any(value => value.OwnerId != TestModeModuleRegistry.CommonOwnerId)) throw new InvalidOperationException("Game page leaked into main menu.");
                GameEntry.UI.OpenUIForm(UIFormId.SettingUIPanel, new SettingUIData(SettingType.Main, null));
                state.playPhase = "settings"; state.nextAction = DateTime.UtcNow.AddSeconds(2).Ticks; Save(state); return;
            }
            if (state.playPhase == "settings")
            {
                var panel = UnityEngine.Object.FindObjectOfType<SettingUIPanel>();
                if (panel == null || !GameEntry.TestMode.IsWindowVisible) return;
                panel.Close(); GameEntry.TestMode.Hide();
                state.playPhase = "start"; Save(state); return;
            }
            if (state.playPhase == "draining")
            {
                state.phase = "stopping"; Save(state); EditorApplication.isPlaying = false; return;
            }
            if (state.playPhase == "start")
            {
                if (!(GameEntry.Procedure.CurrentProcedure is ProcedureMenu menu)) return;
                foreach (var context in OldContexts) if (context.IsActive || !context.CancellationToken.IsCancellationRequested) throw new InvalidOperationException("Exited test context remained active.");
                OldContexts.Clear();
                if (GameEntry.TestMode.Modules.Modules.Any(value => value.OwnerId != TestModeModuleRegistry.CommonOwnerId)) throw new InvalidOperationException("Test module leaked after exit.");
                if (state.gameIndex >= state.modes.Length)
                {
                    GameEntry.TestMode.SetEnabled(false);
                    GameEntry.UI.CloseAllLoadedUIForms();
                    state.playPhase = "draining"; state.nextAction = DateTime.UtcNow.AddSeconds(3).Ticks; Save(state); return;
                }
                menu.StartGame((GameMode)state.modes[state.gameIndex]);
                state.playPhase = "entered"; Save(state); return;
            }
            var manager = GameEntry.SubGames.Get((GameMode)state.modes[state.gameIndex]);
            if (!(GameEntry.Procedure.CurrentProcedure is ProcedureGame procedure) || !manager.IsResourcesInitialized) return;
            if (state.playPhase == "entered")
            {
                if (!manager.IsPlaying) return; // 模块可以先注册；等待 UI 完成 GameStart 再测试暂停。
                var modules = GameEntry.TestMode.Modules.Modules.Where(value => value.OwnerId != TestModeModuleRegistry.CommonOwnerId).ToArray();
                if (modules.Length == 0) return;
                if (modules.Any(value => value.OwnerId != manager.GameMode.ToString()) || modules.Select(value => value.OwnerId + "/" + value.ModuleName).Distinct().Count() != modules.Length)
                    throw new InvalidOperationException("Wrong or duplicate game test pages.");
                OldContexts.AddRange(modules.Select(GameEntry.TestMode.Modules.GetContext));
                GameEntry.TestMode.Show();
                manager.PauseGame();
                if (!manager.IsPaused) throw new InvalidOperationException("Pause failed.");
                manager.ResumeGame();
                if (!manager.IsPlaying) throw new InvalidOperationException("Resume failed.");
                procedure.Restart();
                state.playPhase = "return"; state.nextAction = DateTime.UtcNow.AddSeconds(3).Ticks; Save(state); return;
            }
            if (state.playPhase == "return")
            {
                if ((int)GameEntry.GameManager.PrimaryGameMode != state.primary) throw new InvalidOperationException("Secondary game changed primary.");
                GameEntry.TestMode.Hide(); procedure.ReturnMenu(); state.gameIndex++; state.playPhase = "start"; Save(state);
            }
        }
    }
}
