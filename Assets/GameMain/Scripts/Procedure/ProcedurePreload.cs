using Ads;
using GameFramework;
using GameFramework.Event;
using GameFramework.Localization;
using GameFramework.Resource;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Lokas
{
    public class ProcedurePreload : ProcedureBase
    {
        private int totalProgress;
        private int loadedProgress;
        private float preloadElapsedTime;
        private const float PreloadProgressEnd = ProcedureChangeScene.ContinuedLoadingStartProgress;
        private const float PreloadVirtualProgressDuration = 2.5f;
        private const float PreloadVirtualProgressMaxBeforeComplete = 0.95f;
        private static readonly HashSet<Language> s_SupportedLanguages = new HashSet<Language>
        {
            Language.ChineseSimplified,
            Language.English,
            Language.French,
            Language.Spanish,
            Language.Portuguese,
            Language.Italian,
            Language.Russian,
            Language.Vietnamese,
            Language.Turkish,
            Language.German,
            Language.ChineseTraditional,
            Language.Japanese,
            Language.Korean,
        };

        public static readonly string[] DataTableNames =
        {
            "Entity",
            "Scene",
            "UIForm",
            "Sound",
            "NameDataEN"
        };

        private readonly Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();
        private static string s_FontKey;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            loadedProgress = 0;
            totalProgress = 0;
            preloadElapsedTime = 0f;
            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            m_LoadedFlag.Clear();
            GameEntry.BuiltinView?.BeginLoadingProgress();
            PreloadResources();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);
            // GameEntry.BuiltinView.HideLoadingProgress();

            GameEntry.Rank.StartUp();
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void PreloadResources()
        {
            LoadConfig("DefaultConfig");

            foreach (string dataTableName in DataTableNames)
            {
                LoadDataTable(dataTableName);
            }

            LoadDictionary();

            LoadAllCustomConfigSO();
            AdsServerConfig.Load();     //二次检查加载
            totalProgress = m_LoadedFlag.Count;
        }


        /// <summary>
        /// 加载所有自定义配置
        /// </summary>
        private void LoadAllCustomConfigSO()
        {
            LoadConfigSO<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.AssetName, config => { SubGameAssetRegistry.RegisterConfig(config); });
            // LoadSubGameConfigSO<TilesVisualsDataSO>("HexaAway", "Database", "TilesVisualsData");
            // LoadSubGameConfigSO<PropButtonConfigDatabaseSO>("HexaAway", "Database", "PropButtonConfigDatabase");
            s_FontKey = "Font.Main";
            m_LoadedFlag.Add(s_FontKey, false);
            LoadConfigSO<TMPLanguageFontConfig>("TMPLanguageFontConfig", config =>
            {
                GameEntry.TMPFont.SetLanguageConfig(config, () => MarkLoadComplete(s_FontKey));
            }, () => MarkLoadComplete(s_FontKey));
            LoadConfigSO<RewardDataBaseSO>("RewardDataBase", config => { GameEntry.CustomConfig.SetRewardConfig(config); });
            LoadConfigSO<AvatarDatabaseSO>("Avatar/AvatarDatabase", config => { GameEntry.CustomConfig.SetAvatarConfig(config); });
            LoadConfigSO<PropDataBaseSO>("PropDataBase", config => { GameEntry.CustomConfig.SetPropDataConfig(config); });
            LoadConfigSO<RankTargetConfigSO>("RankTargetConfig", config => { GameEntry.Rank.SetRankTargetConfig(config); });
            // LoadConfigSO<PlayerConfigSO>("PlayerConfigSO", config => { GameEntry.CustomConfig.SetPlayerConfig(config); });

        }

        private void LoadConfigSO<T>(string configSOName, System.Action<T> onSuccess = null, System.Action onFailure = null) where T : ScriptableObject
        {
            string configSOAssetName = AssetUtility.GetScriptableObjectAsset(configSOName);
            m_LoadedFlag.Add(configSOAssetName, false);

            GameEntry.Resource.LoadAsset(configSOAssetName, typeof(T), new LoadAssetCallbacks(
                (assetName, asset, duration, userData) =>
                {
                    MarkLoadComplete(configSOAssetName);
                    onSuccess?.Invoke(asset as T);
                    Log.Info("Load ConfigSO '{0}' OK.", configSOName);
                },
                (assetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not load ConfigSO '{0}' from '{1}' with error message '{2}'.", configSOName, assetName, errorMessage);
                    MarkLoadComplete(configSOAssetName);
                    onFailure?.Invoke();
                }));
        }

        private void LoadSubGameConfigSO<T>(string gameName, string category, string assetName) where T : ScriptableObject
        {
            string configSOAssetName = AssetUtility.GetSubGameScriptableObjectAsset(gameName, category, assetName);
            m_LoadedFlag.Add(configSOAssetName, false);

            GameEntry.Resource.LoadAsset(configSOAssetName, typeof(T), new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    MarkLoadComplete(configSOAssetName);
                    if (!(asset is T))
                    {
                        Log.Error("Preloaded sub game ConfigSO '{0}' from '{1}' is not '{2}'.", assetName, loadedAssetName, typeof(T).Name);
                        return;
                    }

                    Log.Info("Preloaded sub game ConfigSO '{0}/{1}/{2}' OK.", gameName, category, assetName);
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not preload sub game ConfigSO '{0}/{1}/{2}' from '{3}' with error message '{4}'.", gameName, category, assetName, loadedAssetName, errorMessage);
                    MarkLoadComplete(configSOAssetName);
                }));
        }

        private void LoadSubGamePrefab(string gameName, string assetName)
        {
            string prefabAssetName = AssetUtility.GetSubGameUIFormAsset(gameName, assetName);
            m_LoadedFlag.Add(prefabAssetName, false);

            GameEntry.Resource.LoadAsset(prefabAssetName, typeof(GameObject), new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    MarkLoadComplete(prefabAssetName);
                    Log.Info("Preloaded sub game UI prefab '{0}/{1}' OK.", gameName, assetName);
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not preload sub game UI prefab '{0}/{1}' from '{2}' with error message '{3}'.", gameName, assetName, loadedAssetName, errorMessage);
                    MarkLoadComplete(prefabAssetName);
                }));
        }

        private void LoadCustomJsonConfig<T>(string configName, bool fromBytes, System.Action<T> onSuccess = null)
        {
            string configAssetName = AssetUtility.GetCustomConfigAsset(configName, fromBytes, "json");
            m_LoadedFlag.Add(configAssetName, false);

            GameEntry.CustomConfig.LoadJsonConfig<T>(
                configName,
                fromBytes,
                config =>
                {
                    onSuccess?.Invoke(config);
                    MarkLoadComplete(configAssetName);
                },
                errorMessage =>
                {
                    Log.Error("Can not load custom json config '{0}' with error message '{1}'.", configAssetName, errorMessage);
                    MarkLoadComplete(configAssetName);
                });
        }

        /// <summary>
        /// 加载字典
        /// </summary>
        private void LoadDictionary()
        {
            Language language = GameEntry.SaveData.Language;
            if (language == Language.Unspecified)
            {
#if UNITY_EDITOR
                language = GameEntry.Base.EditorLanguage;
#else
                language = GameEntry.Localization.SystemLanguage;
#endif
            }

            Language originalLanguage = language;
            language = NormalizeSupportedLanguage(language);
            GameEntry.SaveData.Language = language;

            if (originalLanguage != language)
            {
                Log.Warning(Utility.Text.Format("Language {0} is not supported, fallback to {1}.", originalLanguage, language));
            }

            string dictionaryAssetName = AssetUtility.GetDictionaryAsset();
            m_LoadedFlag.Add(dictionaryAssetName, false);
            GameEntry.Localization.ReadData(dictionaryAssetName, this);
        }

        private Language NormalizeSupportedLanguage(Language language)
        {
            if (language == Language.PortugueseBrazil || language == Language.PortuguesePortugal)
            {
                return Language.Portuguese;
            }

            return s_SupportedLanguages.Contains(language) ? language : Language.English;
        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="configName"></param>
        private void LoadConfig(string configName)
        {
            string configAssetName = AssetUtility.GetConfigAsset(configName, false);
            m_LoadedFlag.Add(configAssetName, false);
            GameEntry.Config.ReadData(configAssetName, this);
        }
        /// <summary>
        /// 加载数据表
        /// </summary>
        /// <param name="dataTableName"></param>
        private void LoadDataTable(string dataTableName)
        {
            string dataTableAssetName = AssetUtility.GetDataTableAsset(dataTableName, true);
            m_LoadedFlag.Add(dataTableAssetName, false);
            GameEntry.DataTable.LoadDataTable(dataTableName, dataTableAssetName, this);
        }


        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            preloadElapsedTime += elapseSeconds;

            if (GameEntry.BuiltinView != null)
            {
                bool isLoadComplete = loadedProgress >= totalProgress;
                float realProgress = totalProgress > 0 ? loadedProgress / (float)totalProgress : 1f;
                float targetProgress = GetPreloadDisplayProgress(realProgress, isLoadComplete);

                GameEntry.BuiltinView.UpdateLoadingProgress(targetProgress, false, elapseSeconds);
                if (!isLoadComplete || !GameEntry.BuiltinView.IsLoadingProgressReached(PreloadProgressEnd))
                {
                    return;
                }
            }
            else
            {
                foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
                {
                    if (!loadedFlag.Value)
                    {
                        return;
                    }
                }
            }


            int sceneId = GameEntry.Config.GetInt("Scene.Menu");
            bool isFirstLaunch = GameEntry.SaveData.IsFirstLaunch;

            // 首次直达功能目前未启用；启用时应显式查询目标子游戏配置。
            Log.Info("isFirstLaunch: " + isFirstLaunch);
            // if (targetGameConfig.EnableFirstLaunchEnterGame && isFirstLaunch)
            // {
            //     sceneId = GameEntry.Config.GetInt("Scene.HexaAway");
            // }

            Log.Info("sceneId: " + sceneId);


            procedureOwner.SetData<VarInt32>("NextSceneId", sceneId);
            procedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ReuseLoadingProgressKey, GameEntry.BuiltinView != null);
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private float GetPreloadDisplayProgress(float realProgress, bool isLoadComplete)
        {
            if (isLoadComplete)
            {
                return PreloadProgressEnd;
            }

            float realTargetProgress = Mathf.Clamp01(realProgress) * PreloadProgressEnd;
            float virtualTargetProgress = PreloadVirtualProgressDuration > 0f
                ? Mathf.Clamp01(preloadElapsedTime / PreloadVirtualProgressDuration) * PreloadProgressEnd * PreloadVirtualProgressMaxBeforeComplete
                : PreloadProgressEnd * PreloadVirtualProgressMaxBeforeComplete;

            return Mathf.Max(realTargetProgress, virtualTargetProgress);
        }

        private void OnLoadConfigSuccess(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData == this)
            {
                MarkLoadComplete(ne.ConfigAssetName);
            }
        }

        private void OnLoadConfigFailure(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Error("Can not load config '{0}' with error message '{1}'.", ne.ConfigAssetName, ne.ErrorMessage);
                MarkLoadComplete(ne.ConfigAssetName);
            }
        }

        private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
        {
            LoadDataTableSuccessEventArgs ne = (LoadDataTableSuccessEventArgs)e;
            if (ne.UserData == this)
            {
                MarkLoadComplete(ne.DataTableAssetName);
            }
        }

        private void OnLoadDataTableFailure(object sender, GameEventArgs e)
        {
            LoadDataTableFailureEventArgs ne = (LoadDataTableFailureEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Error("Can not load data table '{0}' with error message '{1}'.", ne.DataTableAssetName, ne.ErrorMessage);
                MarkLoadComplete(ne.DataTableAssetName);
            }
        }

        private void OnLoadDictionarySuccess(object sender, GameEventArgs e)
        {
            LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
            if (ne.UserData == this)
            {
                MarkLoadComplete(ne.DictionaryAssetName);
            }
        }

        private void OnLoadDictionaryFailure(object sender, GameEventArgs e)
        {
            LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
            if (ne.UserData == this)
            {
                Log.Error("Can not load dictionary '{0}' with error message '{1}'.", ne.DictionaryAssetName, ne.ErrorMessage);
                MarkLoadComplete(ne.DictionaryAssetName);
            }
        }

        private void MarkLoadComplete(string key)
        {
            if (m_LoadedFlag.TryGetValue(key, out bool loaded) && !loaded)
            {
                m_LoadedFlag[key] = true;
                loadedProgress++;
            }
        }
    }
}
