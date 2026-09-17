using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.SeasonPass.UI
{
    /// <summary>规则页只管理交互和字体；标题、正文和布局由模块内 Prefab 维护。</summary>
    public sealed class SeasonPassRulesPanel : UGuiForm
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TMP_FontAsset m_TextFont;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.onClick.AddListener(() => Close());
            else Debug.LogWarning("[SeasonPassRulesPanel] Bind m_CloseButton on the designer-owned Prefab.");
            ApplyConfiguredFont();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ApplyConfiguredFont();
        }

        private void ApplyConfiguredFont()
        {
            if (m_TextFont == null) return;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) text.font = m_TextFont;
        }
    }
}
