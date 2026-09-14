using System;
using DG.Tweening;
using GameFramework.ObjectPool;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ComboItem : MonoBehaviour, IPoolable
    {
        [SerializeField] private RectTransform m_selfRect;
        [SerializeField] private CanvasGroup m_canvasGroup;

        [Header("Child nodes")]
        [SerializeField] private TextMeshProUGUI m_txtContent;
        [SerializeField] private Image m_imgContent;
        [SerializeField] private SkeletonGraphic m_spineContent;

        private static readonly string[] LevelNames =
        {
            "Good", "Nice", "Great", "Excellent",
            "Awesome", "Amazing", "Fantasy", "Unbelievable","GoodEye","HawEye"
        };

        // 寮瑰叆鏃堕暱
        private const float PunchInDuration = 0.25f;
        // 娑堝け鍔ㄧ敾鏃堕暱
        private const float FadeOutDuration = 0.35f;

        private Sequence m_sequence;
        private Action m_onComplete;

        // 鈹€鈹€ IPoolable 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

        private void Awake()
        {
            if (m_selfRect == null) m_selfRect = GetComponent<RectTransform>();
            if (m_canvasGroup == null) m_canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnSpawn()
        {

            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 1f;

            m_selfRect.localScale = Vector3.zero;
            gameObject.SetActive(true);
        }

        public void OnRecycle()
        {
            m_sequence?.Kill();
            m_sequence = null;
            m_onComplete = null;

            SetAllInactive();
            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 1f;

            gameObject.SetActive(false);
        }

        // 鈹€鈹€ 鍏紑鎺ュ彛 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

        /// <summary>
        /// 鍦ㄦ寚瀹?Canvas 鏈湴鍧愭爣澶勬挱鏀捐繛鍑绘彁绀恒€?        /// </summary>
        /// <param name="data">杩炲嚮鏁版嵁</param>
        /// <param name="canvasLocalPos">宸插仛杩囪竟缂橀€傚簲鐨?Canvas 鏈湴鍧愭爣</param>
        /// <param name="sprite">鍥剧墖妯″紡浣跨敤鐨?Sprite锛堝彲涓?null锛?/param>
        /// <param name="onComplete">鎾斁缁撴潫鍥炶皟锛堢敤浜庡洖鏀讹級</param>
        public void Play(ComboData data, Vector2 canvasLocalPos, Sprite sprite, Action onComplete)
        {
            m_onComplete = onComplete;
            m_selfRect.anchoredPosition = canvasLocalPos;

            ApplyDisplayMode(data, sprite);
            PlayAnimation(data);
        }

        // 鈹€鈹€ 绉佹湁鏂规硶 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

        private void ApplyDisplayMode(ComboData data, Sprite sprite)
        {
            SetAllInactive();

            switch (data.displayMode)
            {
                case ComboDisplayMode.Text:
                    if (m_txtContent != null)
                    {
                        m_txtContent.gameObject.SetActive(true);
                        m_txtContent.SetText(LevelNames[(int)data.level]);
                    }
                    break;

                case ComboDisplayMode.Image:
                    if (m_imgContent != null && sprite != null)
                    {
                        m_imgContent.gameObject.SetActive(true);
                        m_imgContent.sprite = sprite;
                        // m_imgContent.SetNativeSize();
                    }
                    else if (m_txtContent != null)
                    {
                        // 鍥為€€鍒版枃鏈?                        m_txtContent.gameObject.SetActive(true);
                        m_txtContent.SetText(LevelNames[(int)data.level]);
                    }
                    break;

                case ComboDisplayMode.Spine:
                    if (m_spineContent != null)
                    {
                        m_spineContent.gameObject.SetActive(true);
                        m_spineContent.AnimationState.SetAnimation(0, data.spineAnimationName, false);
                    }
                    break;
            }
        }

        private void PlayAnimation(ComboData data)
        {
            m_sequence?.Kill();

            float holdDuration = Mathf.Max(0f, data.duration - PunchInDuration - FadeOutDuration);

            m_sequence = DOTween.Sequence();

            m_sequence.Append(
                m_selfRect.DOScale(1f, PunchInDuration).SetEase(Ease.OutBack)
            );

            m_sequence.AppendInterval(holdDuration);

            // 闃舵 3锛氫笂娴?+ 娣″嚭
            m_sequence.Append(
                m_selfRect.DOAnchorPosY(
                    m_selfRect.anchoredPosition.y + data.floatHeight,
                    FadeOutDuration
                ).SetEase(data.floatEase)
            );

            if (m_canvasGroup != null)
            {
                m_sequence.Join(
                    m_canvasGroup.DOFade(0f, FadeOutDuration).SetEase(Ease.InQuad)
                );
            }

            m_sequence.OnComplete(() => m_onComplete?.Invoke());
        }

        private void SetAllInactive()
        {
            if (m_txtContent != null)  m_txtContent.gameObject.SetActive(false);
            if (m_imgContent != null)  m_imgContent.gameObject.SetActive(false);
            if (m_spineContent != null) m_spineContent.gameObject.SetActive(false);
        }
    }
}


