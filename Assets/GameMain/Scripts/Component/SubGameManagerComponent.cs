using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public abstract class SubGameManagerComponent : GameFrameworkComponent
    {
        [SerializeField] private GameState m_CurrentState = GameState.None;
        public GameResult CurrentResult { get; private set; }


        private bool m_IsResourcesInitialized;
        private UniTaskCompletionSource m_ResourceInitializationSource;

        public abstract GameMode GameMode { get; }
        public virtual string SceneConfigKey => null;
        public virtual Type GameProcedureType => null;
        public virtual int CurrentLevel => 0;
        public virtual int DisplayLevel => CurrentLevel;
        public event Action<int> ProgressChanged;
        protected void NotifyProgressChanged(int level) => ProgressChanged?.Invoke(level);
        public virtual void InitializeModule(SaveDataStore store) { }
        public virtual bool CanGrantProp(PropType type) => false;
        public virtual bool TryGrantProp(PropType type, int count) => false;
        public virtual bool SupportsBlockCountSetting => false;
        public virtual bool ShowBlockCount { get => false; set { } }
        public virtual bool TryConsumeBlockCountHint() => false;

        public GameState CurrentState => m_CurrentState;
        public bool IsPlaying => m_CurrentState == GameState.Playing;
        public bool IsPaused => m_CurrentState == GameState.Paused;
        public bool IsGameOver => m_CurrentState == GameState.GameOver;
        public bool IsResourcesInitialized => m_IsResourcesInitialized;

        public UniTask InitializeResourcesAsync()
        {
            if (m_IsResourcesInitialized)
            {
                return UniTask.CompletedTask;
            }

            if (m_ResourceInitializationSource != null)
            {
                return m_ResourceInitializationSource.Task;
            }

            m_ResourceInitializationSource = new UniTaskCompletionSource();
            InitializeResourcesInternalAsync().Forget();
            return m_ResourceInitializationSource.Task;
        }

        protected virtual UniTask OnInitializeResourcesAsync()
        {
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid InitializeResourcesInternalAsync()
        {
            try
            {
                await OnInitializeResourcesAsync();
                m_IsResourcesInitialized = true;
                m_ResourceInitializationSource.TrySetResult();
            }
            catch (Exception exception)
            {
                Log.Error("Initialize sub game '{0}' resources failed: {1}", GameMode, exception);
                m_ResourceInitializationSource.TrySetException(exception);
            }
            finally
            {
                m_ResourceInitializationSource = null;
            }
        }

        public virtual void GameStart()
        {
            ChangeState(GameState.Playing);
        }

        public virtual void PauseGame()
        {
            if (m_CurrentState != GameState.Playing) return;
            ChangeState(GameState.Paused);
        }

        public virtual void ResumeGame()
        {
            if (m_CurrentState != GameState.Paused) return;
            ChangeState(GameState.Playing);
        }

        public virtual void GameOver()
        {
            if (m_CurrentState == GameState.None) return;
            ChangeState(GameState.GameOver);
        }

        public virtual void ReturnMenu()
        {
            if (m_CurrentState == GameState.None) return;
            ChangeState(GameState.Menu);
        }

        public virtual void Restart()
        {
            if (m_CurrentState == GameState.None) return;
            ChangeState(GameState.Restart);
        }

        public virtual void ResetGame()
        {
            ChangeState(GameState.None);
        }

        protected void ChangeState(GameState newState)
        {
            if (m_CurrentState == newState) return;

            GameState previous = m_CurrentState;
            m_CurrentState = newState;

            GameEntry.GameManager?.HandleLevelStateChanged(GameMode, previous, newState);
            GameEntry.Event.Fire(this, GameStateChangedEventArgs.Create(previous, newState));
        }


        public virtual void GameWin()
        {
            CurrentResult = GameResult.Win;
            GameOver();
        }

        public virtual void GameFail()
        {
            CurrentResult = GameResult.Fail;
            GameOver();
        }
    }
}
