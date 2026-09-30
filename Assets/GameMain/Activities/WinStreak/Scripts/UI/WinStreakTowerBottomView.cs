using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>
    /// 塔底只承载起始检查点和第一个固定高度的进度段；奖励始终由中段和塔顶承载。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WinStreakTowerBottomView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Checkpoint;
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private GameObject m_ClaimedState;
        [SerializeField] private GameObject m_CheckpointBadge;

        public void Bind(int startCheckpoint, int endCheckpoint, int currentProgress, bool isJoined)
        {
            if (m_Checkpoint != null) m_Checkpoint.SetText(startCheckpoint.ToString());
            bool reachedBase = isJoined && currentProgress >= startCheckpoint;
            if (m_ClaimedState != null) m_ClaimedState.SetActive(reachedBase);
            if (m_CheckpointBadge != null) m_CheckpointBadge.SetActive(!reachedBase);
            SetSegmentProgress(WinStreakCheckpointRowView.CalculateSegmentProgress(currentProgress,
                startCheckpoint, endCheckpoint));
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(TMP_Text checkpoint, Image progressFill, GameObject claimedState,
            GameObject checkpointBadge)
        {
            m_Checkpoint = checkpoint;
            m_ProgressFill = progressFill;
            m_ClaimedState = claimedState;
            m_CheckpointBadge = checkpointBadge;
        }
#endif

        private void SetSegmentProgress(float progress)
        {
            if (m_ProgressFill == null) return;
            if (m_ProgressFill.type == Image.Type.Filled)
            {
                m_ProgressFill.fillAmount = progress;
                return;
            }

            RectTransform rect = m_ProgressFill.rectTransform;
            Vector2 max = rect.anchorMax;
            max.y = progress;
            rect.anchorMax = max;
        }
    }
}
