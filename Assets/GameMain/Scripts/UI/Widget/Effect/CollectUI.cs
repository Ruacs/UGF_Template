
using System;
using DG.Tweening; 
using UnityEngine;
using UnityEngine.UI;
using Log = UnityGameFramework.Runtime.Log;

namespace Lokas
{
    [Serializable]
    public class CollectTargetData
    {
        public int Id;
        public Sprite bgSprite { get; private set; }
        public Sprite iconSprite { get; private set; }
        public int amount { get; set; }

        public CollectTargetData(int Id, Sprite bg, Sprite icon, int amount)
        {
            this.Id = Id;
            bgSprite = bg;
            iconSprite = icon;
            this.amount = amount;
        }
    }
    /// <summary>
    /// Collect target UI with FX/complete state, based on generic icon+count UI.
    /// </summary>
    public class CollectUI : IconCountItemUI
    {
        [SerializeField] private RectTransform _selfRT;
        [SerializeField] private Image m_bgImg;
        [SerializeField] private GameObject m_markImg;   //完成图标 替换文本显示
        [SerializeField] private CanvasGroup m_canvasGroup;

        [SerializeField] CollectTargetData m_data;

        public CollectTargetData Data => m_data;

        public RectTransform SelfRT => _selfRT;

        public int Id => m_data.Id;
        private float m_lastFxTime;
        private const float FX_CD = 0.5f;

        public void Init(CollectTargetData collectTargetData)
        {
            m_data = collectTargetData;
            gameObject.SetActive(true);
            m_canvasGroup.alpha = 1f;

            if (collectTargetData == null)
            {
                Log.Error("[CollectUI] Init failed: collectTargetData is null.");
                SetData(null, 0);
                return;
            }


            m_bgImg.sprite = collectTargetData.bgSprite;
            SetData(collectTargetData.iconSprite, collectTargetData.amount);

            if (m_markImg != null)
            {
                m_markImg.SetActive(collectTargetData.amount == 0);

            }

            if (m_countTMP != null)
            {
                m_countTMP.gameObject.SetActive(collectTargetData.amount > 0);
            }
        }

        public override void SetCountValue(int count)
        {
            base.SetCountValue(count);
        }

        public void SetAlpha(float alpha)
        {
            if (m_canvasGroup == null)
            {
                Log.Warning("[CollectUI] SetAlpha ignored: m_canvasGroup is null.");
                return;
            }
            m_canvasGroup.alpha = alpha;
        }
        public void DoFade(float from, float to, float duration)
        {
            if (m_canvasGroup == null)
            {
                Log.Warning("[CollectUI] DoFade ignored: m_canvasGroup is null.");
                return;
            }
            m_canvasGroup.alpha = from;

            m_canvasGroup.DOFade(to, duration).SetUpdate(true);
        }

        public void DecreaseAmount(int delta)
        {
            if (m_data == null)
            {
                Log.Warning("[CollectUI] DecreaseAmount ignored: m_data is null.");
                return;
            }

            if (delta <= 0)
            {
                OnCollectUpdated(m_data.amount);
                return;
            }

            m_data.amount = Mathf.Max(0, m_data.amount - delta);
            OnCollectUpdated(m_data.amount);
        }

        public void AddAmount(int value)
        {
            if (m_data == null)
            {
                Log.Warning("[CollectUI] DecreaseAmount ignored: m_data is null.");
                return;
            }

            if (value <= 0)
            {
                OnCollectUpdated(m_data.amount);
                return;
            }

            m_data.amount = Mathf.Max(0, m_data.amount + value);
            OnCollectUpdated(m_data.amount);
        }

        /// <summary>
        /// Update UI with the latest remaining count.
        /// </summary>
        public void OnCollectUpdated(int remainingCount)
        {
            if (m_data == null)
            {
                Log.Warning("[CollectUI] OnCollectUpdated ignored: m_data is null.");
                return;
            }

            m_data.amount = Mathf.Max(0, remainingCount);
            if (m_data.amount > 0)
            {
                if (m_markImg != null)
                {
                    m_markImg.SetActive(false);
                }

                SetCountValue(m_data.amount);
                PlayCollectFx();
                return;
            }

            if (m_markImg != null)
            {
                m_markImg.SetActive(true);
            }

            if (m_countTMP != null)
            {
                m_countTMP.gameObject.SetActive(false);
            }

            // GameEntry.Sound.PlaySound(SoundId.SFX_LevelTarget_Rise);

        }

        public void PlayCollectFx()
        {
            if (Time.unscaledTime - m_lastFxTime < FX_CD)
            {
                return;
            }

            m_lastFxTime = Time.unscaledTime;

        }


        public override void Reset()
        {
            base.Reset();
            m_bgImg.sprite = null;
        }
    }
}
