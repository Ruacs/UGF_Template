using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class ShopItemView : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string PropItemPrefabAssetPath = "Assets/AssetsPackage/Prefabs/UI/Shop_PropItem.prefab";
#endif

        [SerializeField] private Image m_Bg;
        [SerializeField] private Image m_Icon;
        [SerializeField] private GameObject m_IconShadowGO;
        [SerializeField] private GameObject m_RemoveAdsTipsGO;
        [SerializeField] private Image m_CornerMark;
        [SerializeField] private TMP_Text m_TmpBonus;
        [SerializeField] private TMP_Text m_TmpContent;
        [SerializeField] private TMPStyleApplier m_TmpContentStyleApplier;
        [SerializeField] private Button m_BtnBuy;
        [SerializeField] private Image m_BtnBuyImage;
        [SerializeField] private TMP_Text m_TmpPrice;
        [SerializeField] private LayoutElement m_ItemLayout;

 
        [SerializeField] private GameObject m_RewardBox;
        [SerializeField] private RectTransform m_NoAdsBox;
        [SerializeField] private RectTransform m_PropBox;
        [SerializeField] private LayoutElement m_NoAdsBoxLayout;
        [SerializeField] private LayoutElement m_PropBoxLayout;
        [SerializeField] private GameObject m_PropItemPrefab;
        [SerializeField] private Image m_AddTemplate;
        

        [Header("Reward Layout")]
        [SerializeField] private float m_NoAdsBoxSmallWidth = 200;
        [SerializeField] private Vector2 m_PropItemSmallSize = new(56f, 56f);
        [SerializeField] private Vector2 m_PropItemLargeSize = new(76f, 76f);
        [SerializeField] private Vector2 m_AddSmallSize = new(20f, 20f);
        [SerializeField] private Vector2 m_AddLargeSize = new(28f, 28f);
 
        [Header("No Ads")]
        [SerializeField] private Vector2 m_NoAdsViewSize = new(1030f, 260f);
        [SerializeField] private Vector2 m_NoAdsIconPosition = new(40f, 40f);
        [SerializeField] private Vector2 m_NoAdsTextPosition = new(80f, 40f);
        [SerializeField] private Vector2 m_BuyButtonPosition = new(0f, 0f);

        [Header("View Events")]
        [Tooltip("View event rules triggered by shop item config.")]
        [SerializeField] private List<ShopItemViewEventRule> m_ViewEventRules = new();

        private ShopItemConfigSO m_Config;
        private Action<ShopItemConfigSO> m_OnBuy;
        private bool m_StyleAllowsRewardBox = true;
        private readonly List<GameObject> m_GeneratedRewardObjects = new();
        private Vector2 m_CurrentPropItemSize;
        private Vector2 m_CurrentAddSize;
        private RectTransform m_RectTransform;
        private RectTransform m_BuyButtonRect;
        private Vector2 m_DefaultViewSize;
        private Vector2 m_DefaultIconPosition;
        private Vector2 m_DefaultTextPosition;
        private Vector2 m_DefaultBuyButtonPosition;
        private float m_DefaultPreferredWidth = -1f;
        private float m_DefaultPreferredHeight = -1f;
        private bool m_DefaultIconShadowActive;
        private bool m_HasDefaultNoAdsLayoutValues;
        private bool m_IsBuyClickBound;

        public ShopItemConfigSO Config => m_Config;

        private void Awake()
        {
            BindReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_PropItemPrefab == null)
                m_PropItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PropItemPrefabAssetPath);
        }
#endif

        private void OnDestroy()
        {
            ClearGeneratedRewardObjects();
        }

        public void SetData(ShopItemConfigSO config, Action<ShopItemConfigSO> onBuy)
        {
            BindReferences();

            m_Config = config;
            m_OnBuy = onBuy;

            ApplyStyle(config != null ? config.style : null);
            ApplyContent(config);

            if (m_BtnBuy != null)
            {
                if (!m_IsBuyClickBound)
                {
                    m_BtnBuy.onClick.AddListener(HandleBuyClick);
                    m_IsBuyClickBound = true;
                }
            }
        }


        public void DOScaleX(float from, float to, float delay = 0f)
        {
            transform.DOKill();
            transform.DOScaleX(to, 0.5f).From(from).SetDelay(delay).SetEase(Ease.OutBack);
        }

        private void ResfreshNoAdsItemView()
        {
            bool isStandaloneNoAds = m_Config != null &&
                HasReward(m_Config, ShopRewardTarget.NoAds) &&
                GetPropRewards(m_Config).Count == 0;

            bool useNoAdsLayout = isStandaloneNoAds;
            if (useNoAdsLayout)
            {
                ApplyNoAdsLayout();
            }
            else
            {
                RestoreDefaultLayout();
            }
        }

        private void ApplyNoAdsLayout()
        {
            if (m_RewardBox != null)
                m_RewardBox.SetActive(false);

            SetIconShadowActive(true);

            SetItemSize(m_NoAdsViewSize);

            if (m_Icon != null)
            {
                m_Icon.rectTransform.anchoredPosition = m_NoAdsIconPosition;
                m_Icon.SetNativeSize();
            }

            if (m_TmpContent != null)
                m_TmpContent.rectTransform.anchoredPosition = m_NoAdsTextPosition;

            if (m_BuyButtonRect != null)
                m_BuyButtonRect.anchoredPosition = m_BuyButtonPosition;
        }

        private void RestoreDefaultLayout()
        {
            if (!m_HasDefaultNoAdsLayoutValues)
                return;

            SetIconShadowActive(m_DefaultIconShadowActive);

            SetItemSize(m_DefaultViewSize);

            if (m_ItemLayout != null)
            {
                m_ItemLayout.preferredWidth = m_DefaultPreferredWidth;
                m_ItemLayout.preferredHeight = m_DefaultPreferredHeight;
            }

            if (m_Icon != null)
                m_Icon.rectTransform.anchoredPosition = m_DefaultIconPosition;

            if (m_TmpContent != null)
                m_TmpContent.rectTransform.anchoredPosition = m_DefaultTextPosition;

            if (m_BuyButtonRect != null)
                m_BuyButtonRect.anchoredPosition = m_DefaultBuyButtonPosition;
        }

        private void SetItemSize(Vector2 size)
        {
            if (m_RectTransform != null)
                m_RectTransform.sizeDelta = size;

            if (m_ItemLayout != null)
            {
                m_ItemLayout.preferredWidth = size.x;
                m_ItemLayout.preferredHeight = size.y;
            }
        }

        private void SetIconShadowActive(bool active)
        {
            if (m_IconShadowGO != null)
                m_IconShadowGO.SetActive(active);

            if (m_RemoveAdsTipsGO != null)
                m_RemoveAdsTipsGO.SetActive(active);
        }

        public void RefreshState()
        {
            if (m_Config == null || m_BtnBuy == null || GameEntry.Shop == null)
                return;

            m_BtnBuy.interactable = GameEntry.Shop.ValidateBeforeBuy(m_Config) == ShopBuyResult.Success;
        }

        public void RefreshView()
        {
            ApplyContent(m_Config);
        }

        public void InitLocalization()
        {
            UnityGameFramework.Runtime.UIStringKey[] texts = GetComponentsInChildren<UnityGameFramework.Runtime.UIStringKey>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                UnityGameFramework.Runtime.UIStringKey textKey = texts[i];
                if (textKey.TryGetComponent(out Text textCom))
                {
                    textCom.text = GameEntry.Localization.GetString(textKey.Key);
                }
                else if (textKey.TryGetComponent(out TMP_Text tmpTextCom))
                {
                    tmpTextCom.text = GameEntry.Localization.GetString(textKey.Key);
                }
            }

            UILocalizedImageKey[] images = GetComponentsInChildren<UILocalizedImageKey>(true);
            for (int i = 0; i < images.Length; i++)
            {
                images[i].ApplyLocalization();
            }

            RefreshView();
        }

        private void ApplyStyle(ShopItemStyleConfigSO style)
        {
            if (style == null)
            {
                m_StyleAllowsRewardBox = true;
                if (m_TmpContentStyleApplier != null)
                    m_TmpContentStyleApplier.SetStyleKey(string.Empty);
                return;
            }

            if (m_Bg != null && style.background != null)
                m_Bg.sprite = style.background;

            if (m_BtnBuyImage != null && style.buyButtonBackground != null)
                m_BtnBuyImage.sprite = style.buyButtonBackground;

            if (m_CornerMark != null)
            {
                m_CornerMark.gameObject.SetActive(style.showCornerMark);
                if (style.cornerMarkBackground != null)
                    m_CornerMark.sprite = style.cornerMarkBackground;
            }

            if (m_RewardBox != null)
            {
                m_StyleAllowsRewardBox = style.showRewardBox;
                m_RewardBox.SetActive(style.showRewardBox);
            }



            if (m_TmpContent != null)
            {
                m_TmpContent.color = style.contentTextColor;
                if (m_TmpContentStyleApplier != null)
                    m_TmpContentStyleApplier.SetStyleKey(style.contentTextStyleKey);
            }

            if (m_TmpPrice != null)
                m_TmpPrice.color = style.priceTextColor;

            if (m_TmpBonus != null)
                m_TmpBonus.color = style.bonusTextColor;
        }

        private void ApplyContent(ShopItemConfigSO config)
        {
            if (config == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(config.visible);

            if (m_Icon != null)
            {
                m_Icon.sprite = config.icon;
                m_Icon.enabled = config.icon != null;
            }

            if (m_TmpContent != null)
                m_TmpContent.text = GetLocalizedText(config.contentLocalizationKey, config.contentText);

            if (m_TmpPrice != null)
                m_TmpPrice.text = GetPriceText(config);

            if (m_TmpBonus != null)
            {
                m_TmpBonus.gameObject.SetActive(config.showBonus);
                m_TmpBonus.text = GetLocalizedText(config.bonusLocalizationKey, config.bonusText);
            }

            if (m_CornerMark != null)
            {
                bool styleAllowsCornerMark = config.style == null || config.style.showCornerMark;
                m_CornerMark.gameObject.SetActive(styleAllowsCornerMark && config.showBonus);
            }

            ApplyRewards(config);
            ResfreshNoAdsItemView();
            ApplyViewEventRules(config);
            RefreshState();
        }

        private void ApplyViewEventRules(ShopItemConfigSO config)
        {
            if (m_ViewEventRules == null)
                return;

            for (int i = 0; i < m_ViewEventRules.Count; i++)
            {
                ShopItemViewEventRule rule = m_ViewEventRules[i];
                if (rule == null)
                    continue;

                if (rule.IsMatch(config))
                    rule.onMatched?.Invoke();
                else
                    rule.onUnmatched?.Invoke();
            }
        }

        private void ApplyRewards(ShopItemConfigSO config)
        {
            if (m_RewardBox == null)
                return;

            bool hasNoAds = HasReward(config, ShopRewardTarget.NoAds);
            List<ShopRewardData> propRewards = GetPropRewards(config);
            bool hasProp = propRewards.Count > 0;
            bool showRewardBox = m_StyleAllowsRewardBox && (hasNoAds || hasProp);

            m_RewardBox.SetActive(showRewardBox);
            if (!showRewardBox)
                return;

            if (m_NoAdsBox != null)
                m_NoAdsBox.gameObject.SetActive(hasNoAds);

            if (m_PropBox != null)
                m_PropBox.gameObject.SetActive(hasProp);

            ApplyRewardBoxLayout(hasNoAds, hasProp);
            ApplyPropRewardItems(propRewards);

            RectTransform rewardBoxRect = m_RewardBox.transform as RectTransform;
            if (rewardBoxRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rewardBoxRect);
        }

        private void ApplyRewardBoxLayout(bool hasNoAds, bool hasProp)
        {
            bool propLarge = hasProp && !hasNoAds;
            m_CurrentPropItemSize = propLarge ? m_PropItemLargeSize : m_PropItemSmallSize;
            m_CurrentAddSize = propLarge ? m_AddLargeSize : m_AddSmallSize;

            if (m_NoAdsBoxLayout != null)
            {
                m_NoAdsBoxLayout.preferredWidth = hasNoAds && hasProp ? m_NoAdsBoxSmallWidth : -1f;
                m_NoAdsBoxLayout.flexibleWidth = hasNoAds && !hasProp ? 1f : 0f;
            }

            if (m_PropBoxLayout != null)
            {
                m_PropBoxLayout.preferredWidth = -1f;
                m_PropBoxLayout.flexibleWidth = hasProp ? 1f : 0f;
            }
        }

        private void ApplyPropRewardItems(List<ShopRewardData> propRewards)
        {
            ClearGeneratedRewardObjects();

            if (m_AddTemplate != null)
                m_AddTemplate.gameObject.SetActive(false);

            if (m_PropBox == null || m_PropItemPrefab == null)
                return;

            for (int i = 0; i < propRewards.Count; i++)
            {
                GameObject propItem = Instantiate(m_PropItemPrefab, m_PropBox);
                propItem.name = $"PropItem_{i + 1}";
                propItem.SetActive(true);
                ApplyPropItem(propItem, propRewards[i]);
                SetLayoutSize(GetOrAddLayoutElement(propItem), m_CurrentPropItemSize);
                m_GeneratedRewardObjects.Add(propItem);

                if (i < propRewards.Count - 1 && m_AddTemplate != null)
                {
                    Image addItem = Instantiate(m_AddTemplate, m_PropBox);
                    addItem.name = $"Add_{i + 1}";
                    addItem.gameObject.SetActive(true);
                    SetLayoutSize(GetOrAddLayoutElement(addItem.gameObject), m_CurrentAddSize);
                    m_GeneratedRewardObjects.Add(addItem.gameObject);
                }
            }
        }

        private void ApplyPropItem(GameObject itemObject, ShopRewardData reward)
        {
            if (itemObject == null || reward == null)
                return;

            Sprite sprite = null;
            TryGetPropSprite(reward.propType, out sprite);
            string countText = $"x{Mathf.Max(0, reward.count)}";

            if (itemObject.TryGetComponent(out UI_ItemProp itemProp))
            {
                itemProp.RefreshUI(sprite, countText);
            }
        }

        private void ClearGeneratedRewardObjects()
        {
            for (int i = 0; i < m_GeneratedRewardObjects.Count; i++)
            {
                if (m_GeneratedRewardObjects[i] != null)
                {
                    m_GeneratedRewardObjects[i].SetActive(false);
                    Destroy(m_GeneratedRewardObjects[i]);
                }
            }

            m_GeneratedRewardObjects.Clear();
        }

        private static bool HasReward(ShopItemConfigSO config, ShopRewardTarget target)
        {
            if (config == null || config.rewards == null)
                return false;

            for (int i = 0; i < config.rewards.Count; i++)
            {
                if (config.rewards[i] != null && config.rewards[i].target == target)
                    return true;
            }

            return false;
        }

        private static List<ShopRewardData> GetPropRewards(ShopItemConfigSO config)
        {
            List<ShopRewardData> rewards = new();
            if (config == null || config.rewards == null)
                return rewards;

            for (int i = 0; i < config.rewards.Count; i++)
            {
                ShopRewardData reward = config.rewards[i];
                if (reward == null)
                    continue;

                if (reward.target == ShopRewardTarget.GameProp ||
                    reward.target == ShopRewardTarget.CommonProp)
                {
                    rewards.Add(reward);
                }
            }

            return rewards;
        }

        private static bool TryGetPropSprite(PropType propType, out Sprite sprite)
        {
            sprite = null;
            return false;
        }

        private static void SetLayoutSize(LayoutElement layout, Vector2 size)
        {
            if (layout == null)
                return;

            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
        }

        private static string GetPriceText(ShopItemConfigSO config)
        {
            if (GameEntry.Shop != null)
                return GameEntry.Shop.GetPriceText(config);

            return config.GetFallbackPriceText();
        }

        private static string GetLocalizedText(string localizationKey, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(localizationKey) && GameEntry.Localization != null)
                return GameEntry.Localization.GetString(localizationKey);

            return fallback;
        }

        private void HandleBuyClick()
        {
            GameEntry.Sound.PlaySound(SoundId.UI_Click);
            m_OnBuy?.Invoke(m_Config);
        }


        public void OnRemoveAdsTipsClick()
        {
            GameEntry.Sound.PlayUISound(SoundId.UI_Click);
        }

        private void BindReferences()
        {
            m_RectTransform ??= GetComponent<RectTransform>();
            m_ItemLayout ??= GetComponent<LayoutElement>();
            m_Bg ??= transform.Find("bg")?.GetComponent<Image>();
            m_Icon ??= transform.Find("Icon")?.GetComponent<Image>();
            m_IconShadowGO ??= transform.Find("IconShadow")?.gameObject
                ?? transform.Find("Icon_Shadow")?.gameObject
                ?? transform.Find("IconShadowGO")?.gameObject;
            m_RemoveAdsTipsGO ??= transform.Find("RemoveAdsTips")?.gameObject
                ?? transform.Find("RemoveAdsTipsGO")?.gameObject
                ?? transform.Find("RemoveAds_Tips")?.gameObject;
            m_CornerMark ??= transform.Find("corner_mark")?.GetComponent<Image>();
            m_TmpBonus ??= transform.Find("corner_mark/tmp_bonus")?.GetComponent<TMP_Text>();
            m_TmpContent ??= transform.Find("tmp_Content")?.GetComponent<TMP_Text>();
            m_TmpContentStyleApplier ??= m_TmpContent != null ? m_TmpContent.GetComponent<TMPStyleApplier>() : null;

            Transform buy = transform.Find("btn_Buy");
            if (buy != null)
            {
                m_BtnBuy ??= buy.GetComponent<Button>();
                m_BtnBuyImage ??= buy.GetComponent<Image>();
                m_TmpPrice ??= buy.Find("tmp_price")?.GetComponent<TMP_Text>();
                m_BuyButtonRect ??= buy as RectTransform;
            }

            m_RewardBox ??= transform.Find("RewardBox")?.gameObject;
            if (m_RewardBox != null)
            {
                m_NoAdsBox ??= m_RewardBox.transform.Find("NoAdsBox") as RectTransform;
                m_PropBox ??= m_RewardBox.transform.Find("PropBox") as RectTransform;
                m_NoAdsBoxLayout ??= m_NoAdsBox != null ? GetOrAddLayoutElement(m_NoAdsBox.gameObject) : null;
                m_PropBoxLayout ??= m_PropBox != null ? GetOrAddLayoutElement(m_PropBox.gameObject) : null;

                if (m_AddTemplate == null && m_PropBox != null)
                    m_AddTemplate = m_PropBox.Find("Add")?.GetComponent<Image>();
            }

            CacheDefaultNoAdsLayoutValues();
        }

        private void CacheDefaultNoAdsLayoutValues()
        {
            if (m_HasDefaultNoAdsLayoutValues)
                return;

            if (m_RectTransform == null || m_Icon == null || m_TmpContent == null || m_BuyButtonRect == null)
                return;

            m_DefaultViewSize = m_RectTransform.sizeDelta;
            m_DefaultIconPosition = m_Icon.rectTransform.anchoredPosition;
            m_DefaultTextPosition = m_TmpContent.rectTransform.anchoredPosition;
            m_DefaultBuyButtonPosition = m_BuyButtonRect.anchoredPosition;
            m_DefaultIconShadowActive = m_IconShadowGO != null && m_IconShadowGO.activeSelf;

            if (m_ItemLayout != null)
            {
                m_DefaultPreferredWidth = m_ItemLayout.preferredWidth;
                m_DefaultPreferredHeight = m_ItemLayout.preferredHeight;
            }

            m_HasDefaultNoAdsLayoutValues = true;
        }

        private static LayoutElement GetOrAddLayoutElement(GameObject target)
        {
            if (target == null)
                return null;

            LayoutElement layout = target.GetComponent<LayoutElement>();
            return layout != null ? layout : target.AddComponent<LayoutElement>();
        }
    }
}




