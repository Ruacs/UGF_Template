using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal sealed class MiningBoardTestControls
    {
        private int m_PickaxeCount;

        internal int PickaxeCount => m_PickaxeCount;
        internal event Action<int> PickaxeCountChanged;

        internal void SynchronizePickaxeCount(int pickaxeCount)
        {
            m_PickaxeCount = Math.Max(0, pickaxeCount);
        }

        internal void SetPickaxeCount(int pickaxeCount)
        {
            SynchronizePickaxeCount(pickaxeCount);
            PickaxeCountChanged?.Invoke(m_PickaxeCount);
        }

        internal void ClearListeners()
        {
            PickaxeCountChanged = null;
        }
    }
#endif

    public sealed class MiningBoardOpenArgs
    {
        public int EventId { get; }
        public int StepId { get; }
        public int PickaxeCount { get; }
        public IReadOnlyCollection<int> OpenCellIds { get; }
        public IReadOnlyCollection<int> CollectedGemIds { get; }
        public IReadOnlyCollection<int> ClaimedRewardStepIds { get; }
        public Action<MiningBoardSnapshot> StateChanged { get; }
        public Func<int, UniTask<ActivityRewardReceipt>> ClaimRewardAsync { get; }
        public DateTimeOffset? CountdownEndUtc { get; }
        public Func<DateTimeOffset> UtcNow { get; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal MiningBoardTestControls TestControls { get; private set; }
#endif

        public MiningBoardOpenArgs(int eventId, int stepId, int pickaxeCount,
            IReadOnlyCollection<int> openCellIds = null, IReadOnlyCollection<int> collectedGemIds = null,
            Action<MiningBoardSnapshot> stateChanged = null, DateTimeOffset? countdownEndUtc = null,
            Func<DateTimeOffset> utcNow = null, IReadOnlyCollection<int> claimedRewardStepIds = null,
            Func<int, UniTask<ActivityRewardReceipt>> claimRewardAsync = null)
        {
            EventId = eventId;
            StepId = stepId;
            PickaxeCount = pickaxeCount;
            OpenCellIds = openCellIds;
            CollectedGemIds = collectedGemIds;
            ClaimedRewardStepIds = claimedRewardStepIds;
            StateChanged = stateChanged;
            ClaimRewardAsync = claimRewardAsync;
            CountdownEndUtc = countdownEndUtc?.ToUniversalTime();
            UtcNow = utcNow;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void AttachTestControls(MiningBoardTestControls testControls)
        {
            TestControls = testControls;
        }
#endif
    }

    /// <summary>Mining 主页面的棋盘表现；活动模块后续通过 MiningBoardOpenArgs 注入并持久化状态。</summary>
    public sealed class MiningMainUIPanel : UGuiForm
    {
        private sealed class GemRuntimeView
        {
            public MiningGemPlacement Placement;
            public MiningGemVisualDefinition Visual;
            public Image GemImage;
        }

        private sealed class RewardChestRuntimeView
        {
            public RewardChestView ChestView;
            public Image ClosedImage;
            public GameObject OpenChest;
            public GameObject StepLabel;
            public TMP_Text StepText;
            public Button ClaimButton;
        }

        [Header("Config")]
        [SerializeField] private MiningActivityConfig m_Config;

        [Header("Bindings (auto-resolved when empty)")]
        [SerializeField] private Transform m_GameArea;
        [SerializeField] private GameObject m_GridAreaPrefab;
        [SerializeField] private RectTransform m_GridPanel;
        [SerializeField] private GridLayoutGroup m_GridLayout;
        [SerializeField] private TMP_Text m_PickaxeCountText;
        [SerializeField] private MiningGateView m_GateView;
        [SerializeField] private MiningGateView m_NextGateView;
        [SerializeField] private RectTransform m_FlightLayer;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_DetailButton;
        [SerializeField] private GameObject m_CompletionPage;
        [SerializeField] private Slider m_RewardSlider;
        [SerializeField] private Transform m_ChestArea;
        [SerializeField] private TMP_Text m_CountdownText;

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float m_GemFlightDuration = 0.55f;
        [SerializeField, Min(0.1f)] private float m_GridTransitionDuration = 0.55f;
        [SerializeField, Min(0f)] private float m_RewardProgressDuration = 0.35f;
        [SerializeField, Min(0f)] private float m_GateEntryDelay = 0.3f;

        [Header("Standalone preview")]
        [SerializeField] private int m_PreviewEventId = 1;
        [SerializeField, Range(1, 5)] private int m_PreviewStepId = 1;
        [SerializeField, Min(0)] private int m_PreviewPickaxes = 99;
#if UNITY_EDITOR
        [SerializeField] private bool m_AutoStartPreview;
#endif

        private readonly List<MiningCellView> m_Cells = new List<MiningCellView>();
        private readonly Dictionary<int, GemRuntimeView> m_Gems = new Dictionary<int, GemRuntimeView>();
        private readonly Queue<int> m_PendingGemFlights = new Queue<int>();
        private readonly HashSet<int> m_ClaimedRewardStepIds = new HashSet<int>();
        private readonly List<RewardChestRuntimeView> m_RewardChestViews =
            new List<RewardChestRuntimeView>();
        private RectTransform m_GridArea;
        private RectTransform m_OutgoingGridArea;
        private RectTransform m_GemRoot;
        private RectTransform m_CellTemplate;
        private MiningBoardModel m_Model;
        private MiningBoardOpenArgs m_OpenArgs;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private MiningBoardTestControls m_TestControls;
#endif
        private Tween m_GridTransitionTween;
        private Tween m_GateEntryDelayTween;
        private Tween m_RewardProgressTween;
        private float m_RewardProgressTargetValue;
        private bool m_IsAnimatingGem;
        private bool m_IsStageTransitioning;
        private bool m_IsClaimingReward;
        private bool m_HasNotifiedStageCompleted;
        private float m_NextCountdownRefreshTime;

        public MiningActivityConfig Config => m_Config;
        public MiningBoardModel Model => m_Model;
        public RectTransform CurrentGridArea => m_GridArea;
        public MiningGateView CurrentGateView => m_GateView;
        public MiningGateView NextGateView => m_NextGateView;
        public GameObject CompletionPage => m_CompletionPage;
        public Slider RewardSlider => m_RewardSlider;
        public TMP_Text CountdownText => m_CountdownText;
        public event Action<MiningBoardSnapshot> StateChanged;
        public event Action<int> GemCollected;
        public event Action StageCompleted;

        protected override void OnInit(object userData)
        {
            ResolveBindings();
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_DetailButton != null) m_DetailButton.AddSafeClick(OpenDetails);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ResolveBindings();
            MiningBoardOpenArgs args = ExtractOpenArgs(userData) ??
                new MiningBoardOpenArgs(m_PreviewEventId, m_PreviewStepId, m_PreviewPickaxes);
            InitializeBoard(args);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            // Capture a grid that may already have switched while its visual transition is still running.
            PublishState();
            ResetRuntimeBoard();
            base.OnClose(isShutdown, userData);
        }

#if UNITY_EDITOR
        private void Start()
        {
            if (!m_AutoStartPreview || m_Model != null) return;
            ResolveBindings();
            InitializeBoard(new MiningBoardOpenArgs(m_PreviewEventId, m_PreviewStepId, m_PreviewPickaxes));
        }
#endif

        private void Update()
        {
            if (m_Model == null || m_CountdownText == null || Time.unscaledTime < m_NextCountdownRefreshTime)
                return;
            RefreshCountdown();
        }

        public void InitializeBoard(MiningBoardOpenArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            ResolveBindings();
            if (m_Config == null) throw new InvalidOperationException("MiningMainUIPanel has no MiningActivityConfig.");
            if (m_GridAreaPrefab == null)
                throw new InvalidOperationException("MiningMainUIPanel has no GridArea prefab.");
            if (m_CompletionPage == null)
                throw new InvalidOperationException("MiningMainUIPanel has no completion page.");
            m_Config.ValidateConfiguration();
            MiningStageDefinition stage = m_Config.GetStage(args.EventId, args.StepId);

            ResetRuntimeBoard();
            m_OpenArgs = args;
            if (args.ClaimedRewardStepIds != null)
            {
                foreach (int stepId in args.ClaimedRewardStepIds)
                    if (stepId >= 1 && stepId <= MiningRewardConfig.RewardCount)
                        m_ClaimedRewardStepIds.Add(stepId);
            }
            m_Model = new MiningBoardModel(m_Config, stage, args.PickaxeCount, args.OpenCellIds,
                args.CollectedGemIds);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BindTestControls(args.TestControls);
#endif
            RefreshRewardProgress(stage, false);
            RefreshCountdown();
            ConfigureGates(stage);
            ConfigureGrid(stage);
            BuildGemViews(stage);
            ApplyRestoredState(stage);
            if (stage.StepId >= 5 && m_Model.IsStageComplete)
            {
                m_GateView?.HideImmediately();
                m_NextGateView?.HideImmediately();
                ShowCompletionPage();
            }
            RefreshPickaxeCount();
            RefreshCellInteractivity();
            PublishState();
        }

        private void ResolveBindings()
        {
            if (m_GridArea == null)
                m_GridArea = transform.Find("SafeArea/GameArea/GridArea") as RectTransform;
            if (m_GridArea != null &&
                (m_GridPanel == null || m_GridLayout == null || m_PickaxeCountText == null ||
                 m_CellTemplate == null || !m_GridPanel.IsChildOf(m_GridArea)))
                BindGridArea(m_GridArea);
            if (m_GateView == null)
            {
                Transform gate = transform.Find("SafeArea/GateArea/Gate");
                if (gate != null) m_GateView = gate.GetComponent<MiningGateView>();
            }
            if (m_NextGateView == null)
            {
                Transform nextGate = transform.Find("SafeArea/GateArea/Gate_Next");
                if (nextGate != null) m_NextGateView = nextGate.GetComponent<MiningGateView>();
            }
            if (m_FlightLayer == null) m_FlightLayer = transform as RectTransform;
            if (m_CloseButton == null)
                m_CloseButton = transform.Find("SafeArea/Top/Btn_Close")?.GetComponent<Button>();
            if (m_DetailButton == null)
                m_DetailButton = transform.Find("SafeArea/Top/DetailBtn")?.GetComponent<Button>();
            if (m_CompletionPage == null)
                m_CompletionPage = transform.Find("SafeArea/GameArea/CompletionPage")?.gameObject;
            if (m_RewardSlider == null)
                m_RewardSlider = transform.Find("SafeArea/RewardArea/RewardSlider")?.GetComponent<Slider>();
            if (m_ChestArea == null)
                m_ChestArea = transform.Find("SafeArea/RewardArea/ChestArea");
            if (m_CountdownText == null)
                m_CountdownText = transform.Find("SafeArea/RewardArea/CountdownBg/Countdown")
                    ?.GetComponent<TMP_Text>();
            ResolveRewardChestViews();

            Image gridOverlay = m_GridArea?.Find("BG")?.GetComponent<Image>();
            if (gridOverlay != null) gridOverlay.raycastTarget = false;
        }

        private void ResolveRewardChestViews()
        {
            if (m_ChestArea == null) return;
            bool hasAllViews = m_RewardChestViews.Count == MiningRewardConfig.RewardCount;
            for (int index = 0; hasAllViews && index < m_RewardChestViews.Count; index++)
            {
                RewardChestRuntimeView view = m_RewardChestViews[index];
                hasAllViews = view?.ChestView != null && view.ClosedImage != null &&
                    view.OpenChest != null && view.StepLabel != null && view.StepText != null &&
                    view.ClaimButton != null;
            }
            if (hasAllViews) return;

            m_RewardChestViews.Clear();
            for (int stepId = 1; stepId <= MiningRewardConfig.RewardCount; stepId++)
            {
                Transform chest = m_ChestArea.Find($"Chest_{stepId}");
                var view = new RewardChestRuntimeView
                {
                    ChestView = chest != null ? chest.GetComponent<RewardChestView>() : null,
                    ClosedImage = chest?.Find("Chest_Close")?.GetComponent<Image>(),
                    OpenChest = chest?.Find("Chest_Open")?.gameObject,
                    StepLabel = chest?.Find("Label_Step")?.gameObject,
                    StepText = chest?.Find("Label_Step/Text (TMP)")?.GetComponent<TMP_Text>(),
                    ClaimButton = chest?.Find("Btn_Claim")?.GetComponent<Button>()
                };
                int claimStepId = stepId;
                if (view.ClaimButton != null)
                    view.ClaimButton.onClick.AddListener(() =>
                        ClaimRewardAsync(claimStepId).Forget(Debug.LogException));
                m_RewardChestViews.Add(view);
            }
        }

        private void RefreshRewardProgress(MiningStageDefinition currentStage, bool animateProgress = true)
        {
            if (m_RewardSlider == null || m_ChestArea == null ||
                m_RewardChestViews.Count != MiningRewardConfig.RewardCount)
                throw new InvalidOperationException("Mining reward progress bindings are incomplete.");

            m_RewardSlider.interactable = false;
            int completedStepCount = GetCompletedRewardStepCount(currentStage);
            int progressStepId = GetRewardProgressStepId(currentStage);
            float progress = Mathf.InverseLerp(1f, MiningRewardConfig.RewardCount, progressStepId);
            float progressValue = Mathf.Lerp(m_RewardSlider.minValue, m_RewardSlider.maxValue, progress);
            SetRewardProgress(progressValue, animateProgress);

            for (int stepId = 1; stepId <= MiningRewardConfig.RewardCount; stepId++)
            {
                RewardChestRuntimeView view = m_RewardChestViews[stepId - 1];
                if (view?.ChestView == null || view.ClosedImage == null || view.OpenChest == null ||
                    view.StepLabel == null || view.StepText == null || view.ClaimButton == null)
                    throw new InvalidOperationException($"Mining reward chest Step={stepId} is not fully bound.");

                MiningRewardDefinition reward = m_Config.GetStageReward(currentStage.EventId, stepId);
                Sprite chestSprite = reward.ChestStyle != null ? reward.ChestStyle.sprite : null;
                if (chestSprite == null)
                    throw new InvalidOperationException(
                        $"Mining RewardId={reward.id} has no configured chest sprite.");

                view.ChestView.Bind(chestSprite, reward.Entries);
                view.ClosedImage.preserveAspect = true;
                view.StepText.SetText("Step{0}", stepId);

                bool reached = stepId <= completedStepCount;
                bool claimed = m_ClaimedRewardStepIds.Contains(stepId);
                view.ClosedImage.gameObject.SetActive(!claimed);
                view.OpenChest.SetActive(claimed);
                view.StepLabel.SetActive(!reached);
                view.ClaimButton.gameObject.SetActive(reached && !claimed);
                view.ClaimButton.interactable = reached && !claimed && !m_IsClaimingReward &&
                    m_OpenArgs?.ClaimRewardAsync != null;
            }
        }

        private int GetCompletedRewardStepCount(MiningStageDefinition currentStage)
        {
            int completedStepCount = Mathf.Max(0, currentStage.StepId - 1);
            if (m_Model != null && m_Model.Stage.EventId == currentStage.EventId &&
                m_Model.Stage.StepId == currentStage.StepId && m_Model.IsStageComplete)
                completedStepCount++;
            return Mathf.Clamp(completedStepCount, 0, MiningRewardConfig.RewardCount);
        }

        private int GetRewardProgressStepId(MiningStageDefinition currentStage)
        {
            int progressStepId = currentStage.StepId;
            if (m_Model != null && m_Model.Stage.EventId == currentStage.EventId &&
                m_Model.Stage.StepId == currentStage.StepId && m_Model.IsStageComplete)
                progressStepId++;
            return Mathf.Clamp(progressStepId, 1, MiningRewardConfig.RewardCount);
        }

        private void SetRewardProgress(float targetValue, bool animate)
        {
            targetValue = Mathf.Clamp(targetValue, m_RewardSlider.minValue, m_RewardSlider.maxValue);
            if (!animate || m_RewardProgressDuration <= 0f || !gameObject.activeInHierarchy)
            {
                KillRewardProgressTween();
                m_RewardProgressTargetValue = targetValue;
                m_RewardSlider.SetValueWithoutNotify(targetValue);
                return;
            }

            if (m_RewardProgressTween != null && m_RewardProgressTween.IsActive() &&
                Mathf.Approximately(m_RewardProgressTargetValue, targetValue))
                return;

            KillRewardProgressTween();
            m_RewardProgressTargetValue = targetValue;
            if (Mathf.Approximately(m_RewardSlider.value, targetValue))
            {
                m_RewardSlider.SetValueWithoutNotify(targetValue);
                return;
            }

            Tween tween = null;
            tween = DOTween.To(() => m_RewardSlider.value,
                    value => m_RewardSlider.SetValueWithoutNotify(value), targetValue,
                    m_RewardProgressDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() =>
                {
                    if (m_RewardProgressTween == tween) m_RewardProgressTween = null;
                });
            m_RewardProgressTween = tween;
        }

        private async UniTask ClaimRewardAsync(int stepId)
        {
            if (m_IsClaimingReward || m_OpenArgs?.ClaimRewardAsync == null ||
                m_Model == null || m_ClaimedRewardStepIds.Contains(stepId) ||
                stepId > GetCompletedRewardStepCount(m_Model.Stage))
                return;

            m_IsClaimingReward = true;
            RefreshRewardProgress(m_Model.Stage);
            try
            {
                ActivityRewardReceipt receipt = await m_OpenArgs.ClaimRewardAsync(stepId);
                if (receipt.Status == ActivityRewardStatus.Granted ||
                    receipt.Status == ActivityRewardStatus.AlreadyGranted)
                {
                    m_ClaimedRewardStepIds.Add(stepId);
                    m_RewardChestViews[stepId - 1].ChestView.HidePreview();
                }
                else
                {
                    Debug.LogWarning($"[MiningMainUIPanel] Claim Step{stepId} failed: {receipt.Reason}");
                }
            }
            finally
            {
                m_IsClaimingReward = false;
                if (m_Model != null) RefreshRewardProgress(m_Model.Stage);
            }
        }

        private void RefreshCountdown()
        {
            m_NextCountdownRefreshTime = Time.unscaledTime + 1f;
            if (m_CountdownText == null) return;

            DateTimeOffset? endUtc = m_OpenArgs?.CountdownEndUtc ?? m_Config?.ScheduleConfig?.EndUtc;
            DateTimeOffset nowUtc = m_OpenArgs?.UtcNow?.Invoke() ?? DateTimeOffset.UtcNow;
            TimeSpan remaining = endUtc.HasValue
                ? endUtc.Value - nowUtc.ToUniversalTime()
                : TimeSpan.Zero;
            m_CountdownText.SetText(FormatCountdown(remaining));
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00:00";
            return remaining.TotalDays >= 1d
                ? $"{remaining.Days}d {remaining.Hours:00}h"
                : $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void BindGridArea(RectTransform gridArea)
        {
            m_GridArea = gridArea;
            m_GridPanel = gridArea != null ? gridArea.Find("GridPanel") as RectTransform : null;
            m_GridLayout = m_GridPanel != null ? m_GridPanel.GetComponent<GridLayoutGroup>() : null;
            m_PickaxeCountText = gridArea != null
                ? gridArea.Find("Count_base_frame/frame/CountText")?.GetComponent<TMP_Text>()
                : null;
            m_CellTemplate = m_GridPanel != null && m_GridPanel.childCount > 0
                ? m_GridPanel.GetChild(0) as RectTransform
                : null;
            m_GemRoot = gridArea != null ? gridArea.Find("GemRoot") as RectTransform : null;
            m_Cells.Clear();
            m_Gems.Clear();

            Image gridOverlay = gridArea?.Find("BG")?.GetComponent<Image>();
            if (gridOverlay != null) gridOverlay.raycastTarget = false;
        }

        private void ConfigureGates(MiningStageDefinition stage)
        {
            if (m_GateView == null || m_NextGateView == null || m_GateView == m_NextGateView)
                throw new InvalidOperationException("MiningMainUIPanel needs two distinct Gate views.");

            MiningGateThemeDefinition currentTheme = m_Config.GetGateTheme(stage.StepId);
            if (currentTheme == null)
                throw new InvalidOperationException($"Mining Gate theme Step={stage.StepId} is not configured.");

            int backIndex = Mathf.Min(m_GateView.transform.GetSiblingIndex(),
                m_NextGateView.transform.GetSiblingIndex());
            m_NextGateView.transform.SetSiblingIndex(backIndex);
            m_GateView.transform.SetSiblingIndex(backIndex + 1);

            ConfigureGate(m_GateView, stage, currentTheme);
            ConfigureFollowingGate(stage);
        }

        private void ConfigureFollowingGate(MiningStageDefinition currentStage)
        {
            int backIndex = Mathf.Min(m_GateView.transform.GetSiblingIndex(),
                m_NextGateView.transform.GetSiblingIndex());
            m_NextGateView.transform.SetSiblingIndex(backIndex);
            m_GateView.transform.SetSiblingIndex(backIndex + 1);

            if (currentStage.StepId >= 5)
            {
                m_NextGateView.HideImmediately();
                return;
            }

            MiningStageDefinition nextStage = m_Config.GetStage(currentStage.EventId, currentStage.StepId + 1);
            MiningGateThemeDefinition nextTheme = m_Config.GetGateTheme(nextStage.StepId);
            if (nextTheme == null)
                throw new InvalidOperationException($"Mining Gate theme Step={nextStage.StepId} is not configured.");
            ConfigureGate(m_NextGateView, nextStage, nextTheme);
        }

        private void ConfigureGate(MiningGateView gateView, MiningStageDefinition stage,
            MiningGateThemeDefinition theme)
        {
            gateView.ConfigureTheme(theme);
            PrepareGateSlots(gateView, stage);
        }

        private void PromoteNextGate(MiningStageDefinition currentStage)
        {
            MiningGateView previousGate = m_GateView;
            m_GateView = m_NextGateView;
            m_NextGateView = previousGate;
            ConfigureFollowingGate(currentStage);
        }

        private void PrepareGateSlots(MiningGateView gateView, MiningStageDefinition stage)
        {
            gateView.ClearRuntimeGems();
            for (int index = 0; index < stage.Gems.Count; index++)
            {
                MiningGemPlacement placement = stage.Gems[index];
                MiningGemVisualDefinition visual = m_Config.GetGemVisual(placement.GemType);
                if (visual == null)
                    throw new InvalidOperationException(
                        $"Mining gem type {placement.GemType} is not configured.");
                gateView.ShowEmptySlot(placement, visual);
            }
        }

        private void ConfigureGrid(MiningStageDefinition stage)
        {
            if (m_GridPanel == null || m_GridLayout == null || m_CellTemplate == null)
                throw new InvalidOperationException("MiningMainUIPanel grid bindings are incomplete.");

            int cellTotal = stage.CellCount * stage.CellCount;
            float boardSize = Mathf.Min(m_GridPanel.rect.width, m_GridPanel.rect.height);
            float cellSize = boardSize / stage.CellCount;
            m_GridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            m_GridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            m_GridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            m_GridLayout.constraintCount = stage.CellCount;
            m_GridLayout.spacing = Vector2.zero;
            m_GridLayout.padding = new RectOffset();
            m_GridLayout.cellSize = new Vector2(cellSize, cellSize);

            EnsureCells(cellTotal);
            for (int index = 0; index < m_Cells.Count; index++)
            {
                bool visible = index < cellTotal;
                m_Cells[index].gameObject.SetActive(visible);
                if (!visible) continue;
                m_Cells[index].name = index == 0 ? "Cell" : $"Cell_{index}";
                m_Cells[index].Bind(index, m_Config.ClosedCellSprite, m_Config.CrackCellSprite,
                    HandleCellClicked);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_GridPanel);
        }

        private void EnsureCells(int count)
        {
            m_Cells.Clear();
            for (int index = 0; index < m_GridPanel.childCount; index++)
            {
                Transform child = m_GridPanel.GetChild(index);
                MiningCellView cell = child.GetComponent<MiningCellView>();
                if (cell == null) cell = child.gameObject.AddComponent<MiningCellView>();
                m_Cells.Add(cell);
            }

            while (m_Cells.Count < count)
            {
                RectTransform clone = Instantiate(m_CellTemplate, m_GridPanel);
                clone.gameObject.SetActive(true);
                MiningCellView cell = clone.GetComponent<MiningCellView>();
                if (cell == null) cell = clone.gameObject.AddComponent<MiningCellView>();
                m_Cells.Add(cell);
            }
        }

        private void BuildGemViews(MiningStageDefinition stage)
        {
            EnsureGemRoot();
            ClearChildren(m_GemRoot);
            m_Gems.Clear();
            float cellSize = m_GridLayout.cellSize.x;

            for (int index = 0; index < stage.Gems.Count; index++)
            {
                MiningGemPlacement placement = stage.Gems[index];
                MiningGemVisualDefinition visual = m_Config.GetGemVisual(placement.GemType);
                Vector2Int baseSize = visual.GridSize;
                Vector2Int rotatedSize = visual.GetRotatedGridSize(placement.GridQuarterTurns);

                var containerObject = new GameObject($"PuzzleGem_{placement.GemId}", typeof(RectTransform));
                containerObject.layer = m_GemRoot.gameObject.layer;
                var container = (RectTransform)containerObject.transform;
                container.SetParent(m_GemRoot, false);
                container.anchorMin = container.anchorMax = new Vector2(0f, 1f);
                container.pivot = new Vector2(0f, 1f);
                container.anchoredPosition = new Vector2(placement.GridOrigin.x * cellSize,
                    -placement.GridOrigin.y * cellSize);
                container.sizeDelta = new Vector2(rotatedSize.x * cellSize, rotatedSize.y * cellSize);

                Image hole = CreatePuzzleImage("Hole", container, visual.HoleSprite, baseSize, cellSize,
                    placement.GridQuarterTurns);
                Image gem = CreatePuzzleImage("Gem", container, visual.GemSprite, baseSize, cellSize,
                    placement.GridQuarterTurns);
                hole.raycastTarget = false;
                gem.raycastTarget = false;
                m_Gems.Add(placement.GemId, new GemRuntimeView
                {
                    Placement = placement,
                    Visual = visual,
                    GemImage = gem
                });
            }
        }

        private void ApplyRestoredState(MiningStageDefinition stage)
        {
            for (int index = 0; index < m_Cells.Count; index++)
            {
                if (!m_Cells[index].gameObject.activeSelf) continue;
                if (m_Model.IsCellOpen(index)) m_Cells[index].SetOpenedImmediate();
                else m_Cells[index].SetClosed(m_Model.PickaxeCount > 0);
            }

            for (int index = 0; index < stage.Gems.Count; index++)
            {
                MiningGemPlacement placement = stage.Gems[index];
                if (!m_Gems.TryGetValue(placement.GemId, out GemRuntimeView view)) continue;
                bool collected = m_Model.IsGemCollected(placement.GemId);
                view.GemImage.enabled = !collected;
                if (collected) m_GateView?.ShowCollectedImmediately(placement, view.Visual);
            }
        }

        private void HandleCellClicked(int cellId)
        {
            if (m_Model == null || m_IsAnimatingGem || m_IsStageTransitioning) return;
            MiningDigResult result = m_Model.Dig(cellId);
            if (!result.OpenedCell)
            {
                RefreshCellInteractivity();
                return;
            }

            RefreshPickaxeCount();
            PublishState();
            for (int index = 0; index < result.CollectedGemIds.Count; index++)
                m_PendingGemFlights.Enqueue(result.CollectedGemIds[index]);

            bool hasGemFlight = m_PendingGemFlights.Count > 0;
            m_IsAnimatingGem = hasGemFlight;
            RefreshCellInteractivity();
            m_Cells[cellId].PlayDig(hasGemFlight ? PlayNextGemFlight : null);

            if (!hasGemFlight && result.IsStageComplete) NotifyStageCompleted();
        }

        private void PlayNextGemFlight()
        {
            if (m_PendingGemFlights.Count == 0)
            {
                m_IsAnimatingGem = false;
                RefreshCellInteractivity();
                if (m_Model != null && m_Model.IsStageComplete) NotifyStageCompleted();
                return;
            }

            int gemId = m_PendingGemFlights.Dequeue();
            if (!m_Gems.TryGetValue(gemId, out GemRuntimeView view) || m_GateView == null)
            {
                GemCollected?.Invoke(gemId);
                PlayNextGemFlight();
                return;
            }

            m_GateView.FlyToSlot(view.GemImage, m_FlightLayer, view.Placement, view.Visual,
                m_GemFlightDuration, () =>
                {
                    GemCollected?.Invoke(gemId);
                    PlayNextGemFlight();
                });
        }

        private void RefreshPickaxeCount()
        {
            if (m_PickaxeCountText != null) m_PickaxeCountText.SetText("{0}", m_Model?.PickaxeCount ?? 0);
        }

        private void OpenDetails()
        {
            if (GameEntry.Activities == null ||
                !GameEntry.Activities.TryGetModule(MiningActivityModule.Id, out MiningActivityModule module)) return;
            module.OpenDetailsAsync().Forget(Debug.LogException);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void BindTestControls(MiningBoardTestControls testControls)
        {
            m_TestControls = testControls;
            if (m_TestControls == null) return;
            m_TestControls.PickaxeCountChanged += HandleTestPickaxeCountChanged;
            HandleTestPickaxeCountChanged(m_TestControls.PickaxeCount);
        }

        private void HandleTestPickaxeCountChanged(int pickaxeCount)
        {
            if (m_Model == null) return;
            m_Model.SetPickaxeCount(pickaxeCount);
            RefreshPickaxeCount();
            RefreshCellInteractivity();
        }
#endif

        private void RefreshCellInteractivity()
        {
            bool canDig = m_Model != null && !m_IsAnimatingGem && !m_IsStageTransitioning &&
                !m_Model.IsStageComplete &&
                m_Model.PickaxeCount > 0;
            for (int index = 0; index < m_Cells.Count; index++)
            {
                MiningCellView cell = m_Cells[index];
                if (cell.gameObject.activeSelf) cell.SetInteractable(canDig && !m_Model.IsCellOpen(index));
            }
        }

        private void PublishState()
        {
            if (m_Model == null) return;
            MiningBoardSnapshot snapshot = m_Model.CreateSnapshot();
            m_OpenArgs?.StateChanged?.Invoke(snapshot);
            StateChanged?.Invoke(snapshot);
        }

        private void NotifyStageCompleted()
        {
            if (m_HasNotifiedStageCompleted) return;
            m_HasNotifiedStageCompleted = true;
            PublishState();
            PlayStageTransition();
        }

        private void PlayStageTransition()
        {
            m_IsStageTransitioning = true;
            RefreshCellInteractivity();
            MiningStageDefinition completedStage = m_Model.Stage;
            bool hasNextStage = completedStage.StepId < 5;
            MiningStageDefinition nextStage = hasNextStage
                ? m_Config.GetStage(completedStage.EventId, completedStage.StepId + 1)
                : null;
            int remainingPickaxes = m_Model.PickaxeCount;
            int pendingAnimations = 2;

            void CompleteAnimation()
            {
                pendingAnimations--;
                if (pendingAnimations > 0) return;
                if (hasNextStage) CompleteStageAdvance();
                else CompleteAllStages();
            }

            PlayGateSequence(nextStage, CompleteAnimation);
            if (hasNextStage) PlayGridSwitch(nextStage, remainingPickaxes, CompleteAnimation);
            else PlayFinalGridExit(CompleteAnimation);
        }

        private void PlayGateSequence(MiningStageDefinition nextStage, Action completed)
        {
            KillGateEntryDelay();
            MiningGateView exitingGate = m_GateView;
            MiningGateView enteringGate = m_NextGateView;

            if (nextStage == null)
            {
                if (exitingGate != null && exitingGate.gameObject.activeSelf)
                    exitingGate.PlayExit(completed);
                else
                    completed?.Invoke();
                return;
            }

            bool exitCompleted = false;
            bool entryCompleted = false;
            bool sequenceCompleted = false;

            void TryCompleteSequence()
            {
                if (sequenceCompleted || !exitCompleted || !entryCompleted) return;
                sequenceCompleted = true;
                completed?.Invoke();
            }

            void CompleteExit()
            {
                // PlayExit has hidden the retired Gate at this point. Recycle it behind the
                // entering Gate with the following theme while the entry animation may still run.
                PromoteNextGate(nextStage);
                exitCompleted = true;
                TryCompleteSequence();
            }

            void CompleteEntry()
            {
                entryCompleted = true;
                TryCompleteSequence();
            }

            void StartIncomingGate()
            {
                m_GateEntryDelayTween = null;
                if (enteringGate != null && enteringGate.gameObject.activeSelf)
                    enteringGate.PlayEntry(CompleteEntry);
                else
                    CompleteEntry();
            }

            // Start the front Gate first, then overlap the rear Gate's entry after the configured
            // delay. The stage only advances after both independent animations have completed.
            if (exitingGate != null && exitingGate.gameObject.activeSelf)
                exitingGate.PlayExit(CompleteExit);
            else
                CompleteExit();

            if (m_GateEntryDelay <= 0f)
            {
                StartIncomingGate();
                return;
            }

            Tween delayTween = null;
            delayTween = DOVirtual.DelayedCall(m_GateEntryDelay, StartIncomingGate, true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() =>
                {
                    if (m_GateEntryDelayTween == delayTween) m_GateEntryDelayTween = null;
                });
            m_GateEntryDelayTween = delayTween;
        }

        private void PlayGridSwitch(MiningStageDefinition nextStage, int pickaxeCount, Action completed)
        {
            if (m_GridArea == null || m_GridAreaPrefab == null)
                throw new InvalidOperationException("Mining GridArea transition bindings are incomplete.");
            RectTransform outgoingGrid = m_GridArea;
            RectTransform parent = outgoingGrid.parent as RectTransform;
            if (parent == null) throw new InvalidOperationException("Mining GridArea has no RectTransform parent.");

            GameObject incomingObject = Instantiate(m_GridAreaPrefab, m_GameArea, false);
            incomingObject.name = "GridArea_Next";
            RectTransform incomingGrid = incomingObject.GetComponent<RectTransform>();
            incomingGrid.SetSiblingIndex(outgoingGrid.GetSiblingIndex() + 1);

            Vector2 targetPosition = outgoingGrid.anchoredPosition;
            float slideDistance = GetGridSlideDistance(outgoingGrid, parent);
            incomingGrid.anchoredPosition = targetPosition + Vector2.up * slideDistance;
            m_OutgoingGridArea = outgoingGrid;
            BindGridArea(incomingGrid);
            m_Model = new MiningBoardModel(m_Config, nextStage, pickaxeCount);
            RefreshRewardProgress(nextStage);
            ConfigureGrid(nextStage);
            BuildGemViews(nextStage);
            ApplyRestoredState(nextStage);
            RefreshPickaxeCount();
            RefreshCellInteractivity();

            Sequence sequence = DOTween.Sequence();
            m_GridTransitionTween = sequence;
            sequence.Append(outgoingGrid.DOAnchorPos(targetPosition - Vector2.up * slideDistance,
                    m_GridTransitionDuration).SetEase(Ease.InOutCubic))
                .Join(incomingGrid.DOAnchorPos(targetPosition, m_GridTransitionDuration)
                    .SetEase(Ease.InOutCubic))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (m_GridTransitionTween == sequence) m_GridTransitionTween = null;
                    if (m_OutgoingGridArea != null) DestroyObject(m_OutgoingGridArea.gameObject);
                    m_OutgoingGridArea = null;
                    incomingObject.name = "GridArea";
                    completed?.Invoke();
                })
                .OnKill(() =>
                {
                    if (m_GridTransitionTween == sequence) m_GridTransitionTween = null;
                });
        }

        private void PlayFinalGridExit(Action completed)
        {
            if (m_GridArea == null)
            {
                completed?.Invoke();
                return;
            }

            RectTransform parent = m_GridArea.parent as RectTransform;
            float slideDistance = GetGridSlideDistance(m_GridArea, parent);
            float targetY = m_GridArea.anchoredPosition.y - slideDistance;
            Tween tween = null;
            tween = m_GridArea.DOAnchorPosY(targetY, m_GridTransitionDuration)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (m_GridTransitionTween == tween) m_GridTransitionTween = null;
                    completed?.Invoke();
                })
                .OnKill(() =>
                {
                    if (m_GridTransitionTween == tween) m_GridTransitionTween = null;
                });
            m_GridTransitionTween = tween;
        }

        private void CompleteStageAdvance()
        {
            m_HasNotifiedStageCompleted = false;
            m_IsStageTransitioning = false;
            RefreshPickaxeCount();
            RefreshCellInteractivity();
            PublishState();
            StageCompleted?.Invoke();
        }

        private void CompleteAllStages()
        {
            m_IsStageTransitioning = false;
            RefreshRewardProgress(m_Model.Stage);
            ShowCompletionPage();
            RefreshCellInteractivity();
            StageCompleted?.Invoke();
        }

        private void ShowCompletionPage()
        {
            if (m_CompletionPage == null) return;
            m_CompletionPage.transform.SetAsLastSibling();
            m_CompletionPage.SetActive(true);
        }

        private static float GetGridSlideDistance(RectTransform gridArea, RectTransform parent)
        {
            float gridHeight = gridArea != null ? gridArea.rect.height : 0f;
            float parentHeight = parent != null ? parent.rect.height : 0f;
            return Mathf.Max(1f, Mathf.Max(gridHeight, parentHeight));
        }

        private void EnsureGemRoot()
        {
            if (m_GemRoot != null) return;
            if (m_GridArea == null || m_GridPanel == null)
                throw new InvalidOperationException("MiningMainUIPanel has no GridArea/GridPanel.");
            var rootObject = new GameObject("GemRoot", typeof(RectTransform));
            rootObject.layer = m_GridPanel.gameObject.layer;
            m_GemRoot = (RectTransform)rootObject.transform;
            m_GemRoot.SetParent(m_GridArea, false);
            m_GemRoot.anchorMin = Vector2.zero;
            m_GemRoot.anchorMax = Vector2.one;
            m_GemRoot.offsetMin = Vector2.zero;
            m_GemRoot.offsetMax = Vector2.zero;
            m_GemRoot.pivot = new Vector2(0.5f, 0.5f);
            m_GemRoot.SetSiblingIndex(m_GridPanel.GetSiblingIndex());
        }

        private void ResetRuntimeBoard()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (m_TestControls != null)
                m_TestControls.PickaxeCountChanged -= HandleTestPickaxeCountChanged;
            m_TestControls = null;
#endif
            KillGateEntryDelay();
            KillGridTransition();
            KillRewardProgressTween();
            if (m_OutgoingGridArea != null)
            {
                DestroyObject(m_OutgoingGridArea.gameObject);
                m_OutgoingGridArea = null;
            }
            if (m_GridArea != null)
            {
                m_GridArea.gameObject.SetActive(true);
                m_GridArea.name = "GridArea";
                m_GridArea.anchoredPosition = Vector2.zero;
            }
            if (m_CompletionPage != null) m_CompletionPage.SetActive(false);
            m_IsAnimatingGem = false;
            m_IsStageTransitioning = false;
            m_IsClaimingReward = false;
            m_HasNotifiedStageCompleted = false;
            m_ClaimedRewardStepIds.Clear();
            m_PendingGemFlights.Clear();
            m_GateView?.CancelAnimations();
            m_NextGateView?.CancelAnimations();
            m_GateView?.ClearRuntimeGems();
            m_NextGateView?.ClearRuntimeGems();
            if (m_GemRoot != null) ClearChildren(m_GemRoot);
            m_Cells.Clear();
            m_Gems.Clear();
            m_Model = null;
            m_OpenArgs = null;
            m_NextCountdownRefreshTime = 0f;
        }

        private void KillGridTransition()
        {
            if (m_GridTransitionTween != null && m_GridTransitionTween.IsActive())
                m_GridTransitionTween.Kill();
            m_GridTransitionTween = null;
        }

        private void KillGateEntryDelay()
        {
            if (m_GateEntryDelayTween != null && m_GateEntryDelayTween.IsActive())
                m_GateEntryDelayTween.Kill();
            m_GateEntryDelayTween = null;
        }

        private void KillRewardProgressTween()
        {
            if (m_RewardProgressTween != null && m_RewardProgressTween.IsActive())
                m_RewardProgressTween.Kill();
            m_RewardProgressTween = null;
        }

        private static Image CreatePuzzleImage(string objectName, Transform parent, Sprite sprite,
            Vector2Int baseGridSize, float cellSize, int quarterTurns)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            var rect = (RectTransform)imageObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(baseGridSize.x * cellSize, baseGridSize.y * cellSize);
            rect.localEulerAngles = new Vector3(0f, 0f,
                -90f * MiningGemVisualDefinition.NormalizeQuarterTurns(quarterTurns));
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                GameObject child = root.GetChild(index).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        private static MiningBoardOpenArgs ExtractOpenArgs(object userData)
        {
            if (userData is MiningBoardOpenArgs directArgs) return directArgs;
            return (userData as ActivityPageUserData)?.Request.Arguments as MiningBoardOpenArgs;
        }
    }
}
