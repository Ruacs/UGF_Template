using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.UI
{
    /// <summary>规则页的三段说明由 Prefab 文案维护，脚本只处理关闭交互。</summary>
    public sealed class RaceDetailUIPanel : UGuiForm
    {
        private Button m_CloseButton;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CloseButton = RaceUiLookup.Button(transform, "Root/Btn_Close");
            if (m_CloseButton == null)
            {
                enabled = false;
                return;
            }
            m_CloseButton.AddSafeClick(() => Close());
        }
    }
}
