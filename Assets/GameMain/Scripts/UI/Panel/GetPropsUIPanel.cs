using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class GetPropsUIData
    {
        public string Title;        //标题
        public string Content;      //描述
        public Action OnSuccessed;  //激励成功回调
        public Action<string> OnFailed;     //激励失败回调
        public Action OnClosed;     //关闭页面回调


        public RewardData RewardData;    //奖励数据

        public Action OnPurchasedRewardClick;

        public GetPropsUIData()
        {
        }


    }

    public class GetPropsUIPanel : UGuiForm
    {
        [SerializeField] private Button m_CloseBtn;
        [SerializeField] private RectTransform m_GetBtnRT;
        [SerializeField] private Button m_GetBtn;
        [SerializeField] private TMP_Text m_GetBtnTMP;
        [SerializeField] private RectTransform m_GetBtnAdIconRT;
        [SerializeField] private TMP_Text m_TitleTMP;
        [SerializeField] private TMP_Text m_ContentTMP;
        [SerializeField] private Image m_PropImg;
        [SerializeField] private TMP_Text m_CountTMP;

        private GetPropsUIData m_UIData;
        private ShopBottomPagedView m_ShopBottomPagedView;
        private bool m_IsProcessing;
        private bool m_CallbackHandled;
        private bool m_HasPurchasedReward;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            m_GetBtn?.AddSafeClick(OnGetClick);
            m_CloseBtn?.AddSafeClick(OnCloseClick);
            BindShopBottomPagedView();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            m_UIData = userData as GetPropsUIData ?? new GetPropsUIData();
            m_IsProcessing = false;
            m_CallbackHandled = false;
            m_HasPurchasedReward = false;
            RefreshShopBottomPagedViewVisible();
            RefreshGetButtonState();

            if (GameEntry.CustomConfig.PropDataBaseSO.TryGetPropData(m_UIData.RewardData.propType, out PropData propData))
            {
                m_PropImg.sprite = propData.sprite_big;
            }


            if (m_ContentTMP != null && !string.IsNullOrEmpty(m_UIData.Content))
            {
                m_ContentTMP.text = m_UIData.Content;
            }
            if (m_TitleTMP != null && !string.IsNullOrEmpty(m_UIData.Title))
            {
                m_TitleTMP.text = m_UIData.Title;
            }
            if (m_TitleTMP != null)
            {
                m_CountTMP.text = "x" + m_UIData.RewardData.Count;
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (!m_CallbackHandled)
            {
                m_UIData?.OnClosed?.Invoke();
            }

            m_UIData = null;
            m_IsProcessing = false;
            m_CallbackHandled = false;
            m_HasPurchasedReward = false;
            base.OnClose(isShutdown, userData);
        }

        private void OnGetClick()
        {
            if (m_IsProcessing)
            {
                return;
            }

            PlayUISound(SoundId.UI_Click);

            if (m_HasPurchasedReward)
            {
                m_CallbackHandled = true;
                if (m_UIData?.OnPurchasedRewardClick != null)
                    m_UIData.OnPurchasedRewardClick.Invoke();
                else
                    m_UIData?.OnSuccessed?.Invoke();

                Close();
                return;
            }


            m_IsProcessing = true;
            GetPropsUIData uiData = m_UIData;
            AdsManager.ShowRewardedAd(
                () => HandleRewardedAdSuccess(uiData),
                error => HandleRewardedAdFailed(uiData, error));
        }

        private void HandleRewardedAdSuccess(GetPropsUIData uiData)
        {
            if (m_CallbackHandled)
            {
                return;
            }

            m_CallbackHandled = true;
            uiData?.OnSuccessed?.Invoke();

            if (m_UIData == uiData)
            {
                Close();
            }
        }

        private void HandleRewardedAdFailed(GetPropsUIData uiData, string error)
        {
            if (m_CallbackHandled)
            {
                return;
            }

            m_CallbackHandled = true;
            uiData?.OnFailed?.Invoke(error);

            if (m_UIData == uiData)
            {
                Close();
            }
        }


        private void OnCloseClick()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        private void BindShopBottomPagedView()
        {
            if (m_ShopBottomPagedView == null)
                m_ShopBottomPagedView = GetComponentInChildren<ShopBottomPagedView>(true);

            if (m_ShopBottomPagedView == null)
                return;

            m_ShopBottomPagedView.SetAnalyticsPage(ShopAnalytics.PageGetProps);

            if (m_GetBtnRT != null)
                m_ShopBottomPagedView.SetRewardFlyTarget(m_GetBtnRT, OnShopRewardFlyArrived);
        }

        private void RefreshShopBottomPagedViewVisible()
        {
            if (m_ShopBottomPagedView == null)
                BindShopBottomPagedView();

            m_ShopBottomPagedView?.RefreshVisibleState();
        }

        private void OnShopRewardFlyArrived(RewardData reward)
        {
            if (m_GetBtnRT != null)
                _ = m_GetBtnRT.PlayScale(0.8f, 0.6f, 0.3f);

            if (!IsRewardForCurrentProp(reward))
                return;

            m_HasPurchasedReward = true;
            RefreshGetButtonState();
        }

        private bool IsRewardForCurrentProp(RewardData reward)
        {
            return reward != null &&
                   m_UIData?.RewardData != null &&
                   reward.propType == m_UIData.RewardData.propType;
        }

        private void RefreshGetButtonState()
        {
            if (m_GetBtnAdIconRT != null)
                m_GetBtnAdIconRT.gameObject.SetActive(!m_HasPurchasedReward);

            // if (m_GetBtnTMP != null)
            // {
            //     string key = m_HasPurchasedReward
            //         ? LocalizationKeys.Fail_Shuffle
            //         : LocalizationKeys.Btn_Get;
            //     m_GetBtnTMP.text = GameEntry.Localization.GetString(key);
            // }
        }

    }
}
