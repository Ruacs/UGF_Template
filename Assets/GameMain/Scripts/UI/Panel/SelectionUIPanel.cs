using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public sealed class SelectionUIData
    {
        public System.Action<SelectionUIPanel> Opened { get; }
        public System.Action<SelectionUIPanel, bool> Closed { get; }

        public SelectionUIData(System.Action<SelectionUIPanel> opened, System.Action<SelectionUIPanel, bool> closed)
        {
            Opened = opened;
            Closed = closed;
        }
    }

    public class SelectionUIPanel : UGuiForm
    {
        [SerializeField] private Button m_BtnClose;

        private SelectionUIData m_Data;
        private bool m_IsSelectionConfirmed;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (m_BtnClose == null)
            {
                Transform closeTransform = transform.Find("Btn_Close") ?? transform.Find("SelectionRoot/Btn_Close");
                if (closeTransform != null)
                {
                    m_BtnClose = closeTransform.GetComponent<Button>();
                }
            }

            if (m_BtnClose != null)
            {
                m_BtnClose.onClick.AddListener(OnClickClose);
            }
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_Data = userData as SelectionUIData;
            m_IsSelectionConfirmed = false;
            ConfigureRaycastTargets();
            m_Data?.Opened?.Invoke(this);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            SelectionUIData data = m_Data;
            bool isSelectionConfirmed = m_IsSelectionConfirmed;

            m_Data = null;
            m_IsSelectionConfirmed = false;

            data?.Closed?.Invoke(this, isSelectionConfirmed);
            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 由棋盘成功选中目标后调用。手动点关闭按钮不会走这个入口，因此不会消耗道具。
        /// </summary>
        public void CloseAsSelectionConfirmed()
        {
            m_IsSelectionConfirmed = true;
            Close();
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        private void ConfigureRaycastTargets()
        {
            Transform closeRoot = m_BtnClose != null ? m_BtnClose.transform : null;
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                bool isCloseGraphic = closeRoot != null && (graphic.transform == closeRoot || graphic.transform.IsChildOf(closeRoot));
                graphic.raycastTarget = isCloseGraphic;
            }
        }
    }
}
