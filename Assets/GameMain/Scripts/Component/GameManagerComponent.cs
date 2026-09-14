using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class GameManagerComponent : GameFrameworkComponent
    {
        public struct LevelTimerResult
        {
            public GameMode Mode;
            public int Level;
            public long DurationSeconds;
        }

        [SerializeField] private GameMode m_CurrentGameMode = GameMode.None;
        [SerializeField] private float m_CurrentLevelLogicTime;
        [SerializeField] private bool m_IsLevelTimerRunning;
        [SerializeField] private bool m_WaitingForPlayerStart;
        [SerializeField] private bool m_LevelResultReported;
        [SerializeField] private GameState m_CurrentGameState = GameState.None;
        [SerializeField] private bool m_ResumeLevelTimerAfterApplicationPause;
        [SerializeField] private float m_LastLevelTimerRealtime;
        [SerializeField] private float m_LastPlayerOperationRealtime;

        public GameMode CurrentGameMode => m_CurrentGameMode;

        [Header("主玩法（公共功能进度来源）")]
        [Tooltip("在启动场景配置主玩法。None 或对应模块未注册时，公共关卡解锁判断不通过。")]
        [SerializeField] private GameMode m_PrimaryGameMode = GameMode.None;
        public GameMode PrimaryGameMode => m_PrimaryGameMode;
        [Tooltip("显式安装列表，由示例包工具维护；空列表表示不安装示例。")]
        [SerializeField] private SubGameManagerComponent[] m_SubGameManagers = System.Array.Empty<SubGameManagerComponent>();
        public SubGameRuntimeRegistry SubGames { get; } = new();
        public void InitializeSubGames(SaveDataStore store)
        {
            SubGames.Rebuild(m_SubGameManagers);
            foreach (var manager in SubGames.Installed) manager.InitializeModule(store);
        }
        public GameState CurrentGameState => m_CurrentGameState;
        public float CurrentLevelLogicTime => m_CurrentLevelLogicTime;
        public int CurrentLevelLogicSeconds => Mathf.FloorToInt(m_CurrentLevelLogicTime);
        public bool IsLevelTimerRunning => m_IsLevelTimerRunning;
        public bool IsGaming => CurrentGameMode != GameMode.None;
        public bool IsPaused => m_CurrentGameState == GameState.Paused;

        private void Update()
        {
            if (m_IsLevelTimerRunning)
            {
                float realtime = Time.realtimeSinceStartup;
                float deltaTime = m_LastLevelTimerRealtime > 0f
                    ? realtime - m_LastLevelTimerRealtime
                    : Time.unscaledDeltaTime;

                m_LastLevelTimerRealtime = realtime;
                if (deltaTime > 0f)
                {
                    float activeDeltaTime = GetActiveLevelTimerDelta(deltaTime, realtime);
                    if (HasPlayerOperationInput())
                    {
                        m_LastPlayerOperationRealtime = realtime;
                    }

                    if (activeDeltaTime > 0f)
                    {
                        m_CurrentLevelLogicTime += activeDeltaTime;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                GameEntry.UI.OpenUIForm(UIFormId.GameOverUIPanel, null);
            }
        }

        private void StartLevelTimer(GameMode mode)
        {
            if (mode == GameMode.None) return;

            m_CurrentGameMode = mode;
            m_CurrentLevelLogicTime = 0f;
            m_IsLevelTimerRunning = false;
            m_ResumeLevelTimerAfterApplicationPause = false;
            m_LastLevelTimerRealtime = 0f;
            m_LastPlayerOperationRealtime = 0f;
            m_WaitingForPlayerStart = true;
            m_LevelResultReported = false;
            m_CurrentGameState = GameState.Playing;
        }

        public void NotifyPlayerStartedLevelTimer(GameMode mode)
        {
            if (!IsTimerMode(mode) || !m_WaitingForPlayerStart) return;

            m_WaitingForPlayerStart = false;
            m_LastLevelTimerRealtime = Time.realtimeSinceStartup;
            m_LastPlayerOperationRealtime = m_LastLevelTimerRealtime;
            m_IsLevelTimerRunning = true;
        }

        private void PauseLevelTimer(GameMode mode)
        {
            if (IsTimerMode(mode))
            {
                m_ResumeLevelTimerAfterApplicationPause = false;
                m_IsLevelTimerRunning = false;
                m_LastLevelTimerRealtime = 0f;
            }
        }

        private void ResumeLevelTimer(GameMode mode)
        {
            if (IsTimerMode(mode) && !m_WaitingForPlayerStart)
            {
                m_LastLevelTimerRealtime = Time.realtimeSinceStartup;
                m_LastPlayerOperationRealtime = m_LastLevelTimerRealtime;
                m_IsLevelTimerRunning = true;
            }
        }

        private int StopLevelTimer(GameMode mode)
        {
            if (!IsTimerMode(mode)) return 0;

            m_IsLevelTimerRunning = false;
            m_ResumeLevelTimerAfterApplicationPause = false;
            m_LastLevelTimerRealtime = 0f;
            return CurrentLevelLogicSeconds;
        }

        private void ResetLevelTimer(GameMode mode)
        {
            if (!IsTimerMode(mode)) return;

            m_IsLevelTimerRunning = false;
            m_WaitingForPlayerStart = false;
            m_CurrentLevelLogicTime = 0f;
            m_LevelResultReported = false;
            m_ResumeLevelTimerAfterApplicationPause = false;
            m_LastLevelTimerRealtime = 0f;
            m_LastPlayerOperationRealtime = 0f;
            m_CurrentGameMode = GameMode.None;
            m_CurrentGameState = GameState.None;
        }

        public LevelTimerResult GetLevelTimerResult(GameMode mode)
        {
            return new LevelTimerResult
            {
                Mode = mode,
                Level = GetCurrentLevel(mode),
                DurationSeconds = IsTimerMode(mode) ? CurrentLevelLogicSeconds : 0
            };
        }

        private static int GetCurrentLevel(GameMode mode)
        {
            return GameEntry.SubGames?.Get(mode)?.CurrentLevel ?? 0;
        }

        public int GetCurrentLevel()
        {
            return GetCurrentLevel(CurrentGameMode);
        }

        private void OutputLevelTimerResult(GameMode mode, string levelStatus)
        {
            if (!IsTimerMode(mode) || m_LevelResultReported) return;

            LevelTimerResult result = GetLevelTimerResult(mode);
            Ads.AdsAnalytics.EventLevelDuration(result.Level, levelStatus, result.DurationSeconds, GameEntry.GameName);
            m_LevelResultReported = true;
        }

        public void HandleLevelStateChanged(GameMode mode, GameState previous, GameState current)
        {
            if (mode == GameMode.None) return;
            m_CurrentGameState = current;
            switch (current)
            {
                case GameState.Playing:
                    if (ShouldStartNewTimer(mode, previous))
                        StartLevelTimer(mode);
                    else
                        ResumeLevelTimer(mode);
                    break;

                case GameState.Paused:
                    StopLevelTimer(mode);
                    break;

                case GameState.TimeOver:
                    StopLevelTimer(mode);
                    break;

                case GameState.GameOver:
                    StopLevelTimer(mode);
                    OutputLevelTimerResult(mode, "LevelStatus.GameWin");
                    break;

                case GameState.Menu:
                case GameState.Restart:
                case GameState.None:
                    StopLevelTimer(mode);
                    OutputLevelTimerResult(mode, "LevelStatus.GameFail");
                    break;
            }
        }
        private bool ShouldStartNewTimer(GameMode mode, GameState previous)
        {
            if (!IsTimerMode(mode)) return true;

            return previous == GameState.None
                || previous == GameState.Menu
                || previous == GameState.Restart
                || previous == GameState.GameOver;
        }
        private bool IsTimerMode(GameMode mode)
        {
            return mode != GameMode.None && m_CurrentGameMode == mode;
        }

        private float GetActiveLevelTimerDelta(float deltaTime, float realtime)
        {
            float idleStopSeconds = Ads.AdsManager.LevelTimerIdleStopSeconds;
            if (idleStopSeconds <= 0f) return deltaTime;

            if (m_LastPlayerOperationRealtime <= 0f)
            {
                m_LastPlayerOperationRealtime = realtime;
                return deltaTime;
            }

            float idleSecondsBeforeUpdate = Mathf.Max(0f, realtime - deltaTime - m_LastPlayerOperationRealtime);
            float remainingActiveSeconds = idleStopSeconds - idleSecondsBeforeUpdate;
            if (remainingActiveSeconds <= 0f) return 0f;

            return Mathf.Min(deltaTime, remainingActiveSeconds);
        }

        private bool HasPlayerOperationInput()
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                return true;

            if (Input.touchCount > 0) return true;

            return Input.anyKey;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                PauseLevelTimerForApplicationPause();
            else
                ResumeLevelTimerAfterApplicationPause();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ResumeLevelTimerAfterApplicationPause();
            else
                PauseLevelTimerForApplicationPause();
        }

        private void PauseLevelTimerForApplicationPause()
        {
            if (!m_IsLevelTimerRunning) return;

            m_ResumeLevelTimerAfterApplicationPause = true;
            m_IsLevelTimerRunning = false;
            m_LastLevelTimerRealtime = 0f;
        }

        private void ResumeLevelTimerAfterApplicationPause()
        {
            if (!m_ResumeLevelTimerAfterApplicationPause) return;

            m_ResumeLevelTimerAfterApplicationPause = false;
            ResumeLevelTimer(m_CurrentGameMode);
        }


        public int GetLevelLogicSeconds(GameMode mode)
        {
            return IsTimerMode(mode) ? CurrentLevelLogicSeconds : 0;
        }
    }
}
