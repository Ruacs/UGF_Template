using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ads;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Log = UnityGameFramework.Runtime.Log;

namespace Lokas
{

    public enum ChestSkinType
    {
        Chest_1 = 0,       //宝箱一
        Chest_2 = 1,     //宝箱二
        Chest_3 = 2,     //宝箱三
        Chest_4 = 3,    //  宝箱4
        FirstWin = 4,           //首胜宝箱
        WinStreak = 5           //连胜/FullCombo宝箱

    }


    public class ChestRewardData
    {


        public bool isChest;
        public ChestSkinType chestSkinType;
        public List<RewardData> rewardDatas;

        public Action OnChestOpen;

        public Action OnClaim;


        public object userData;

        public string sourcePage;


        public bool skipChestOpenAnimation;

        public bool allowDoubleClaim;

        public bool showClaimButton = true;

        public bool applyRewardsOnClaim = true;

        public bool useShopChest;

        public int shopChestSkinIndex;

        public ChestRewardData(bool isChest, ChestSkinType skinType, List<RewardData> rewardDatas, Action callback = null, string sourcePage = "", bool skipChestOpenAnimation = false, bool allowDoubleClaim = true)
        {
            this.isChest = isChest;
            this.chestSkinType = skinType;
            this.rewardDatas = rewardDatas;
            this.OnChestOpen = callback;
            this.sourcePage = sourcePage;
            this.skipChestOpenAnimation = skipChestOpenAnimation;
            this.allowDoubleClaim = allowDoubleClaim;
        }

        //跳过宝箱动画，当个奖励
        public ChestRewardData(RewardData rewardData, string sourcePage = "", bool skipChestOpenAnimation = false, bool allowDoubleClaim = true)
        {
            this.rewardDatas = new List<RewardData>();
            rewardDatas.Add(rewardData);
            this.sourcePage = sourcePage;
            this.skipChestOpenAnimation = skipChestOpenAnimation;
            this.allowDoubleClaim = allowDoubleClaim;
        }
    }

    public class RewardFlyTargetData
    {
        public object target;
        public Action<RewardData> onRewardArrived;
        public Action onAllArrived;

        public RewardFlyTargetData(object target, Action<RewardData> onRewardArrived = null, Action onAllArrived = null)
        {
            this.target = target;
            this.onRewardArrived = onRewardArrived;
            this.onAllArrived = onAllArrived;
        }
    }

    public class ClaimRewardsUIPanel : UGuiForm
    {
        #region Auto-Generated Fields
        private const string Anima_Idle = "idle";
        private const string Anima_Open = "open";

        private static readonly string[] ChestSkinNames =
        {
            "fenSe",
            "shenLanSe",
            "zongSe",
            "lvSe",
            "lanSe",
            "ziSe",
        };


        private static readonly string[] GiftSkinName =
        {
            "lanSe",
            "hongSe",
            "ziSe"
        };

        private static readonly string[] ShopChestSkinNames =
        {
            "lhz4",
            "lhz2",
            "lhz1",
            "lhz3",
        };
        [SerializeField] private RectTransform m_TitleGroupRT;

        [SerializeField] private CanvasGroup m_canvasGroupRoot;
        [SerializeField] private RectTransform m_Claim2BtnRT;
        [SerializeField] private RectTransform m_ClaimBtnRT;

        [SerializeField] private List<UI_ItemProp> m_ItemPropList;

        [SerializeField] private RectTransform m_chestRoot;

        [SerializeField] private SkeletonGraphic m_skgChest;
        [SerializeField] private SkeletonGraphic m_skgGift;
        [SerializeField] private SkeletonGraphic m_skgShopChest;

        #endregion



        private ChestRewardData m_ChestRewardData;
        private bool m_IsClaiming;
        private DG.Tweening.Sequence m_OpenSequence;

        // 不同数量时的布局位置
        private static readonly Vector2[][] Layouts = new[]
        {
            new[] { Vector2.zero },                                                     // 1个
            new[] { new Vector2(-210, 0), new Vector2(210, 0) },                        // 2个
            new[] { new Vector2(-210, 200), new Vector2(0,200 - 210 * Mathf.Sqrt(3)), new Vector2(210, 200) }, // 3个
        };

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            #region Auto-Generated Bindings
            m_Claim2BtnRT.GetComponentInChildren<Button>().AddSafeClick(OnClickClaim2);
            m_ClaimBtnRT.GetComponentInChildren<Button>().AddSafeClick(OnClickClaim);


            #endregion
        }


        protected override void OnOpen(object userData)
        {
            EnsureRootCanvasGroup();
            m_IsClaiming = false;
            m_canvasGroupRoot.alpha = 1f;
            m_canvasGroupRoot.blocksRaycasts = true;
            m_Claim2BtnRT.localScale = Vector3.zero;
            m_ClaimBtnRT.localScale = Vector3.zero;
            m_chestRoot.localScale = Vector3.one;
            m_OpenSequence?.Kill();
            base.OnOpen(userData);

            foreach (var item in m_ItemPropList)
            {
                item.transform.localScale = Vector3.zero;
                ResetItemCanvasGroup(item);
            }

            m_ChestRewardData = userData as ChestRewardData;
            if (m_ChestRewardData == null || m_ChestRewardData.rewardDatas.Count == 0)
            {
                Log.Warning("[ClaimRewards] rewardDatas 为空");
                return;
            }

            bool showClaim2Button = m_ChestRewardData.showClaimButton
                                    && m_ChestRewardData.allowDoubleClaim
                                    && ShouldShowClaim2Button();
            m_Claim2BtnRT.gameObject.SetActive(showClaim2Button);
            m_ClaimBtnRT.gameObject.SetActive(m_ChestRewardData.showClaimButton);
            m_TitleGroupRT.gameObject.SetActive(showClaim2Button);

            if (m_ChestRewardData.skipChestOpenAnimation)
            {
                m_skgChest.gameObject.SetActive(false);
                m_skgGift.gameObject.SetActive(false);
                if (m_skgShopChest != null)
                    m_skgShopChest.gameObject.SetActive(false);

                m_chestRoot.localScale = Vector3.zero;
                RefreshItemDisplay();

                AutoClaim();
                return;
            }




            // 根据 isChest 显示宝箱或礼盒，并设置皮肤
            bool isChest = m_ChestRewardData.isChest;
            bool requestShopChest = m_ChestRewardData.useShopChest || m_ChestRewardData.sourcePage == "Shop";
            bool useShopChest = requestShopChest && m_skgShopChest != null;
            m_skgChest.gameObject.SetActive(!useShopChest && isChest);
            m_skgGift.gameObject.SetActive(!requestShopChest && !isChest);
            if (m_skgShopChest != null)
                m_skgShopChest.gameObject.SetActive(useShopChest);

            var skg = useShopChest || requestShopChest ? m_skgShopChest : isChest ? m_skgChest : m_skgGift;
            var skinNames = useShopChest || requestShopChest ? ShopChestSkinNames : isChest ? ChestSkinNames : GiftSkinName;
            int rawSkinIndex = useShopChest || requestShopChest ? m_ChestRewardData.shopChestSkinIndex : (int)m_ChestRewardData.chestSkinType;
            if (skg == null)
            {
                Log.Warning("[ClaimRewards] Shop chest SkeletonGraphic is missing.");
                return;
            }

            int skinIndex = Mathf.Clamp(rawSkinIndex, 0, skinNames.Length - 1);
            skg.Skeleton.SetSkin(skinNames[skinIndex]);
            skg.Skeleton.SetSlotsToSetupPose();
            skg.AnimationState.Apply(skg.Skeleton);

            // 播放 idle，等1秒后播 open，再等0.3秒缩放 chestRoot 并刷新道具
            skg.AnimationState.SetAnimation(0, Anima_Idle, false);
            float idleDuration = GetAnimationDuration(skg, Anima_Idle, 1f);
            float openDuration = useShopChest ? 1.2f : 0.3f;

            skg.AnimationState.SetAnimation(0, Anima_Idle, false);
            PlayUISound(SoundId.SFX_ClaimReward);
            m_OpenSequence = DOTween.Sequence();
            m_OpenSequence.AppendInterval(idleDuration);
            m_OpenSequence.AppendCallback(() =>
            {
                skg.AnimationState.SetAnimation(0, Anima_Open, false);
                PlayUISound(SoundId.UI_OpenBox);
            });
            m_OpenSequence.AppendInterval(openDuration);
            m_OpenSequence.AppendCallback(() =>
            {
                m_chestRoot.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                RefreshItemDisplay();
            });
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_canvasGroupRoot?.DOKill();
            m_Claim2BtnRT?.DOKill();
            m_ClaimBtnRT?.DOKill();
            m_chestRoot?.DOKill();
            m_OpenSequence?.Kill();
            if (m_ItemPropList != null)
            {
                foreach (var item in m_ItemPropList)
                {
                    item?.transform.DOKill();
                    item?.GetComponent<RectTransform>()?.DOKill();
                }
            }

            base.OnClose(isShutdown, userData);
        }

        private void RefreshItemDisplay()
        {
            int count = Mathf.Min(m_ChestRewardData.rewardDatas.Count, m_ItemPropList.Count);
            int layoutIndex = Mathf.Clamp(count - 1, 0, Layouts.Length - 1);
            var positions = Layouts[layoutIndex];

            for (int i = 0; i < m_ItemPropList.Count; i++)
            {
                if (i < count)
                {
                    m_ItemPropList[i].gameObject.SetActive(true);

                    var reward = m_ChestRewardData.rewardDatas[i];
                    if (GameEntry.CustomConfig.PropDataBaseSO.TryGetPropData(reward.propType, out PropData propData))
                    {
                        m_ItemPropList[i].SetIcon(propData.sprite_big, true);
                        m_ItemPropList[i].SetContent("x" + reward.Count);
                        m_ItemPropList[i].Init();
                        m_ItemPropList[i].DoPlayMove(positions[i]);
                    }
                }
                else
                {
                    m_ItemPropList[i].gameObject.SetActive(false);
                }
            }

            m_ChestRewardData?.OnChestOpen?.Invoke();

            m_Claim2BtnRT.DOScale(0.8f, 0.3f).SetDelay(count * 0.2f);
            m_ClaimBtnRT.DOScale(0.8f, 0.3f).SetDelay(count * 0.2f + 0.1f);
        }

        private void RefreshItemCountDisplay(int multiplier)
        {
            if (m_ChestRewardData?.rewardDatas == null)
                return;

            int count = Mathf.Min(m_ChestRewardData.rewardDatas.Count, m_ItemPropList.Count);
            for (int i = 0; i < count; i++)
            {
                if (m_ItemPropList[i] == null || !m_ItemPropList[i].gameObject.activeSelf)
                    continue;

                var reward = m_ChestRewardData.rewardDatas[i];
                m_ItemPropList[i].SetContent("x" + reward.Count * multiplier);
            }
        }

        private bool ShouldShowClaim2Button()
        {
            if (m_ChestRewardData == null || m_ChestRewardData.sourcePage != "FirstWin")
                return true;

            return AdsServerConfig.Common.ShowFirstWinClaim2Button;
        }

        private static float GetAnimationDuration(SkeletonGraphic skg, string animationName, float fallback)
        {
            if (skg == null || skg.Skeleton == null || skg.Skeleton.Data == null)
                return fallback;

            Spine.Animation animation = skg.Skeleton.Data.FindAnimation(animationName);
            return animation != null ? animation.Duration : fallback;
        }

        // ── Button Handlers ──

        /// <summary>
        /// 广告双倍领取
        /// </summary>
        private void OnClickClaim2()
        {
            if (m_IsClaiming) return;

            PlayUISound(SoundId.UI_Click);

            AdsAnalytics.EventWithName("ClickClaim2",
                ("lv", GetCurrentLevel()),
                ("scene", string.IsNullOrEmpty(m_ChestRewardData?.sourcePage) ? "Unknown" : m_ChestRewardData.sourcePage));

            Action action = () =>
            {
                if (m_IsClaiming) return;
                m_IsClaiming = true;
                RefreshItemCountDisplay(2);
                ApplyRewards(2);
                AdsAnalytics.EventWithName("Claim2Success",
                    ("lv", GetCurrentLevel()),
                    ("scene", string.IsNullOrEmpty(m_ChestRewardData?.sourcePage) ? "Unknown" : m_ChestRewardData.sourcePage));
                PlayClaimAnimation().Forget();
            };

            if (GameEntry.GMMode)
            {
                action?.Invoke();
                return;
            }

            AdsManager.ShowRewardedAd(action);
        }

        /// <summary>
        /// 普通领取
        /// </summary>
        private void OnClickClaim()
        {
            if (m_IsClaiming) return;

            m_IsClaiming = true;
            PlayUISound(SoundId.UI_Click);
            ApplyRewards(1);

            PlayClaimAnimation().Forget();
        }


        private async void AutoClaim()
        {
            await UniTask.Delay(1000);
            ApplyRewards(1);
            PlayClaimAnimation().Forget();
        }

        private void ApplyRewards(int multiplier)
        {
            if (m_ChestRewardData.rewardDatas == null) return;
            PlayUISound(SoundId.SFX_ItemReward);
            if (!m_ChestRewardData.applyRewardsOnClaim)
            {
                Log.Info("[ClaimRewards] Rewards already applied before claim panel.");
                return;
            }

            foreach (var reward in m_ChestRewardData.rewardDatas)
            {
                int amount = reward.Count * multiplier;
                switch (reward.propType)
                {
                    case PropType.Hint:
                        AddHintCount(amount);
                        break;
                    default:
                        Log.Warning("[ClaimRewards] 未处理的道具类型: {0}", reward.propType);
                        break;
                }
            }

            Log.Info("[ClaimRewards] 领取奖励完成，倍率: {0}", multiplier);
        }

        private int GetCurrentLevel()
        {
            return GameEntry.GameManager != null ? GameEntry.GameManager.GetCurrentLevel() : 0;
        }

        private void AddHintCount(int amount)
        {
            if (GameEntry.GameManager == null || GameEntry.SaveData == null)
            {
                return;
            }

            GameEntry.SubGames?.Get(GameEntry.GameManager.CurrentGameMode)?.TryGrantProp(PropType.Hint, amount);
        }



        private async UniTask PlayClaimAnimation()
        {
            // 这里可以添加一些额外的动画效果，比如奖励飞出等
            EnsureRootCanvasGroup();

            await m_canvasGroupRoot.DOFade(0f, 0.2f).SetEase(Ease.OutQuad).ToUniTask();

            RectTransform targetParent = GetFirstActiveItemParent();
            Vector2 targetPos = GetClaimTargetAnchoredPosition(targetParent);
            RewardFlyTargetData flyTargetData = m_ChestRewardData.userData as RewardFlyTargetData;
            DG.Tweening.Sequence flySequence = DOTween.Sequence();
            int flyIndex = 0;

            for (int i = 0; i < m_ItemPropList.Count; i++)
            {
                var item = m_ItemPropList[i];
                if (item == null || !item.gameObject.activeInHierarchy)
                    continue;

                RectTransform itemRT = item.GetComponent<RectTransform>();
                if (itemRT == null)
                    continue;

                PrepareItemCanvasGroupForClaim(item);
                itemRT.DOKill();
                itemRT.SetAsLastSibling();

                if (itemRT.parent != targetParent && targetParent != null)
                {
                    Vector3 worldPosition = itemRT.position;
                    itemRT.SetParent(targetParent, true);
                    itemRT.position = worldPosition;
                }

                Vector2 startPos = itemRT.anchoredPosition;
                Vector2 liftPos = startPos + Vector2.up * 90f;
                float delay = flyIndex * 0.3f;
                RewardData reward = GetRewardData(flyIndex);

                DG.Tweening.Sequence itemSequence = DOTween.Sequence();
                _ = itemSequence.Append(itemRT.DOAnchorPos(liftPos, 0.15f).SetEase(Ease.OutQuad));
                _ = itemSequence.Append(itemRT.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.InCubic));
                _ = itemSequence.Join(itemRT.DOScale(0f, 0.3f).SetEase(Ease.InBack));

                _ = itemSequence.onComplete = () =>
                {
                    try
                    {
                        m_ChestRewardData?.OnClaim?.Invoke();
                        flyTargetData?.onRewardArrived?.Invoke(reward);
                    }
                    catch (Exception e)
                    {
                        Log.Error("[ClaimRewards] Claim callback failed: {0}", e);
                    }

                    PlayUISound(SoundId.UI_CoinsReward);
                };

                _ = flySequence.Insert(delay, itemSequence);
                flyIndex++;
            }


            try
            {
                if (flyIndex > 0)
                    await flySequence.ToUniTask();

                flyTargetData?.onAllArrived?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error("[ClaimRewards] Fly claim animation failed: {0}", e);
            }
            finally
            {
                if (gameObject.activeInHierarchy)
                    Close(false);
            }
        }

        private RewardData GetRewardData(int index)
        {
            if (m_ChestRewardData?.rewardDatas == null || index < 0 || index >= m_ChestRewardData.rewardDatas.Count)
                return null;

            return m_ChestRewardData.rewardDatas[index];
        }

        private RectTransform GetFirstActiveItemParent()
        {
            foreach (var item in m_ItemPropList)
            {
                if (item != null && item.gameObject.activeInHierarchy)
                    return item.transform.parent as RectTransform;
            }

            return transform as RectTransform;
        }

        private Vector2 GetClaimTargetAnchoredPosition(RectTransform parent)
        {
            if (parent == null || m_ChestRewardData?.userData == null)
                return Vector2.zero;

            Vector2 screenPos;
            object targetData = m_ChestRewardData.userData is RewardFlyTargetData flyTargetData
                ? flyTargetData.target
                : m_ChestRewardData.userData;

            switch (targetData)
            {
                case Vector2 vector2:
                    screenPos = vector2;
                    break;
                case Vector3 vector3:
                    screenPos = vector3;
                    break;
                case RectTransform rectTransform:
                    screenPos = RectTransformUtility.WorldToScreenPoint(GetUICamera(), rectTransform.position);
                    break;
                case Transform trans:
                    screenPos = RectTransformUtility.WorldToScreenPoint(GetUICamera(), trans.position);
                    break;
                default:
                    return Vector2.zero;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, GetUICamera(), out Vector2 localPos);
            return localPos;
        }

        private Camera GetUICamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private void EnsureRootCanvasGroup()
        {
            if (m_canvasGroupRoot == null)
                m_canvasGroupRoot = GetComponent<CanvasGroup>();
        }

        private void ResetItemCanvasGroup(UI_ItemProp item)
        {
            if (item == null || !item.TryGetComponent(out CanvasGroup canvasGroup))
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.ignoreParentGroups = false;
        }

        private void PrepareItemCanvasGroupForClaim(UI_ItemProp item)
        {
            if (item == null)
                return;

            if (!item.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = item.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            canvasGroup.ignoreParentGroups = true;
        }


    }
}
