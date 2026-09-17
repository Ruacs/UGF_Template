using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.SeasonPass.UI
{
    /// <summary>Gold Pass 购买页。当前购买成功由活动模块模拟，后续可替换为商店支付结果。</summary>
    public sealed class SeasonPassGoldPassPurchasePanel : UGuiForm
    {
        [SerializeField] private TMP_FontAsset m_TextFont;

        [SerializeField] private Button m_GetButton;
        [SerializeField] private Button m_CloseButton;
        private SeasonPassActivityModule m_Module;
        private bool m_IsPurchasing;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_GetButton == null) m_GetButton = FindButton("Btn_Get");
            if (m_CloseButton == null) m_CloseButton = FindButton("Btn_Close");
            if (m_GetButton != null) m_GetButton.AddSafeClick(OnClickGet);
            else Debug.LogWarning("[SeasonPassGoldPassPurchasePanel] The Prefab needs a Btn_Get button.");
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            else Debug.LogWarning("[SeasonPassGoldPassPurchasePanel] The Prefab needs a Btn_Close button.");
            ApplyConfiguredFont();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ApplyConfiguredFont();
            m_Module = (userData as ActivityPageUserData)?.Request.Arguments as SeasonPassActivityModule;
            if (m_Module == null)
                Debug.LogWarning("[SeasonPassGoldPassPurchasePanel] Expected SeasonPassActivityModule as page arguments.");
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            m_Module = null;
            m_IsPurchasing = false;
        }

        private void OnClickGet()
        {
            PurchaseAsync().Forget(Debug.LogException);
        }

        private async UniTask PurchaseAsync()
        {
            if (m_IsPurchasing || m_Module == null) return;
            m_IsPurchasing = true;
            Refresh();
            bool purchased = await m_Module.ActivatePremiumAsync();
            m_IsPurchasing = false;
            if (purchased) Close();
            else Refresh();
        }

        private void Refresh()
        {
            if (m_GetButton == null) return;
            m_GetButton.interactable = !m_IsPurchasing && m_Module != null && !m_Module.GetSnapshot().IsPremiumActivated;
        }

        private void ApplyConfiguredFont()
        {
            if (m_TextFont == null) return;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) text.font = m_TextFont;
        }

        private Button FindButton(string buttonName)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName) return button;
            }
            return null;
        }
    }
}
