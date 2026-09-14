using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Lokas
{
    public class WinStreakMultiplierUI : MonoBehaviour
    {
        [SerializeField] private List<CanvasGroup> m_MulHighLightCGs;
        [SerializeField] private List<TMP_Text> m_MulTexts;
        [SerializeField] private int[] m_Multipliers = new[] { 1, 2, 4, 8, 10 };

        public int GetCurrentMultiplier(int streak)
        {
            if (m_Multipliers == null || m_Multipliers.Length == 0)
                return 1;

            int index = Mathf.Clamp(streak - 1, 0, m_Multipliers.Length - 1);
            return m_Multipliers[index];
        }

        public TMP_Text GetMultiplierText(int streak)
        {
            if (m_MulTexts == null || m_MulTexts.Count == 0)
                return null;

            int index = Mathf.Clamp(streak - 1, 0, m_MulTexts.Count - 1);
            return m_MulTexts[index];
        }

        public void Refresh(int streak)
        {
            if (m_MulTexts != null && m_Multipliers != null)
            {
                int textCount = Mathf.Min(m_MulTexts.Count, m_Multipliers.Length);
                for (int i = 0; i < textCount; i++)
                {
                    if (m_MulTexts[i] == null)
                        continue;

                    m_MulTexts[i].text = "x" + m_Multipliers[i];
                }
            }

            if (m_MulHighLightCGs == null || m_MulHighLightCGs.Count == 0)
                return;

            int lastIndex = m_MulHighLightCGs.Count - 1;
            int activeIndex = Mathf.Clamp(streak - 1, 0, lastIndex);
            int previousIndex = streak > 1 ? Mathf.Clamp(streak - 2, 0, lastIndex) : -1;

            for (int i = 0; i < m_MulHighLightCGs.Count; i++)
            {
                CanvasGroup highlight = m_MulHighLightCGs[i];
                if (highlight == null)
                    continue;

                highlight.DOKill();

                float initialAlpha = i == previousIndex ? 1f : 0f;
                highlight.alpha = initialAlpha;

                GameEntry.Sound.PlayUISound(SoundId.SFX_Progress_Rise);

                if (previousIndex == activeIndex)
                    highlight.alpha = i == activeIndex ? 1f : 0f;
                else if (i == previousIndex)
                    highlight.DOFade(0f, 0.4f);
                else if (i == activeIndex)
                    highlight.DOFade(1f, 0.4f);
            }
        }
    }
}
