using System;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class HexaAwayUIPanel : UGuiForm
    {
        [SerializeField] private Button m_BtnClose;

        [SerializeField] private Button m_BtnSetting;
        [SerializeField] private TMP_Text m_LevelTMP;
        [SerializeField] private TMP_Text m_MovesCountTMP;
        [SerializeField] private ProcedureGame m_ProcedureGame;

        [SerializeField] private PropButtonConfigDatabaseSO m_PropButtonConfigDatabase;
        [SerializeField] private PropButtonView[] m_propButtonViews;
        [SerializeField] private HexaAwayPropPrefabBinding[] m_PropPrefabs;
        [SerializeField] private int m_AddMoveAmount = 5;
        [SerializeField] private bool m_UseInfiniteProps = true;

        private readonly HexaAwayPropButtonController m_PropButtonController = new HexaAwayPropButtonController();
        private SelectionUIPanel m_SelectionUIPanel;
        private bool m_IsPropSelectionMode;
        private bool m_CloseSelectionWhenOpened;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_BtnClose != null)
            {
                m_BtnClose.onClick.AddListener(OnClickClose);
            }

            if (m_BtnSetting != null)
            {
                m_BtnSetting.AddSafeClick(OnSettingClicked);
            }
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_ProcedureGame = (ProcedureGame)userData;
            InitializePropButtons();
        }

        protected override void OnReveal()
        {
            base.OnReveal();
            if (!m_IsPropSelectionMode)
            {
                GameEntry.HexaAway.ResumeGame();
            }

            RefreshLevelText();
            m_PropButtonController.RefreshAll();

        }

        protected override void OnCover()
        {
            base.OnCover();
            if (!m_IsPropSelectionMode)
            {
                GameEntry.HexaAway.PauseGame();
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            CloseSelectionPanel(false);
            m_PropButtonController.Dispose();
            base.OnClose(isShutdown, userData);
        }


        private void RefreshLevelText()
        {
            if (m_LevelTMP == null)
            {
                return;
            }

            m_LevelTMP.text = GameEntry.Localization.GetString(LocalizationKeys.LEVEL, GameEntry.SaveData.Get<HexaAwayGameData>().CurrentLevel + 1);
        }

        private void RefreshMovesCountTMP(int count)
        {
            if (m_MovesCountTMP == null)
            {
                return;
            }

            m_MovesCountTMP.text = count.ToString();
        }


        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        private void OnSettingClicked()
        {
            PlayUISound(SoundId.UI_Click);
            GameEntry.UI.OpenUIForm(UIFormId.SettingUIPanel, new SettingUIData(GameMode.HexaAway, m_ProcedureGame));

        }

        private void InitializePropButtons()
        {
            // CurrentLevel 是 0 基索引，PropButtonConfig.RequiredLevel 按玩家看到的关卡号配置。
            int displayLevel = GameEntry.SaveData.Get<HexaAwayGameData>().CurrentLevel + 1;
            m_PropButtonController.Initialize(m_propButtonViews, m_PropButtonConfigDatabase, m_PropPrefabs, displayLevel, m_AddMoveAmount, m_UseInfiniteProps);
            m_PropButtonController.PurchaseRequested -= OnPropPurchaseRequested;
            m_PropButtonController.SelectionRequested -= OnPropSelectionRequested;
            m_PropButtonController.SelectionCompleted -= OnPropSelectionCompleted;
            m_PropButtonController.PurchaseRequested += OnPropPurchaseRequested;
            m_PropButtonController.SelectionRequested += OnPropSelectionRequested;
            m_PropButtonController.SelectionCompleted += OnPropSelectionCompleted;
        }


        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            GameEntry.Event.Subscribe(GameStateChangedEventArgs.EventId, OnGameStateChanged);
            GameEntry.HexaAway.MovesCountChanged += RefreshMovesCountTMP;
            GameEntry.HexaAway.ObjectClickIntercepted += m_PropButtonController.TryHandleObjectClick;
            GameEntry.SaveData.Get<HexaAwayGameData>().OnCurrentLevelChanged += OnHexaAwayLevelChanged;
            GameEntry.SaveData.Get<HexaAwayGameData>().OnPropCountChanged += OnHexaAwayPropCountChanged;
        }

        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            GameEntry.Event.Unsubscribe(GameStateChangedEventArgs.EventId, OnGameStateChanged);
            GameEntry.HexaAway.MovesCountChanged -= RefreshMovesCountTMP;
            GameEntry.HexaAway.ObjectClickIntercepted -= m_PropButtonController.TryHandleObjectClick;
            GameEntry.SaveData.Get<HexaAwayGameData>().OnCurrentLevelChanged -= OnHexaAwayLevelChanged;
            GameEntry.SaveData.Get<HexaAwayGameData>().OnPropCountChanged -= OnHexaAwayPropCountChanged;
        }

        private void OnHexaAwayLevelChanged(int level)
        {
            RefreshLevelText();
            m_PropButtonController.SetCurrentLevel(level + 1);
        }

        private void OnHexaAwayPropCountChanged(HexaAwayPropType propType, int count)
        {
            m_PropButtonController.RefreshProp(propType);
        }

        private void OnPropPurchaseRequested(HexaAwayPropType propType, PropButtonModel model)
        {
            // 购买/广告页面后续单独接入；按钮只负责把“需要补充哪个道具”抛出来。
            Log.Info($"[HexaAway] Prop purchase requested: {propType}");
        }

        private void OnPropSelectionRequested(HexaAwayPropType propType, PropButtonModel model)
        {
            m_IsPropSelectionMode = true;
            SetHexaAwayVisible(false);

            SelectionUIData selectionData = new SelectionUIData(
                OnSelectionUIPanelOpened,
                OnSelectionUIPanelClosed);

            int? serialId = GameEntry.UI.OpenUIForm(UIFormId.SelectionUIPanel, selectionData);
            if (!serialId.HasValue)
            {
                OnSelectionUIPanelClosed(null, false);
            }
        }

        private void OnPropSelectionCompleted(HexaAwayPropType propType)
        {
            if (m_SelectionUIPanel == null)
            {
                m_CloseSelectionWhenOpened = true;
                return;
            }

            CloseSelectionPanel(true);
        }

        private void OnSelectionUIPanelOpened(SelectionUIPanel panel)
        {
            m_SelectionUIPanel = panel;
            if (m_CloseSelectionWhenOpened)
            {
                CloseSelectionPanel(true);
            }
        }

        private void OnSelectionUIPanelClosed(SelectionUIPanel panel, bool isSelectionConfirmed)
        {
            if (m_SelectionUIPanel == panel)
            {
                m_SelectionUIPanel = null;
            }

            m_CloseSelectionWhenOpened = false;
            SetHexaAwayVisible(true);
            GameEntry.HexaAway?.SetPointerOverUIBlocking(true);
            m_IsPropSelectionMode = false;

            if (!isSelectionConfirmed)
            {
                m_PropButtonController.ClearSelection();
            }
        }

        private void CloseSelectionPanel(bool isSelectionConfirmed)
        {
            if (m_SelectionUIPanel == null)
            {
                return;
            }

            SelectionUIPanel selectionUIPanel = m_SelectionUIPanel;
            m_SelectionUIPanel = null;

            if (isSelectionConfirmed)
            {
                selectionUIPanel.CloseAsSelectionConfirmed();
            }
            else
            {
                selectionUIPanel.Close();
            }
        }

        private void SetHexaAwayVisible(bool visible)
        { 
            if (visible)
            {
                OnOpenUIAnimation();
            }
            else
            {
                OnCloseUIAnimation();
            }
        }



        private async void OnGameStateChanged(object sender, GameEventArgs e)
        {
            GameStateChangedEventArgs args = (GameStateChangedEventArgs)e;


            Action openAction = null;

            if (args.Current != GameState.GameOver)
            {
                return;
            }

            SetBlocksRaycasts(false);

            Log.Info("[Hexa Away] Game Over");
            await UniTask.Delay(1000);

            SetBlocksRaycasts(true);
            if (m_ProcedureGame == null)
            {
                return;
            }

            if (GameEntry.HexaAway.CurrentResult == GameResult.Win)
            {
                GameEntry.UI.OpenUIForm(UIFormId.GameOverUIPanel, m_ProcedureGame);
            }
            else if (GameEntry.HexaAway.CurrentResult == GameResult.Fail)
            {
                GameEntry.UI.OpenUIForm(UIFormId.ReviveUIPanel, m_ProcedureGame);
            }
        }

    }
}
