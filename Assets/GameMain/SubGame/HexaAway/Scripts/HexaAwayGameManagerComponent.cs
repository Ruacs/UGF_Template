using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    /// <summary>
    /// HexaAway 子游戏运行入口，负责关卡构建、步数状态和基础生命周期衔接。
    /// </summary>
    public sealed partial class HexaAwayGameManagerComponent : SubGameManagerComponent
    {
        // 关卡配置、元素注册表、视觉配置和启动关卡均由 Inspector 或资源注册流程注入。
        [SerializeField] private string compactLevelConfigName = "HexaAway/CompactLevels";
        [SerializeField] private bool compactLevelConfigFromBytes;
        [SerializeField] private ElementRegistrySO elementRegistry;
        [SerializeField] private TilesVisualsDataSO visualsData;
        [SerializeField] private int startupLevelIndex;
        [SerializeField] private bool buildOnGameStart = true;

        // 单局运行上下文，仅在关卡加载期间有效。
        private CompactLevelConfig levelConfig;
        private LevelContext levelContext;
        private HexaAwayInputController inputController;
        private int movesCount;

        public override GameMode GameMode => GameMode.HexaAway;
        public HexaAwayServerConfig ServerConfig { get; } = new();

        protected override void Awake()
        {
            base.Awake();
            Ads.AdsServerConfig.Register(ServerConfig);
        }

        private void OnDestroy()
        {
            if (m_ModuleData != null) m_ModuleData.OnCurrentLevelChanged -= NotifyProgressChanged;
            Ads.AdsServerConfig.Unregister(ServerConfig);
        }

        public CompactLevelConfig LevelConfig => levelConfig;
        public ElementRegistrySO ElementRegistry => elementRegistry;
        public TilesVisualsDataSO VisualsData => visualsData;
        public LevelRuntime LevelRepresentation { get; private set; }
        public int AmountOfLevels => levelConfig != null ? levelConfig.AmountOfLevels : 0;
        public int MovesCount => movesCount;
        public bool IsLevelLoaded => LevelRepresentation != null;

        public event Action<int> MovesCountChanged;
        public event Action LevelLoaded;
        public event Action LevelCompleted;
        public event Func<Vector2Int, bool> ObjectClickIntercepted;

        public void SetLevelConfig(CompactLevelConfig levelConfig)
        {
            this.levelConfig = levelConfig;
        }

        public void SetElementRegistry(ElementRegistrySO elementRegistry)
        {
            this.elementRegistry = elementRegistry;
        }

        public void SetVisualConfig(TilesVisualsDataSO tilesVisualsData)
        {
            visualsData = tilesVisualsData;
        }

        protected override async UniTask OnInitializeResourcesAsync()
        {
            SetElementRegistry(await SubGameResourceLoader.LoadConfigSOAsync<ElementRegistrySO>("ElementRegistry"));
            SetVisualConfig(await SubGameResourceLoader.LoadConfigSOAsync<TilesVisualsDataSO>("TilesVisualsData"));
            SetLevelConfig(await SubGameResourceLoader.LoadJsonConfigAsync<CompactLevelConfig>(compactLevelConfigName, compactLevelConfigFromBytes));
        }




        /// <summary>
        /// 进入子游戏时按配置自动构建启动关卡。
        /// </summary>
        public override void GameStart()
        {
            base.GameStart();

            if (inputController == null)
            {
                inputController = GetComponent<HexaAwayInputController>();
                if (inputController == null)
                {
                    inputController = gameObject.AddComponent<HexaAwayInputController>();
                }
            }

            inputController.Bind(this);
            inputController.BlockPointerOverUI = true;

            if (buildOnGameStart && !IsLevelLoaded)
            {
                BuildLevel(GameEntry.SaveData.Get<HexaAwayGameData>().CurrentLevel);
            }
        }

        /// <summary>
        /// 清理当前关卡并按当前启动关卡索引重新开始。
        /// </summary>
        public override void Restart()
        {
            ClearLevel();
            base.Restart();
            BuildLevel(startupLevelIndex);
            GameStart();
        }

        /// <summary>
        /// 重置子游戏状态，释放当前关卡对象。
        /// </summary>
        public override void ResetGame()
        {
            ClearLevel();
            base.ResetGame();
        }

        /// <summary>
        /// 根据关卡库索引构建运行时关卡表现。
        /// </summary>
        /// <param name="levelIndex">关卡库中的 0 基索引。</param>
        /// <returns>构建成功返回 true；缺少配置或索引非法返回 false。</returns>
        public bool BuildLevel(int levelIndex)
        {
            if (levelConfig == null)
            {
                Debug.LogError("HexaAway compact level config is not assigned.", this);
                return false;
            }

            if (elementRegistry == null)
            {
                Debug.LogError("HexaAway element registry is not assigned.", this);
                return false;
            }

            if (visualsData == null)
            {
                Debug.LogError("HexaAway visuals data is not assigned.", this);
                return false;
            }

            CompactLevelLib compactLevel = levelConfig.GetLevelByIndex(levelIndex);
            RuntimeLevelData runtimeLevelData = CompactLevelParser.Parse(compactLevel);
            if (runtimeLevelData == null)
            {
                Debug.LogError($"HexaAway level index {levelIndex} is not configured.", this);
                return false;
            }

            ClearLevel();
            startupLevelIndex = levelIndex;
            movesCount = Mathf.Max(0, runtimeLevelData.Moves);
            levelContext = new LevelContext(this, elementRegistry, visualsData, GameEntry.SaveData?.Get<HexaAwayGameData>()?.TileSkinId);
            LevelRepresentation = new LevelRuntime(levelContext, runtimeLevelData);
            LevelRepresentation.SpawnLevel();
            MovesCountChanged?.Invoke(movesCount);
            LevelLoaded?.Invoke();
            return true;
            
        }

        /// <summary>
        /// 释放当前关卡表现和运行上下文。
        /// </summary>
        public void ClearLevel()
        {
            LevelRepresentation?.Dispose();
            LevelRepresentation = null;
            levelContext = null;
        }

        /// <summary>
        /// 消耗一步并广播步数变化。
        /// </summary>
        public void ConsumeMove()
        {
            if (movesCount <= 0)
            {
                return;
            }

            movesCount--;
            MovesCountChanged?.Invoke(movesCount);
        }

        /// <summary>
        /// 增加当前关卡可用步数。
        /// </summary>
        public bool AddMoves(int count)
        {
            if (count <= 0 || LevelRepresentation == null)
            {
                return false;
            }

            movesCount += count;
            MovesCountChanged?.Invoke(movesCount);
            return true;
        }

        public void SetPointerOverUIBlocking(bool enabled)
        {
            if (inputController == null)
            {
                inputController = GetComponent<HexaAwayInputController>();
            }

            if (inputController != null)
            {
                inputController.BlockPointerOverUI = enabled;
            }
        }

        /// <summary>
        /// 复活后恢复当前关卡的可操作状态，不重新构建关卡。
        /// </summary>
        public void ResumeAfterRevive()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// 检查关卡完成或步数耗尽，并触发结束流程。
        /// </summary>
        public void CheckCompleteStatus()
        {
            if (LevelRepresentation == null || LevelRepresentation.HasMovingTiles || LevelRepresentation.IsTileInputBlocked)
            {
                return;
            }

            if (LevelRepresentation.LevelCleared)
            {
                LevelCompleted?.Invoke();
                GameWin();
            }
            else if (movesCount <= 0)
            {
                GameFail();
            }
        }

        // Selection is persisted immediately and resolved once when the next level is built.
        public bool SelectTileSkin(string skinId)
        {
            if (visualsData == null || GameEntry.SaveData?.Get<HexaAwayGameData>() == null) return false;
            TileVisualSkinDataSO skin = visualsData.GetSkinData(skinId);
            if (skin == null || skin.SkinId != skinId) return false;
            GameEntry.SaveData.Get<HexaAwayGameData>().TileSkinId = skinId;
            return true;
        }

        /// <summary>
        /// 处理棋盘坐标点击，优先派发给 Tile，其次派发给交互物。
        /// </summary>
        public void OnObjectClicked(Vector2Int position)
        {
            if (!IsPlaying || movesCount < 1 || LevelRepresentation == null || LevelRepresentation.IsTileInputBlocked)
            {
                return;
            }

            if (TryInterceptObjectClick(position))
            {
                return;
            }

            if (LevelRepresentation.TilesGrid.TryGet(position.x, position.y, out TileBehavior tile))
            {
                tile.OnObjectClicked();
                return;
            }

            if (LevelRepresentation.InteractablesGrid.TryGet(position.x, position.y, out GimmickBehavior interactableObject))
            {
                if (LevelRepresentation.HasMovingTiles) return;
                interactableObject.OnClicked();
                return;
            }

            if (LevelRepresentation.PlatformsGrid.TryGet(position.x, position.y, out PlatformBehavior platform) && platform.IsInteractivePlatform)
            {
                platform.OnClicked();
            }
        }

        private bool TryInterceptObjectClick(Vector2Int position)
        {
            if (ObjectClickIntercepted == null)
            {
                return false;
            }

            Delegate[] handlers = ObjectClickIntercepted.GetInvocationList();
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] is Func<Vector2Int, bool> handler && handler.Invoke(position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
