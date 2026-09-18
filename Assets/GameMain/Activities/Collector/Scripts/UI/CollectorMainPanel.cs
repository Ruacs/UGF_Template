using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lokas.Activities.Collector.UI
{
    /// <summary>
    /// Collector 主页面。页面只使用活动模块快照，任务行由 Inspector 绑定的模板动态复用。
    /// </summary>
    public sealed class CollectorMainPanel : UGuiForm
    {
        [Header("Header")]
        [SerializeField] private TMP_Text m_Title;
        [FormerlySerializedAs("m_HelpButton")]
        [SerializeField] private Button m_RulesButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Collector summary")]
        [SerializeField] private TMP_Text m_Progress;
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private Button m_CollectButton;
        [SerializeField] private Image m_CollectIcon;
        [SerializeField] private RewardItemView m_CurrentReward;
        [SerializeField] private RewardItemView m_NextReward;

        [Header("Task list")]
        [SerializeField] private ScrollRect m_TaskScroll;
        [SerializeField] private Transform m_TaskContent;
        [SerializeField] private CollectorTaskRowView m_TaskRowTemplate;

        [Header("Typography")]
        [Tooltip("可选；页面包含中文正文时绑定覆盖所需字符的 TMP Font Asset。")]
        [SerializeField] private TMP_FontAsset m_TextFont;

        private readonly List<CollectorTaskRowView> m_Rows = new List<CollectorTaskRowView>();
        private CollectorActivityModule m_Module;
        private bool m_ReportedMissingBindings;
        private bool m_IsClaiming;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_RulesButton != null) m_RulesButton.interactable = false;
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_CollectButton != null) m_CollectButton.AddSafeClick(OnClickCollect);
            if (m_TaskRowTemplate != null) m_TaskRowTemplate.gameObject.SetActive(false);
            ApplyConfiguredFont();
            ReportMissingBindingsOnce();
        }

        protected override void OnOpen(object userData)
        {
            m_Module = ExtractModule(userData);
            if (m_Module == null)
                GameEntry.Activities?.TryGetModule(CollectorActivityModule.Id, out m_Module);
            base.OnOpen(userData);
            ApplyConfiguredFont();
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_IsClaiming = false;
            base.OnClose(isShutdown, userData);
            m_Module = null;
        }

        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            if (m_Module != null) m_Module.StateChanged += Refresh;
        }

        protected override void UnsubscribeEvents()
        {
            if (m_Module != null) m_Module.StateChanged -= Refresh;
            base.UnsubscribeEvents();
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            CollectorSnapshot snapshot = m_Module.GetSnapshot();

            SetText(m_Title, snapshot.DisplayName);
            SetText(m_Progress, "{0}/{1}", snapshot.CollectedCount, snapshot.TargetCount);
            SetProgress(snapshot.Progress01);
            if (m_CollectIcon != null && m_Module.Config.CollectibleIcon != null)
            {
                m_CollectIcon.sprite = m_Module.Config.CollectibleIcon;
                m_CollectIcon.enabled = true;
            }

            BindReward(m_CurrentReward, snapshot.CurrentTask?.Reward);
            BindReward(m_NextReward, snapshot.NextTask?.Reward);
            if (m_CollectButton != null)
                m_CollectButton.interactable = !m_IsClaiming && HasClaimableTask(snapshot);

            EnsureRows(snapshot.Tasks.Count);
            for (int displayIndex = 0; displayIndex < m_Rows.Count; displayIndex++)
            {
                bool visible = displayIndex < snapshot.Tasks.Count;
                m_Rows[displayIndex].gameObject.SetActive(visible);
                if (!visible) continue;

                // 设计稿从高档位到低档位显示；RequiredCount 是各档独立目标，不是累计阈值。
                CollectorTaskSnapshot task = snapshot.Tasks[snapshot.Tasks.Count - 1 - displayIndex];
                CollectorTaskRowView.CollectorTaskState state = task.IsReached
                    ? CollectorTaskRowView.CollectorTaskState.Completed
                    : ReferenceEquals(task, snapshot.CurrentTask)
                        ? CollectorTaskRowView.CollectorTaskState.Active
                        : CollectorTaskRowView.CollectorTaskState.Locked;
                m_Rows[displayIndex].Bind(task.Tier, task.Reward, state, displayIndex, snapshot.Tasks.Count);
            }
        }

        private void EnsureRows(int count)
        {
            if (m_TaskContent == null || m_TaskRowTemplate == null) return;
            while (m_Rows.Count < count)
            {
                CollectorTaskRowView row = Instantiate(m_TaskRowTemplate, m_TaskContent);
                row.name = string.Concat("CollectorTask_", m_Rows.Count + 1);
                row.gameObject.SetActive(true);
                m_Rows.Add(row);
            }
        }

        private void OnClickCollect()
        {
            if (m_Module == null || m_IsClaiming) return;
            CollectorSnapshot snapshot = m_Module.GetSnapshot();
            int tier = 0;
            foreach (CollectorTaskSnapshot task in snapshot.Tasks)
            {
                if (task != null && task.CanClaim)
                {
                    tier = task.Tier;
                    break;
                }
            }
            if (tier > 0) ClaimAsync(tier).Forget(Debug.LogException);
        }

        private async UniTask ClaimAsync(int tier)
        {
            if (m_Module == null) return;
            m_IsClaiming = true;
            Refresh();
            try
            {
                ActivityRewardReceipt receipt = await m_Module.ClaimAsync(tier);
                if (receipt.Status != ActivityRewardStatus.Granted &&
                    receipt.Status != ActivityRewardStatus.AlreadyGranted)
                    Debug.LogWarning($"[CollectorMainPanel] Claim failed: {receipt.Reason}");
            }
            finally
            {
                m_IsClaiming = false;
                if (m_Module != null) Refresh();
            }
        }

        private void BindReward(RewardItemView view, RewardDataSO bundle)
        {
            if (view == null) return;
            if (bundle == null)
            {
                view.Bind(null);
                return;
            }

            try
            {
                IReadOnlyList<RewardItemViewData> items = RewardPresentation.Build(bundle);
                view.Bind(items.Count > 0 ? items[0] : null);
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError(error.Message, bundle);
                view.Bind(null);
            }
        }

        private static bool HasClaimableTask(CollectorSnapshot snapshot)
        {
            if (snapshot == null) return false;
            foreach (CollectorTaskSnapshot task in snapshot.Tasks)
                if (task != null && task.CanClaim) return true;
            return false;
        }

        private void SetProgress(float fill)
        {
            if (m_ProgressFill == null) return;
            if (m_ProgressFill.type == Image.Type.Filled)
            {
                m_ProgressFill.fillAmount = fill;
                return;
            }

            RectTransform rect = m_ProgressFill.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = Mathf.Clamp01(fill);
            rect.anchorMax = anchorMax;
        }

        private void ApplyConfiguredFont()
        {
            if (m_TextFont == null) return;
            if (m_Title != null) m_Title.font = m_TextFont;
            if (m_Progress != null) m_Progress.font = m_TextFont;
        }

        private void ReportMissingBindingsOnce()
        {
            if (m_ReportedMissingBindings) return;
            var missing = new List<string>();
            if (m_Title == null) missing.Add(nameof(m_Title));
            if (m_Progress == null) missing.Add(nameof(m_Progress));
            if (m_ProgressFill == null) missing.Add(nameof(m_ProgressFill));
            if (m_CloseButton == null) missing.Add(nameof(m_CloseButton));
            if (m_TaskContent == null) missing.Add(nameof(m_TaskContent));
            if (m_TaskRowTemplate == null) missing.Add(nameof(m_TaskRowTemplate));
            if (missing.Count == 0) return;
            m_ReportedMissingBindings = true;
            Debug.LogWarning($"[CollectorMainPanel] Bind these fields on the designer-owned Prefab: {string.Join(", ", missing)}.");
        }

        private static CollectorActivityModule ExtractModule(object userData) =>
            (userData as ActivityPageUserData)?.Request.Arguments as CollectorActivityModule;

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.SetText(value ?? string.Empty);
        }

        private static void SetText(TMP_Text text, string format, int arg0, int arg1)
        {
            if (text != null) text.SetText(format, arg0, arg1);
        }
    }
}
