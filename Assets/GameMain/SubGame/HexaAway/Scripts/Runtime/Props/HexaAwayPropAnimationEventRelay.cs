using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 挂在 Animator 所在节点上的动画事件转发器，把 AnimationClip 事件转回根节点播放器。
    /// </summary>
    public sealed class HexaAwayPropAnimationEventRelay : MonoBehaviour
    {
        private HexaAwayPropAnimationPlayer m_Player;

        public void Init(HexaAwayPropAnimationPlayer player)
        {
            m_Player = player;
        }

        public void OnDrillingStarted()
        {
            m_Player?.OnDrillingStarted();
        }

        public void OnHitAnimationEnded()
        {
            m_Player?.OnHitAnimationEnded();
        }
    }
}
