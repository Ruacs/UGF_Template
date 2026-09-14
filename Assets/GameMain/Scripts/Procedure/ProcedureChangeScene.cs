using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.DataTable;
using GameFramework.Event;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Lokas
{
    public class ProcedureChangeScene : ProcedureBase
    {
        public const string ReuseLoadingProgressKey = "ReuseLoadingProgress";
        public const string ShowLoadingProgressKey = "ShowLoadingProgress";
        public const string ShowLoadingProgressBarKey = "ShowLoadingProgressBar";
        public const float ContinuedLoadingStartProgress = 0.7f;
        private const float SceneLoadingEndProgress = 0.9f;
        private const float SubGameResourceLoadingProgress = 0.95f;
        private const int MenuSceneId = 1;

        private bool m_IsChangeSceneComplete;
        private bool m_IsSceneLoadComplete;
        private bool m_IsSubGameResourceLoading;
        private bool m_ReuseLoadingProgress;
        private bool m_ShowLoadingProgress;
        private bool m_ShowLoadingProgressBar;
        private int m_BackgroundMusicId;
        private float m_SceneLoadingProgress;
        private Camera m_UiCamera;

        int m_NextSceneId;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Sound.Mute("Music", !GameEntry.SaveData.IsMusic);
            GameEntry.Sound.Mute("UISound", !GameEntry.SaveData.IsSound);
            GameEntry.Sound.Mute("Sound", !GameEntry.SaveData.IsSound);

            m_IsChangeSceneComplete = false;
            m_IsSceneLoadComplete = false;
            m_IsSubGameResourceLoading = false;
            m_SceneLoadingProgress = 0f;
            VarBoolean reuseLoadingProgress = procedureOwner.GetData<VarBoolean>(ReuseLoadingProgressKey);
            m_ReuseLoadingProgress = reuseLoadingProgress != null && reuseLoadingProgress;
            procedureOwner.SetData<VarBoolean>(ReuseLoadingProgressKey, false);

            VarBoolean showLoadingProgress = procedureOwner.GetData<VarBoolean>(ShowLoadingProgressKey);
            m_ShowLoadingProgress = showLoadingProgress == null || showLoadingProgress;
            procedureOwner.SetData<VarBoolean>(ShowLoadingProgressKey, true);

            VarBoolean showLoadingProgressBar = procedureOwner.GetData<VarBoolean>(ShowLoadingProgressBarKey);
            m_ShowLoadingProgressBar = showLoadingProgressBar == null || showLoadingProgressBar;
            procedureOwner.SetData<VarBoolean>(ShowLoadingProgressBarKey, true);

            if (m_ShowLoadingProgress)
            {
                GameEntry.BuiltinView?.ShowTransition(
                    m_ShowLoadingProgressBar,
                    m_ReuseLoadingProgress ? ContinuedLoadingStartProgress : 0f,
                    !m_ReuseLoadingProgress);
            }
            else
            {
                GameEntry.BuiltinView?.HideTransition(0f);
            }

            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
            GameEntry.Event.Subscribe(LoadSceneDependencyAssetEventArgs.EventId, OnLoadSceneDependencyAsset);

            BindCamerasForCurrentScene();

            ChangeScene(procedureOwner).Forget();
        }

        private async UniTaskVoid ChangeScene(ProcedureOwner procedureOwner)
        {
            GameEntry.Sound.StopAllLoadingSounds();
            GameEntry.Sound.StopAllLoadedSounds();
            GameEntry.Entity.HideAllLoadingEntities();
            GameEntry.Entity.HideAllLoadedEntities();

            string[] loadedSceneAssetNames = GameEntry.Scene.GetLoadedSceneAssetNames();
            for (int i = 0; i < loadedSceneAssetNames.Length; i++)
            {
                GameEntry.Scene.UnloadScene(loadedSceneAssetNames[i]);
            }

            GameEntry.Base.ResetNormalGameSpeed();

            m_NextSceneId = procedureOwner.GetData<VarInt32>("NextSceneId");
            if (m_NextSceneId <= 0)
            {
                m_NextSceneId = MenuSceneId;
            }



            IDataTable<DRScene> dtScene = GameEntry.DataTable.GetDataTable<DRScene>();
            DRScene drScene = dtScene.GetDataRow(m_NextSceneId);
            if (drScene == null)
            {
                Log.Warning("Can not load scene '{0}' from data table.", m_NextSceneId.ToString());
                return;
            }

            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset(drScene.AssetName), Constant.AssetPriority.SceneAsset, this);
            m_BackgroundMusicId = drScene.BackgroundMusicId;
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.BuiltinView?.HideTransition(m_ShowLoadingProgress ? 0.3f : 0f);

            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
            GameEntry.Event.Unsubscribe(LoadSceneDependencyAssetEventArgs.EventId, OnLoadSceneDependencyAsset);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_ShowLoadingProgress && GameEntry.BuiltinView != null)
            {
                if (!GameEntry.BuiltinView.UpdateTransitionProgress(GetSceneLoadingProgress(), m_IsChangeSceneComplete, elapseSeconds))
                {
                    return;
                }
            }
            else if (!m_IsChangeSceneComplete)
            {
                return;
            }

            if (m_NextSceneId == MenuSceneId)
            {
                ChangeState<ProcedureMenu>(procedureOwner);
            }
            else
            {

                ChangeToGameProcedure(procedureOwner);

            }
        }



        private void OnLoadSceneSuccess(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
            if (ne.UserData != this) return;

            Log.Info("Load scene '{0}' OK.", ne.SceneAssetName);

            BindCamerasForCurrentScene();

            if (m_BackgroundMusicId > 0)
            {
                GameEntry.Sound.PlayMusic(m_BackgroundMusicId);
            }

            m_IsSceneLoadComplete = true;
            m_SceneLoadingProgress = 1f;
            InitializeLoadedSceneResourcesAsync().Forget();
        }

        private async UniTaskVoid InitializeLoadedSceneResourcesAsync()
        {
            SubGameManagerComponent subGameManager = GetTargetSubGameManager();
            if (subGameManager == null || subGameManager.IsResourcesInitialized)
            {
                m_IsChangeSceneComplete = true;
                return;
            }

            m_IsSubGameResourceLoading = true;
            try
            {
                await subGameManager.InitializeResourcesAsync();
            }
            catch
            {
                // Keep the transition from hanging forever. The target game procedure can
                // still retry initialization and report the concrete failure path.
            }
            finally
            {
                m_IsSubGameResourceLoading = false;
                m_IsChangeSceneComplete = true;
            }
        }

        private SubGameManagerComponent GetTargetSubGameManager()
        {
            return GameEntry.SubGames?.GetForScene(m_NextSceneId);
        }

        private void ChangeToGameProcedure(ProcedureOwner procedureOwner)
        {
         
            var manager = GetTargetSubGameManager();
            if (manager != null)
            {
                ChangeState(procedureOwner, manager.GameProcedureType);
                return;
            }

            ChangeState<ProcedureGameDefault>(procedureOwner);
        }

        /// <summary>
        /// Keeps the persistent Launcher UI camera as an overlay on the active scene's
        /// base camera. While the Launcher scene has no base camera of its own, the UI
        /// camera temporarily renders as a base camera so the bootstrap UI remains visible.
        /// </summary>
        private void BindCamerasForCurrentScene()
        {
            if (m_UiCamera == null)
            {
                m_UiCamera = GameEntry.UI.UICamera;
            }

            if (m_UiCamera == null)
            {
                Log.Warning("Can not bind URP cameras because the persistent UI camera was not found.");
                return;
            }

            UniversalAdditionalCameraData uiCameraData = m_UiCamera.GetUniversalAdditionalCameraData();
            if (uiCameraData == null)
            {
                Log.Warning("Can not bind URP cameras because UI camera '{0}' has no UniversalAdditionalCameraData.", m_UiCamera.name);
                return;
            }
            Camera baseCamera = FindBaseCameraInActiveScene();

            RemoveUiCameraFromAllStacks(m_UiCamera);

            if (baseCamera == null || baseCamera == m_UiCamera)
            {
                uiCameraData.renderType = CameraRenderType.Base;
                return;
            }

            UniversalAdditionalCameraData baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
            if (baseCameraData == null || baseCameraData.cameraStack == null)
            {
                Log.Warning("Can not bind UI camera because base camera '{0}' has no valid URP camera stack.", baseCamera.name);
                return;
            }

            baseCameraData.renderType = CameraRenderType.Base;
            uiCameraData.renderType = CameraRenderType.Overlay;

            if (!baseCameraData.cameraStack.Contains(m_UiCamera))
            {
                baseCameraData.cameraStack.Add(m_UiCamera);
            }
        }

        private Camera FindBaseCameraInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Camera[] cameras = GameObject.FindObjectsOfType<Camera>(true);

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == m_UiCamera || cameras[i].gameObject.scene != activeScene)
                {
                    continue;
                }

                UniversalAdditionalCameraData cameraData = cameras[i].GetUniversalAdditionalCameraData();
                if (cameraData != null && cameraData.renderType == CameraRenderType.Base)
                {
                    return cameras[i];
                }
            }

            return null;
        }

        private static void RemoveUiCameraFromAllStacks(Camera uiCamera)
        {
            Camera[] cameras = GameObject.FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                UniversalAdditionalCameraData cameraData = cameras[i].GetUniversalAdditionalCameraData();
                if (cameraData != null && cameraData.cameraStack != null)
                {
                    cameraData.cameraStack.Remove(uiCamera);
                }
            }
        }

        private void OnLoadSceneFailure(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Error("Load scene '{0}' failure, error message '{1}'.", ne.SceneAssetName, ne.ErrorMessage);
            }
        }

        private void OnLoadSceneUpdate(object sender, GameEventArgs e)
        {
            LoadSceneUpdateEventArgs ne = (LoadSceneUpdateEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Info("Load scene '{0}' update, progress '{1}'.", ne.SceneAssetName, ne.Progress.ToString("P2"));
                m_SceneLoadingProgress = ne.Progress;
            }
        }

        private float GetSceneLoadingProgress()
        {
            float loadingProgress = m_IsSceneLoadComplete
                ? (m_IsSubGameResourceLoading ? SubGameResourceLoadingProgress : 1f)
                : m_SceneLoadingProgress * SceneLoadingEndProgress;

            if (!m_ReuseLoadingProgress)
            {
                return loadingProgress;
            }

            return Mathf.Lerp(ContinuedLoadingStartProgress, 1f, loadingProgress);
        }

        private void OnLoadSceneDependencyAsset(object sender, GameEventArgs e)
        {
            LoadSceneDependencyAssetEventArgs ne = (LoadSceneDependencyAssetEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Info("Load scene '{0}' dependency asset '{1}', count '{2}/{3}'.", ne.SceneAssetName, ne.DependencyAssetName, ne.LoadedCount.ToString(), ne.TotalCount.ToString());
            }
        }
    }
}
