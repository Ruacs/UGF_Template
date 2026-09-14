using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class UI_RankingItem : MonoBehaviour
    {
        [System.Serializable]
        public struct NameStyle
        {
            public Color color;
            public string styleKey;
        }

        [SerializeField] private RectTransform m_BoxRootRT;

        [Header("Bg")]
        [SerializeField] private Image[] m_bgDefaults;
        [SerializeField] private Image m_bgSelf;

        [Header("Left")]
        [SerializeField] private TextMeshProUGUI m_tmpRank;
        [SerializeField] private Image[] m_imgRanks;
        [SerializeField] private Image m_imgRank;

        [SerializeField] private UI_AvatarBox m_AvatarBox;
        [SerializeField] private TextMeshProUGUI m_tmpName;

        [Header("Right - Rewards")]
        [SerializeField] private Button m_rewardsBtn;
        [SerializeField] private Image m_iconReward;

        [SerializeField] private UI_RewardTipsBox m_RewardTipBox;

        [Header("Right - ItemSlot")]
        [SerializeField] private Image m_itemBg;
        [SerializeField] private Image m_itemIcon;
        [SerializeField] private TextMeshProUGUI m_tmpNum;

        [SerializeField] private UIParticle m_RankAnimEndEffect;
        [SerializeField] private UIParticle m_RankUp;

        [Header("Name Style")]
        [SerializeField] private NameStyle m_nameStyleDefault;
        [SerializeField] private NameStyle m_nameStyleSelf;
        [SerializeField] private List<NameStyle> m_namStyleList;

        private bool m_isSelf;
        [SerializeField] bool showX = true;

        public Button RewardsBtn => m_rewardsBtn;
        public bool IsSelf => m_isSelf;
        public RectTransform ItemIconRT => m_itemIcon != null ? m_itemIcon.rectTransform : null;

        public bool isHomeRank = true;

        [SerializeField] private RectTransform m_bgLight;

        private int m_rank;

        private void Awake()
        {
            AutoBindReferences();
            m_rewardsBtn.AddSafeClick(OnClickReward);
        }

        private void OnValidate()
        {
            AutoBindReferences();
        }

        private void OnClickReward()
        {
            m_RewardTipBox.Show(GameEntry.CustomConfig.RewardConfig.RankRewardList[m_rank - 1].rewardDatas);
        }


        public void SetData(int rank, string playerName, Sprite avatar, Sprite avatarFrame, int itemCount, bool isSelf, bool isHomeRank = true)
        {
            AutoBindReferences();
            m_rank = rank;



            this.isHomeRank = isHomeRank;
            SetRank(rank);
            SetName(playerName);
            SetAvatar(avatar, avatarFrame);
            SetItemCount(itemCount);
            SetBg(isSelf, rank);

            m_rewardsBtn.gameObject.SetActive(isHomeRank && rank < 7 && rank > 0);

            if (m_bgLight != null)
            {
                m_bgLight?.gameObject?.SetActive(false);
            }


            if (isHomeRank && isSelf && rank <= 3 && m_bgLight != null)
            {
                m_bgLight?.gameObject?.SetActive(true);

                m_bgLight.transform?.DOKill();
                m_bgLight.transform.DORotate(new Vector3(0, 0, 360f), 2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1);
            }

        }

        public void Play(float delay = 0.01f)
        {
            m_BoxRootRT.anchoredPosition = new Vector2(1200, 0);
            m_BoxRootRT.DOAnchorPosX(0, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);
        }

        public void SetBg(bool isSelf, int rank)
        {
            AutoBindReferences();
            m_isSelf = isSelf;

            // ✅ 如果是自己，直接只显示 self，其他全关
            if (isSelf)
            {
                if (m_bgDefaults != null)
                {
                    for (int i = 0; i < m_bgDefaults.Length; i++)
                    {
                        m_bgDefaults[i].gameObject.SetActive(false);
                    }
                }

                if (m_bgSelf != null)
                    m_bgSelf.gameObject.SetActive(true);

                ApplyNameStyle(m_nameStyleSelf);
                return; // ⚠️ 关键：直接结束，避免后面逻辑干扰
            }

            // ❌ 不是自己，关掉 self
            if (m_bgSelf != null)
                m_bgSelf.gameObject.SetActive(false);

            ApplyNameStyle(m_nameStyleDefault);

            if (m_bgDefaults != null)
            {
                if (m_bgDefaults.Length == 1)
                {
                    m_bgDefaults[0].gameObject.SetActive(true);
                    return;
                }

                for (int i = 0; i < m_bgDefaults.Length; i++)
                {
                    if (rank > 0 && rank <= 3)
                    {
                        // rank 1~3 → 对应 index 0~2
                        m_bgDefaults[i].gameObject.SetActive(i == rank - 1);


                    }
                    else
                    {
                        // rank >=4 → 只显示 index 3
                        m_bgDefaults[i].gameObject.SetActive(i == 3);

                    }
                }
            }

        }

        public void SetRank(int rank)
        {
            AutoBindReferences();

            bool useRankImage = rank > 0 && rank <= 3;

            if (m_imgRanks != null && m_imgRanks.Length > 0)
            {
                for (int i = 0; i < m_imgRanks.Length; i++)
                {
                    if (m_imgRanks[i] == null) continue;
                    m_imgRanks[i].gameObject.SetActive(useRankImage && i == rank - 1);
                }
            }

            if (m_imgRank != null)
                m_imgRank.gameObject.SetActive(useRankImage);

            if (m_tmpRank != null)
            {
                m_tmpRank.gameObject.SetActive(!useRankImage);
                m_tmpRank.text = rank > 0 ? rank.ToString() : "-";
            }
        }

        public void SetName(string playerName)
        {
            AutoBindReferences();
            if (m_tmpName != null)
                m_tmpName.text = playerName;
        }

        public void SetAvatar(Sprite avatar, Sprite frame = null)
        {
            AutoBindReferences();
            if (avatar != null)
                m_AvatarBox.SetAvatar(avatar);
            if (frame != null)
                m_AvatarBox.SetFrame(frame);
        }

        public void SetRewardIcon(Sprite icon)
        {
            AutoBindReferences();

            if(icon == null) return;
            
            if (m_iconReward != null)
                m_iconReward.sprite = icon;
        }

        public void SetItemSlot(Sprite itemIcon, int count)
        {
            AutoBindReferences();
            if (m_itemIcon != null)
                m_itemIcon.sprite = itemIcon;
            SetItemCount(count);
        }

        public void SetItemCount(int count)
        {
            AutoBindReferences();
            if (m_tmpNum != null)
            {
                if (showX)
                {
                    m_tmpNum.text = "x" + count.ToString();
                }
                else
                {
                    m_tmpNum.text = count.ToString();
                }
            }

        }

        private void ApplyNameStyle(NameStyle style)
        {
            if (m_tmpName == null) return;
            m_tmpName.color = style.color;
            if (style.styleKey != "Default")
            {
                m_tmpName.fontSharedMaterial = GameEntry.TMPFont.GetMaterialByStyleKey(style.styleKey);
                m_tmpName.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

            }

            TMPStyleApplier styleApplier = m_tmpName.GetComponent<TMPStyleApplier>();
            if (styleApplier != null)
                styleApplier.SetStyleKey(style.styleKey);
        }

        public void PlayRankAnimEnd() => m_RankAnimEndEffect.Play();
        public void PlayRankUp() => m_RankUp.Play();
        private void Reset()
        {
            AutoBindReferences();
        }



        private void AutoBindReferences()
        {
            return; // ⚠️ 先直接 return，避免重复绑定，后续可以根据需要再打开
            // BindIfNull(ref m_bgDefault, "Box/Bg_Default");
            BindIfNull(ref m_bgSelf, "Box/Bg_Self");

            BindIfNull(ref m_tmpRank, "Box/Left/tmp_Rank", "Box/Top/tmp_Rank");
            BindIfNull(ref m_imgRank, "Box/Top/img_Rank");
            BindIfNull(ref m_AvatarBox, "Box/Left/AvatarBox", "Box/Top/AvatarBox");
            BindIfNull(ref m_tmpName, "Box/Left/tmp_Name", "Box/Top/NameBox/tmp_Name");

            BindIfNull(ref m_rewardsBtn, "Box/Right/Rewards", "Box/Bottom/Rewards");
            BindIfNull(ref m_iconReward, "Box/Right/Rewards/Icon_reward", "Box/Bottom/Rewards/Icon_reward");
            if (m_iconReward == null && m_rewardsBtn != null)
                m_iconReward = m_rewardsBtn.GetComponent<Image>();

            BindIfNull(ref m_itemBg, "Box/Right/ItemSlot/itembg", "Box/Bottom/ItemSlot/itembg");
            BindIfNull(ref m_itemIcon, "Box/Right/ItemSlot/itembg/Icon", "Box/Bottom/ItemSlot/itembg/Icon");
            BindIfNull(ref m_tmpNum, "Box/Right/ItemSlot/tmp_num", "Box/Bottom/ItemSlot/tmp_num");
        }

        private void BindIfNull<T>(ref T field, params string[] paths) where T : Component
        {
            if (field != null || paths == null) return;

            for (int i = 0; i < paths.Length; i++)
            {
                var tr = transform.Find(paths[i]);
                if (tr == null) continue;

                field = tr.GetComponent<T>();
                if (field != null) return;
            }
        }
    }
}
