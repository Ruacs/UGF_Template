using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.UI
{
    public sealed class RaceLaneView : MonoBehaviour
    {
        [SerializeField] private int m_LaneIndex;

        [SerializeField] private TMP_Text m_PlayerNameText;
        [SerializeField] private TMP_Text m_Rank;
        [SerializeField] private Graphic m_Car;
        [SerializeField] private UI_AvatarBox m_Avatar;
        [SerializeField] private RectTransform m_RankTransform;
        [SerializeField] private RectTransform m_CarTransform;
        [SerializeField] private float m_RankOffset = 8f;
        [SerializeField, Range(0.1f, 1f)] private float m_TrackTravel01 = 0.87f;

        private bool m_CarBasePositionInitialized;
        private Vector2 m_CarBaseAnchoredPosition;

        public void SetLaneIndex(int index) => m_LaneIndex = index;

        public void BindSerializedReferences(TMP_Text playerNameText, TMP_Text rank, Graphic car,
            UI_AvatarBox avatar, RectTransform rankTransform, RectTransform carTransform)
        {
            m_PlayerNameText = playerNameText;
            m_Rank = rank;
            m_Car = car;
            m_Avatar = avatar;
            m_RankTransform = rankTransform;
            m_CarTransform = carTransform;
        }

        public void Bind(RaceRacerSnapshot racer, bool showRank = true)
        {
            if (racer == null) return;

            if (m_PlayerNameText != null)
                m_PlayerNameText.text = racer.IsPlayer ? GameEntry.SaveData.PlayerName : racer.DisplayName;
            if (m_Rank != null)
            {
                m_Rank.text = racer.Rank.ToString();
                m_Rank.color = RaceUiFactory.RankColor(racer.Rank);
                m_Rank.gameObject.SetActive(showRank);
            }

            if (m_CarTransform != null)
            {
                float position = Mathf.Clamp01(racer.Progress01) * m_TrackTravel01;
                UpdateCarPosition(position);
            }

            UpdateRankPosition();
            BindAvatar(racer.IsPlayer);
        }

        private void UpdateCarPosition(float normalizedPosition)
        {
            if (!m_CarBasePositionInitialized)
            {
                m_CarBaseAnchoredPosition = m_CarTransform.anchoredPosition;
                m_CarBasePositionInitialized = true;
            }

            // RaceCar is a fixed-size image. Keep both vertical anchors equal so
            // Unity never turns the progress value into a stretched height.
            float fixedAnchorY = m_CarTransform.anchorMin.y;
            m_CarTransform.anchorMin = new Vector2(m_CarTransform.anchorMin.x, fixedAnchorY);
            m_CarTransform.anchorMax = new Vector2(m_CarTransform.anchorMax.x, fixedAnchorY);

            RectTransform parent = m_CarTransform.parent as RectTransform;
            float travel = parent == null ? 0f : parent.rect.height * normalizedPosition;
            m_CarTransform.anchoredPosition = m_CarBaseAnchoredPosition + Vector2.up * travel;
        }

        private void UpdateRankPosition()
        {
            if (m_RankTransform == null || m_CarTransform == null) return;

            var corners = new Vector3[4];
            m_CarTransform.GetWorldCorners(corners);
            Vector3 carTopCenter = (corners[1] + corners[2]) * 0.5f;
            float rankHalfHeight = m_RankTransform.rect.height * m_RankTransform.lossyScale.y * 0.5f;
            m_RankTransform.position = carTopCenter + Vector3.up * (m_RankOffset + rankHalfHeight);
            if (m_RankTransform.GetSiblingIndex() < m_CarTransform.GetSiblingIndex())
                m_RankTransform.SetAsLastSibling();
        }

        private void BindAvatar(bool isPlayer)
        {
            if (m_Avatar == null) return;

            AvatarDatabaseSO database = GameEntry.CustomConfig == null ? null : GameEntry.CustomConfig.AvatarConfig;
            if (database == null) return;

            Sprite avatar = null;
            Sprite frame = null;
            if (isPlayer)
            {
                if (database.TryGetAvatar(GameEntry.SaveData.AvatarId, out AvatarEntrySO avatarEntry))
                    avatar = avatarEntry.sprite;
                if (database.TryGetFrame(GameEntry.SaveData.AvatarFrameId, out AvatarFrameEntrySO frameEntry))
                    frame = frameEntry.sprite;
            }
            else
            {
                IReadOnlyList<AvatarEntrySO> avatars = database.GetAllAvatars();
                if (avatars.Count > 0)
                {
                    AvatarEntrySO entry = avatars[m_LaneIndex % avatars.Count];
                    if (entry != null) avatar = entry.sprite;
                }
            }
            m_Avatar.SetAvatar(avatar);
            m_Avatar.SetFrame(frame);
        }
    }
}
