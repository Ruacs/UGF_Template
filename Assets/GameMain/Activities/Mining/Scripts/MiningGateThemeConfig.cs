using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    [Serializable]
    public sealed class MiningGateThemeDefinition
    {
        [SerializeField, Range(1, 5)] private int m_StepId = 1;
        [SerializeField] private Sprite m_BackSprite;
        [SerializeField] private Sprite m_SceneSprite;

        public int StepId => m_StepId;
        public Sprite BackSprite => m_BackSprite;
        public Sprite SceneSprite => m_SceneSprite;

        internal MiningGateThemeDefinition(int stepId)
        {
            m_StepId = stepId;
        }
    }

    /// <summary>Step1-5 的 Gate 主题图片配置，与关卡和宝石配置独立维护。</summary>
    [CreateAssetMenu(fileName = "MiningGateThemeConfig", menuName = "GF/Activity/Mining/Gate Theme Config")]
    public sealed class MiningGateThemeConfig : ScriptableObject
    {
        [SerializeField] private MiningGateThemeDefinition[] m_Themes = Array.Empty<MiningGateThemeDefinition>();

        public IReadOnlyList<MiningGateThemeDefinition> Themes => m_Themes;

        private void OnEnable()
        {
            if (m_Themes == null || m_Themes.Length == 0)
                m_Themes = CreateDefaultThemes();
        }

        public MiningGateThemeDefinition GetTheme(int stepId)
        {
            if (m_Themes != null)
            {
                for (int index = 0; index < m_Themes.Length; index++)
                {
                    MiningGateThemeDefinition theme = m_Themes[index];
                    if (theme != null && theme.StepId == stepId) return theme;
                }
            }

            return null;
        }

        public void ValidateConfiguration(bool requireSprites = true)
        {
            if (m_Themes == null || m_Themes.Length != 5)
                throw new InvalidOperationException("Mining Gate theme config needs exactly 5 themes.");

            var stepIds = new HashSet<int>();
            for (int index = 0; index < m_Themes.Length; index++)
            {
                MiningGateThemeDefinition theme = m_Themes[index];
                if (theme == null)
                    throw new InvalidOperationException($"Mining Gate theme {index} is null.");
                if (theme.StepId < 1 || theme.StepId > 5)
                    throw new InvalidOperationException($"Mining Gate theme Step={theme.StepId} is invalid.");
                if (!stepIds.Add(theme.StepId))
                    throw new InvalidOperationException($"Mining Gate theme Step={theme.StepId} is duplicated.");
                if (requireSprites && (theme.BackSprite == null || theme.SceneSprite == null))
                    throw new InvalidOperationException(
                        $"Mining Gate theme Step={theme.StepId} has incomplete sprites.");
            }

            for (int stepId = 1; stepId <= 5; stepId++)
                if (!stepIds.Contains(stepId))
                    throw new InvalidOperationException($"Mining Gate theme Step={stepId} is missing.");
        }

        private static MiningGateThemeDefinition[] CreateDefaultThemes()
        {
            return new[]
            {
                new MiningGateThemeDefinition(1),
                new MiningGateThemeDefinition(2),
                new MiningGateThemeDefinition(3),
                new MiningGateThemeDefinition(4),
                new MiningGateThemeDefinition(5)
            };
        }
    }
}
