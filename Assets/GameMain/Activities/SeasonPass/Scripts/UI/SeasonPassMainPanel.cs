using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.SeasonPass.UI
{
    /// <summary>
    /// 通行证主页面的行为脚本。视觉层完全由模块内 Prefab 持有；设计师在 Inspector 绑定字段。
    /// </summary>
    public sealed class SeasonPassMainPanel : UGuiForm
    {
        [Header("Designer-owned bindings")]
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text m_Progress;
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private TMP_Text m_TierTMP;
        [SerializeField] private Button m_ActivateButton;
        [SerializeField] private Button m_RulesButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Transform m_TierContent;
        [SerializeField] private SeasonPassTierRowView m_TierRowTemplate;
        [Tooltip("留空时保留项目当前语言字体；有中文正文时由页面 Prefab 显式指定。")]
        [SerializeField] private TMP_FontAsset m_TextFont;

        private readonly List<SeasonPassTierRowView> m_Rows = new List<SeasonPassTierRowView>();
        private SeasonPassActivityModule m_Module;
        private Tween m_FocusTierTween;
        private bool m_ReportedMissingBindings;
        private bool m_ShouldFocusCurrentTier;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_RulesButton != null) m_RulesButton.AddSafeClick(OnClickRules);
            if (m_ActivateButton != null) m_ActivateButton.AddSafeClick(OnClickActivate);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_TierRowTemplate != null) m_TierRowTemplate.gameObject.SetActive(false);
            ApplyConfiguredFont();
            ReportMissingBindingsOnce();
        }

        protected override void OnOpen(object userData)
        {
            m_Module = ExtractModule(userData);
            m_ShouldFocusCurrentTier = true;
            base.OnOpen(userData);
            ApplyConfiguredFont();
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_FocusTierTween?.Kill();
            m_FocusTierTween = null;
            m_ShouldFocusCurrentTier = false;
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
            SeasonPassSnapshot snapshot = m_Module.GetSnapshot();
            SeasonPassTierProgress tierProgress = snapshot.CurrentTierProgress;

            SetText(m_Title, snapshot.DisplayName);
            SetText(m_TierTMP, "{0}", snapshot.CurrentTier);
            TimeSpan remaining = snapshot.EndUtc - DateTimeOffset.UtcNow;
            SetText(m_Countdown, "{0}d{1:00}h", remaining.Days, remaining.Hours);
            SetText(m_Progress, "{0} / {1}", tierProgress.CurrentCharge, tierProgress.RequiredCharge);
            SetActive(m_ActivateButton, !snapshot.IsPremiumActivated);
            SetProgress(tierProgress.FillAmount);

            EnsureRows(snapshot.Tiers.Count);
            for (int i = 0; i < m_Rows.Count; i++)
            {
                bool visible = i < snapshot.Tiers.Count;
                m_Rows[i].gameObject.SetActive(visible);
                if (visible)
                {
                    SeasonPassTierSnapshot tier = snapshot.Tiers[i];
                    m_Rows[i].Bind(tier, snapshot.Charge, snapshot.GetTierFillAmount(tier), OnClaimFree, OnClaimPremium,
                        OnClickPremiumLane, snapshot.IsPremiumActivated);
                }
            }

            if (!m_ShouldFocusCurrentTier) return;
            m_ShouldFocusCurrentTier = false;
            FocusCurrentTierAsync(snapshot.CurrentTier).Forget(Debug.LogException);
        }

        private async UniTask FocusCurrentTierAsync(int tier)
        {
            // Wait for the dynamic rows and their layout groups to calculate their final size.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (!isActiveAndEnabled || m_TierContent == null || tier <= 0 || tier > m_Rows.Count) return;

            ScrollRect scrollRect = m_TierContent.GetComponentInParent<ScrollRect>();
            RectTransform content = scrollRect != null ? scrollRect.content : null;
            RectTransform viewport = scrollRect != null
                ? scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>()
                : null;
            RectTransform target = m_Rows[tier - 1].transform as RectTransform;
            if (scrollRect == null || content == null || viewport == null || target == null) return;

            Canvas.ForceUpdateCanvases();
            float scrollableHeight = content.rect.height - viewport.rect.height;
            if (scrollableHeight <= 0f) return;

            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);
            float targetOffset = Mathf.Clamp(-targetBounds.center.y - viewport.rect.height * 0.5f, 0f, scrollableHeight);
            float targetPosition = 1f - targetOffset / scrollableHeight;

            scrollRect.StopMovement();
            m_FocusTierTween?.Kill();
            m_FocusTierTween = DOTween.To(
                    () => scrollRect.verticalNormalizedPosition,
                    value => scrollRect.verticalNormalizedPosition = value,
                    targetPosition,
                    0.35f)
                .SetEase(Ease.OutCubic)
                .SetTarget(this);
        }

        private void EnsureRows(int count)
        {
            if (m_TierContent == null || m_TierRowTemplate == null) return;
            while (m_Rows.Count < count)
            {
                SeasonPassTierRowView row = Instantiate(m_TierRowTemplate, m_TierContent);
                row.name = $"Tier_{m_Rows.Count + 1}";
                if (m_TextFont != null) row.ApplyFont(m_TextFont);
                row.gameObject.SetActive(true);
                m_Rows.Add(row);
            }
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
            rect.anchorMax = new Vector2(fill, rect.anchorMax.y);
        }

        private void OnClaimFree(int tier)
        {
            ClaimAsync(tier, SeasonPassTrack.Free).Forget(Debug.LogException);
        }

        private void OnClaimPremium(int tier)
        {
            ClaimAsync(tier, SeasonPassTrack.Premium).Forget(Debug.LogException);
        }

        private async UniTask ClaimAsync(int tier, SeasonPassTrack track)
        {
            if (m_Module == null) return;
            ActivityRewardReceipt receipt = track == SeasonPassTrack.Premium
                ? await m_Module.ClaimPremiumAsync(tier)
                : await m_Module.ClaimFreeAsync(tier);
            Refresh();
            if (receipt.Status != ActivityRewardStatus.Granted && receipt.Status != ActivityRewardStatus.AlreadyGranted)
            {
                Debug.LogWarning($"[SeasonPassMainPanel] Claim failed: {receipt.Reason}");
            }
        }

        private void OnClickRules()
        {
            if (m_Module != null) m_Module.OpenRulesAsync().Forget(Debug.LogException);
        }

        private void OnClickActivate()
        {
            if (m_Module != null) m_Module.OpenGoldPassPurchaseAsync().Forget(Debug.LogException);
        }

        private void OnClickPremiumLane()
        {
            if (m_Module != null) m_Module.OpenGoldPassPurchaseAsync().Forget(Debug.LogException);
        }

        private void ApplyConfiguredFont()
        {
            if (m_TextFont == null) return;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) text.font = m_TextFont;
        }

        private void ReportMissingBindingsOnce()
        {
            if (m_ReportedMissingBindings) return;
            var missing = new List<string>();
            if (m_Title == null) missing.Add(nameof(m_Title));
            if (m_Countdown == null) missing.Add(nameof(m_Countdown));
            if (m_Progress == null) missing.Add(nameof(m_Progress));
            if (m_ProgressFill == null) missing.Add(nameof(m_ProgressFill));
            if (m_ActivateButton == null) missing.Add(nameof(m_ActivateButton));
            if (m_RulesButton == null) missing.Add(nameof(m_RulesButton));
            if (m_CloseButton == null) missing.Add(nameof(m_CloseButton));
            if (m_TierContent == null) missing.Add(nameof(m_TierContent));
            if (m_TierRowTemplate == null) missing.Add(nameof(m_TierRowTemplate));
            if (missing.Count == 0) return;
            m_ReportedMissingBindings = true;
            Debug.LogWarning($"[SeasonPassMainPanel] Bind these fields on the designer-owned Prefab: {string.Join(", ", missing)}.");
        }

        private static SeasonPassActivityModule ExtractModule(object userData)
        {
            return (userData as ActivityPageUserData)?.Request.Arguments as SeasonPassActivityModule;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.SetText(value);
        }

        private static void SetText(TMP_Text text, string format,  int arg0)
        {
            if (text != null) text.SetText(format, arg0);
        }


        private static void SetText(TMP_Text text, string format, float arg0, float arg1, float arg2)
        {
            if (text != null) text.SetText(format, arg0, arg1, arg2);
        }

        private static void SetText(TMP_Text text, string format, float arg0, float arg1)
        {
            if (text != null) text.SetText(format, arg0, arg1);
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }
    }
}
