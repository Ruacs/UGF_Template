using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>Prefab 中预置的一个头像单元；引用与 CanvasGroup 均由 Inspector 提供。</summary>
    public sealed class GalaxyChallengeAvatarCellView : MonoBehaviour
    {
        [SerializeField] private UI_AvatarBox m_Avatar;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private RectTransform m_RectTransform;

        public RectTransform RectTransform => m_RectTransform != null
            ? m_RectTransform
            : m_RectTransform = transform as RectTransform;
        public CanvasGroup CanvasGroup => m_CanvasGroup;

        public void Bind(Sprite avatar, Sprite frame)
        {
            if (m_Avatar == null) return;
            m_Avatar.SetAvatar(avatar);
            m_Avatar.SetFrame(frame);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (m_CanvasGroup != null) m_CanvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
