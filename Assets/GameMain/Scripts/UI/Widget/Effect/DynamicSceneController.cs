using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public enum DynamicScenePhase
    {
        Day,
        Dusk,
        Night
    }

    public class DynamicSceneController : MonoBehaviour
    {
        [Serializable]
        public class GraphicPhaseSprites
        {
            public string name;
            public Graphic graphic;
            public Sprite daySprite;
            public Sprite duskSprite;
            public Sprite nightSprite;
            [Range(0f, 1f)] public float dayAlpha = 1f;
            [Range(0f, 1f)] public float duskAlpha = 1f;
            [Range(0f, 1f)] public float nightAlpha = 1f;
        }

        [Serializable]
        public class PhaseSprites
        {
            public Sprite day;
            public Sprite dusk;
            public Sprite night;
        }

        [Serializable]
        public class PhaseTint
        {
            public Color day = Color.white;
            public Color dusk = new Color(1f, 0.92f, 0.68f, 1f);
            public Color night = new Color(0.48f, 0.48f, 0.52f, 1f);
        }

        [Serializable]
        public class WaterMotion
        {
            public float daySpeed = 0.018f;
            public float duskSpeed = 0.024f;
            public float nightSpeed = 0.014f;
        }

        [Serializable]
        public class NoSpriteElementTints
        {
            public PhaseTint desk = new PhaseTint();
            public PhaseTint role = new PhaseTint();
            public PhaseTint rawWater = new PhaseTint();
            public PhaseTint fallback = new PhaseTint();
        }

        [Header("Time")]
        [SerializeField] private int m_DuskHour = 18;
        [SerializeField] private int m_NightHour = 19;
        [SerializeField] private float m_TransitionDuration = 1.5f;
        [SerializeField] private bool m_AnimateFirstApply;

        [Header("Auto Bind")]
        [SerializeField] private bool m_AutoBindOnAwake = true;
        [SerializeField] private GraphicPhaseSprites[] m_Graphics;
        [SerializeField] private RawImage m_RawWater;

        [Header("Phase Sprites")]
        [SerializeField] private PhaseSprites m_SkySprites;
        [SerializeField] private PhaseSprites m_SceneSprites;
        [SerializeField] private PhaseSprites m_WaterBgSprites;
        [SerializeField] private PhaseSprites m_Cloud1Sprites;
        [SerializeField] private PhaseSprites m_Cloud2Sprites;

        [Header("No Sprite Tints")]
        [SerializeField] private NoSpriteElementTints m_NoSpriteTints = new NoSpriteElementTints();

        [Header("Motion")]
        [SerializeField] private WaterMotion m_WaterMotion = new WaterMotion();

        private readonly Dictionary<Graphic, Graphic> m_TransitionOverlays = new Dictionary<Graphic, Graphic>();
        private DynamicScenePhase? m_CurrentPhase;
        private float m_CurrentRawWaterSpeed;

        private void Awake()
        {
            if (m_AutoBindOnAwake)
            {
                AutoBind();
            }
        }

        private void OnEnable()
        {
            UpdateScene(m_AnimateFirstApply);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(HandleScheduledPhaseChange));
        }

        private void Update()
        {
            if (m_RawWater == null || Mathf.Approximately(m_CurrentRawWaterSpeed, 0f))
            {
                return;
            }

            Rect uv = m_RawWater.uvRect;
            uv.x += m_CurrentRawWaterSpeed * Time.deltaTime;
            m_RawWater.uvRect = uv;
        }

        public void UpdateScene(bool animate = true)
        {
            DynamicScenePhase phase = GetPhase(DateTime.Now);
            ApplyPhase(phase, animate && m_CurrentPhase.HasValue);
            ScheduleNextPhaseCheck();
        }

        public void ApplyPhase(DynamicScenePhase phase, bool animate)
        {
            if (m_AutoBindOnAwake && (m_Graphics == null || m_Graphics.Length == 0))
            {
                AutoBind();
            }

            if (m_CurrentPhase == phase)
            {
                return;
            }

            m_CurrentPhase = phase;
            m_CurrentRawWaterSpeed = GetRawWaterSpeed(phase);

            if (m_Graphics == null)
            {
                return;
            }

            foreach (GraphicPhaseSprites item in m_Graphics)
            {
                if (item == null || item.graphic == null)
                {
                    continue;
                }

                Sprite targetSprite = GetTargetSprite(item, phase);
                Color targetColor = targetSprite != null ? Color.white : GetTargetTint(item, phase);
                float targetAlpha = GetTargetAlpha(item, phase);
                targetColor.a *= targetAlpha;
                ApplyGraphic(item.graphic, targetSprite, targetColor, animate);
            }
        }

        private void HandleScheduledPhaseChange()
        {
            UpdateScene(true);
        }

        private void ScheduleNextPhaseCheck()
        {
            CancelInvoke(nameof(HandleScheduledPhaseChange));

            DateTime now = DateTime.Now;
            DateTime nextDusk = now.Date.AddHours(m_DuskHour);
            DateTime nextNight = now.Date.AddHours(m_NightHour);
            DateTime nextTime;

            if (now < nextDusk)
            {
                nextTime = nextDusk;
            }
            else if (now < nextNight)
            {
                nextTime = nextNight;
            }
            else
            {
                nextTime = now.Date.AddDays(1).AddHours(m_DuskHour);
            }

            float delay = Mathf.Max(1f, (float)(nextTime - now).TotalSeconds);
            Invoke(nameof(HandleScheduledPhaseChange), delay);
        }

        private DynamicScenePhase GetPhase(DateTime time)
        {
            int hour = time.Hour;
            if (hour >= m_NightHour)
            {
                return DynamicScenePhase.Night;
            }

            if (hour >= m_DuskHour)
            {
                return DynamicScenePhase.Dusk;
            }

            return DynamicScenePhase.Day;
        }

        private float GetRawWaterSpeed(DynamicScenePhase phase)
        {
            switch (phase)
            {
                case DynamicScenePhase.Dusk:
                    return m_WaterMotion.duskSpeed;
                case DynamicScenePhase.Night:
                    return m_WaterMotion.nightSpeed;
                default:
                    return m_WaterMotion.daySpeed;
            }
        }

        private Color GetTargetTint(GraphicPhaseSprites item, DynamicScenePhase phase)
        {
            PhaseTint tint;
            switch (item.name)
            {
                case "img_desk":
                    tint = m_NoSpriteTints.desk;
                    break;
                case "skg_Role":
                    tint = m_NoSpriteTints.role;
                    break;
                case "rawImage_Water":
                    tint = m_NoSpriteTints.rawWater;
                    break;
                default:
                    tint = m_NoSpriteTints.fallback;
                    break;
            }

            switch (phase)
            {
                case DynamicScenePhase.Dusk:
                    return tint.dusk;
                case DynamicScenePhase.Night:
                    return tint.night;
                default:
                    return tint.day;
            }
        }

        private static float GetTargetAlpha(GraphicPhaseSprites item, DynamicScenePhase phase)
        {
            switch (phase)
            {
                case DynamicScenePhase.Dusk:
                    return item.duskAlpha;
                case DynamicScenePhase.Night:
                    return item.nightAlpha;
                default:
                    return item.dayAlpha;
            }
        }

        private Sprite GetTargetSprite(GraphicPhaseSprites item, DynamicScenePhase phase)
        {
            Sprite sprite = GetSpriteFromConfiguredSet(item.name, phase);
            if (sprite != null)
            {
                return sprite;
            }

            if (!HasManualPhaseSprites(item))
            {
                return null;
            }

            switch (phase)
            {
                case DynamicScenePhase.Dusk:
                    return item.duskSprite != null ? item.duskSprite : item.daySprite;
                case DynamicScenePhase.Night:
                    return item.nightSprite != null ? item.nightSprite : item.daySprite;
                default:
                    return item.daySprite;
            }
        }

        private static bool HasManualPhaseSprites(GraphicPhaseSprites item)
        {
            return item.daySprite != null && (item.duskSprite != null || item.nightSprite != null);
        }

        private Sprite GetSpriteFromConfiguredSet(string itemName, DynamicScenePhase phase)
        {
            PhaseSprites sprites = GetConfiguredSet(itemName);
            if (sprites == null)
            {
                return null;
            }

            switch (phase)
            {
                case DynamicScenePhase.Dusk:
                    return sprites.dusk != null ? sprites.dusk : sprites.day;
                case DynamicScenePhase.Night:
                    return sprites.night != null ? sprites.night : sprites.day;
                default:
                    return sprites.day;
            }
        }

        private PhaseSprites GetConfiguredSet(string itemName)
        {
            switch (itemName)
            {
                case "Sky":
                    return m_SkySprites;
                case "img_scene":
                    return m_SceneSprites;
                case "Water_bg":
                    return m_WaterBgSprites;
                case "Cloudy_1":
                case "Cloudy_2":
                    return m_Cloud1Sprites;
                case "Cloudy_3":
                case "Cloudy_4":
                    return m_Cloud2Sprites;
                default:
                    return null;
            }
        }

        private void ApplyGraphic(Graphic graphic, Sprite targetSprite, Color targetColor, bool animate)
        {
            graphic.DOKill();

            if (targetSprite != null && TryGetSprite(graphic, out Sprite currentSprite) && currentSprite != targetSprite)
            {
                CrossFadeSprite(graphic, targetSprite, targetColor, animate);
                return;
            }

            if (animate)
            {
                graphic.DOColor(targetColor, m_TransitionDuration).SetEase(Ease.InOutSine);
            }
            else
            {
                graphic.color = targetColor;
            }
        }

        private void CrossFadeSprite(Graphic graphic, Sprite targetSprite, Color targetColor, bool animate)
        {
            Graphic overlay = GetOrCreateOverlay(graphic);
            if (overlay == null)
            {
                SetSprite(graphic, targetSprite);
                graphic.color = targetColor;
                return;
            }

            SetSprite(overlay, targetSprite);
            overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
            overlay.gameObject.SetActive(true);

            if (!animate)
            {
                SetSprite(graphic, targetSprite);
                graphic.color = targetColor;
                overlay.gameObject.SetActive(false);
                return;
            }

            overlay.DOKill();
            overlay.DOColor(targetColor, m_TransitionDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    SetSprite(graphic, targetSprite);
                    graphic.color = targetColor;
                    overlay.gameObject.SetActive(false);
                });
        }

        private Graphic GetOrCreateOverlay(Graphic source)
        {
            if (m_TransitionOverlays.TryGetValue(source, out Graphic overlay) && overlay != null)
            {
                return overlay;
            }

            if (source is Image image)
            {
                GameObject overlayGo = new GameObject(source.gameObject.name + "_PhaseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform overlayRect = overlayGo.GetComponent<RectTransform>();
                CopyRectTransform(source.rectTransform, overlayRect);
                overlayGo.transform.SetParent(source.transform.parent, false);
                overlayGo.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

                Image overlayImage = overlayGo.GetComponent<Image>();
                overlayImage.raycastTarget = false;
                overlayImage.sprite = image.sprite;
                overlayImage.type = image.type;
                overlayImage.preserveAspect = image.preserveAspect;
                overlayImage.fillCenter = image.fillCenter;
                overlayImage.fillMethod = image.fillMethod;
                overlayImage.fillAmount = image.fillAmount;
                overlayImage.fillClockwise = image.fillClockwise;
                overlayImage.fillOrigin = image.fillOrigin;
                overlayImage.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
                overlayImage.material = image.material;
                overlay = overlayImage;
            }
            else if (source is RawImage rawImage)
            {
                GameObject overlayGo = new GameObject(source.gameObject.name + "_PhaseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                RectTransform overlayRect = overlayGo.GetComponent<RectTransform>();
                CopyRectTransform(source.rectTransform, overlayRect);
                overlayGo.transform.SetParent(source.transform.parent, false);
                overlayGo.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

                RawImage overlayRawImage = overlayGo.GetComponent<RawImage>();
                overlayRawImage.raycastTarget = false;
                overlayRawImage.texture = rawImage.texture;
                overlayRawImage.uvRect = rawImage.uvRect;
                overlayRawImage.material = rawImage.material;
                overlay = overlayRawImage;
            }

            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
                m_TransitionOverlays[source] = overlay;
            }

            return overlay;
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static bool TryGetSprite(Graphic graphic, out Sprite sprite)
        {
            if (graphic is Image image)
            {
                sprite = image.sprite;
                return true;
            }

            sprite = null;
            return false;
        }

        private static void SetSprite(Graphic graphic, Sprite sprite)
        {
            if (graphic is Image image)
            {
                image.sprite = sprite;
            }
        }

        private void AutoBind()
        {
            string[] names =
            {
                "Sky",
                "img_scene",
                "img_desk",
                "Water_bg",
                "rawImage_Water",
                "skg_Role",
                "Cloudy_1",
                "Cloudy_2",
                "Cloudy_3",
                "Cloudy_4"
            };

            var list = new List<GraphicPhaseSprites>();
            foreach (string childName in names)
            {
                Transform child = FindDeepChild(transform, childName);
                if (child == null)
                {
                    continue;
                }

                Graphic graphic = child.GetComponent<Graphic>();
                if (graphic == null)
                {
                    continue;
                }

                if (childName == "rawImage_Water")
                {
                    m_RawWater = graphic as RawImage;
                }

                var item = new GraphicPhaseSprites
                {
                    name = childName,
                    graphic = graphic,
                    dayAlpha = graphic.color.a,
                    duskAlpha = graphic.color.a,
                    nightAlpha = Mathf.Min(graphic.color.a, childName.StartsWith("Cloudy_", StringComparison.Ordinal) ? 0.75f : 1f)
                };

                if (graphic is Image image)
                {
                    item.daySprite = image.sprite;
                }

                list.Add(item);
            }

            m_Graphics = list.ToArray();
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
