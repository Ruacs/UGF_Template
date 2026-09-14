using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Generic icon + count UI item.
    /// </summary>
    public class IconCountItemUI : MonoBehaviour
    {
        [SerializeField] protected Image m_icon;
        [SerializeField] protected TMP_Text m_countTMP;

        public virtual void SetData(Sprite icon, int count)
        {
            gameObject.SetActive(true);
            SetIcon(icon);
            SetCountValue(count);
        }

        public virtual void SetCountValue(int count)
        {
            if (m_countTMP != null)
            {
                m_countTMP.text = Mathf.Max(0, count).ToString();
                m_countTMP.gameObject.SetActive(true);
            }
        }

        public RectTransform GetIconRectTransform()
        {
            return m_icon != null ? m_icon.rectTransform : null;
        }

        protected void SetIcon(Sprite icon)
        {
            if (m_icon != null)
            {
                m_icon.sprite = icon;
            }
        }


        public virtual void Reset()
        {
             SetData(null,0);
        }
    }
}
