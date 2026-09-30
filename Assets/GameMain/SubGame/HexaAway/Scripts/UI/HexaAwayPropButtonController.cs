using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway 的道具按钮组控制器，负责把通用 PropButtonView 和本玩法的存档、效果入口连接起来。
    /// </summary>
    public sealed class HexaAwayPropButtonController : IDisposable
    {
        private readonly Dictionary<HexaAwayPropType, PropButtonModel> m_ModelMap = new Dictionary<HexaAwayPropType, PropButtonModel>();
        private readonly List<PropButtonView> m_BoundViews = new List<PropButtonView>();
        private readonly Dictionary<HexaAwayPropType, GameObject> m_PrefabMap = new Dictionary<HexaAwayPropType, GameObject>();
        private readonly Dictionary<HexaAwayPropType, HexaAwayPropAnimationPlayer> m_PlayerMap = new Dictionary<HexaAwayPropType, HexaAwayPropAnimationPlayer>();

        private PropButtonConfigDatabaseSO m_ConfigDatabase;
        private HexaAwayPropType? m_SelectedPropType;
        private int m_CurrentLevel;
        private int m_AddMoveAmount;
        private bool m_UseInfiniteProps;
        private bool m_IsApplyingProp;

        public HexaAwayPropType? SelectedPropType => m_SelectedPropType;

        /// <summary>数量不足且配置允许补充时触发，外部可在这里打开购买或广告页面。</summary>
        public event Action<HexaAwayPropType, PropButtonModel> PurchaseRequested;

        /// <summary>选中状态变化时触发，后续接入棋盘道具效果时可监听。</summary>
        public event Action<HexaAwayPropType?> SelectedPropChanged;
        public event Action<HexaAwayPropType, PropButtonModel> SelectionRequested;
        public event Action<HexaAwayPropType> SelectionCompleted;

        public void Initialize(
            PropButtonView[] propButtonViews,
            PropButtonConfigDatabaseSO configDatabase,
            HexaAwayPropPrefabBinding[] propPrefabs,
            int currentLevel,
            int addMoveAmount,
            bool useInfiniteProps)
        {
            DisposeBindings();

            m_ConfigDatabase = configDatabase;
            m_CurrentLevel = currentLevel;
            m_AddMoveAmount = Mathf.Max(1, addMoveAmount);
            m_UseInfiniteProps = useInfiniteProps;
            BuildPrefabMap(propPrefabs);
            EnsureInitialPropCounts();

            if (propButtonViews == null || configDatabase == null)
            {
                return;
            }

            IReadOnlyList<PropButtonConfigSO> configs = configDatabase.Configs;
            int count = Mathf.Min(propButtonViews.Length, configs.Count);

            for (int i = 0; i < count; i++)
            {
                PropButtonView view = propButtonViews[i];
                PropButtonConfigSO configSO = configs[i];
                if (view == null || configSO == null || !TryGetPropType(configSO.ItemId, out HexaAwayPropType propType))
                {
                    continue;
                }

                PropButtonConfig config = configSO.CreateRuntimeConfig();
                PropButtonState state = new PropButtonState();
                state.SetCurrentLevel(m_CurrentLevel);
                state.SetCount(GameEntry.SaveData.Get<HexaAwayGameData>().GetPropCount(propType));
                state.SetDisplayMode(m_UseInfiniteProps ? PropButtonDisplayMode.InfiniteForever : PropButtonDisplayMode.Normal);

                PropButtonModel model = new PropButtonModel(config, state);
                m_ModelMap[propType] = model;

                view.gameObject.SetActive(true);
                view.Bind(model);
                view.Clicked += OnButtonClicked;
                m_BoundViews.Add(view);
            }

            for (int i = count; i < propButtonViews.Length; i++)
            {
                if (propButtonViews[i] == null)
                {
                    continue;
                }

                propButtonViews[i].Bind(null);
                propButtonViews[i].gameObject.SetActive(false);
            }

            RefreshSelectedState();
        }

        public void Dispose()
        {
            DisposeBindings();
            PurchaseRequested = null;
            SelectedPropChanged = null;
            SelectionRequested = null;
            SelectionCompleted = null;
        }

        public void SetCurrentLevel(int currentLevel)
        {
            m_CurrentLevel = currentLevel;
            foreach (PropButtonModel model in m_ModelMap.Values)
            {
                model.SetCurrentLevel(currentLevel);
            }
        }

        public void RefreshProp(HexaAwayPropType propType)
        {
            if (!m_ModelMap.TryGetValue(propType, out PropButtonModel model))
            {
                return;
            }

            model.SetCount(GameEntry.SaveData.Get<HexaAwayGameData>().GetPropCount(propType));
            model.SetDisplayMode(m_UseInfiniteProps ? PropButtonDisplayMode.InfiniteForever : PropButtonDisplayMode.Normal);
        }

        public void RefreshAll()
        {
            foreach (HexaAwayPropType propType in m_ModelMap.Keys)
            {
                RefreshProp(propType);
            }

            SetCurrentLevel(m_CurrentLevel);
            RefreshSelectedState();
        }

        /// <summary>
        /// 棋盘点击前调用。已选中选择型道具时会接管本次点击，避免继续触发普通 Tile 点击。
        /// </summary>
        public bool TryHandleObjectClick(Vector2Int position)
        {
            if (m_IsApplyingProp)
            {
                return true;
            }

            if (!m_SelectedPropType.HasValue)
            {
                return false;
            }

            HexaAwayPropType propType = m_SelectedPropType.Value;
            if (!m_ModelMap.TryGetValue(propType, out PropButtonModel model) || !model.CanUse())
            {
                ClearSelection();
                return true;
            }

            bool applied = TryApplySelectedProp(propType, position);
            if (applied)
            {
                ConsumeProp(propType);
                ClearSelection();
                SelectionCompleted?.Invoke(propType);
            }

            return true;
        }

        public void ClearSelection()
        {
            if (!m_SelectedPropType.HasValue)
            {
                return;
            }

            m_SelectedPropType = null;
            RefreshTargetCrosshairs();
            RefreshSelectedState();
            SelectedPropChanged?.Invoke(null);
        }

        private void OnButtonClicked(PropButtonView view, PropButtonModel model)
        {
            if (model == null || !TryGetPropType(model.ItemId, out HexaAwayPropType propType))
            {
                return;
            }

            if (!model.CanUse())
            {
                if (model.ShouldShowPurchase())
                {
                    PurchaseRequested?.Invoke(propType, model);
                }

                return;
            }

            if (propType == HexaAwayPropType.AddMove)
            {
                UseAddMove(propType);
                return;
            }

            if (!HasSelectableTarget(propType))
            {
                return;
            }

            RequestSelection(propType, model);
        }

        private void UseAddMove(HexaAwayPropType propType)
        {
            if (!GameEntry.HexaAway.AddMoves(m_AddMoveAmount))
            {
                return;
            }

            ConsumeProp(propType);
            ClearSelection();
        }

        private void RequestSelection(HexaAwayPropType propType, PropButtonModel model)
        {
            m_SelectedPropType = propType;
            RefreshTargetCrosshairs();
            RefreshSelectedState();
            SelectedPropChanged?.Invoke(m_SelectedPropType);
            SelectionRequested?.Invoke(propType, model);
        }

        private bool HasSelectableTarget(HexaAwayPropType propType)
        {
            LevelRuntime level = GameEntry.HexaAway.LevelRepresentation;
            if (level == null)
            {
                return false;
            }

            switch (propType)
            {
                case HexaAwayPropType.Drill:
                    return level.HasNormalPlatformPropTarget();
                case HexaAwayPropType.Tnt:
                    return level.HasTntPlatformPropTarget();
                default:
                    return true;
            }
        }

        private bool TryApplySelectedProp(HexaAwayPropType propType, Vector2Int position)
        {
            switch (propType)
            {
                case HexaAwayPropType.Hammer:
                    return TryApplyHammer(position);
                case HexaAwayPropType.Drill:
                    return TryApplyDrill(position);
                case HexaAwayPropType.Tnt:
                    return TryApplyTnt(position);
                default:
                    return false;
            }
        }

        private bool TryApplyHammer(Vector2Int position)
        {
            LevelRuntime level = GameEntry.HexaAway.LevelRepresentation;
            if (level == null || !level.TilesGrid.TryGet(position.x, position.y, out TileBehavior tile) || tile.IsCollected)
            {
                return false;
            }

            PlayPropAnimation(HexaAwayPropType.Hammer, tile.transform.position, () =>
            {
                CollectTile(level, tile);
                GameEntry.HexaAway.CheckCompleteStatus();
            });

            return true;
        }

        private bool TryApplyDrill(Vector2Int position)
        {
            LevelRuntime level = GameEntry.HexaAway.LevelRepresentation;
            if (level == null
                || level.TilesGrid.Has(position.x, position.y)
                || level.InteractablesGrid.Has(position.x, position.y)
                || !level.PlatformsGrid.TryGet(position.x, position.y, out PlatformBehavior platform)
                || !level.IsNormalPlatformPropTarget(platform))
            {
                return false;
            }

            PlayPropAnimation(HexaAwayPropType.Drill, platform.transform.position, () =>
            {
                if (platform != null)
                {
                    level.RemovePlatform(platform);
                }

                GameEntry.HexaAway.CheckCompleteStatus();
            });

            return true;
        }

        private bool TryApplyTnt(Vector2Int position)
        {
            LevelRuntime level = GameEntry.HexaAway.LevelRepresentation;
            if (level == null
                || !level.PlatformsGrid.TryGet(position.x, position.y, out PlatformBehavior platform)
                || !level.IsTntPlatformPropTarget(platform))
            {
                return false;
            }

            Vector3 worldPosition = platform.transform.position;

            PlayPropAnimation(HexaAwayPropType.Tnt, worldPosition, () =>
            {
                Vector2Int[] neighbors = DirectionHelper.GetNeighbors(position);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    Vector2Int neighbor = neighbors[i];
                    if (level.TilesGrid.TryGet(neighbor.x, neighbor.y, out TileBehavior tile) && !tile.IsCollected)
                    {
                        CollectTile(level, tile);
                    }
                }

                GameEntry.HexaAway.CheckCompleteStatus();
            });

            return true;
        }

        private static void CollectTile(LevelRuntime level, TileBehavior tile)
        {
            if (level == null || tile == null)
            {
                return;
            }

            tile.OnTileCollected();
            tile.DisableEffects();
            level.OnTileDestructed(tile);
            UnityEngine.Object.Destroy(tile.gameObject);
        }

        private void PlayPropAnimation(HexaAwayPropType propType, Vector3 worldPosition, Action onHit)
        {
            SetBusy(propType, true);

            if (!TryGetAnimationPlayer(propType, out HexaAwayPropAnimationPlayer player))
            {
                onHit?.Invoke();
                SetBusy(propType, false);
                ClearSelection();
                return;
            }

            m_IsApplyingProp = true;
            player.Play(worldPosition, onHit, () =>
            {
                m_IsApplyingProp = false;
                SetBusy(propType, false);
                ClearSelection();
            });
        }

        private bool ConsumeProp(HexaAwayPropType propType)
        {
            // 临时无限数量模式下保留使用逻辑和动画，但不改玩家存档数量。
            if (m_UseInfiniteProps)
            {
                return true;
            }

            return true;

            return GameEntry.SaveData.Get<HexaAwayGameData>().TryConsumeProp(propType);
        }

        private bool TryGetAnimationPlayer(HexaAwayPropType propType, out HexaAwayPropAnimationPlayer player)
        {
            if (m_PlayerMap.TryGetValue(propType, out player) && player != null)
            {
                return true;
            }

            if (!m_PrefabMap.TryGetValue(propType, out GameObject prefab) || prefab == null)
            {
                return false;
            }

            Transform parent = GameEntry.HexaAway.LevelRepresentation != null
                ? GameEntry.HexaAway.LevelRepresentation.LevelTransform
                : null;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
            player = instance.GetComponent<HexaAwayPropAnimationPlayer>();
            if (player == null)
            {
                player = instance.AddComponent<HexaAwayPropAnimationPlayer>();
            }

            m_PlayerMap[propType] = player;
            return true;
        }

        private void RefreshTargetCrosshairs()
        {
            GameEntry.HexaAway.LevelRepresentation?.RefreshPropTargetCrosshairs(m_SelectedPropType);
        }

        private void SetBusy(HexaAwayPropType propType, bool isBusy)
        {
            if (m_ModelMap.TryGetValue(propType, out PropButtonModel model))
            {
                model.SetBusy(isBusy);
            }
        }

        private void BuildPrefabMap(HexaAwayPropPrefabBinding[] propPrefabs)
        {
            m_PrefabMap.Clear();
            if (propPrefabs == null)
            {
                return;
            }

            for (int i = 0; i < propPrefabs.Length; i++)
            {
                HexaAwayPropPrefabBinding binding = propPrefabs[i];
                if (binding == null || binding.Prefab == null)
                {
                    continue;
                }

                m_PrefabMap[binding.PropType] = binding.Prefab;
            }
        }

        private void RefreshSelectedState()
        {
            foreach (KeyValuePair<HexaAwayPropType, PropButtonModel> pair in m_ModelMap)
            {
                pair.Value.SetSelected(m_SelectedPropType.HasValue && m_SelectedPropType.Value == pair.Key);
            }
        }

        private void EnsureInitialPropCounts()
        {
            HexaAwayGameData gameData = GameEntry.SaveData.Get<HexaAwayGameData>();
            if (gameData == null || gameData.PropCountsInitialized || m_ConfigDatabase == null)
            {
                return;
            }

            foreach (PropButtonConfigSO configSO in m_ConfigDatabase.Configs)
            {
                if (configSO == null || !TryGetPropType(configSO.ItemId, out HexaAwayPropType propType))
                {
                    continue;
                }

                gameData.SetPropCount(propType, configSO.DefaultCount);
            }

            gameData.PropCountsInitialized = true;
        }

        private void DisposeBindings()
        {
            foreach (PropButtonView view in m_BoundViews)
            {
                if (view == null)
                {
                    continue;
                }

                view.Clicked -= OnButtonClicked;
                view.Bind(null);
            }

            m_BoundViews.Clear();
            m_ModelMap.Clear();
            m_PrefabMap.Clear();
            DisposePlayers();
            m_PlayerMap.Clear();
            m_SelectedPropType = null;
            m_IsApplyingProp = false;
            GameEntry.HexaAway.LevelRepresentation?.ClearPropTargetCrosshairs();
        }

        private void DisposePlayers()
        {
            foreach (HexaAwayPropAnimationPlayer player in m_PlayerMap.Values)
            {
                if (player == null)
                {
                    continue;
                }

                player.Stop();
                UnityEngine.Object.Destroy(player.gameObject);
            }

            m_PlayerMap.Clear();
        }

        private static bool TryGetPropType(int itemId, out HexaAwayPropType propType)
        {
            if (Enum.IsDefined(typeof(HexaAwayPropType), itemId))
            {
                propType = (HexaAwayPropType)itemId;
                return true;
            }

            propType = default;
            return false;
        }
    }
}
